# Dresses the Sketchfab "Free Rigged Male Basemesh" (CC BY) as CJ and exports Assets/Resources/Models/CJ/cj.glb.
# Usage: Blender --background --factory-startup --python Tools/blender/cj_dress.py -- <scene.gltf> <out.glb> <preview.png>
import bpy, sys, math, json
from mathutils import Vector
argv = sys.argv[sys.argv.index("--") + 1:]
src, out, preview = argv[0], argv[1], argv[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
for o in list(bpy.context.scene.objects):
    if o.name == 'Icosphere': bpy.data.objects.remove(o)
arm = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
for o in bpy.context.scene.objects: o.animation_data_clear()
for pb in arm.pose.bones: pb.matrix_basis.identity()
bpy.context.scene.frame_set(0)
bpy.context.view_layer.update()
# Flatten the Sketchfab hierarchy (empties with x100 scale) and bake every transform into the data.
keep = [o for o in bpy.context.scene.objects if o.type in ('ARMATURE', 'MESH')]
for o in keep:
    m = o.matrix_world.copy(); o.parent = None; o.matrix_world = m
for o in list(bpy.context.scene.objects):
    if o not in keep: bpy.data.objects.remove(o)
bpy.ops.object.select_all(action='DESELECT')
for o in keep: o.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.context.view_layer.update()
zs0 = [v.co.z for o in keep if o.type == 'MESH' for v in o.data.vertices]
k = 1.86 / (max(zs0) - min(zs0))
for o in keep: o.scale = (k, k, k)
bpy.context.view_layer.update()
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
for o in keep: o.location.z -= min(zs0) * k
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)
bpy.context.view_layer.update()
bpy.context.view_layer.update()
def head(name):
    b = next(b for b in arm.data.bones if name in b.name)
    return arm.matrix_world @ b.head_local
body = max((o for o in bpy.context.scene.objects if o.type == 'MESH'), key=lambda o: len(o.data.vertices))
extra = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o != body]
mw = body.matrix_world
zs = [(mw @ v.co).z for v in body.data.vertices]
H = max(zs) - min(zs); Z0 = min(zs)
P = {k: head(k) for k in ['Hips_', 'Spine2_', 'Neck_', 'Head_', 'LeftArm_', 'LeftShoulder_', 'LeftLeg_', 'LeftFoot_', 'LeftUpLeg_']}
info = {k: [round(c, 2) for c in v] for k, v in P.items()}
print("JOINTS", json.dumps(info), "H", round(H, 2), "Z0", round(Z0, 2))
for o in extra:
    c = sum((o.matrix_world @ v.co for v in o.data.vertices), Vector()) / len(o.data.vertices)
    print("EXTRA", o.name, [round(x, 2) for x in c])
names = {g.index: g.name for g in body.vertex_groups}
hipsZ, neckZ, ankleZ, kneeZ = P['Hips_'].z, P['Neck_'].z, P['LeftFoot_'].z, P['LeftLeg_'].z
topZ = Z0 + H; headY = P['Head_'].y; chestY = P['Spine2_'].y
armpitZ = P['LeftArm_'].z - .045 * H
def classify(w):
    x, y, z = abs(w.x), w.y, w.z
    front = y < chestY - .012 * H
    if z < ankleZ + .014 * H: return 'sole' if z < Z0 + .012 * H else 'shoe'
    if x > .13 * H: return 'skin'
    if z < hipsZ + .035 * H: return 'denim'
    if z > neckZ + .025 * H:
        cap = z > topZ - .032 * H or (z > topZ - .1 * H and y > headY + .004 * H)
        return 'hair' if cap else 'skin'
    if z < armpitZ: return 'tank'
    # Upper chest: deep armholes, a front scoop neckline and a higher back neckline.
    if x > .07 * H or z > neckZ - .02 * H: return 'skin'
    if front and x < .052 * H and z > neckZ - .115 * H: return 'skin'
    if not front and x < .04 * H and z > neckZ - .06 * H: return 'skin'
    return 'tank'
