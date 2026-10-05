"""Generate original exact-distance format branches into a separate draft root.

Shared Main is never copied to another physical loop ID. Each open branch has
actual whole-mm world chords, explicit directed seams and local world guards.
This tool does not import Unity Assets or claim static/runtime acceptance.
"""
from pathlib import Path
import argparse, bisect, hashlib, json, math, shutil
from compact_range_authoring import reflect_range_family

def validated_quarter_segments(value):
    # Even arcs keep the existing fixed-control paired-chord solver applicable.
    if type(value) is not int or not 2<=value<=1024 or value%2:
        raise ValueError('quarter_segments must be an even integer between 2 and 1024')
    return value

def validated_world_chord_slack(value):
    if type(value) not in (int,float) or not math.isfinite(value) or not 0<value<=1:
        raise ValueError('world_chord_real_slack_m must be finite, positive and at most 1m')
    return value

SCRIPT=Path(__file__).resolve().parent
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--project-root',type=Path,default=SCRIPT.parents[2])
parser.add_argument('--base-root',type=Path,default=None)
parser.add_argument('--output-root',type=Path,default=None)
parser.add_argument('--recipe',type=Path,default=SCRIPT/'recipes/woodland_basin_formats_recipe_v1.json')
args=parser.parse_args()
PROJECT=args.project_root.resolve()
BASE=(args.base_root or PROJECT/'artifacts/wp169i/draft').resolve()
OUT=(args.output_root or PROJECT/'artifacts/wp169j/formats-draft').resolve()
assert OUT!=BASE,'Full formats must not overwrite the Mass checkpoint.'
RES=OUT/'resources'
recipe=json.loads(args.recipe.read_text(encoding='utf-8-sig'))
directed_passages=recipe.get('directed_format_passages',False)
if type(directed_passages) is not bool:
    raise ValueError('directed_format_passages must be an explicit boolean when supplied')
assert recipe['schema_version']==1
default_quarter_segments=validated_quarter_segments(recipe.get('quarter_segments',8))
format_quarter_segments={key:validated_quarter_segments(recipe[key].get('quarter_segments',default_quarter_segments))
                         for key in ('pursuit','sprint')}
default_world_chord_slack=validated_world_chord_slack(recipe['world_chord_real_slack_m'])
format_world_chord_slack={key:validated_world_chord_slack(recipe[key].get('world_chord_real_slack_m',default_world_chord_slack))
                          for key in ('pursuit','sprint')}
analytic_paid_arcs=recipe['pursuit'].get('analytic_paid_arcs',False)
if type(analytic_paid_arcs) is not bool:
    raise ValueError('pursuit analytic_paid_arcs must be an explicit boolean when supplied')
VENUE=recipe['venue_id'];assert VENUE.replace('_','').isalnum() and VENUE.endswith('_v1')
LABEL=VENUE[:-3];PREFIX='Biathlon/Venues/'+VENUE
OUT.mkdir(parents=True,exist_ok=True)

raw_manifest=json.loads((BASE/'resources'/PREFIX/'manifest.json').read_text(encoding='utf-8-sig'))
compact_range=raw_manifest['range_layout'].get('mat_definition') is not None
lane_count=json.loads((BASE/'resources'/PREFIX/'paths.json').read_text(encoding='utf-8-sig'))['lanes']
lane_count=len(lane_count)
def read(name):
    data=json.loads((BASE/'resources'/PREFIX/(name+'.json')).read_text(encoding='utf-8-sig'))
    return reflect_range_family(data,lane_count) if compact_range else data
def write(name,data):
    if compact_range:data=reflect_range_family(data,lane_count)
    path=RES/PREFIX/(name+'.json');path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(data,indent=2,allow_nan=False)+'\n',encoding='utf-8')
def mm(x):
    n=round(x*1000);assert abs(n/1000-x)<1e-9,(x,n);return n
def add(a,b):return tuple(x+y for x,y in zip(a,b))
def sub(a,b):return tuple(x-y for x,y in zip(a,b))
def mul(a,s):return tuple(x*s for x in a)
def dot(a,b):return sum(x*y for x,y in zip(a,b))
def norm(a):return math.sqrt(dot(a,a))
def unit(a):return mul(a,1/norm(a))
def coord(k):return tuple(k[x] for x in ('x','y','z'))

