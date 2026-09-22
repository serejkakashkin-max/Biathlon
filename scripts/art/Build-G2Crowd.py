"""Local-only Renderpeople head derivatives plus original winter crowd clothing.
The source meshes, textures and generated combined meshes MUST NOT be published.
Only this recipe, source/output hashes and rendered review images may enter Git.
"""
import bpy,bmesh,json,hashlib,math
from pathlib import Path
from mathutils import Vector,Matrix
r=Path.cwd();base=r/'external_downloads/g2_visual_remediation/01_characters/crowd_people/renderpeople';out=r/'artifacts/g2-crowd';out.mkdir(exist_ok=True)
runtime=r/'Assets/Resources/Biathlon/RestrictedG2Crowd';runtime.mkdir(parents=True,exist_ok=True)
coat=(.8,.8,.8,1);cloth=(.055,.07,.09,0);boot=(.025,.032,.04,0);scarf=(.39,.43,.45,0)
sources=[];outputs=[]
for variant,(person,name,neck,scale) in enumerate([('person_06','rp_eric_rigged_001',1.565,1),('person_07','rp_carla_rigged_001',1.46,1.1)]):
 source=base/person/'extracted'/(name+'_u3d.fbx');texture=base/person/'extracted/tex'/(name+'_dif.jpg')
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(source));o=next(o for o in bpy.data.objects if o.type=='MESH')
 # Keep only head/upper-neck geometry; all office clothing, watches and shoes are discarded.
 transform=o.matrix_world.copy()
 for v in o.data.vertices:v.co=transform@v.co
 o.matrix_world=Matrix.Identity(4);o.modifiers.clear()
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.delete(bm,geom=[f for f in bm.faces if any(v.co.z<neck or abs(v.co.x)>.18 for v in f.verts)],context='FACES');bm.to_mesh(o.data);bm.free()
 original=o.data.copy();im=bpy.data.images.load(str(texture),check_existing=False);im.scale(1024,1024);dest=runtime/('Face'+str(variant)+'.png');im.filepath_raw=str(dest);im.file_format='PNG';im.save()
 sources.append(dict(variant=variant,person=person,model=source.name,source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),diffuse_sha256=hashlib.sha256(texture.read_bytes()).hexdigest(),license='Renderpeople T&C (restricted; local-only derivatives)',url='https://renderpeople.com/free-3d-people/',crop_min_z=neck))
 outputs.append(dict(file=dest.relative_to(r).as_posix(),sha256=hashlib.sha256(dest.read_bytes()).hexdigest()))
 for lod,ratio in enumerate([.20,.045]):
  o.data=original.copy();bpy.context.view_layer.objects.active=o;o.select_set(True);mod=o.modifiers.new('Bounded head LOD','DECIMATE');mod.ratio=ratio;bpy.ops.object.modifier_apply(modifier=mod.name)
  mesh=o.data;mesh.calc_loop_triangles();data=dict(schema_version=1,id=f'g2-crowd-{variant}-{lod}',vertices=[],normals=[],colors=[],pivots=[],face_uv=[],indices=[])
  def append(p,n,color,pivot=(0,0,0,0),uv=(0,0,0)):
   i=len(data['vertices'])//3;data['vertices'].extend(p);data['normals'].extend(n);data['colors'].extend(color);data['pivots'].extend(pivot);data['face_uv'].extend(uv);return i
  # Reflection (x,z,y) preserves the negative-Z facing of the existing seated crowd; reverse winding.
  for tri in mesh.loop_triangles:
   ids=[]
   for loop in tri.loops:
    p=mesh.vertices[mesh.loops[loop].vertex_index].co;n=mesh.corner_normals[loop].vector;uv=mesh.uv_layers.active.data[loop].uv
    ids.append(append((p.x*scale,(p.z-neck)*scale+1.08,p.y*scale),(n.x,n.z,n.y),(1,1,1,0),uv=(uv.x,uv.y,1)))
   data['indices'].extend((ids[0],ids[2],ids[1]))
  sides=12 if lod==0 else 7
  def loft(rings,color,pivot=(0,0,0,0),axis=None):
   verts=[];faces=[]
   for j,(center,rx,rz) in enumerate(rings):
    for k in range(sides):
     a=k*math.tau/sides;radial=Vector((math.cos(a)*rx,0,math.sin(a)*rz))
     if axis is not None:radial=axis@radial
     verts.append(Vector(center)+radial)
   for j in range(len(rings)-1):
    for k in range(sides):a=j*sides+k;b=j*sides+(k+1)%sides;faces.append((a,a+sides,b+sides,b))
   faces.extend((tuple(range(sides)),tuple((len(rings)-1)*sides+k for k in reversed(range(sides)))))
   m=bpy.data.meshes.new('Original winter clothing');m.from_pydata(verts,[],faces);m.update()
   for f in m.polygons:f.use_smooth=True
   m.calc_loop_triangles()
   for tri in m.loop_triangles:
    for loop in tri.loops:
     p=m.vertices[m.loops[loop].vertex_index].co;n=m.corner_normals[loop].vector
     data['indices'].append(append(p,n,color,pivot))
   bpy.data.meshes.remove(m)
  def limb(a,b,radii,color,pivot=(0,0,0,0)):
   a=Vector(a);b=Vector(b);q=Vector((0,1,0)).rotation_difference((b-a).normalized()).to_matrix();loft([(a+(b-a)*t,rx,rz) for t,rx,rz in radii],color,pivot,q)
  # Shaped coat rings and raised hem/collar replace the former disconnected torso ellipsoids.
  loft([((0,.33,0),.18,.135),((0,.38,0),.205,.155),((0,.45,0),.213,.16),((0,.55,0),.2,.155),((0,.68,0),.206,.16),((0,.81,0),.218,.163),((0,.94,0),.229,.145),((0,1.01,0),.20,.13),((0,1.04,0),.10,.105)],coat)
  loft([((0,1.00,0),.112,.114),((0,1.035,0),.13,.122),((0,1.08,0),.124,.119),((0,1.10,0),.105,.11)],scarf)
  for sign in [-1,1]:
   hip=(sign*.105,.39,0);knee=(sign*.115,.3,-.29);ankle=(sign*.115,-.015,-.31)
   limb(hip,knee,[(0,.092,.085),(.25,.09,.08),(.75,.08,.074),(1,.072,.067)],cloth)
   limb(knee,ankle,[(0,.073,.068),(.25,.072,.066),(.8,.063,.057),(1,.06,.052)],cloth)
   loft([((sign*.115,-.09,-.38),.076,.145),((sign*.115,-.065,-.38),.077,.15),((sign*.115,.015,-.37),.073,.134),((sign*.115,.09,-.31),.061,.065)],boot)
   shoulder=(sign*.20,.96,0);elbow=(sign*.245,.69,-.055);hand=(sign*.21,.69,-.255);pivot=(*shoulder,1)
   limb(shoulder,elbow,[(0,.085,.085),(.2,.088,.082),(.6,.073,.069),(1,.067,.064)],coat,pivot)
   limb(elbow,hand,[(0,.068,.065),(.4,.065,.061),(.85,.052,.051),(1,.049,.046)],coat,pivot)
   limb(hand,(sign*.21,.675,-.34),[(0,.046,.043),(.45,.052,.042),(1,.037,.027)],boot,pivot)
  loft([((0,1.285,.012),.113,.107),((0,1.32,.012),.119,.112),((0,1.37,.014),.112,.104),((0,1.42,.016),.075,.077),((0,1.445,.016),.012,.016)],coat)
  path=out/f'Crowd{variant}LOD{lod}.json';path.write_text(json.dumps(data,separators=(',',':')))
  outputs.append(dict(file=path.name,triangles=len(data['indices'])//3,vertices=len(data['vertices'])//3,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
 manifest=dict(schema_version=1,id='g2-local-crowd-derivation-v1',sources=sources,outputs=outputs,modifications=['head/upper-neck geometry only; all original clothing, watches, shoes and source materials discarded','two reduced head LODs,1K face diffuse; original winter coats/scarves/hats/gloves/trousers/boots combined locally','no public source or derivative mesh/texture distribution; rendered review images only','static seated geometry with existing bounded presentation shader reactions; no gameplay authority'])
 (out/'manifest.json').write_text(json.dumps(manifest,indent=2))
print(json.dumps([x for x in outputs if 'triangles' in x]))
