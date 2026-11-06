"""Measure actual saved print/harness triangles against their fabric supports."""
import argparse,json,sys
from pathlib import Path
import bpy
from mathutils.bvhtree import BVHTree

p=argparse.ArgumentParser();p.add_argument('--asset',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.open_mainfile(filepath=str(Path(a.asset).resolve()))
scene=bpy.context.scene;graph=bpy.context.evaluated_depsgraph_get();bib=bpy.data.objects['Woven_Bib_27'];cloth=bpy.data.objects['RaceSuit_Continuous_Tailored_Shell']
def geometry(obj):
    mesh=obj.evaluated_get(graph).to_mesh();mesh.calc_loop_triangles()
    vertices=[obj.matrix_world@v.co for v in mesh.vertices];faces=[tuple(f.vertices) for f in mesh.loop_triangles]
    obj.evaluated_get(graph).to_mesh_clear();return vertices,faces
def tree(obj):
    v,f=geometry(obj);return BVHTree.FromPolygons(v,f,all_triangles=True)
bib_tree=tree(bib);cloth_tree=tree(bpy.data.objects.get('Suit_Bib_Lining_Reference') or cloth);rows=[]
points,faces=geometry(bib);inward=0;buried=0
for face in faces:
    corners=[points[k] for k in face];normal=(corners[1]-corners[0]).cross(corners[2]-corners[0])
    if normal.length<1e-10:continue
    normal.normalize();centre=sum(corners,corners[0]*0)/3;support=cloth_tree.find_nearest(centre)
    if normal.dot(support[1])<0:inward+=1
    if (centre-support[0]).dot(support[1])<-.00015:buried+=1
rows.append({'object':bib.name,'kind':'fabric_orientation','triangles':len(faces),'inward_triangles':inward,'buried_samples':buried,'pass':inward==0 and buried==0})
for obj in scene.objects:
    print_mesh=obj.name.startswith(('Bib27_','Bib_Header','Bib_Footer'))
    harness='Rifle_Harness_Webbing' in obj.name
    if obj.type!='MESH' or not(print_mesh or harness):continue
    points,faces=geometry(obj);distances=[]
    for face in faces:
        corners=[points[k] for k in face]
        samples=corners+[(corners[0]+corners[1]+corners[2])/3]
        for point in samples:
            surface=bib_tree if print_mesh else min([bib_tree,cloth_tree],key=lambda t:t.find_nearest(point)[3])
            hit=surface.find_nearest(point);distances.append((point-hit[0]).dot(hit[1]))
    buried=sum(d<-.00015 for d in distances)
    rows.append({'object':obj.name,'kind':'print' if print_mesh else 'harness','samples':len(distances),'buried_samples':buried,'minimum_signed_clearance_m':min(distances),'pass':buried==0})
report={'schema_version':1,'id':'m167_garment_detail_layer_clearance','asset':a.asset,'rows':rows,'pass':all(r['pass'] for r in rows),'art_acceptance':False,'method':'Actual saved evaluated triangles and barycentres, compared with the nearest real fabric triangle. 0.15mm signed numerical tolerance. Rest-pose gate; runtime deformation is separately qualified.'}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Garment detail triangles sink through the cloth')
