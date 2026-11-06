"""Check the actual glove, handle volume and strap surfaces in a saved source."""
import argparse,json,math,sys
from pathlib import Path
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

p=argparse.ArgumentParser();p.add_argument('--asset',required=True);p.add_argument('--anatomy',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:])
meta=json.loads(Path(a.anatomy).with_suffix('.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(Path(a.asset).resolve()));bpy.context.view_layer.update()

def tree(obj):
    return BVHTree.FromPolygons([obj.matrix_world@v.co for v in obj.data.vertices],[list(f.vertices) for f in obj.data.polygons])

with bpy.data.libraries.load(str(Path(a.anatomy).resolve()),link=False) as (_,data):data.objects=['MakeHuman_Athletic_Fitted']
source=data.objects[0]
rows=[]
for prefix,side in [('Left',1),('Right',-1)]:
    suffix='.L' if side>0 else '.R';wrist=Vector(meta['heads'][prefix+'Hand'])
    longitudinal=(Vector(meta['finger_bones']['finger3-1'+suffix]['head'])-wrist).normalized()
    knuckles=Vector(meta['finger_bones']['finger5-1'+suffix]['head'])-Vector(meta['finger_bones']['finger2-1'+suffix]['head'])
    across=(knuckles-longitudinal*knuckles.dot(longitudinal)).normalized();normal=across.cross(longitudinal).normalized()
    for variant in ['Skate','Tuck']:
        glove=bpy.data.objects[prefix+('_Anatomical_Glove' if variant=='Skate' else '_Tuck_Glove')]
        skin=tree(glove);strap=tree(bpy.data.objects[prefix+'_'+variant+'_Glove_Strap'])
        contact=bpy.data.objects[prefix+('_Pole_Grip_Contact' if variant=='Skate' else '_Tuck_Grip_Contact')]
        origin=contact.matrix_world.translation;axis=(contact.matrix_world.to_3x3()@Vector((0,1,0))).normalized()
        radial=axis.cross(longitudinal).normalized();other=axis.cross(radial).normalized();intersections=0
        # The rendered handle radius is 12 mm. Probe its interior to 10.5 mm,
        # allowing 1.5 mm for a compressed finger pad at contact.
        for radius in [0,.0035,.007,.0105]:
            for k in range(24 if radius else 1):
                offset=(radial*math.cos(k*math.tau/24)+other*math.sin(k*math.tau/24))*radius
                if skin.ray_cast(origin-axis*.075+offset,axis,.12)[0] is not None:intersections+=1
        # Subdivision retains the original cage vertices. Each subdivided
        # quad contains its source corner, which identifies the actual index
        # and thumb surface independently of the generated target positions.
        source_direction=(wrist-Vector(meta['heads'][prefix+'ForeArm'])).normalized()
        keep={v.index for v in source.data.vertices if (v.co-wrist).dot(source_direction)>-.008 and side*v.co.x>.41}
        indices=sorted({i for f in source.data.polygons if all(k in keep for k in f.vertices) for i in f.vertices})
        points=[glove.matrix_world@v.co for v in glove.data.vertices];contact_gaps={}
        for digit in ([0,1,2,3,4,5] if variant=='Tuck' else [1,2]):
            faces=[]
            for face in glove.data.polygons:
                corners=[k for k in face.vertices if k<len(indices)]
                if digit==0:
                    selected=any(sum(value for key,value in meta['finger_weights'][indices[k]].items() if key.startswith('metacarpal'))>.5 and sum(value for key,value in meta['finger_weights'][indices[k]].items() if key.startswith('finger'))<.25 for k in corners)
                else:
                    selected=bool(corners) and max(sum(value for key,value in meta['finger_weights'][indices[k]].items() if key.startswith('finger'+str(digit)+'-')) for k in corners)>.5
                if selected:faces.append(list(face.vertices))

            region=BVHTree.FromPolygons(points,faces);minimum=float('inf')
            for t in ([-.073,-.067,-.061,-.055,-.049,-.045] if variant=='Tuck' and digit in [1,2] else [-.065,-.05,-.035,-.02,-.005,.01,.025,.04]):
                for k in range(32):
                    sample=origin+axis*t+(radial*math.cos(k*math.tau/32)+other*math.sin(k*math.tau/32))*.012
                    minimum=min(minimum,region.find_nearest(sample)[3])
            for t in ([] if digit==0 else ([-.075] if variant=='Tuck' and digit in [1,2] else [-.075,.045])):
                for r in [0,.004,.008,.012]:
                    for k in range(24):
                        sample=origin+axis*t+(radial*math.cos(k*math.tau/24)+other*math.sin(k*math.tau/24))*r
                        minimum=min(minimum,region.find_nearest(sample)[3])
            contact_gaps[['palm','thumb','index','middle','ring','little'][digit]]=minimum
        enclosed=0;clearances=[];centre=wrist+longitudinal*.012
        for k in range(32):
            direction=normal*math.cos(k*math.tau/32)+across*math.sin(k*math.tau/32)
            hit=strap.ray_cast(centre,direction,.08);hand=skin.ray_cast(centre,direction,.08)
            if hit[0] is not None and hand[0] is not None:
                clearance=hit[3]-hand[3];clearances.append(clearance)
                if .0003<clearance<.006:enclosed+=1
        rows.append({'hand':prefix,'grip':variant,'handle_interior_rays_intersecting_glove':intersections,'handle_probe_rays':73,'wrist_enclosed_directions':enclosed,'wrist_probe_directions':32,'strap_skin_clearance_m':clearances,'thumb_index_handle_clearance_m':[contact_gaps['thumb'],contact_gaps['index']],'palm_and_finger_contact_clearance_m':contact_gaps,'contact_band_from_upper_end_m':([0,.030] if variant=='Tuck' else [0,.120]),'pass':intersections==0 and enclosed==32 and max(contact_gaps.values())<.004})
report={'schema_version':1,'id':'m167_actual_glove_handle_and_wrist_loop','asset':a.asset,'hands':rows,'pass':all(r['pass'] for r in rows),'art_acceptance':False,'method':'BVH ray intersections with actual subdivided glove and strap surfaces, using imported source contact positions; full wrist circumference and handle interior, rather than fingertip target coordinates.'}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Handle penetrates glove or strap does not enclose the wrist with clearance')
