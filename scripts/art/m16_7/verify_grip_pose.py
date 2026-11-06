"""Inspect the actual saved glove surface, using pinned source vertex correspondence."""
import argparse,json,sys
from pathlib import Path
import bpy
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--asset',required=True);p.add_argument('--anatomy',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);anatomy=Path(a.anatomy).resolve();meta=json.loads(anatomy.with_suffix('.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(Path(a.asset).resolve()))
with bpy.data.libraries.load(str(anatomy),link=False) as (_,data):data.objects=['MakeHuman_Athletic_Fitted']
source=data.objects[0];vertices=source.data.vertices;rows=[]
for pre,side in [('Left',1),('Right',-1)]:
 suffix='.L' if side>0 else '.R';wrist=Vector(meta['heads'][pre+'Hand']);direction=(wrist-Vector(meta['heads'][pre+'ForeArm'])).normalized()
 keep={v.index for v in vertices if (v.co-wrist).dot(direction)>-.008 and side*v.co.x>.41}
 faces=[f for f in source.data.polygons if all(k in keep for k in f.vertices)];indices=sorted({i for f in faces for i in f.vertices})
 glove=bpy.data.objects[pre+'_Anatomical_Glove'];contact=bpy.data.objects[pre+'_Pole_Grip_Contact'];axis=(contact.matrix_world.to_3x3()@Vector((0,1,0))).normalized();centre=contact.matrix_world.translation
 def region(digit,joints):
  selected=[]
  for k,i in enumerate(indices):
   weight=sum(meta['finger_weights'][i].get(f'finger{digit}-{j}{suffix}',0) for j in joints)
   if weight>.70:selected.append(glove.matrix_world@glove.data.vertices[k].co)
  if not selected:raise RuntimeError('No source-corresponding fingertip surface')
  return sum(selected,Vector())/len(selected)
 thumb=region(1,[3]);index=region(2,[2,3]);middle=region(3,[2,3])
 offset=(thumb-index).dot(axis);gap=(middle-index).dot(axis)
 radial=(thumb-centre)-axis*(thumb-centre).dot(axis)
 rows.append({'hand':pre,'thumb_index_axis_offset_m':offset,'index_middle_axis_gap_m':gap,'thumb_radius_from_handle_axis_m':radial.length,'pass':bool(offset<.003 and radial.length>.012)})
report={'schema_version':1,'id':'m167_thumb_outside_index_middle_gap','asset':a.asset,'hands':rows,'pass':all(r['pass'] for r in rows),'art_acceptance':False,'method':'Actual subdivided glove surface at retained original cage vertex slots, grouped by pinned source distal finger skin weights; not a test of generated target coordinates.'}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Thumb surface remains in the index-middle gap or penetrates the handle')
