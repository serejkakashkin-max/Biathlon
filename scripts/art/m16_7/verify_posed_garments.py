"""Check actual imported Unity skinning snapshots, not independently posed Blender art."""
import argparse,json,sys
from pathlib import Path
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
p=argparse.ArgumentParser();p.add_argument('--poses',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);data=json.loads(Path(a.poses).read_text());triangles={key:[value[i:i+3] for i in range(0,len(value),3)] for key,value in data['triangles'].items()};rows=[]
def strict_crossing(first,second):
    for left,right in [(first,second),(second,first)]:
        a,b,c=right;e1=b-a;e2=c-a
        for start,end in zip(left,left[1:]+left[:1]):
            direction=end-start;cross=direction.cross(e2);det=e1.dot(cross)
            if abs(det)<1e-12:continue
            delta=start-a;u=delta.dot(cross)/det
            if u<=1e-5 or u>=1-1e-5:continue
            q=delta.cross(e1);v=direction.dot(q)/det;t=e2.dot(q)/det
            if v>1e-5 and u+v<1-1e-5 and 1e-5<t<1-1e-5:return True
    return False
for pose in data['poses']:
    cloth=BVHTree.FromPolygons(pose['parts']['suit'],triangles['suit'],all_triangles=True);points=[Vector(v) for v in pose['parts']['bib']];minimum=1;buried=0
    bib=BVHTree.FromPolygons(points,triangles['bib'],all_triangles=True);candidates=bib.overlap(cloth);suit=[Vector(v)for v in pose['parts']['suit']]
    intersections=[pair for pair in candidates if strict_crossing([points[k]for k in triangles['bib'][pair[0]]],[suit[k]for k in triangles['suit'][pair[1]]])]
    for face in triangles['bib']:
        # Centroids detect support cuts that corner-only tests miss.
        centre=sum((points[k] for k in face),Vector())/3;hit=cloth.find_nearest(centre);signed=(centre-hit[0]).dot(hit[1]);minimum=min(minimum,signed)
        if signed<-.00015:buried+=1
    rows.append({'state':pose['state'],'phase':pose['phase'],'bib_triangles':len(triangles['bib']),'minimum_signed_clearance_m':minimum,'negative_nearest_plane_centroids':buried,'actual_visible_triangle_intersections':len(intersections),'pass':len(intersections)==0})
report={'schema_version':1,'id':'m167_actual_unity_posed_bib_clearance','source_export_fbx_sha256':data['source_export_fbx_sha256'],'rows':rows,'pass':all(row['pass'] for row in rows),'art_acceptance':False,'method':'Actual Unity BakeMesh at16poses; bib/suit BVH candidates followed by strict edge/triangle interior intersections. Shared edges/vertices are contact rather than strict interior intersections. Nearest-plane sign is diagnostic only, including near concave fabric. Rejected earlier layer snapshots fail the same gate. No continuous-time/coplanar-overlap proof.'}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Actual posed bib sinks through the racing suit')
