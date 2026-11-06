"""Check distinct distal finger surfaces, independent of IK target positions."""
import argparse, itertools, json, sys
from pathlib import Path
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

p=argparse.ArgumentParser()
p.add_argument('--asset',required=True);p.add_argument('--anatomy',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:])
anatomy=Path(a.anatomy).resolve();meta=json.loads(anatomy.with_suffix('.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(Path(a.asset).resolve()))
with bpy.data.libraries.load(str(anatomy),link=False) as (_,data):data.objects=['MakeHuman_Athletic_Fitted']
source=data.objects[0];rows=[]
for prefix,side in [('Left',1),('Right',-1)]:
    suffix='.L' if side>0 else '.R';wrist=Vector(meta['heads'][prefix+'Hand'])
    direction=(wrist-Vector(meta['heads'][prefix+'ForeArm'])).normalized()
    keep={v.index for v in source.data.vertices if (v.co-wrist).dot(direction)>-.008 and side*v.co.x>.41}
    indices=sorted({i for f in source.data.polygons if all(k in keep for k in f.vertices) for i in f.vertices})
    for variant in ['Skate','Tuck']:
        glove=bpy.data.objects[prefix+('_Anatomical_Glove' if variant=='Skate' else '_Tuck_Glove')]
        points=[glove.matrix_world@v.co for v in glove.data.vertices];regions={}
        for digit in range(1,6):
            faces=[]
            for face in glove.data.polygons:
                corners=[k for k in face.vertices if k<len(indices)]
                if any(sum(meta['finger_weights'][indices[k]].get(f'finger{digit}-{joint}{suffix}',0) for joint in [2,3])>.65 for k in corners):
                    faces.append(list(face.vertices))
            if not faces:raise RuntimeError('Missing actual distal finger surface')
            regions[digit]=(faces,BVHTree.FromPolygons(points,faces))
        pairs=[]
        for left,right in itertools.combinations(range(1,6),2):
            faces_a,tree_a=regions[left];faces_b,tree_b=regions[right]
            overlaps=tree_a.overlap(tree_b);depth=0
            # The proximal webs are excluded by source distal skin weights.
            # Only actual intersecting face pairs are sampled, so an unrelated
            # concave surface cannot be mistaken for another finger's interior.
            for first,second in overlaps:
                for face,other in [(faces_a[first],tree_b),(faces_b[second],tree_a)]:
                    samples=[points[k] for k in face]
                    samples.append(sum(samples,Vector())/len(samples))
                    for point in samples:
                        location,normal,_,distance=other.find_nearest(point)
                        if location is not None and (point-location).dot(normal)<0:depth=max(depth,distance)
            pairs.append({'digits':[left,right],'intersecting_distal_face_pairs':len(overlaps),'maximum_sampled_penetration_m':depth,'pass':depth<=.002})
        rows.append({'hand':prefix,'grip':variant,'pairs':pairs,'pass':all(row['pass'] for row in pairs)})
report={'schema_version':1,'id':'m167_actual_distal_finger_clearance','asset':a.asset,'hands':rows,'pass':all(row['pass'] for row in rows),'art_acceptance':False,
        'method':'BVH intersections of actual distinct distal finger regions, classified by source skin weights. Intersecting face vertices/centroids are checked for inward signed proximity; 2mm compression allowance. This sampled test does not prove continuous nonintersection.'}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Distal finger surfaces interpenetrate beyond the glove compression allowance')