track=read('track');visual=read('visual');junctions=read('junctions');connections=read('connections');manifest=read('manifest')
main=next(loop for loop in visual['loops'] if loop['loop_id']==recipe['main_loop_id'])
main_distances=[k['distance_m'] for k in main['knots']]
main_end=main_distances[-1]
# Preserve the historical integer JSON representation of whole-metre endpoints.
if main_end==int(main_end):main_end=int(main_end)
def sample(d):
    i=min(bisect.bisect_right(main_distances,d)-1,len(main_distances)-2)
    a,b=main['knots'][i:i+2];t=(d-a['distance_m'])/(b['distance_m']-a['distance_m'])
    return add(coord(a),mul(sub(coord(b),coord(a)),t))
def tangent(d):
    i=min(bisect.bisect_right(main_distances,d)-1,len(main_distances)-2)
    return unit(sub(coord(main['knots'][i+1]),coord(main['knots'][i])))
def length(points):return sum(math.dist(a,b) for a,b in zip(points,points[1:]))
def append_arc(points,center,radius,start,end,q,height):
    for i in range(1,q+1):
        angle=start+(end-start)*i/q
        x,z=center[0]+radius*math.cos(angle),center[1]+radius*math.sin(angle)
        points.append((x,height(z),z))
def append_straight(points,end):
    # An even pair leaves fixed control points on both ends for chord solving.
    points.append(mul(add(points[-1],end),.5));points.append(end)

terminal=recipe['aligned_terminal_edge_m'];q=default_quarter_segments;slack=default_world_chord_slack

def solve_parameter(builder,target,lo,hi):
    increasing=length(builder(hi))>length(builder(lo))
    assert min(length(builder(lo)),length(builder(hi)))<=target<=max(length(builder(lo)),length(builder(hi)))
    for _ in range(80):
        mid=(lo+hi)/2
        if (length(builder(mid))<target)==increasing:lo=mid
        else:hi=mid
    return (lo+hi)/2

def exact_chords(points,target_mm):
    """Keep even controls and move odd controls onto exact sphere intersections."""
    assert len(points)%2==1
    pairs=[]
    for i in range(0,len(points)-2,2):
        a,p,b=points[i:i+3];l1,l2=math.dist(a,p),math.dist(p,b);d=math.dist(a,b)
        budget=max(math.floor(l1*1000)+math.floor(l2*1000),math.ceil(d*1000-1e-8))
        pairs.append({'i':i,'budget':budget,'raw':(l1+l2)*1000,'l1':l1,'l2':l2})
    remaining=target_mm-sum(p['budget'] for p in pairs)
    assert remaining>=0,('Increase real geometric slack',remaining)
    ordered=sorted(pairs,key=lambda p:-(p['raw']-p['budget']))
    for i in range(remaining):ordered[i%len(ordered)]['budget']+=1
    result=points[:];edge_mm=[]
    for p in pairs:
        i=p['i'];a,old,b=points[i:i+3];d=math.dist(a,b)
        l1_mm=round(p['budget']*p['l1']/(p['l1']+p['l2']))
        l2_mm=p['budget']-l1_mm;l1,l2=l1_mm/1000,l2_mm/1000
        assert abs(l1-l2)<=d+1e-9 and d<=l1+l2+1e-9,(i,d,l1,l2)
        axis=unit(sub(b,a));axial=(l1*l1-l2*l2+d*d)/(2*d)
        projection=add(a,mul(axis,axial));perp=sub(old,add(a,mul(axis,dot(sub(old,a),axis))))
        if norm(perp)<1e-8:
            perp=(-axis[2],0,axis[0]);perp=sub(perp,mul(axis,dot(perp,axis)))
        h=math.sqrt(max(0,l1*l1-axial*axial))
        result[i+1]=add(projection,mul(unit(perp),h));edge_mm.extend((l1_mm,l2_mm))
    assert sum(edge_mm)==target_mm
    assert max(abs(math.dist(a,b)-n/1000) for a,b,n in zip(result,result[1:],edge_mm))<1e-8
    return result,edge_mm

