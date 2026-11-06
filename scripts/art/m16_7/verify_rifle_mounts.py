"""Check decorative prop furniture mounts against the actual wooden shell."""
import bpy,argparse,sys,json
from pathlib import Path
from mathutils.bvhtree import BVHTree
p=argparse.ArgumentParser();p.add_argument('--asset',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.open_mainfile(filepath=str(Path(a.asset).resolve()));graph=bpy.context.evaluated_depsgraph_get()
def points_faces(obj):
    evaluated=obj.evaluated_get(graph);mesh=evaluated.to_mesh();points=[obj.matrix_world@v.co for v in mesh.vertices];faces=[tuple(f.vertices)for f in mesh.polygons];evaluated.to_mesh_clear();return points,faces
stock=bpy.data.objects['PULSE_Biathlon_Walnut_Stock'];v,f=points_faces(stock);tree=BVHTree.FromPolygons(v,f);rows=[]
for obj in bpy.context.scene.objects:
    if obj.type!='MESH' or not obj.name.startswith(('PULSE_Magazine_Rail','PULSE_Cheek_Rest_Post','PULSE_Stock_Fastener')):continue
    points,faces=points_faces(obj);samples=points+[sum((points[i]for i in face),points[0]*0)/len(face)for face in faces];gaps=[]
    for point in samples:
        hit=tree.find_nearest(point);gaps.append((point-hit[0]).dot(hit[1]))
    minimum=min(gaps);rows.append({'object':obj.name,'minimum_signed_stock_distance_m':minimum,'samples':len(gaps),'pass':minimum<-.0002})
report={'schema_version':1,'id':'m167_visible_external_prop_mount_contact','pass':len(rows)==10 and all(r['pass']for r in rows),'mounts':rows,'method':'saved evaluated decorative mesh vertices and face barycentres against nearest real stock surface; at least0.2mm embed required; not a functional mechanism/assembly test'}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Visible rifle prop mounts float above stock')