zone = [classify(mw @ v.co) for v in body.data.vertices]
colors = {'skin': (.36, .2, .13), 'tank': (.88, .87, .83), 'denim': (.17, .27, .42), 'shoe': (.86, .85, .82), 'sole': (.22, .22, .23), 'hair': (.05, .035, .03), 'eye': (.9, .88, .84), 'iris': (.08, .05, .04)}
mats = {}
for k, c in colors.items():
    m = bpy.data.materials.new("CJ " + k); m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (c[0], c[1], c[2], 1)
    bsdf.inputs["Roughness"].default_value = {'skin': .55, 'tank': .85, 'denim': .9, 'shoe': .5, 'sole': .7, 'hair': .95, 'eye': .2, 'iris': .15}[k]
    m.diffuse_color = (c[0], c[1], c[2], 1)
    mats[k] = m
body.data.materials.clear()
order = list(colors.keys())
for k in order: body.data.materials.append(mats[k])
for poly in body.data.polygons:
    poly.material_index = order.index(classify(mw @ poly.center))
    poly.use_smooth = True
# Clothes sit on top of the body: jeans loosen towards the ankle, the tank top barely.
inv = mw.inverted().to_3x3()
nmat = mw.to_3x3()
for v in body.data.vertices:
    k = zone[v.index]
    if k not in ('denim', 'tank', 'shoe', 'sole'): continue
    n = (nmat @ v.normal).normalized()
    z = (mw @ v.co).z
    if k == 'denim': amount = .01 * H + .014 * H * max(0, min(1, (kneeZ - z) / (kneeZ - ankleZ)))
    elif k == 'tank': amount = .004 * H
    else: amount = .009 * H
    v.co += inv @ (n * amount)
# Fabric hides the anatomy underneath: relax the cloth vertices.
import bmesh
bm = bmesh.new(); bm.from_mesh(body.data)
bm.verts.ensure_lookup_table()
for kind, rounds in (('denim', 14), ('tank', 6), ('shoe', 10), ('sole', 10)):
    verts = [bm.verts[i] for i, k in enumerate(zone) if k == kind]
    for _ in range(rounds): bmesh.ops.smooth_vert(bm, verts=verts, factor=.5, use_axis_x=True, use_axis_y=True, use_axis_z=True)
bm.to_mesh(body.data); bm.free()
for o in extra:
    o.data.materials.clear(); o.data.materials.append(mats['eye']); o.data.materials.append(mats['iris'])
    ys = [(o.matrix_world @ v.co).y for v in o.data.vertices]; front = min(ys) + (max(ys) - min(ys)) * .22
    for poly in o.data.polygons:
        c = o.matrix_world @ poly.center
        poly.material_index = 1 if c.y < front else 0
        poly.use_smooth = True
counts = {k: zone.count(k) for k in colors}
print("ZONES", counts)
for o in keep:
    if o.type == 'MESH': o.parent = arm; o.matrix_parent_inverse.identity()
bpy.context.view_layer.update()
print("MODS", [(o.name, [m.type + ":" + (m.object.name if getattr(m, "object", None) else "") for m in o.modifiers]) for o in keep if o.type == 'MESH'])
print("SCALES", [(o.name, [round(v, 3) for v in o.matrix_world.to_scale()], o.parent.name if o.parent else None) for o in bpy.context.scene.objects], "H", round(H, 3))
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_skins=True, export_animations=False, export_morph=False, export_yup=True, export_apply=False)
scene = bpy.context.scene; scene.render.engine = 'BLENDER_WORKBENCH'; scene.display.shading.color_type = 'MATERIAL'
scene.render.resolution_x = 700; scene.render.resolution_y = 900
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); scene.collection.objects.link(cam); scene.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = H * 1.1; cam.data.clip_end = H * 20
center = Vector((0, 0, Z0 + H / 2))
for i, (loc, rot) in enumerate([((0, -H * 3, 0), (90, 0, 0)), ((H * 3, 0, 0), (90, 0, 90))]):
    cam.location = center + Vector(loc); cam.rotation_euler = tuple(math.radians(a) for a in rot)
    scene.render.filepath = preview.replace('.png', '_%d.png' % i); bpy.ops.render.render(write_still=True)
