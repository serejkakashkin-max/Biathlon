"""Read-only source inventory and actual Blender renders. No Unity writes."""
import bpy, json, sys, argparse, hashlib
from pathlib import Path
from mathutils import Vector

p=argparse.ArgumentParser();p.add_argument('--source',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);source=Path(a.source).resolve();out=Path(a.output).resolve();out.mkdir(parents=True,exist_ok=True)
report={'schema_version':1,'id':'m167_source_audit','blender':bpy.app.version_string,'assets':[]}
def point(obj,target):obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
for path in sorted(source.rglob('*')):
    if path.suffix.lower() not in ('.glb','.fbx'):continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if path.suffix=='.glb':bpy.ops.import_scene.gltf(filepath=str(path))
    else:bpy.ops.import_scene.fbx(filepath=str(path),use_anim=False)
    for obj in bpy.context.scene.objects:
        if obj.animation_data:obj.animation_data_clear()
        if obj.type=='ARMATURE':obj.data.pose_position='REST'
    bpy.context.view_layer.update()
    item={'file':str(path.relative_to(source)).replace('\\','/'),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'meshes':[],'armatures':[],'images':[],'actions':[act.name for act in bpy.data.actions]}
    for obj in bpy.context.scene.objects:
        if obj.type=='MESH':
            obj.data.calc_loop_triangles()
            bb=[obj.matrix_world@Vector(v) for v in obj.bound_box]
            item['meshes'].append({'name':obj.name,'vertices':len(obj.data.vertices),'triangles':len(obj.data.loop_triangles),'bounds_min':[min(v[i] for v in bb) for i in range(3)],'bounds_max':[max(v[i] for v in bb) for i in range(3)],'uv_layers':[x.name for x in obj.data.uv_layers],'materials':[x.name for x in obj.data.materials if x],'weighted_vertices':sum(bool(v.groups) for v in obj.data.vertices)})
        elif obj.type=='ARMATURE':
            item['armatures'].append({'name':obj.name,'bones':[{'name':b.name,'parent':b.parent.name if b.parent else None,'head':list(obj.matrix_world@b.head_local),'tail':list(obj.matrix_world@b.tail_local)} for b in obj.data.bones]})
    for img in bpy.data.images:
        item['images'].append({'name':img.name,'size':list(img.size),'channels':img.channels,'packed':bool(img.packed_file)})
    report['assets'].append(item)
    if path.name=='man-base-character-rigged.glb':
        bpy.ops.wm.save_as_mainfile(filepath=str(out/'source-character-audit.blend'))
        scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32
        scene.render.resolution_x=900;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
        scene.world=bpy.data.worlds.new('StudioWorld');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.16,.18,.22,1)
        for pos,power,size in [((3,-4,4),700,4),((-3,-2,2.5),450,3),((1,3,3),900,3)]:
            bpy.ops.object.light_add(type='AREA',location=pos);l=bpy.context.object;l.data.energy=power;l.data.shape='DISK';l.data.size=size;point(l,(0,0,1))
        bpy.ops.object.camera_add(location=(0,-3.5,1));cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=2.1;point(cam,(0,0,1))
        for name,pos,target,scale in [('source-front',(0,-3.5,1),(0,0,.95),2.1),('source-back',(0,3.5,1),(0,0,.95),2.1),('source-face',(.18,-1.5,1.7),(0,0,1.65),.47)]:
            cam.location=pos;point(cam,target);cam.data.ortho_scale=scale;scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
        for img in bpy.data.images:
            if img.packed_file:
                img.filepath_raw=str(out/(img.name+'.png'));img.file_format='PNG';img.save()
    (out/'source-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('M167_SOURCE_AUDIT_COMPLETE',len(report['assets']))
