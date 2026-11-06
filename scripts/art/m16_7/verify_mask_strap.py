"""Actual saved mask webbing volume and rear-cap fit. Old flat strip rejects."""
import argparse,sys,json
from pathlib import Path
import bpy,bmesh
from mathutils.bvhtree import BVHTree
p=argparse.ArgumentParser();p.add_argument('--asset',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.open_mainfile(filepath=str(Path(a.asset).resolve()));graph=bpy.context.evaluated_depsgraph_get()
o=bpy.data.objects['Mask_PULSE_Fitted_Elastic_Strap'];evaluated=o.evaluated_get(graph);m=evaluated.to_mesh();bm=bmesh.new();bm.from_mesh(m);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
boundary=sum(not e.is_manifold for e in bm.edges);volume=abs(bm.calc_volume(signed=True));bm.free();points=[o.matrix_world@v.co for v in m.vertices];evaluated.to_mesh_clear()
cap=bpy.data.objects['Race_Cap'];e=cap.evaluated_get(graph);m=e.to_mesh();tree=BVHTree.FromPolygons([cap.matrix_world@v.co for v in m.vertices],[tuple(f.vertices)for f in m.polygons]);e.to_mesh_clear()
gaps=[]
for point in points:
 if point.y>.035:
  near=tree.find_nearest(point);gaps.append((point-near[0]).dot(near[1]))
rows=[{'name':'closed_solid_webbing','pass':boundary==0 and .000005<volume<.00008,'nonmanifold_edges':boundary,'volume_m3':volume},{'name':'rear_webbing_on_cap','pass':bool(gaps) and min(gaps)>-.00015 and max(gaps)<.004,'minimum_signed_gap_m':min(gaps),'maximum_signed_gap_m':max(gaps),'samples':len(gaps)}]
report={'schema_version':1,'id':'m167_mask_strap_volume_and_cap_fit','pass':all(r['pass']for r in rows),'rows':rows,'method':'saved evaluated actual mesh; welded manifold volume and signed nearest-cap distance for all rear webbing vertices; rest pose only'}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Mask strap remains a flat/floating ribbon')