def pursuit_points(parameter):
    spec=recipe['pursuit'];a,b=sample(spec['main_depart_m']),sample(spec['main_return_m'])
    p=add(a,mul(tangent(spec['main_depart_m']),terminal));end=sub(b,mul(tangent(spec['main_return_m']),terminal))
    r1=parameter;r2=r1+end[2]-p[2]
    climb=-tangent(spec['main_depart_m'])[1]/tangent(spec['main_depart_m'])[2]
    descent=-tangent(spec['main_return_m'])[1]/tangent(spec['main_return_m'])[2]
    height1=lambda z:a[1]+climb*(a[2]-z)
    height2=lambda z:b[1]+descent*(b[2]-z)
    points=[p]
    append_arc(points,(p[0]+r1,p[2]),r1,math.pi,1.5*math.pi,q,height1)
    straight_end=(end[0]-r2,height2(end[2]-r2),end[2]-r2)
    append_straight(points,straight_end)
    append_arc(points,(end[0]-r2,end[2]),r2,1.5*math.pi,2*math.pi,q,height2)
    points[-1]=end
    return points

def exact_analytic_arc(point_at,start,end,radius_guess):
    """Pay every chord on the analytic arc; calibrate its radius, not odd controls."""
    nominal=[point_at(radius_guess,start+(end-start)*i/q) for i in range(q+1)]
    lengths=[round(math.dist(a,b)*1000) for a,b in zip(nominal,nominal[1:])]
    assert min(lengths)>0
    def walk(radius):
        angle=start;points=[point_at(radius,angle)]
        for paid_mm in lengths:
            low,high=angle,angle+2*(end-start)/q
            assert math.dist(points[-1],point_at(radius,high))>paid_mm/1000
            for _ in range(42):
                middle=(low+high)/2
                if math.dist(points[-1],point_at(radius,middle))<paid_mm/1000:low=middle
                else:high=middle
            angle=(low+high)/2;points.append(point_at(radius,angle))
        return angle,points
    low,high=radius_guess*.99,radius_guess*1.01
    assert walk(low)[0]>end and walk(high)[0]<end
    for _ in range(44):
        middle=(low+high)/2
        if walk(middle)[0]>end:low=middle
        else:high=middle
    radius=(low+high)/2;angle,points=walk(radius)
    assert abs(angle-end)<1e-10
    points[-1]=point_at(radius,end)
    assert max(abs(math.dist(a,b)-n/1000) for a,b,n in zip(points,points[1:],lengths))<1e-8
    return points,lengths

def exact_analytic_pursuit(parameter,target_mm):
    spec=recipe['pursuit'];a,b=sample(spec['main_depart_m']),sample(spec['main_return_m'])
    p=add(a,mul(tangent(spec['main_depart_m']),terminal));end=sub(b,mul(tangent(spec['main_return_m']),terminal))
    climb=-tangent(spec['main_depart_m'])[1]/tangent(spec['main_depart_m'])[2]
    descent=-tangent(spec['main_return_m'])[1]/tangent(spec['main_return_m'])[2]
    def first(radius,angle):
        x=p[0]+radius*(1+math.cos(angle));z=p[2]+radius*math.sin(angle)
        return (x,a[1]+climb*(a[2]-z),z)
    def second(radius,angle):
        x=end[0]+radius*(math.cos(angle)-1);z=end[2]+radius*math.sin(angle)
        return (x,b[1]+descent*(b[2]-z),z)
    left,left_mm=exact_analytic_arc(first,math.pi,1.5*math.pi,parameter)
    right,right_mm=exact_analytic_arc(second,1.5*math.pi,2*math.pi,parameter+end[2]-p[2])
    left[0]=p;right[-1]=end
    middle=mul(add(left[-1],right[0]),.5)
    straight,straight_mm=exact_chords([left[-1],middle,right[0]],target_mm-sum(left_mm)-sum(right_mm))
    points=left+straight[1:]+right[1:];lengths=left_mm+straight_mm+right_mm
    assert sum(lengths)==target_mm
    assert max(abs(math.dist(a,b)-n/1000) for a,b,n in zip(points,points[1:],lengths))<1e-8
    return points,lengths

