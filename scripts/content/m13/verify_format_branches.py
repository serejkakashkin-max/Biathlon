"""Independent serialized checks for exact shared-identity Venue01 format routes."""
from pathlib import Path
import argparse, hashlib, json, math

SCRIPT=Path(__file__).resolve().parent
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--project-root',type=Path,default=SCRIPT.parents[2])
parser.add_argument('--base-root',type=Path,default=None)
parser.add_argument('--output-root',type=Path,default=None)
parser.add_argument('--venue-id',default='woodland_basin_v1')
args=parser.parse_args();PROJECT=args.project_root.resolve()
BASE=(args.base_root or PROJECT/'artifacts/wp169i/draft').resolve()
OUT=(args.output_root or PROJECT/'artifacts/wp169j/formats-draft').resolve()
RES=OUT/'resources';VENUE=args.venue_id;PREFIX='Biathlon/Venues/'+VENUE
def read(name):return json.loads((RES/PREFIX/(name+'.json')).read_text(encoding='utf-8-sig'))
def mm(x):
    n=round(x*1000);assert abs(n/1000-x)<1e-9,('non-mm',x);return n
def point(k):return tuple(k[key] for key in ('x','y','z'))
track=read('track');visual=read('visual');paths=read('paths');junctions=read('junctions');connections=read('connections')
loops={l['id']:l for l in track['loops']};maps={l['loop_id']:l for l in visual['loops']}
assert len(loops)==len(track['loops']) and len(maps)==len(visual['loops'])
assert loops.keys()==maps.keys()
maximum_error=maximum_grade_error=0.;edge_count=0
for loop_id,loop in loops.items():
    knots=maps[loop_id]['knots'];assert len(knots)==len(loop['segments'])+1
    total=0
    for segment,a,b in zip(loop['segments'],knots,knots[1:]):
        assert mm(a['distance_m'])==total;total+=mm(segment['length_m']);assert mm(b['distance_m'])==total
        actual=math.dist(point(a),point(b));error=abs(actual-segment['length_m']);assert error<1e-8
        horizontal=math.hypot(b['x']-a['x'],b['z']-a['z']);grade=(b['y']-a['y'])/horizontal
        grade_error=max(abs(grade-segment[key]) for key in ('start_grade','end_grade'));assert grade_error<1e-12
        maximum_error=max(maximum_error,error);maximum_grade_error=max(maximum_grade_error,grade_error);edge_count+=1
    if loop['closed']:assert math.dist(point(knots[0]),point(knots[-1]))<1e-8
for name in ('track','visual'):
    original=json.loads((BASE/'resources'/PREFIX/(name+'.json')).read_text(encoding='utf-8-sig'))
    field='id' if name=='track' else 'loop_id'
    current=track if name=='track' else visual
    original_by_id={l[field]:l for l in original['loops']};current_by_id={l[field]:l for l in current['loops']}
    assert all(current_by_id[key]==value for key,value in original_by_id.items()),'Existing physical loop geometry changed'
original_paths=json.loads((BASE/'resources'/PREFIX/'paths.json').read_text(encoding='utf-8-sig'))
assert paths==original_paths and len(paths['lanes'])==30

def at(anchor):
    knots=maps[anchor['loop_id']]['knots'];d=anchor['distance_m']
    for a,b in zip(knots,knots[1:]):
        if a['distance_m']<=d<=b['distance_m']:
            t=(d-a['distance_m'])/(b['distance_m']-a['distance_m'])
            return tuple(a[key]+(b[key]-a[key])*t for key in ('x','y','z'))
    raise AssertionError(('outside-loop',anchor))
ports={p['id']:p for j in junctions['junctions'] for p in j['ports']}
seams={c['id']:c for c in connections['connections']};world_gaps=[]
for connection in seams.values():
    assert connection['kind']=='coincident_seam'
    world_gaps.append(math.dist(at(ports[connection['from_port_id']]['anchor']),at(ports[connection['to_port_id']]['anchor'])))
assert max(world_gaps)<1e-8
def same(a,b):return a['loop_id']==b['loop_id'] and mm(a['distance_m'])==mm(b['distance_m'])
formats=[];slice_count=0
for key in ('mass_start','sprint','pursuit'):
    profile=read(key+'_profile');route=read(key+'_route');assert route['track_id']==track['id'] and route['competition_profile_id']==profile['id']
    assert len(route['laps'])==profile['laps'];required={b['after_lap'] for b in profile['shooting_bouts']}
    total=0;lap_lengths=[];previous=None
    for lap_index,lap in enumerate(route['laps'],1):
        sections=lap.get('slices') or [{'loop_id':loop,'start_m':0,'end_m':maps[loop]['knots'][-1]['distance_m']} for loop in lap['loop_ids']]
        lap_mm=0;range_occurrences=0
        for section in sections:
            loop=loops[section['loop_id']];start,end=mm(section['start_m']),mm(section['end_m'])
            assert 0<=start<end<=mm(maps[loop['id']]['knots'][-1]['distance_m']);assert loop['kind']=='race'
            here={'loop_id':loop['id'],'distance_m':start/1000}
            if previous is not None:
                if previous['loop_id']==here['loop_id']:
                    assert same(previous,here) or loop['closed'] and mm(previous['distance_m'])==mm(maps[loop['id']]['knots'][-1]['distance_m']) and start==0
                    assert section.get('enter_connection_id') is None
                else:
                    connection=seams[section['enter_connection_id']]
                    assert same(ports[connection['from_port_id']]['anchor'],previous) and same(ports[connection['to_port_id']]['anchor'],here)
                assert math.dist(at(previous),at(here))<1e-8
            else:assert section.get('enter_connection_id') is None
            if loop['id']==paths['main_loop_id'] and start<=mm(paths['entry_distance_m']) and end>=mm(paths['exit_distance_m']):range_occurrences+=1
            previous={'loop_id':loop['id'],'distance_m':end/1000};lap_mm+=end-start;slice_count+=1
        if lap_index in required:assert range_occurrences==1
        lap_lengths.append(lap_mm/1000);total+=lap_mm
    assert total==mm(profile['distance_m'])
    formats.append({'format':key,'distance_m':total/1000,'lap_distances_m':lap_lengths,'finish_anchor':previous})
assert formats[1]['finish_anchor']=={'loop_id':'main','distance_m':.001}
result={'schema_version':1,'id':VENUE[:-3]+'_serialized_format_routes_verification_v1',
    'status':'PASS_EXACT_SERIALIZED_GEOMETRY_TOPOLOGY_ROUTE_ARITHMETIC_RUNTIME_PENDING','venue_id':VENUE,
    'edge_count':edge_count,'slice_count':slice_count,'maximum_world_edge_length_error_m':maximum_error,'maximum_grade_error':maximum_grade_error,
    'directed_connection_count':len(world_gaps),'maximum_directed_seam_world_gap_m':max(world_gaps),'unchanged_base_loop_count':len(loops)-2,
    'unchanged_lane_path_count':len(paths['lanes']),'formats':formats,
    'authority_boundary':'Independent serialized proof; full cold catalog/occupancy/Unity/race/replay acceptance still required',
    'source_hashes':{name:hashlib.sha256((RES/PREFIX/(name+'.json')).read_bytes()).hexdigest() for name in ('track','visual','paths','junctions','connections','sprint_route','pursuit_route')}}
(OUT/'formats-verification.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2),flush=True)
