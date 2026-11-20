"""Prepare a downloaded rigged character (Sketchfab glTF) for Unity's Humanoid importer.

Blender (headless):
  Blender -b --factory-startup -P Tools/blender/character_prep.py -- --profile cj --source <scene.gltf> --out <folder>

Steps: import, drop helper meshes, repair face winding and normals (game rips mix both orientations), strip the
importer's numeric bone suffixes, put the arms into a T-pose and apply it as the rest pose (Unity maps muscles from
the bind pose), scale to the profile height with the feet on the ground, merge materials that share a texture,
write Unity-ready textures (<part>_A albedo with alpha, <part>_N normal, <part>_ORM occlusion/roughness/metal) and
export an FBX plus a front preview render.
"""
import argparse
import json
import math
import os
import re
import shutil
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

PROFILES = {
    # height (m), arm bones (upper, fore, hand) left/right, materials to rename by texture stem, cutout parts
    "cj": {
        "height": 1.86,
        "arms": {"L": ("SKEL_L_UpperArm", "SKEL_L_Forearm", "SKEL_L_Hand"), "R": ("SKEL_R_UpperArm", "SKEL_R_Forearm", "SKEL_R_Hand")},
        "parts": {"feet": "Shoes", "hair": "Hair", "hand": "Hands", "head": "Head", "lowr": "Jeans", "teef": "Teeth", "uppr": "Body"},
        "cutout": {"Hair"},
        # Teeth alpha is only padding: keep them opaque.
        "opaque": {"Teeth"},
        # Helper bones Humanoid playback does not drive: their skin weights move to the bone they belong to.
        "merge": {"RB_L_ThighRoll": "SKEL_L_Thigh", "RB_R_ThighRoll": "SKEL_R_Thigh"},
        # Face bones the Humanoid mapper would grab (jaw, eyes): renamed so the body clips leave the face alone.
        "rename": {"FB_Jaw_000": "FB_Chin_000", "FB_L_Eye_000": "FB_L_Ocular_000", "FB_R_Eye_000": "FB_R_Ocular_000"},
        # The mod's posed face bones tear the upper lip when baked: relax the vertices the lip bones drive.
        "smooth": {"groups": "Lip", "threshold": .15, "factor": .5, "iterations": 6},
    },
    # NS conductor (Sketchfab, CC BY) as the ČD ticket inspector; Mixamo rig.
    "revizor": {
        "height": 1.82, "decimate": .42,
        "arms": {"L": ("mixamorig:LeftArm", "mixamorig:LeftForeArm", "mixamorig:LeftHand"), "R": ("mixamorig:RightArm", "mixamorig:RightForeArm", "mixamorig:RightHand")},
        "parts": {},
    },
    # Old man in a coat (Sketchfab, CC BY) as the underpass "alchemist"; Mixamo rig.
    "alchymista": {
        "height": 1.76, "decimate": .6, "reset_pose": True,
        "arms": {"L": ("mixamorig:LeftArm", "mixamorig:LeftForeArm", "mixamorig:LeftHand"), "R": ("mixamorig:RightArm", "mixamorig:RightForeArm", "mixamorig:RightHand")},
        "parts": {},
    },
    # Flying pigeon (Sketchfab, CC BY): not humanoid, keeps its flapping action; body length scaled, beak along +X.
    "holub": {"length": .42, "decimate": .1, "animal": True, "parts": {}},
}


def args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--profile", required=True, choices=PROFILES)
    parser.add_argument("--source", required=True)
    parser.add_argument("--out", required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1:])


