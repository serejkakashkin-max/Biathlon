"""Measure actual visual prop against athlete; no functional weapon dimensions."""
import bpy,argparse,json,sys,numpy as np
from pathlib import Path
from mathutils import Vector
p=argparse.ArgumentParser();p.add_argument('--asset',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.open_mainfile(filepath=str(Path(a.asset).resolve()))
def points(obj):return [obj.matrix_world@v.co for v in obj.data.vertices]
def span(vertices,axis):return max(v[axis]for v in vertices)-min(v[axis]for v in vertices)
# Flexible shoulder suspension belongs to the skinned body, not the rigid prop.
rifle=[o for o in bpy.context.scene.objects if o.type=='MESH' and '_Rifle_Suspension_' not in o.name and any(m and ('Rifle_' in m.name)for m in o.data.materials)]
allpoints=[v for o in rifle for v in points(o)];stock=points(bpy.data.objects['PULSE_Biathlon_Walnut_Stock'])
arm=bpy.data.objects['BiathleteSkeleton'];shoulders=abs((arm.matrix_world@arm.data.bones['LeftArm'].head_local).x-(arm.matrix_world@arm.data.bones['RightArm'].head_local).x)
body=[v for o in bpy.context.scene.objects if o.type=='MESH' and (o.name in ['Race_Cap','Anatomical_Head_Neck']or o.name.endswith('_Nordic_Boot'))for v in points(o)];height=span(body,2)
rows=[]
def check(name,value,minimum,maximum):rows.append({'name':name,'value':value,'minimum':minimum,'maximum':maximum,'pass':minimum<=value<=maximum})
check('assembly_height_to_athlete',span(allpoints,2)/height,.49,.58)
check('assembly_width_to_shoulders',span(allpoints,0)/shoulders,.15,.50)
check('assembly_centre_offset_m',abs((min(v.x for v in allpoints)+max(v.x for v in allpoints))*.5),0,.015)
check('wooden_stock_width_to_shoulders',span(stock,0)/shoulders,.10,.34)
check('wooden_stock_thickness_m',span(stock,1),.022,.036)
upper=[o for o in rifle if o.name=='PULSE_Slim_Barrel_Exterior' or o.name=='CC0_Visible_Metal_8']
def transverse_size(obj):
    p=np.array([list(v)for v in points(obj)]);p-=p.mean(axis=0);_,axes=np.linalg.eigh(np.cov(p.T));cross=p@axes[:,:2];return float(max(np.ptp(cross,axis=0)))
check('upper_visible_barrel_profile_m',max(transverse_size(o)for o in upper),.012,.024)
report={'schema_version':1,'id':'m167_owner_rifle_visual_proportion_gate','pass':all(r['pass']for r in rows),'athlete_height_m':height,'shoulder_span_m':shoulders,'assembly_world_bounds_m':{'x':span(allpoints,0),'y':span(allpoints,1),'z':span(allpoints,2)},'rows':rows,'scope':'art proportions for this1.8mhero; approximated from owner CGTrader/full-body reference and real competition photos, not official manufacturing limits'}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Rifle remains visually oversized for athlete')
