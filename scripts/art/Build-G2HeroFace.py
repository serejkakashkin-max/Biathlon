"""Local-only licensed Renderpeople face for the hero; no source/derivative payload in Git.
Run in Blender background. Retain only head/neck, crop out source clothing and branding.
"""
from pathlib import Path
import bpy,bmesh,json,hashlib,math
from mathutils import Matrix,Vector
import numpy as np

root=Path.cwd()
source=root/'external_downloads/g2_visual_remediation/01_characters/crowd_people/renderpeople/person_06/extracted/rp_eric_rigged_001_u3d.fbx'
diffuse=source.parent/'tex/rp_eric_rigged_001_dif.jpg'
out=root/'artifacts/g2-hero-face';out.mkdir(parents=True,exist_ok=True)
runtime=root/'Assets/Resources/Biathlon/RestrictedG2Hero';runtime.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(source))
obj=next(o for o in bpy.data.objects if o.type=='MESH')
matrix=obj.matrix_world.copy()
for v in obj.data.vertices:v.co=matrix@v.co
obj.matrix_world=Matrix.Identity(4);obj.modifiers.clear()
# Original smooth textile shell fitted to the measured head envelope. No copied
# hair/ear topology: those details must not emboss through a winter cap.
vertices=[];faces=[];segments=64
profile=((0,1),(.015,1),(.035,.96),(.055,.85),(.075,.65),(.092,.36),(.100,.025))
for row,(height,radius) in enumerate(profile):
 for col in range(segments):
  angle=col*2*math.pi/segments
  ux=-.011+math.cos(angle)*.0805*radius;uy=1.722+height;uz=.026+math.sin(angle)*.099*radius
  vertices.append((ux/.9,-uz/.9,(uy-1.59)/.9+1.565))
  if row<len(profile)-1:
   a=row*segments+col;b=row*segments+(col+1)%segments
   faces.extend(((a,a+segments,b),(b,a+segments,b+segments)))
