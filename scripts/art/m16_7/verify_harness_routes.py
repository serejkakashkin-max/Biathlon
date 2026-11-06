"""Shoulder webbing must return under the arm; a capped dangling strip fails."""
import bpy,bmesh,json,sys,argparse
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--asset',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.open_mainfile(filepath=str(Path(a.asset).resolve()));rows=[]
for o in bpy.context.scene.objects:
    if o.type!='MESH'or not o.name.endswith('_Rifle_Harness_Webbing'):continue
    bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
    boundary=sum(not e.is_manifold for e in bm.edges);euler=len(bm.verts)-len(bm.edges)+len(bm.faces);volume=abs(bm.calc_volume(signed=True))
    points=[o.matrix_world@v.co for v in bm.verts];front=sum(p.y<-.045 and p.z<1.32 for p in points);side=sum(abs(p.x)>.16 and p.z<1.29 for p in points);back=sum(p.y>.09 and p.z<1.32 for p in points)
    rows.append({'object':o.name,'nonmanifold_edges':boundary,'euler_characteristic':euler,'volume_m3':volume,'front_lower_vertices':front,'underarm_return_vertices':side,'rear_lower_vertices':back,'pass':boundary==0 and euler==0 and volume>1e-6 and front>8 and side>8 and back>8});bm.free()
points=[];faces=[]
for o in bpy.context.scene.objects:
    if o.type!='MESH'or not o.name.endswith('_Rifle_Harness_Webbing'):continue
    start=len(points);points.extend(o.matrix_world@v.co for v in o.data.vertices);faces.extend(tuple(i+start for i in f.vertices)for f in o.data.polygons)
from mathutils.bvhtree import BVHTree
from mathutils import Vector
tree=BVHTree.FromPolygons(points,faces);adjusters=[]
for o in bpy.context.scene.objects:
    if o.type!='MESH'or not o.name.startswith('Harness_Adjuster'):continue
    centre=sum((o.matrix_world@v.co for v in o.data.vertices),Vector())/len(o.data.vertices);gap=tree.find_nearest(centre)[3]
    adjusters.append({'object':o.name,'centre_to_webbing_m':gap,'pass':gap<.010})
report={'schema_version':1,'id':'m167_complete_load_bearing_harness_routes','pass':len(rows)==2 and all(r['pass']for r in rows)and len(adjusters)==2 and all(r['pass']for r in adjusters),'rows':rows,'adjusters':adjusters}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Rifle shoulder straps are still open or lack underarm return paths')
