# Normalizes a downloaded Sketchfab glTF (size, decimation, texture size) and exports a GLB prop.
# Usage: Blender --background --factory-startup --python Tools/blender/prop_import.py -- <scene.gltf> <out.glb> height|length <metres> <max tris> <max texture px>
import bpy, sys, json
from mathutils import Vector
argv = sys.argv[sys.argv.index("--") + 1:]
src, out, mode, target, max_tris, max_tex = argv[0], argv[1], argv[2], float(argv[3]), int(argv[4]), int(argv[5])
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
for o in bpy.context.scene.objects: o.animation_data_clear()
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
for o in meshes:
    m = o.matrix_world.copy(); o.parent = None; o.matrix_world = m
for o in list(bpy.context.scene.objects):
    if o.type != 'MESH': bpy.data.objects.remove(o)
bpy.ops.object.select_all(action='DESELECT')
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
def bounds():
    lo = Vector((1e18,)*3); hi = Vector((-1e18,)*3)
    for o in meshes:
        for v in o.data.vertices: lo = Vector(map(min, lo, v.co)); hi = Vector(map(max, hi, v.co))
    return lo, hi
lo, hi = bounds(); size = hi - lo
measure = size.z if mode == 'height' else max(size.x, size.y)
k = target / measure
for o in meshes:
    for v in o.data.vertices: v.co = Vector(((v.co.x - (lo.x + hi.x) / 2) * k, (v.co.y - (lo.y + hi.y) / 2) * k, (v.co.z - lo.z) * k))
tris = sum(len(p.vertices) - 2 for o in meshes for p in o.data.polygons)
if tris > max_tris:
    ratio = max_tris / tris
    for o in meshes:
        mod = o.modifiers.new("decimate", 'DECIMATE'); mod.ratio = ratio
        bpy.context.view_layer.objects.active = o; bpy.ops.object.modifier_apply(modifier=mod.name)
for img in bpy.data.images:
    if img.size[0] > max_tex: img.scale(max_tex, max(1, int(img.size[1] * max_tex / img.size[0])))
    img.pack()
lo, hi = bounds()
tris = sum(len(p.vertices) - 2 for o in meshes for p in o.data.polygons)
print("PROP", json.dumps({"tris": tris, "size": [round(v, 2) for v in hi - lo], "meshes": len(meshes), "images": len(bpy.data.images)}))
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_yup=True, export_animations=False, export_image_format='JPEG', export_jpeg_quality=85)