def repair_normals(obj):
    """Consistent outward winding and plain smooth vertex normals instead of the broken imported split normals."""
    mesh = obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    # glTF splits vertices along UV seams; weld them (UVs live on loops and survive) so each shell is connected.
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    # recalc_face_normals can turn an open shell (a head with a neck hole) inside out: flip any connected island
    # whose faces mostly point towards the island's own centre.
    seen = set()
    for face in bm.faces:
        if face.index in seen:
            continue
        island, stack = [], [face]
        while stack:
            f = stack.pop()
            if f.index in seen:
                continue
            seen.add(f.index)
            island.append(f)
            stack.extend(g for e in f.edges for g in e.link_faces if g.index not in seen)
        centre = sum((f.calc_center_median() for f in island), Vector()) / len(island)
        inward = sum(f.calc_area() for f in island if f.normal.dot(f.calc_center_median() - centre) < 0)
        total = sum(f.calc_area() for f in island)
        if len(island) > 20 and inward > total * .6:
            bmesh.ops.reverse_faces(bm, faces=island)
    bm.to_mesh(mesh)
    bm.free()
    if mesh.has_custom_normals:
        with bpy.context.temp_override(object=obj, active_object=obj, selected_objects=[obj], selected_editable_objects=[obj]):
            bpy.ops.mesh.customdata_custom_splitnormals_clear()
    for poly in mesh.polygons:
        poly.use_smooth = True


def strip_suffixes(armature, meshes):
    """SKEL_L_Thigh_03 -> SKEL_L_Thigh (vertex groups follow the rename automatically)."""
    for bone in armature.data.bones:
        clean = re.sub(r"_\d+$", "", bone.name)
        if clean != bone.name and clean not in armature.data.bones:
            bone.name = clean


def merge_weights(meshes, merge):
    """Adds each helper group's weights to its target group and removes the helper group."""
    for obj in meshes:
        for source, target in merge.items():
            src = obj.vertex_groups.get(source)
            if src is None:
                continue
            dst = obj.vertex_groups.get(target) or obj.vertex_groups.new(name=target)
            for v in obj.data.vertices:
                for g in v.groups:
                    if g.group == src.index and g.weight > 0:
                        dst.add([v.index], g.weight, "ADD")
            obj.vertex_groups.remove(src)


def smooth_region(meshes, spec):
    """Laplacian-smooths the vertices weighted (above threshold) to vertex groups whose name contains spec['groups']."""
    for obj in meshes:
        groups = {g.index for g in obj.vertex_groups if spec["groups"] in g.name}
        if not groups:
            continue
        picked = {v.index for v in obj.data.vertices if sum(g.weight for g in v.groups if g.group in groups) > spec["threshold"]}
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bm.verts.ensure_lookup_table()
        verts = [bm.verts[i] for i in picked]
        for _ in range(spec["iterations"]):
            bmesh.ops.smooth_vert(bm, verts=verts, factor=spec["factor"], use_axis_x=True, use_axis_y=True, use_axis_z=True)
        bm.to_mesh(obj.data)
        bm.free()
        print("SMOOTHED", obj.name, len(verts), "vertices")


def t_pose(armature, arms):
    """Rotate each arm chain so it points straight out along ±X, then make that the rest pose."""
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode="POSE")
    for side, chain in arms.items():
        target = Vector((1 if side == "L" else -1, 0, 0))
        for name in chain[:2]:
            pb = armature.pose.bones[name]
            bpy.context.view_layer.update()
            world = armature.matrix_world @ pb.matrix
            head = world.translation.copy()
            child = armature.pose.bones[chain[chain.index(name) + 1]]
            tip = (armature.matrix_world @ child.matrix).translation
            direction = (tip - head).normalized()
            # Characters face -Y in Blender: their left arm points to +X.
            if direction.x * target.x < 0 and abs(direction.x) > .5:
                target = -target
            rotation = direction.rotation_difference(target).to_matrix().to_4x4()
            new_world = Matrix.Translation(head) @ rotation @ Matrix.Translation(-head) @ world
            pb.matrix = armature.matrix_world.inverted() @ new_world
            bpy.context.view_layer.update()
    bpy.ops.object.mode_set(mode="OBJECT")
    # Bake the posed shape into the meshes, then make the pose the rest pose.
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue
        for mod in obj.modifiers:
            if mod.type == "ARMATURE" and mod.object == armature:
                name = mod.name
                with bpy.context.temp_override(object=obj, active_object=obj):
                    bpy.ops.object.modifier_apply(modifier=name)
                new = obj.modifiers.new("Armature", "ARMATURE")
                new.object = armature
                break
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.mode_set(mode="POSE")
    bpy.ops.pose.select_all(action="SELECT")
    bpy.ops.pose.armature_apply(selected=False)
    bpy.ops.object.mode_set(mode="OBJECT")