def sprint_points(straight):
    spec=recipe['sprint'];a,b=sample(spec['main_depart_m']),sample(spec['main_return_m'])
    p=add(a,mul(tangent(spec['main_depart_m']),terminal));end=sub(b,mul(tangent(spec['main_return_m']),terminal))
    r=spec['turn_radius_m'];vertical=p[2]-end[2]-4*r;assert vertical>0
    climb=-tangent(spec['main_depart_m'])[1]/tangent(spec['main_depart_m'])[2]
    height=lambda z:a[1]+climb*(a[2]-z)
    points=[p]
    append_arc(points,(p[0]-r,p[2]),r,0,-math.pi/2,q,height)
    left=p[0]-r-straight
    append_straight(points,(left,height(p[2]-r),p[2]-r))
    append_arc(points,(left,p[2]-2*r),r,math.pi/2,math.pi,q,height)
    append_straight(points,(left-r,height(p[2]-2*r-vertical),p[2]-2*r-vertical))
    append_arc(points,(left,p[2]-2*r-vertical),r,math.pi,1.5*math.pi,q,height)
    append_straight(points,(p[0]-r,height(p[2]-3*r-vertical),p[2]-3*r-vertical))
    append_arc(points,(p[0]-r,end[2]),r,math.pi/2,0,q,height)
    points[-1]=end
    return points

def branch(spec,builder,lo,hi):
    target=spec['length_m']-2*terminal
    parameter=solve_parameter(builder,target-slack,lo,hi)
    if spec is recipe['pursuit'] and analytic_paid_arcs:
        points,chords=exact_analytic_pursuit(parameter,mm(target))
    else:
        raw=builder(parameter);points,chords=exact_chords(raw,mm(target))
    points=[sample(spec['main_depart_m'])]+points+[sample(spec['main_return_m'])]
    chords=[mm(terminal)]+chords+[mm(terminal)]
    knots=[];segments=[];travel=0
    for i,p in enumerate(points):
        knots.append({'distance_m':travel/1000,'x':p[0],'y':p[1],'z':p[2]})
        if i==len(points)-1:break
        delta=sub(points[i+1],p);horizontal=math.hypot(delta[0],delta[2]);grade=delta[1]/horizontal
        curvature=0
        if i>0:
            prior=sub(p,points[i-1]);angle=math.atan2(prior[0]*delta[2]-prior[2]*delta[0],prior[0]*delta[0]+prior[2]*delta[2])
            curvature=angle/((math.hypot(prior[0],prior[2])+horizontal)/2)
        segments.append({'id':f'edge_{i:03d}','length_m':chords[i]/1000,'start_grade':grade,'end_grade':grade,
            'curvature_per_m':curvature,'width_m':recipe['width_m'],'snow_profile_id':'snow_test_packed','wind_exposure':.45 if spec is recipe['sprint'] else .25,
            'terrain_muscle_factor':1.18 if grade>.02 else 1,'draft_allowed':True,'overtake_allowed':True,'risk_level':.25 if abs(curvature)>.01 else .05,'semantic_zone':'climb_long' if grade>.02 else 'descent_technical' if grade<-.02 else 'rolling' if abs(curvature)>.01 else 'flat'})
        overrides=spec.get('tactical_profile',{})
        assert set(overrides)<= {'wind_exposure','draft_allowed','overtake_allowed','risk_level'}
        segments[-1].update(overrides)
        travel+=chords[i]
    assert travel==mm(spec['length_m'])
    assert math.dist(unit(sub(points[1],points[0])),tangent(spec['main_depart_m']))<1e-10
    assert math.dist(unit(sub(points[-1],points[-2])),tangent(spec['main_return_m']))<1e-10
    return {'id':spec['loop_id'],'kind':'race','closed':False,'segments':segments},{'loop_id':spec['loop_id'],'knots':knots},parameter

