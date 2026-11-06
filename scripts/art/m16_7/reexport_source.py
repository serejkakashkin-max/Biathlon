"""Export an edited, already authored source through the same runtime/LOD code.

The source blend retains the rig, semantic parts, UVs and original materials.
This avoids rebaking unchanged textile maps when editing garment geometry.
"""
import argparse,sys,bpy,json,hashlib,math
from pathlib import Path
from mathutils import Matrix,Vector
p=argparse.ArgumentParser();p.add_argument('--source',required=True);p.add_argument('--manifest',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);out=Path(a.output).resolve();(out/'Models').mkdir(parents=True,exist_ok=True);(out/'Documentation').mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(Path(a.source).resolve()));scene=bpy.context.scene;arm=bpy.data.objects['BiathleteSkeleton'];arm.data.pose_position='REST'
objects=[o for o in scene.objects if o.type in ['MESH','EMPTY']];heads={b.name:arm.matrix_world@b.head_local for b in arm.data.bones}
glasses_parts=[o for o in objects if o.type=='MESH' and o.name.startswith(('Panoramic_Amber_Lens','Goggle_'))]
mask_meshes=[o for o in objects if o.type=='MESH' and o.name.startswith(('Mask_Derived','Mask_PULSE'))]
rifleparts=[o for o in objects if o.type=='MESH' and '_Rifle_Suspension_'not in o.name and not o.name.endswith('_Rifle_Harness_Webbing') and any(m and 'Rifle_'in m.name for m in o.data.materials)]
assert len(glasses_parts)==6 and len(mask_meshes)>=4 and len(rifleparts)>20,'Incomplete editable character source'
root=Path(__file__).resolve().parents[3];EQUIPMENT_SETTINGS=json.loads((root/'Assets/Biathlon/Characters/Biathlete_16_7/Documentation/equipment-detail-v1.json').read_text())
def rigid(obj,bone):
    bpy.context.view_layer.update();matrix=obj.matrix_world.copy();obj.parent=arm;obj.parent_type='BONE';obj.parent_bone=bone;obj.matrix_world=matrix
    if obj not in objects:objects.append(obj)
    return obj
text=(Path(__file__).with_name('build_biathlete.py')).read_text();start=text.index('def join_meshes(');end=text.index('# Source walk/run preserved',start)
exec(compile(text[start:end],str(Path(__file__).with_name('build_biathlete.py')),'exec'))
manifest=json.loads(Path(a.manifest).read_text());manifest['export_fbx_sha256']=hashlib.sha256((out/'Models/Biathlete_16_7_Natural.fbx').read_bytes()).hexdigest();manifest['meshes']=[]
for obj in character_objects:
    if obj.type=='MESH':
        obj.data.calc_loop_triangles();manifest['meshes'].append({'name':obj.name,'triangles':len(obj.data.loop_triangles),'vertices':len(obj.data.vertices),'uv_layers':len(obj.data.uv_layers),'skinned':any(m.type=='ARMATURE'for m in obj.modifiers),'bone_parent':obj.parent_bone})
manifest['triangles']=sum(x['triangles']for x in manifest['meshes'])
manifest['stored_lod0_triangles']=sum(x['triangles']for x in manifest['meshes']if not x['name'].endswith(('LOD1','LOD2')))
manifest['active_lod0_triangles']=sum(x['triangles']for x in manifest['meshes']if not x['name'].endswith(('LOD1','LOD2'))and not x['name'].endswith('Tuck_Glove')and x['name']!='Eyewear_Mask')
manifest['source_edit_export']='same runtime assembly and LOD code as build_biathlete.py; textile maps reused unchanged'
(out/'Documentation/natural-asset-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('M167_SOURCE_REEXPORT_COMPLETE',manifest['triangles'])