def image_of(material, socket_name):
    bsdf = next((n for n in material.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf is None:
        return None
    stack = [bsdf.inputs[socket_name]] if socket_name in bsdf.inputs else []
    while stack:
        socket = stack.pop()
        for link in socket.links:
            node = link.from_node
            if node.type == "TEX_IMAGE":
                return node.image
            stack.extend(i for i in node.inputs if i.is_linked)
    return None


def save_image(image, path, mode):
    """Copies the source file (no colour transform); 'orm' builds R=1 occlusion, G roughness, B metal from a glTF map."""
    source = bpy.path.abspath(image.filepath)
    if mode != "orm":
        shutil.copyfile(source, path)
        return
    width, height = image.size
    pixels = list(image.pixels)
    for i in range(0, len(pixels), 4):
        pixels[i] = 1.0
        pixels[i + 3] = 1.0
    orm = bpy.data.images.new(os.path.basename(path), width, height, alpha=False, is_data=True)
    orm.pixels = pixels
    orm.filepath_raw = path
    orm.file_format = "PNG"
    orm.save()
    bpy.data.images.remove(orm)


def main():
    opts = args()
    profile = PROFILES[opts.profile]
    out = os.path.abspath(opts.out)
    os.makedirs(os.path.join(out, "Textures"), exist_ok=True)
    bpy.ops.import_scene.gltf(filepath=opts.source)
    armature = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    if profile.get("reset_pose"):
        # Some rigs import posed at the first frame of their own animation: back to the bind pose.
        armature.animation_data_clear()
        for pb in armature.pose.bones:
            pb.matrix_basis = Matrix.Identity(4)
    for obj in list(bpy.data.objects):
        if obj.type == "MESH" and not any(m.type == "ARMATURE" for m in obj.modifiers):
            bpy.data.objects.remove(obj, do_unlink=True)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    for obj in meshes:
        repair_normals(obj)
        if profile.get("decimate"):
            mod = obj.modifiers.new("Decimate", "DECIMATE")
            mod.ratio = profile["decimate"]
            with bpy.context.temp_override(object=obj, active_object=obj):
                bpy.ops.object.modifier_move_to_index(modifier="Decimate", index=0)
                bpy.ops.object.modifier_apply(modifier="Decimate")
    strip_suffixes(armature, meshes)
    merge_weights(meshes, profile.get("merge", {}))
    for old, new in profile.get("rename", {}).items():
        if old in armature.data.bones:
            armature.data.bones[old].name = new

    # Flatten the importer's hierarchy (Sketchfab root empties with axis and unit transforms) into world metres.
    for obj in list(bpy.data.objects):
        if obj.parent:
            world = obj.matrix_world.copy()
            obj.parent = None
            obj.matrix_world = world
    for obj in [o for o in bpy.data.objects if o.type == "EMPTY"]:
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    print("BONES", [b.name for b in armature.data.bones][:40])
    if not profile.get("animal"):
        t_pose(armature, profile["arms"])
    if profile.get("smooth"):
        smooth_region(meshes, profile["smooth"])

    # Height and ground contact.
    low, high = math.inf, -math.inf
    for obj in meshes:
        for v in obj.data.vertices:
            z = (obj.matrix_world @ v.co).z
            low, high = min(low, z), max(high, z)
    if profile.get("animal"):
        xs = [(obj.matrix_world @ v.co).x for obj in meshes for v in obj.data.vertices]
        scale = profile["length"] / (max(xs) - min(xs))
    else:
        scale = profile["height"] / (high - low)
    for obj in [armature] + meshes:
        obj.scale = (scale, scale, scale)
        obj.location = (0, 0, -low * scale)
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for obj in meshes:
        obj.parent = armature

    # Materials named after their part; parts sharing a texture share a material. Each source material is handled once.
    by_part, part_of = {}, {}
    for obj in meshes:
        for slot in obj.material_slots:
            material = slot.material
            if material is None:
                continue
            if material.name_full not in part_of:
                stem = material.name.split("_")[0]
                part = profile["parts"].get(stem) or (stem.capitalize() if opts.profile == "cj" else re.sub(r"[^A-Za-z0-9]", "", material.name.title()))
                if part not in by_part:
                    albedo, normal, orm = image_of(material, "Base Color"), image_of(material, "Normal"), image_of(material, "Roughness")
                    # Parts whose albedo really uses its alpha (lashes, hair cards) render with the cutout shader.
                    file_part = part
                    if albedo and albedo.channels == 4 and part not in profile.get("cutout", ()) and part not in profile.get("opaque", ()):
                        alpha = list(albedo.pixels)[3::4]
                        if alpha and min(alpha) < .5:
                            file_part = part + "Cutout"
                    if albedo:
                        save_image(albedo, os.path.join(out, "Textures", file_part + "_A.png"), "albedo")
                    if normal:
                        save_image(normal, os.path.join(out, "Textures", file_part + "_N.png"), "normal")
                    if orm and orm.size[0] > 8:
                        save_image(orm, os.path.join(out, "Textures", file_part + "_ORM.png"), "orm")
                    by_part[part] = (material, file_part)
                part_of[material.name_full] = part
            target, file_part = by_part[part_of[material.name_full]]
            slot.material = target
    # Colour and finish of every part, for parts without textures (badges, keys, signal disc…).
    info = {}
    for target, file_part in by_part.values():
        target.name = opts.profile.upper() + "_" + file_part
        bsdf = next((n for n in target.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
        if bsdf:
            info[file_part] = {"color": list(bsdf.inputs["Base Color"].default_value), "metallic": bsdf.inputs["Metallic"].default_value,
                               "roughness": bsdf.inputs["Roughness"].default_value}
    with open(os.path.join(out, "Textures", "materials.json"), "w", encoding="utf-8") as handle:
        json.dump({"parts": [dict(name=k, **v) for k, v in info.items()]}, handle, indent=1)
    for material in list(bpy.data.materials):
        if material.users == 0:
            bpy.data.materials.remove(material)

    name = opts.profile.upper() if opts.profile == "cj" else opts.profile.capitalize()
    animal = profile.get("animal", False)
    if animal and armature.animation_data and armature.animation_data.action:
        action = armature.animation_data.action
        action.name = "Fly"
        # Bake exactly the action's frames, so the clip loops without a frozen tail.
        bpy.context.scene.frame_start, bpy.context.scene.frame_end = int(action.frame_range[0]), int(action.frame_range[1])
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, name + ".fbx"), use_selection=False, object_types={"ARMATURE", "MESH"},
                             axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False,
                             bake_anim=animal, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                             use_tspace=True, mesh_smooth_type="FACE", path_mode="STRIP", embed_textures=False)

    # Front preview for the gate checks.
    scene = bpy.context.scene
    camera = bpy.data.objects.new("Preview camera", bpy.data.cameras.new("Preview camera"))
    scene.collection.objects.link(camera)
    camera.data.type = "ORTHO"
    size = profile.get("height", profile.get("length", 1))
    camera.data.ortho_scale = size * 1.3
    camera.location = (0, -6, size * .5)
    camera.rotation_euler = (math.radians(90), 0, 0)
    scene.camera = camera
    sun = bpy.data.objects.new("Preview sun", bpy.data.lights.new("Preview sun", "SUN"))
    scene.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(50), 0, math.radians(-35))
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "TEXTURE"
    scene.render.resolution_x, scene.render.resolution_y = 700, 900
    scene.render.filepath = os.path.join(out, name + "_preview.png")
    bpy.ops.render.render(write_still=True)
    print("CHARACTER_READY", name, round(scale, 4))


main()
