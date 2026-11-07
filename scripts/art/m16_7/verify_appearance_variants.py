"""Verify saved CC0 face sources, morph bounds and the solid racing headband."""
import argparse
import bpy,bmesh,hashlib,json,sys,struct
from pathlib import Path
parser=argparse.ArgumentParser()
parser.add_argument('--asset',required=True,type=Path)
parser.add_argument('--output',required=True,type=Path)
parser.add_argument('--baseline',type=Path)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:])
asset=args.asset;out=args.output
def mesh_signatures():
 result={}
 for obj in bpy.data.objects:
  if obj.type!='MESH':continue
  digest=hashlib.sha256()
  for vertex in obj.data.vertices:
   digest.update(struct.pack('<3f',*vertex.co))
   for group in vertex.groups:
    digest.update(obj.vertex_groups[group.group].name.encode()+b'\0')
    digest.update(struct.pack('<f',group.weight))
  for polygon in obj.data.polygons:
   digest.update(struct.pack('<I',len(polygon.vertices)))
   digest.update(struct.pack('<'+'I'*len(polygon.vertices),*polygon.vertices))
  result[obj.name]=digest.hexdigest()
 return result
baseline=None
if args.baseline:
 bpy.ops.wm.open_mainfile(filepath=str(args.baseline.resolve()))
 baseline=mesh_signatures()
bpy.ops.wm.open_mainfile(filepath=str(asset.resolve()))
source=Path('Assets/Biathlon/Characters/Biathlete_16_7/Models/Source~/MakeHuman')
verified=[]
for directory in ('FaceTargets','HeadOptions'):
 d=json.loads((source/directory/'provenance.json').read_text())
 assert d.get('asset_license',d.get('license'))=='CC0-1.0',directory
 for row in d['files']:
  path=source/directory/row['file']
  assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'],path
  verified.append(row['file'])
head=bpy.data.objects['Anatomical_Head_Neck']
basis=head.data.shape_keys.key_blocks[0]
faces={}
for key in head.data.shape_keys.key_blocks[1:]:
 offsets=[(a.co-b.co).length for a,b in zip(key.data,basis.data)]
 faces[key.name]={'maximum_displacement_m':max(offsets),'moved_vertices':sum(v>1e-6 for v in offsets)}
 assert max(offsets)<.01501
band=bpy.data.objects['Headband_Prop']
bm=bmesh.new();bm.from_mesh(band.data)
boundary=sum(not e.is_manifold for e in bm.edges);volume=abs(bm.calc_volume(signed=True));bm.free()
assert boundary==0 and .00001<volume<.00005,(boundary,volume)
data={'schema_version':1,'id':'m167_cc0_appearance_source_verification','verified_source_files':verified,'faces':faces,'headband_nonmanifold_edges':boundary,'headband_volume_m3':volume,'pass':True}
if baseline is not None:
 current=mesh_signatures()
 changed=[name for name,sha in baseline.items() if current.get(name)!=sha]
 assert not changed,changed
 data['preserved_basis_meshes']=len(baseline)
 data['added_meshes']=sorted(set(current)-set(baseline))
 assert data['added_meshes']==['Hair_Crew_Prop','Hair_Short_Prop','Headband_Prop']
out.write_bytes((json.dumps(data,indent=2)+'\n').encode())
print(len(verified),'hashed CC0 files; headband',boundary,'nonmanifold edges',volume,'m3')