proofs=[]
for key,builder,lo,hi in [('pursuit',pursuit_points,20,250),('sprint',sprint_points,20,250)]:
    spec=recipe[key];q=format_quarter_segments[key];slack=format_world_chord_slack[key]
    definition,mapping,parameter=branch(spec,builder,lo,hi)
    track['loops'].append(definition);visual['loops'].append(mapping)
    guard=spec.get('guard_distance_m',recipe['guard_distance_m'])
    for side,main_d,branch_d in [('depart',spec['main_depart_m'],0),('return',spec['main_return_m'],spec['length_m'])]:
        rid=f'{key}_{side}';main_port=rid+'_main';branch_port=rid+'_branch'
        junctions['junctions'].append({'id':rid,'ports':[
            {'id':main_port,'anchor':{'loop_id':'main','distance_m':main_d},'guard_before_m':guard,'guard_after_m':guard},
            {'id':branch_port,'anchor':{'loop_id':spec['loop_id'],'distance_m':branch_d},'guard_before_m':guard if side=='return' else 0,'guard_after_m':guard if side=='depart' else 0}]})
        connections['connections'].append({'id':rid+'_connection','junction_id':rid,
            'from_port_id':main_port if side=='depart' else branch_port,'to_port_id':branch_port if side=='depart' else main_port,'kind':'coincident_seam'})
    profile={'schema_version':1,'id':f'{LABEL}_{key}_men_v1','rules_revision':recipe.get('rules_revision','production_distance_structure_v1'),
        'format':0 if key=='sprint' else 1,'category':0,'distance_m':spec['race_distance_m'],'laps':spec['laps'],
        'start_type':0 if key=='sprint' else 1,'start_interval_s':30 if key=='sprint' else 0,'field_capacity':spec['field_capacity'],
        'shots_per_bout':5,'penalty_loop_m':150,'shooting_bouts':[{'id':f'bout_{i+1}','after_lap':i+1,'stance':0 if i<(1 if key=='sprint' else 2) else 1} for i in range(2 if key=='sprint' else 4)]}
    laps=[]
    for i in range(spec['laps']):
        slices=[{'loop_id':'main','start_m':0,'end_m':spec['main_depart_m']},
            {'loop_id':spec['loop_id'],'start_m':0,'end_m':spec['length_m'],'enter_connection_id':key+'_depart_connection'},
            {'loop_id':'main','start_m':spec['main_return_m'],'end_m':main_end,'enter_connection_id':key+'_return_connection'}]
        if key=='sprint' and i==spec['laps']-1:slices.append({'loop_id':'main','start_m':0,'end_m':spec['paid_final_main_continuation_m']})
        laps.append({'id':f'lap_{i+1}','slices':slices})
    route={'schema_version':2,'id':f'{LABEL}_{key}_route_v2','track_id':track['id'],'competition_profile_id':profile['id'],'laps':laps}
    race_mm=sum(mm(s['end_m'])-mm(s['start_m']) for lap in laps for s in lap['slices'])
    assert race_mm==mm(profile['distance_m'])
    write(key+'_profile',profile);write(key+'_route',route)
    errors=[abs(math.dist(coord(a),coord(b))-(b['distance_m']-a['distance_m'])) for a,b in zip(mapping['knots'],mapping['knots'][1:])]
    proofs.append({'format':key,'loop_id':spec['loop_id'],'length_m':spec['length_m'],'main_depart_m':spec['main_depart_m'],'main_return_m':spec['main_return_m'],
        'world_parameter_m':parameter,'edge_count':len(definition['segments']),'maximum_world_edge_length_error_m':max(errors),
        'maximum_grade':max(abs(s['start_grade']) for s in definition['segments']),'maximum_curvature_per_m':max(abs(s['curvature_per_m']) for s in definition['segments']),
        'actual_race_distance_m':race_mm/1000,'lap_distances_m':[sum(mm(s['end_m'])-mm(s['start_m']) for s in lap['slices'])/1000 for lap in laps],
        'seam_world_gap_m':0,'endpoint_tangents':'EXACT_MAIN_DIRECTION','guard_before_after_m':guard})

# Only small definitions and actual native dependencies are copied. Environment
# is regenerated from all resulting corridors, never carried over as stale data.
for path in (BASE/'resources').rglob('*'):
    relative=path.relative_to(BASE/'resources');unix=str(relative).replace('\\','/')
    if not path.is_file() or '/environment/' in unix or path.name=='environment.json':continue
    if relative.parts[-1] in ('track.json','visual.json','junctions.json','connections.json'):continue
    destination=RES/relative;destination.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(path,destination)
manifest['format_bindings_resource']=PREFIX+'/format_bindings'
for name,data in [('track',track),('visual',visual),('junctions',junctions),('connections',connections),('manifest',manifest)]:write(name,data)
catalog={'schema_version':1,'id':LABEL+'_format_bindings_v1','venue_id':recipe['venue_id'],'track_id':track['id'],
    'formats':[{'id':key,'profile_resource':PREFIX+'/'+key+'_profile','route_resource':PREFIX+'/'+key+'_route'} for key in ('mass_start','sprint','pursuit')]}
