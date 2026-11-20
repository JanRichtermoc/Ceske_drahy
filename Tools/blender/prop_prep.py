"""Prepare a downloaded static prop (Sketchfab glTF) for the game.

Blender (headless):
  Blender -b --factory-startup -P Tools/blender/prop_prep.py -- --profile tree --source <scene.gltf> --out <folder>

Steps: import, apply the importer's transforms, optionally thin out alpha cards (keep a random share of the
connected islands of the named materials) and decimate the rest, normalise the size (height or length = 1 m, the game
scales per instance), put the pivot at the base centre, write <Part>_A/_N/_ORM textures and materials.json, export FBX.
"""
import argparse
import json
import math
import os
import random
import re
import shutil
import sys

import bmesh
import bpy
from mathutils import Vector

PROFILES = {
    # Linden (Sketchfab CC BY): leaf cards thinned to a fifth, bark decimated; height normalised to 1 m.
    # Leaf cards thinned to 30 % and the kept cards grown 1.5× around their centre, so the crown stays full.
    "tree": {"size": ("z", 1.0), "thin": {"M_Branch": .3}, "grow": {"M_Branch": 1.5}, "decimate": {"M_Bark": .08}, "cutout": {"M_Branch"},
             "names": {"M_Bark": "Bark", "M_Branch": "Leaves"}, "max_texture": 1024},
    # Modern LED street light (Sketchfab CC BY): height normalised to 1 m.
    "lamp": {"size": ("z", 1.0), "thin": {}, "decimate": {}, "cutout": set(), "names": {}, "max_texture": 1024},
}


def args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--profile", required=True, choices=PROFILES)
    parser.add_argument("--source", required=True)
    parser.add_argument("--out", required=True)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1:])


def material_key(material, names):
    for key in names:
        if material.name.startswith(key):
            return key
    return material.name


def islands(bm, faces):
    seen, result = set(), []
    face_set = set(f.index for f in faces)
    for face in faces:
        if face.index in seen:
            continue
        group, stack = [], [face]
        while stack:
            f = stack.pop()
            if f.index in seen:
                continue
            seen.add(f.index)
            group.append(f)
            stack.extend(g for e in f.edges for g in e.link_faces if g.index in face_set and g.index not in seen)
        result.append(group)
    return result


def image_of(material, socket):
    bsdf = next((n for n in material.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if not bsdf or socket not in bsdf.inputs:
        return None
    stack = [bsdf.inputs[socket]]
    while stack:
        for link in stack.pop().links:
            if link.from_node.type == "TEX_IMAGE":
                return link.from_node.image
            stack.extend(i for i in link.from_node.inputs if i.is_linked)
    return None


def main():
    opts = args()
    profile = PROFILES[opts.profile]
    random.seed(7)
    out = os.path.abspath(opts.out)
    os.makedirs(os.path.join(out, "Textures"), exist_ok=True)
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.ops.import_scene.gltf(filepath=opts.source)
    for obj in list(bpy.data.objects):
        if obj.parent:
            world = obj.matrix_world.copy()
            obj.parent = None
            obj.matrix_world = world
    for obj in [o for o in bpy.data.objects if o.type != "MESH"]:
        bpy.data.objects.remove(obj, do_unlink=True)
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    bpy.ops.object.select_all(action="SELECT")
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    for obj in meshes:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bm.faces.ensure_lookup_table()
        for index, slot in enumerate(obj.material_slots):
            key = material_key(slot.material, profile["names"]) if slot.material else None
            faces = [f for f in bm.faces if f.material_index == index]
            if key in profile["thin"]:
                groups = islands(bm, faces)
                keep = [g for g in groups if random.random() <= profile["thin"][key]]
                grow = profile.get("grow", {}).get(key, 1)
                if grow != 1:
                    for group in keep:
                        verts = {v for f in group for v in f.verts}
                        centre = sum((v.co for v in verts), Vector()) / len(verts)
                        for v in verts:
                            v.co = centre + (v.co - centre) * grow
                kept = {f for g in keep for f in g}
                doomed = [f for g in groups for f in g if f not in kept]
                bmesh.ops.delete(bm, geom=doomed, context="FACES")
                bm.faces.ensure_lookup_table()
        bm.to_mesh(obj.data)
        bm.free()
        for key, ratio in profile["decimate"].items():
            if any(s.material and s.material.name.startswith(key) for s in obj.material_slots):
                mod = obj.modifiers.new("Decimate", "DECIMATE")
                mod.ratio = ratio
                with bpy.context.temp_override(object=obj, active_object=obj):
                    bpy.ops.object.modifier_apply(modifier="Decimate")

    # One object, base centre at the origin, normalised size.
    bpy.ops.object.select_all(action="SELECT")
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = opts.profile.capitalize()
    coords = [obj.matrix_world @ v.co for v in obj.data.vertices]
    low = Vector((min(c.x for c in coords), min(c.y for c in coords), min(c.z for c in coords)))
    high = Vector((max(c.x for c in coords), max(c.y for c in coords), max(c.z for c in coords)))
    axis, size = profile["size"]
    scale = size / (high - low)["xyz".index(axis)]
    for v in obj.data.vertices:
        v.co = (v.co - Vector(((low.x + high.x) / 2, (low.y + high.y) / 2, low.z))) * scale
    obj.data.update()
    for poly in obj.data.polygons:
        poly.use_smooth = True

    info = {}
    for slot in obj.material_slots:
        material = slot.material
        if material is None:
            continue
        key = material_key(material, profile["names"])
        part = profile["names"].get(key, re.sub(r"[^A-Za-z0-9]", "", material.name.title()))
        if key in profile["cutout"]:
            part += "Cutout"
        for socket, suffix in (("Base Color", "A"), ("Normal", "N")):
            image = image_of(material, socket)
            if image:
                path = os.path.join(out, "Textures", part + "_" + suffix + ".png")
                shutil.copyfile(bpy.path.abspath(image.filepath), path)
        bsdf = next((n for n in material.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
        if bsdf:
            info[part] = {"color": list(bsdf.inputs["Base Color"].default_value), "metallic": bsdf.inputs["Metallic"].default_value,
                          "roughness": bsdf.inputs["Roughness"].default_value}
        material.name = opts.profile.upper() + "_" + part
    with open(os.path.join(out, "Textures", "materials.json"), "w", encoding="utf-8") as handle:
        json.dump({"parts": [dict(name=k, **v) for k, v in info.items()]}, handle, indent=1)
    triangles = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    name = opts.profile.capitalize()
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, name + ".fbx"), object_types={"MESH"}, axis_forward="-Z", axis_up="Y",
                             apply_scale_options="FBX_SCALE_ALL", use_tspace=True, mesh_smooth_type="FACE",
                             path_mode="STRIP", embed_textures=False)
    print("PROP_READY", name, triangles, "triangles")


main()
