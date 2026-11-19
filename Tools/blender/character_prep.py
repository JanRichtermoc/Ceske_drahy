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
        # Helper bones Humanoid playback does not drive: their skin weights move to the bone they belong to.
        "merge": {"RB_L_ThighRoll": "SKEL_L_Thigh", "RB_R_ThighRoll": "SKEL_R_Thigh"},
        # Face bones the Humanoid mapper would grab (jaw, eyes): renamed so the body clips leave the face alone.
        "rename": {"FB_Jaw_000": "FB_Chin_000", "FB_L_Eye_000": "FB_L_Ocular_000", "FB_R_Eye_000": "FB_R_Ocular_000"},
    },
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
    for obj in list(bpy.data.objects):
        if obj.type == "MESH" and not any(m.type == "ARMATURE" for m in obj.modifiers):
            bpy.data.objects.remove(obj, do_unlink=True)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    for obj in meshes:
        repair_normals(obj)
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
    t_pose(armature, profile["arms"])

    # Height and ground contact.
    low, high = math.inf, -math.inf
    for obj in meshes:
        for v in obj.data.vertices:
            z = (obj.matrix_world @ v.co).z
            low, high = min(low, z), max(high, z)
    scale = profile["height"] / (high - low)
    for obj in [armature] + meshes:
        obj.scale = (scale, scale, scale)
        obj.location = (0, 0, -low * scale)
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for obj in meshes:
        obj.parent = armature

    # Materials named after their part; parts sharing a texture share a material.
    by_part = {}
    for obj in meshes:
        for slot in obj.material_slots:
            material = slot.material
            if material is None:
                continue
            stem = material.name.split("_")[0]
            part = profile["parts"].get(stem, stem.capitalize())
            if part not in by_part:
                by_part[part] = material
                material.name = "CJ_" + part if opts.profile == "cj" else part
                albedo, normal, orm = image_of(material, "Base Color"), image_of(material, "Normal"), image_of(material, "Roughness")
                if albedo:
                    save_image(albedo, os.path.join(out, "Textures", part + "_A.png"), "albedo")
                if normal:
                    save_image(normal, os.path.join(out, "Textures", part + "_N.png"), "normal")
                if orm and orm.size[0] > 8:
                    save_image(orm, os.path.join(out, "Textures", part + "_ORM.png"), "orm")
            slot.material = by_part[part]
    for material in list(bpy.data.materials):
        if material.users == 0:
            bpy.data.materials.remove(material)

    name = opts.profile.upper() if opts.profile == "cj" else opts.profile.capitalize()
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, name + ".fbx"), use_selection=False, object_types={"ARMATURE", "MESH"},
                             axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False,
                             bake_anim=False, use_tspace=True, mesh_smooth_type="FACE", path_mode="STRIP", embed_textures=False)

    # Front preview for the gate checks.
    scene = bpy.context.scene
    camera = bpy.data.objects.new("Preview camera", bpy.data.cameras.new("Preview camera"))
    scene.collection.objects.link(camera)
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = profile["height"] * 1.15
    camera.location = (0, -6, profile["height"] * .5)
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