if directed_passages:
    # This optional authority binds only the two explicit Pursuit seams. The
    # additional window extent reuses the authored guard distance; it neither
    # shortens guards nor changes the source centreline. Complete paid receipts
    # can begin/end outside a guarded region without losing their world proof.
    body_relative=Path(manifest['occupancy_resource']+'.json')
    body_path=BASE/'resources'/body_relative
    if not body_path.is_file():body_path=PROJECT/'Assets/Resources'/body_relative
    body=json.loads(body_path.read_text(encoding='utf-8-sig'))
    half_body=(body['body_length_m']+body['personal_clearance_m'])/2
    visual_loops={loop['loop_id']:loop for loop in visual['loops']}
    def corridor(loop_id,start,end):
        original=visual_loops[loop_id]['knots'];distances=[p['distance_m'] for p in original]
        def endpoint(distance):
            i=min(bisect.bisect_right(distances,distance)-1,len(original)-2)
            a,b=original[i:i+2];fraction=(distance-a['distance_m'])/(b['distance_m']-a['distance_m'])
            return {'distance_m':distance,**{axis:a[axis]+(b[axis]-a[axis])*fraction for axis in ('x','y','z')}}
        knots=[endpoint(start)]+[dict(p) for p in original if start<p['distance_m']<end]+[endpoint(end)]
        return {'loop_id':loop_id,'knots':knots}
    spec=recipe['pursuit'];guard=spec.get('guard_distance_m',recipe['guard_distance_m'])
    extent=2*guard+half_body
    before=lambda distance:max(0,math.floor((distance-extent)*1000)/1000)
    after=lambda distance,length:min(length,math.ceil((distance+extent)*1000)/1000)
    passage_definition={'schema_version':1,'id':LABEL+'_pursuit_directed_passages_v1','track_id':track['id'],
        'route_id':LABEL+'_pursuit_route_v2','occupancy_definition_id':body['id'],
        'validation_policy_resource':read('start_continuation')['validation_policy_resource'],
        'passages':[
            {'id':'pursuit_depart_forward','connection_id':'pursuit_depart_connection',
             'source':corridor(recipe['main_loop_id'],before(spec['main_depart_m']),spec['main_depart_m']),
             'target':corridor(spec['loop_id'],0,after(0,spec['length_m']))},
            {'id':'pursuit_return_forward','connection_id':'pursuit_return_connection',
             'source':corridor(spec['loop_id'],before(spec['length_m']),spec['length_m']),
             'target':corridor(recipe['main_loop_id'],spec['main_return_m'],after(spec['main_return_m'],main['knots'][-1]['distance_m']))}]}
    write('pursuit_directed_passages',passage_definition)
    next(binding for binding in catalog['formats'] if binding['id']=='pursuit')['directed_passages_resource']=PREFIX+'/pursuit_directed_passages'
write('format_bindings',catalog)
shutil.copyfile(SCRIPT/'presentation-resource-recipe.json' if VENUE=='woodland_basin_v1' else BASE/'presentation-resource-recipe.json',OUT/'presentation-resource-recipe.json')
(OUT/'formats-recipe.json').write_text(json.dumps(recipe,indent=2)+'\n',encoding='utf-8')
audit={'schema_version':1,'id':LABEL+'_format_branch_geometry_audit_v1','status':'DRAFT_WORLD_GUARD_AND_FORMAT_BINDING_VALIDATION_PENDING',
    'venue_id':recipe['venue_id'],'track_id':track['id'],'base_main_identity_unchanged':main==next(l for l in visual['loops'] if l['loop_id']=='main'),
    'branches':proofs,'catalog_binding':'EXPLICIT_MANIFEST_CONTRACT_COLD_VALIDATION_PENDING','runtime_and_visual_acceptance':'PENDING',
    'generator_sha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'recipe_sha256':hashlib.sha256(args.recipe.read_bytes()).hexdigest(),
    'generated_definition_sha256':{name:hashlib.sha256((RES/PREFIX/(name+'.json')).read_bytes()).hexdigest() for name in ('track','visual','junctions','connections','sprint_route','pursuit_route','format_bindings')}}
(OUT/'formats-audit.json').write_text(json.dumps(audit,indent=2)+'\n',encoding='utf-8')
print(json.dumps(proofs,indent=2),flush=True)
