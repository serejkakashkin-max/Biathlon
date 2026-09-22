"""Selective CC0 screen frame and architecture maps; no source signage/materials imported."""
import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector
r=Path.cwd();out=r/'artifacts/g2-event';out.mkdir(parents=True,exist_ok=True)
src=r/'external_downloads/g2_visual_remediation/06_stadium_event/screens_signage/3dassets_velodrome/_source/velodrome-and-track-cycling-big-screen-frame-04899fcc.glb'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(src))
groups={}
for o in bpy.data.objects:
 if o.type!='MESH':continue
 group='ScreenSteel' if o.data.materials[0].name=='steel' else 'ScreenFrame'
 data=groups.setdefault(group,dict(schema_version=1,id=group,vertices=[],normals=[],indices=[]));mesh=o.data;mesh.calc_loop_triangles()
 for tri in mesh.loop_triangles:
  for loop in tri.loops:
   p=o.matrix_world@mesh.vertices[mesh.loops[loop].vertex_index].co;n=(o.matrix_world.to_3x3().inverted().transposed()@mesh.corner_normals[loop].vector).normalized()
   data['indices'].append(len(data['vertices'])//3);data['vertices'].extend((p.x,p.z,-p.y));data['normals'].extend((n.x,n.z,-n.y))
outputs=[]
for group,data in groups.items():
 p=out/(group+'.json');p.write_text(json.dumps(data,separators=(',',':')));outputs.append(dict(file=p.name,triangles=len(data['indices'])//3,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
textures=r/'Assets/Resources/Biathlon/Stadium/G2Event/Textures';textures.mkdir(parents=True,exist_ok=True)
sources=[dict(asset='G2-029',url='https://3dassets.dev/packs/velodrome-and-track-cycling',license='CC0',sha256=hashlib.sha256(src.read_bytes()).hexdigest())]
for asset,name in [('G2-034','concrete'),('G2-035','corrugated_iron')]:
 folder=r/'external_downloads/g2_visual_remediation/08_materials/architecture'/name/'extracted/textures'
 maps=[('diff','Albedo'),('nor_gl','Normal'),('rough','Roughness')]
 if name=='corrugated_iron':maps.append(('metal','Metallic'))
 for suffix,label in maps:
  f=folder/(name+'_'+suffix+'_4k.jpg');im=bpy.data.images.load(str(f),check_existing=False)
  if suffix!='diff':im.colorspace_settings.name='Non-Color'
  im.scale(1024,1024);p=textures/(name+'_'+label+'.png');im.filepath_raw=str(p);im.file_format='PNG';im.save()
  outputs.append(dict(file=p.relative_to(r).as_posix(),source_file=f.name,source_sha256=hashlib.sha256(f.read_bytes()).hexdigest(),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
 sources.append(dict(asset=asset,author='Poly Haven',url='https://polyhaven.com/a/'+name,license='CC0'))
(out/'manifest.json').write_text(json.dumps(dict(schema_version=1,id='g2-event-derivation-v1',sources=sources,outputs=outputs,modifications=['selected108-triangle screen frame only, source materials discarded; original event artwork/text in Unity','seven1K architecture maps; normal/roughness/metallic exported as Non-Color, URP R=metallic A=1-roughness packed during authoring','no source textures on screen, no source branding or colliders']),indent=2))
print('Screen and selected architecture maps derived.')
