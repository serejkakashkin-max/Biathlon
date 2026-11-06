"""Reject a claw grip that touches the handle only at proximal finger joints."""
import argparse,json,sys
from pathlib import Path
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
p=argparse.ArgumentParser();p.add_argument('--asset',required=True);p.add_argument('--anatomy',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);source=Path(a.anatomy).resolve();meta=json.loads(source.with_suffix('.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(Path(a.asset).resolve()))
with bpy.data.libraries.load(str(source),link=False) as (_,data):data.objects=['MakeHuman_Athletic_Fitted']
body=data.objects[0];rows=[]
for prefix,side in [('Left',1),('Right',-1)]:
    suffix='.L' if side>0 else '.R';wrist=Vector(meta['heads'][prefix+'Hand']);direction=(wrist-Vector(meta['heads'][prefix+'ForeArm'])).normalized()
    keep={v.index for v in body.data.vertices if (v.co-wrist).dot(direction)>-.008 and side*v.co.x>.41}
    indices=sorted({i for f in body.data.polygons if all(k in keep for k in f.vertices) for i in f.vertices});reverse={v:i for i,v in enumerate(indices)}
    for variant in ['Skate','Tuck']:
        glove=bpy.data.objects[prefix+('_Anatomical_Glove' if variant=='Skate' else '_Tuck_Glove')];handle=bpy.data.objects[prefix+'_Pole_Grip']
        planting=bpy.data.objects[prefix+'_Pole_Grip_Contact'];contact=bpy.data.objects[prefix+('_Pole_Grip_Contact' if variant=='Skate' else '_Tuck_Grip_Contact')]
        mapping=contact.matrix_world@planting.matrix_world.inverted()@handle.matrix_world
        tree=BVHTree.FromPolygons([mapping@v.co for v in handle.data.vertices],[list(f.vertices) for f in handle.data.polygons])
        normal_matrix=glove.matrix_world.to_3x3().inverted().transposed()
        for digit in range(1,6):
            key=f'finger{digit}-3{suffix}';patch=[reverse[k] for k in meta['finger_palmar_pad_vertex_ids'][key]]
            centre=sum((glove.matrix_world@glove.data.vertices[k].co for k in patch),Vector())/len(patch)
            normal=sum((normal_matrix@glove.data.vertices[k].normal for k in patch),Vector()).normalized()
            location,_,_,distance=tree.find_nearest(centre)
            facing=normal.dot((location-centre).normalized())
            distances=[tree.find_nearest(glove.matrix_world@glove.data.vertices[k].co)[3] for k in patch]
            rows.append({'hand':prefix,'grip':variant,'digit':digit,'measured_patch_vertices':len(patch),'palmar_pad_handle_clearance_m':distance,'pad_faces_handle':facing,
                         'minimum_palmar_patch_distance_m':min(distances),'palmar_patch_fraction_within_4mm':sum(value<.004 for value in distances)/len(distances),
                         'pass':min(distances)<.001 and sum(value<.004 for value in distances)/len(distances)>=.25 and facing>.3})
report={'schema_version':1,'id':'m167_actual_palmar_distal_pad_contact','asset':a.asset,'pads':rows,'pass':all(r['pass'] for r in rows),'art_acceptance':False,'method':'Both Skate and Tuck: actual saved subdivided palmar fingertip patches, classified by source distal weights and original palmar normals. Every digit requires patch contact <1mm, >=25% of its vertices within4mm and an inward facing pad. The mean patch centre is inside the curved fingertip, not a contact point. Proximal contact cannot pass this gate.'}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('A finger only touches proximally or its palmar pad faces away from the handle')