faces.append(tuple((len(profile)-1)*segments+i for i in reversed(range(segments))))
capmesh=bpy.data.meshes.new('Original fitted textile shell');capmesh.from_pydata(vertices,[],faces);capmesh.update()
cap=bpy.data.objects.new('Fitted textile cap',capmesh);bpy.context.collection.objects.link(cap)
capuv=capmesh.uv_layers.new(name='UVMap')
for poly in capmesh.polygons:
 poly.use_smooth=True
 for loop in poly.loop_indices:
  index=capmesh.loops[loop].vertex_index;capuv.data[loop].uv=(index%segments/segments,(index//segments)/(len(profile)-1))
bm=bmesh.new();bm.from_mesh(obj.data)
# Source office clothing is below this boundary; no clothing/source materials retained.
bmesh.ops.delete(bm,geom=[f for f in bm.faces if any(v.co.z<(1.635 if v.co.y>-.045 else 1.595) or abs(v.co.x)>.18 for v in f.verts)],context='FACES')
bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,plane_co=(0,0,1.714),plane_no=(0,0,1),clear_outer=True,clear_inner=False)
bm.to_mesh(obj.data);bm.free();obj.data.materials.clear()
image=bpy.data.images.load(str(diffuse),check_existing=False)
# Crop to just the retained UV region before resizing, preserving more face texels than
# resizing the entire head-to-toe atlas. Crop metadata supports reproducible audit.
uv=obj.data.uv_layers.active
used=[uv.data[l].uv.copy() for p in obj.data.polygons for l in p.loop_indices]
w,h=image.size
lo=[max(0,int(min(v[i] for v in used)*s)-4) for i,s in enumerate((w,h))]
hi=[min(s,int(max(v[i] for v in used)*s)+5) for i,s in enumerate((w,h))]
pixels=np.empty(w*h*4,dtype=np.float32);image.pixels.foreach_get(pixels)
crop=pixels.reshape((h,w,4))[lo[1]:hi[1],lo[0]:hi[0]].copy()
face=bpy.data.images.new('Licensed hero face',width=hi[0]-lo[0],height=hi[1]-lo[1],alpha=True)
face.pixels.foreach_set(crop.reshape(-1));face.scale(2048,2048)
for l in uv.data:l.uv=((l.uv.x*w-lo[0])/(hi[0]-lo[0]),(l.uv.y*h-lo[1])/(hi[1]-lo[1]))
# Retain only pixels actually referenced by the cropped head. Source office clothes and
# logo/watermark-filled atlas background never enter the runtime map. Extend island edge
# colors into a mip gutter instead of retaining adjacent source background pixels.
obj.data.calc_loop_triangles();mask=np.zeros((2048,2048),dtype=bool)
for tri in obj.data.loop_triangles:
 a,b,c=[np.array(uv.data[l].uv)*2048-.5 for l in tri.loops]
 low=np.maximum(0,np.floor(np.minimum(np.minimum(a,b),c)).astype(int));high=np.minimum(2047,np.ceil(np.maximum(np.maximum(a,b),c)).astype(int))
 if np.any(high<low):continue
 xx,yy=np.meshgrid(np.arange(low[0],high[0]+1),np.arange(low[1],high[1]+1))
 det=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
 if abs(det)<1e-8:continue
 s=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/det
 t=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/det
 mask[low[1]:high[1]+1,low[0]:high[0]+1]|=(s>=0)&(t>=0)&(s+t<=1)
rgba=np.empty(2048*2048*4,dtype=np.float32);face.pixels.foreach_get(rgba);rgba=rgba.reshape((2048,2048,4));rgba[~mask]=0
for _ in range(16):
 total=np.zeros_like(rgba);count=np.zeros_like(mask,dtype=np.float32)
 for dy,dx in ((-1,0),(1,0),(0,-1),(0,1)):
  neighbor=np.roll(mask,(dy,dx),(0,1));values=np.roll(rgba,(dy,dx),(0,1))
  if dy==-1:neighbor[-1,:]=False
  if dy==1:neighbor[0,:]=False
  if dx==-1:neighbor[:,-1]=False
  if dx==1:neighbor[:,0]=False
  total+=values*neighbor[:,:,None];count+=neighbor
 fill=(~mask)&(count>0);rgba[fill]=total[fill]/count[fill,None];mask|=fill
rgba[~mask]=(.08,.07,.06,1);face.pixels.foreach_set(rgba.reshape(-1))
face.filepath_raw=str(runtime/'Face.png');face.file_format='PNG';face.save()
outputs=[]
for part,label in ((obj,'Face'),(cap,'Cap')):
 original=part.data.copy()
 for lod,ratio in enumerate((.6,.24,.07)):
  part.data=original.copy();bpy.context.view_layer.objects.active=part;part.select_set(True)
  mod=part.modifiers.new('Hero LOD','DECIMATE');mod.ratio=ratio;bpy.ops.object.modifier_apply(modifier=mod.name)
  mesh=part.data;mesh.calc_loop_triangles();data=dict(schema_version=1,id=f'g2-hero-{label.lower()}-{lod}',vertices=[],normals=[],uv=[],indices=[])
  for tri in mesh.loop_triangles:
   for loop in tri.loops:
    v=mesh.vertices[mesh.loops[loop].vertex_index].co;n=mesh.corner_normals[loop].vector;t=mesh.uv_layers.active.data[loop].uv
   # Source faces -Y in Blender. Rotation (x,z,-y) preserves winding and faces +Z in Unity.
    data['indices'].append(len(data['vertices'])//3)
    fitted_y=(v.z-1.565)*.9+1.59
    data['vertices'].extend((v.x*.9,fitted_y,-v.y*.9))
    normal=Vector((n.x,n.z,-n.y)).normalized()
    data['normals'].extend(normal);data['uv'].extend(t)
  path=out/f'{label}LOD{lod}.json';path.write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')
  outputs.append(dict(file=path.name,vertices=len(data['vertices'])//3,triangles=len(data['indices'])//3,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
manifest=dict(schema_version=1,id='g2-local-hero-face-v1',source=source.relative_to(root).as_posix(),source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),diffuse_sha256=hashlib.sha256(diffuse.read_bytes()).hexdigest(),source_url='https://renderpeople.com/free-3d-people/',license_url='https://renderpeople.com/general-terms-and-conditions/',license='Renderpeople restricted game use; no public raw/modified 3D data',texture_crop_pixels=[lo,hi],texture_sha256=hashlib.sha256((runtime/'Face.png').read_bytes()).hexdigest(),outputs=outputs,changes=['head/upper-neck only; source clothes/materials excluded','source UV region cropped to 2K; no invented facial texture','three rigid-head LODs, 0.9 uniform scale and fitted vertical offset','original smooth cap fitted to head envelope; source hair/ear topology not copied','unused diffuse atlas pixels discarded; UV island edge colors padded 16 pixels','local ignored derivatives only; integrated compiled game/rendered review evidence permitted'])
(out/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(json.dumps(outputs))
