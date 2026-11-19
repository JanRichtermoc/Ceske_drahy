# Renders front and side workbench previews of a glTF/GLB file.
# Usage: Blender --background --factory-startup --python Tools/blender/preview.py -- <model> <out.png>
import bpy, sys, math
from mathutils import Vector
argv = sys.argv[sys.argv.index("--") + 1:]
src, out = argv[0], argv[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
for o in list(bpy.context.scene.objects):
    if o.name=='Icosphere': bpy.data.objects.remove(o)
for o in bpy.context.scene.objects:
    if o.type=='ARMATURE': o.data.pose_position='REST'; print('ARM',o.name,[round(v,3) for v in o.matrix_world.to_scale()], [round(v,2) for v in o.matrix_world.to_euler()])
bpy.context.view_layer.update()
objs=[o for o in bpy.context.scene.objects if o.type=='MESH']
mins=Vector((1e9,)*3);maxs=Vector((-1e9,)*3)
for o in objs:
    for c in o.bound_box:
        w=o.matrix_world@Vector(c);mins=Vector(map(min,mins,w));maxs=Vector(map(max,maxs,w))
    print("OBJ",o.name,[round(v,2) for v in o.matrix_world.translation],len(o.data.vertices), [g.name for g in o.vertex_groups][:3], o.parent.name if o.parent else None)
center=(mins+maxs)/2;size=max(maxs-mins)
print("BOUNDS",[round(v,2) for v in mins],[round(v,2) for v in maxs])
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=900;scene.render.resolution_y=700
cam=bpy.data.objects.new("cam",bpy.data.cameras.new("cam"));scene.collection.objects.link(cam);scene.camera=cam
cam.data.type='ORTHO';cam.data.ortho_scale=size*1.1;cam.data.clip_end=size*10
cam.location=center+Vector((0,-size*2,0));cam.rotation_euler=(math.radians(90),0,0)
scene.render.filepath=out;bpy.ops.render.render(write_still=True)
cam.location=center+Vector((size*2,0,0));cam.rotation_euler=(math.radians(90),0,math.radians(90))
scene.render.filepath=out.replace('.png','_side.png');bpy.ops.render.render(write_still=True)
