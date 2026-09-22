"""CC BY stand structure/seat derivation plus a CC0 canopy; all source advertisements discarded."""
import bpy,json,zipfile,hashlib
from pathlib import Path
from mathutils import Vector
r=Path.cwd();out=r/'artifacts/g2-grandstand';out.mkdir(parents=True,exist_ok=True)
base=r/'external_downloads/g2_visual_remediation/06_stadium_event'
archive=base/'grandstands/sketchfab_william_prosser/extracted/source/model.zip';dae=out/'model.dae'
with zipfile.ZipFile(archive) as z:dae.write_bytes(z.read('model/model.dae'))
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.wm.collada_import(filepath=str(dae))
objects=[o for o in bpy.data.objects if o.type=='MESH'];scale=26.0
removed=[o.name for o in objects if any(m.name=='Adverts' for m in o.data.materials)]
objects=[o for o in objects if o.name not in removed]
pts=[o.matrix_world@v.co for o in objects for v in o.data.vertices];origin=Vector((min(p.x for p in pts),(min(p.y for p in pts)+max(p.y for p in pts))*.5,min(p.z for p in pts)))
groups={};seats=[]
def convert(p):p=(p-origin)*scale;return Vector((p.y,p.z,p.x))
def add_object(o,group,point,normal):
 data=groups.setdefault(group,dict(vertices=[],normals=[],uv=[],indices=[]));mesh=o.data;mesh.calc_loop_triangles();remap={}
 for tri in mesh.loop_triangles:
  for loop in tri.loops:
   index=mesh.loops[loop].vertex_index;n=normal(mesh.corner_normals[loop].vector).normalized();p=point(mesh.vertices[index].co);key=(index,tuple(round(v,5) for v in n))
   if key not in remap:
    remap[key]=len(data['vertices'])//3;data['vertices'].extend(p);data['normals'].extend(n);data['uv'].extend((p.x,p.z))
   data['indices'].append(remap[key])
for o in objects:
 name=o.data.materials[0].name;group='Seats' if name in ['Seats_white','lambert3'] else 'Railings' if name=='Barriers_and_railings' else 'Structure'
 def normal(n,o=o):n=o.matrix_world.to_3x3()@n;return Vector((n.y,n.z,n.x))
 add_object(o,group,lambda p,o=o:convert(o.matrix_world@p),normal)
 if group=='Seats':
  planes={}
  for f in o.data.polygons:
   n=(o.matrix_world.to_3x3()@f.normal).normalized()
   if n.z<.9:continue
   c=o.matrix_world@f.center;key=round(c.z,6);entry=planes.setdefault(key,[0,Vector()]);entry[0]+=f.area;entry[1]+=c*f.area
  largest=max(v[0] for v in planes.values());height=max(k for k,v in planes.items() if v[0]>=largest*.5);area,center=planes[height];p=convert(center/area)
  seats.append(dict(source_object=o.name,seat_surface=list(p),anchor=[p.x,p.y-.30,p.z]))
# Same row, then left-to-right gives stable crowd identities independent of Blender object ordering.
seats.sort(key=lambda s:(round(s['anchor'][1],2),s['anchor'][0]))
canopy=base/'grandstands/3dassets_racecourse/_source/racecourse-and-parade-ring-grandstand-roof-canopy-7353c5da.glb'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(canopy))
for o in bpy.data.objects:
 if o.type!='MESH':continue
 material=o.data.materials[0].name;group='CanopyFascia' if material=='white' else 'CanopyRoof' if material in ['green','charcoal'] else 'CanopySteel'
 # Source canopy width12.2m expands to19.52m. Depth/rise preserved; all neutral replacement materials.
 def point(p,o=o):p=o.matrix_world@p;return Vector((p.x*1.6,p.z,-p.y+3.6))
 def normal(n,o=o):n=o.matrix_world.to_3x3()@n;return Vector((n.x/1.6,n.z,-n.y))
 add_object(o,group,point,normal)
files=[]
for name,data in groups.items():
 data.update(schema_version=1,id='g2-stand-'+name.lower());path=out/(name+'.json');path.write_text(json.dumps(data,separators=(',',':')))
 files.append(dict(group=name,vertices=len(data['vertices'])//3,triangles=len(data['indices'])//3,file=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
seat_path=out/'seat-bindings.json';seat_path.write_text(json.dumps(dict(schema_version=1,id='g2-grandstand-seats-v1',seats=seats),indent=2))
manifest=dict(schema_version=1,id='g2-grandstand-derivation-v1',sources=[dict(asset='G2-030',author='William Prosser',license='CC-BY-4.0',url='https://sketchfab.com/3d-models/race-track-stadium-viewing-stand-and-seating-f47b51bcda6e449495795c66eb7089a9',archive_sha256=hashlib.sha256(archive.read_bytes()).hexdigest(),model_sha256=hashlib.sha256(dae.read_bytes()).hexdigest()),dict(asset='G2-027',author='3dassets.dev',license='CC0',url='https://3dassets.dev/packs/racecourse-and-parade-ring',file=canopy.name,sha256=hashlib.sha256(canopy.read_bytes()).hexdigest())],
 blender=bpy.app.version_string,removed_advertisement_objects=removed,seat_count=len(seats),seat_manifest_sha256=hashlib.sha256(seat_path.read_bytes()).hexdigest(),source_scale=scale,
 modifications=['selected structural/seat geometry only; all advertisement objects removed, every source material/texture discarded','stand rescaled uniformly26 to metre-sized seating, axis conversion and ground/width/front pivot; source chair-top planes generate stable explicit seat anchors','selected CC0 roof canopy only, width adapted1.6, neutral winter replacement materials, no source branding; source white fascia remains a panel, separate project snow cap added in Unity authoring','merged material groups; source normals preserved, own world-scale UVs; no colliders/gameplay identifiers','original author/source/license/modification notice shipped with player'],outputs=files)
(out/'manifest.json').write_text(json.dumps(manifest,indent=2));print(json.dumps(files));print('SEATS',len(seats))
