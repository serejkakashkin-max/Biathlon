"""Actual source suspension: solid volume and both ends attached to real surfaces."""
import bpy,bmesh,argparse,json,sys
from pathlib import Path
from mathutils.bvhtree import BVHTree
p=argparse.ArgumentParser();p.add_argument('--asset',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.open_mainfile(filepath=str(Path(a.asset).resolve()))
graph=bpy.context.evaluated_depsgraph_get()
def tree(objects):
    points=[];faces=[]
    for o in objects:
        e=o.evaluated_get(graph);m=e.to_mesh();offset=len(points);points.extend(o.matrix_world@v.co for v in m.vertices);faces.extend(tuple(i+offset for i in f.vertices)for f in m.polygons);e.to_mesh_clear()
    return BVHTree.FromPolygons(points,faces)
wood=tree([bpy.data.objects['PULSE_Biathlon_Walnut_Stock']]);straps=tree([o for o in bpy.context.scene.objects if o.name.endswith('_Rifle_Harness_Webbing')]);rows=[]
for o in bpy.context.scene.objects:
    if o.type!='MESH' or '_Rifle_Suspension_'not in o.name:continue
    bm=bmesh.new();bm.from_mesh(o.data);boundary=sum(not e.is_manifold for e in bm.edges);volume=abs(bm.calc_volume(signed=True));bm.free()
    v=[o.matrix_world@v.co for v in o.data.vertices];start=sum(v[:4],v[0]*0)/4;end=sum(v[-4:],v[0]*0)/4
    shoulder_gap=straps.find_nearest(start)[3];stock_gap=wood.find_nearest(end)[3];weight_error=max(abs(sum(g.weight for g in vertex.groups)-1)for vertex in o.data.vertices)
    rows.append({'object':o.name,'nonmanifold_edges':boundary,'volume_m3':volume,'shoulder_gap_m':shoulder_gap,'stock_gap_m':stock_gap,'maximum_weight_sum_error':weight_error,'pass':boundary==0 and volume>1e-6 and shoulder_gap<.002 and stock_gap<.002 and weight_error<1e-5})
report={'schema_version':1,'id':'m167_solid_attached_rifle_suspension','pass':len(rows)==4 and all(r['pass']for r in rows),'rows':rows}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Rifle suspension missing, open or detached')
