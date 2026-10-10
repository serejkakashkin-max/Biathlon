"""Owner 2026-10-10: a straight ski start and a distinct original start arch.

Retains the main route, finish, timing, 30-athlete cap and licensed art.
Run refresh_woodland_ground.py after this recipe to cap the authored snow.
"""
import copy, json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
VENUE=ROOT/'Assets/Resources/Biathlon/Venues/woodland_basin_v1'
read=lambda name:json.loads((VENUE/name).read_text(encoding='utf-8-sig'))
def write(name,data):
    (VENUE/name).write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
track=read('track.json');visual=read('visual.json');env=read('environment.json');continuation=read('start_continuation.json')
start=next(v for v in visual['loops'] if v['loop_id']=='start_approach')
end=copy.deepcopy(start['knots'][-1]);first=copy.deepcopy(end)
first.update(distance_m=0.0,x=end['x']-40)
start['knots']=[first,end]
loop=next(v for v in track['loops'] if v['id']=='start_approach')
segment=copy.deepcopy(loop['segments'][0]);segment.update(id='straight_start',length_m=40.0,curvature_per_m=0.0,draft_allowed=True,overtake_allowed=True)
loop['segments']=[segment]
next(v for v in continuation['corridors'] if v['loop_id']=='start_approach')['knots']=copy.deepcopy(start['knots'])
for c in env['route_corridors']:
    if c['loop_id']=='start_approach':
        c['knots']=copy.deepcopy(start['knots']);c['widths_m']=[segment['width_m']]*2
env['mesh_assets']=[a for a in env['mesh_assets'] if not a['id'].startswith('start_gantry_')]
for a in list(env['mesh_assets']):
    if a['id'].startswith('finish_gantry_'):
        b=copy.deepcopy(a);b['id']=a['id'].replace('finish_gantry_','start_gantry_')
        b['position']={k:end[k] for k in ('x','y','z')};b['yaw_deg']=90
        env['mesh_assets'].append(b)
for name,data in [('track.json',track),('visual.json',visual),('environment.json',env),('start_continuation.json',continuation)]:write(name,data)
print(json.dumps({'straight_start_m':40,'start_arch_parts':6,'start':first,'join':end}))
