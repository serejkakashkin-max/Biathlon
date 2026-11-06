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

def handle_surface_samples(prefix,contact):
    """Sample the actual rounded handle mesh, not a coarse cylinder lattice."""
    handle=bpy.data.objects.get(prefix+'_Pole_Grip')
    if handle is None:
        # Tiny retained historical glove-only fixtures predate handle retention.
        # Their large rejected gaps are checked with an explicit legacy fallback.
        axis=(contact.matrix_world.to_3x3()@Vector((0,1,0))).normalized()
        radial=axis.orthogonal().normalized();other=axis.cross(radial)
        return [contact.matrix_world.translation+axis*(-.075+i*.0015)+(radial*math.cos(k*math.tau/64)+other*math.sin(k*math.tau/64))*.012 for i in range(81) for k in range(64)],'legacy_cylinder_fixture'
    planting=bpy.data.objects[prefix+'_Pole_Grip_Contact']
    mapping=contact.matrix_world@planting.matrix_world.inverted()@handle.matrix_world
    points=[mapping@v.co for v in handle.data.vertices];samples=[]
    for face in handle.data.polygons:
        corners=[points[k] for k in face.vertices]
        if len(corners)==4:
            a,b,c,d=corners;nu=max(1,math.ceil((b-a).length/.0015));nv=max(1,math.ceil((d-a).length/.0015))
            for i in range(nu+1):
                u=i/nu
                for j in range(nv+1):
                    v=j/nv;samples.append(a*((1-u)*(1-v))+b*(u*(1-v))+c*(u*v)+d*((1-u)*v))
        else:
            centre=sum(corners,Vector())/len(corners)
            for a,b in zip(corners,corners[1:]+corners[:1]):
                steps=max(1,math.ceil(max((a-centre).length,(b-centre).length)/.0015))
                for i in range(steps+1):
                    for j in range(steps+1-i):samples.append(centre*(1-(i+j)/steps)+a*(i/steps)+b*(j/steps))
    return samples,'actual_rounded_handle_mesh'

def handle_volume(prefix,contact):
    handle=bpy.data.objects.get(prefix+'_Pole_Grip')
    if handle is None:return None
    mapping=contact.matrix_world@bpy.data.objects[prefix+'_Pole_Grip_Contact'].matrix_world.inverted()@handle.matrix_world
    return BVHTree.FromPolygons([mapping@v.co for v in handle.data.vertices],[list(f.vertices) for f in handle.data.polygons])

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
        volume=handle_volume(prefix,contact)
        orientation=1
        if volume is not None:
            nearest,handle_normal,_,_=volume.find_nearest(origin)
            orientation=1 if (origin-nearest).dot(handle_normal)<0 else -1
        # The rendered handle radius is 12 mm. Probe its interior to 10.5 mm,
        # allowing 1.5 mm for a compressed finger pad at contact.
        for radius in [0,.0035,.007,.0105]:
            for k in range(24 if radius else 1):
                offset=(radial*math.cos(k*math.tau/24)+other*math.sin(k*math.tau/24))*radius
                start=origin-axis*.075+offset;remaining=.12
                for _ in range(16):
                    hit=skin.ray_cast(start,axis,remaining)
                    if hit[0] is None:break
                    if volume is None:
                        intersections+=1;break
                    nearest,handle_normal,_,depth=volume.find_nearest(hit[0])
                    if (hit[0]-nearest).dot(handle_normal)*orientation<0 and depth>.0015:
                        intersections+=1;break
                    advance=hit[3]+.00001;start+=axis*advance;remaining-=advance
                    if remaining<=0:break
        # Subdivision retains the original cage vertices. Each subdivided
        # quad contains its source corner, which identifies the actual index
        # and thumb surface independently of the generated target positions.
        source_direction=(wrist-Vector(meta['heads'][prefix+'ForeArm'])).normalized()
        keep={v.index for v in source.data.vertices if (v.co-wrist).dot(source_direction)>-.008 and side*v.co.x>.41}
        indices=sorted({i for f in source.data.polygons if all(k in keep for k in f.vertices) for i in f.vertices})
        points=[glove.matrix_world@v.co for v in glove.data.vertices];contact_gaps={}
        handle_samples,sample_source=handle_surface_samples(prefix,contact)
        upper_samples=[point for point in handle_samples if (point-origin).dot(axis)<=-.045+1e-7]
        for digit in [0,1,2,3,4,5]:
            faces=[]
            for face in glove.data.polygons:
                corners=[k for k in face.vertices if k<len(indices)]
                if digit==0:
                    selected=any(sum(value for key,value in meta['finger_weights'][indices[k]].items() if key.startswith('metacarpal'))>.5 and sum(value for key,value in meta['finger_weights'][indices[k]].items() if key.startswith('finger'))<.25 for k in corners)
                else:
                    selected=bool(corners) and max(sum(value for key,value in meta['finger_weights'][indices[k]].items() if key.startswith('finger'+str(digit)+'-')) for k in corners)>.5
                if selected:faces.append(list(face.vertices))

            region=BVHTree.FromPolygons(points,faces);minimum=float('inf')
            for sample in (upper_samples if digit in [1,2] else handle_samples):
                minimum=min(minimum,region.find_nearest(sample)[3])
            contact_gaps[['palm','thumb','index','middle','ring','little'][digit]]=minimum
        strap_offset=float(glove.get('wrist_strap_centre_offset_m',.012))
        enclosed=0;clearances=[];centre=wrist+longitudinal*strap_offset
        for k in range(32):
            direction=normal*math.cos(k*math.tau/32)+across*math.sin(k*math.tau/32)
            hit=strap.ray_cast(centre,direction,.08);hand=skin.ray_cast(centre,direction,.08)
            if hit[0] is not None and hand[0] is not None:
                clearance=hit[3]-hand[3];clearances.append(clearance)
                if .0003<clearance<.006:enclosed+=1
        rows.append({'hand':prefix,'grip':variant,'handle_surface_source':sample_source,'handle_surface_samples':len(handle_samples),'handle_interior_rays_intersecting_glove':intersections,'handle_probe_rays':73,'wrist_enclosed_directions':enclosed,'wrist_probe_directions':32,'strap_skin_clearance_m':clearances,'thumb_index_handle_clearance_m':[contact_gaps['thumb'],contact_gaps['index']],'palm_and_finger_contact_clearance_m':contact_gaps,'thumb_index_contact_band_from_upper_end_m':[0,.030],'palmar_other_fingers_contact_band_from_upper_end_m':[0,.120],'pass':intersections==0 and enclosed==32 and max(contact_gaps.values())<.004})
report={'schema_version':1,'id':'m167_actual_glove_handle_and_wrist_loop','asset':a.asset,'hands':rows,'pass':all(r['pass'] for r in rows),'art_acceptance':False,'method':'Actual rounded handle surface sampled at <=1.5mm with glove-region BVH proximity; full wrist circumference and handle interior rays. Retained glove-only historical fixtures explicitly use a dense legacy cylinder fallback. Generated fingertip targets are not contact proof.'}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Handle penetrates glove or strap does not enclose the wrist with clearance')
