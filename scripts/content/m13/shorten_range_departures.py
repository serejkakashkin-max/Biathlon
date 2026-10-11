"""Apply the bounded M13 post-shoot departure revision once to the authored venue.
Keep firing anchors, incoming approaches, crossings and the course rejoin fixed.
"""
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
VENUE = ROOT / 'Assets/Resources/Biathlon/Venues/woodland_basin_v1'
REMOVED = 1.5
OLD_TURN = 10.116
OLD_END = 11.452

def read(name):
    return json.loads((VENUE / name).read_text(encoding='utf-8-sig'))

def write(name, data):
    # Preserve the established formatting for each large authored definition.
    (VENUE / name).write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8')

visual = read('visual.json')
private = [v for v in visual['loops'] if v['loop_id'].startswith('mat_')]
assert all(abs(v['knots'][-1]['distance_m'] - OLD_END) < 1e-8 for v in private), 'Revision already applied or unexpected source geometry'
for v in private:
    for k in v['knots']:
        if k['distance_m'] >= OLD_TURN - 1e-8:
            k['distance_m'] = round(k['distance_m'] - REMOVED, 3)
            k['z'] += REMOVED
exit_knots = next(v for v in visual['loops'] if v['loop_id'] == 'exit_aisle')['knots']
for k in exit_knots[:-1]:
    k['z'] += REMOVED
# Move the removed private distance into the shared, player-controlled rejoin.
# The physical map requires each range lane to replace the same main distance.
# A gentle S below the entry road keeps both rejoin endpoints/tangents fixed.
a, b = exit_knots[-2:]
def curve(amplitude):
    return [(a['x'] + (b['x'] - a['x']) * t,
             a['z'] + (b['z'] - a['z']) * t*t*(3-2*t) - amplitude*math.sin(math.pi*t)**2)
            for t in [i/24 for i in range(25)]]
low, high = 0., 20.
for _ in range(60):
    mid = (low + high)/2
    points = curve(mid)
    length = sum(math.dist(p, q) for p, q in zip(points, points[1:]))
    if length < 45 + REMOVED: low = mid
    else: high = mid
points = curve((low+high)/2)
station = a['distance_m']
for i, ((x0,z0),(x,z)) in enumerate(zip(points,points[1:])):
    station += math.hypot(x-x0,z-z0)
    k = dict(distance_m=round(station,3),x=x,y=0.,z=z)
    if i==23:k['distance_m']=143. + REMOVED
    if i==0:exit_knots.pop()
    exit_knots.append(k)
write('visual.json', visual)

track = read('track.json')
for loop in track['loops']:
    if loop['id'].startswith('mat_'):
        long_segment = next(s for s in loop['segments'] if s['length_m'] > 3)
        long_segment['length_m'] = round(long_segment['length_m'] - REMOVED, 3)
    elif loop['id']=='exit_aisle':
        template=loop['segments'].pop()
        for i,(a,b) in enumerate(zip(exit_knots[30:],exit_knots[31:])):
            s=dict(template,id=f'rejoin_s_{i:02}',length_m=round(b['distance_m']-a['distance_m'],3))
            dx,dz=b['x']-a['x'],b['z']-a['z']
            if i+32<len(exit_knots):
                c=exit_knots[i+32]
                turn=math.atan2(dx*(c['z']-b['z'])-dz*(c['x']-b['x']),dx*(c['x']-b['x'])+dz*(c['z']-b['z']))
                s['curvature_per_m']=turn/s['length_m']
            else:s['curvature_per_m']=0.
            loop['segments'].append(s)
write('track.json', track)

paths = read('paths.json')
for lane in paths['lanes']:
    for s in lane['slices']:
        if s['loop_id'].startswith('mat_'):
            s['end_m'] = round(s['end_m'] - REMOVED, 3)
        elif s['loop_id']=='exit_aisle':s['end_m']=143. + REMOVED
write('paths.json', paths)

junctions = read('junctions.json')
for junction in junctions['junctions']:
    for p in junction['ports']:
        if p['anchor']['loop_id']=='exit_aisle' and p['anchor']['distance_m']==143:
            p['anchor']['distance_m']=143. + REMOVED
        if p['anchor']['loop_id'].startswith('mat_'):
            if abs(p['anchor']['distance_m'] - OLD_END) < 1e-8:
                p['anchor']['distance_m'] = round(OLD_END - REMOVED, 3)
            remaining = OLD_END - REMOVED - p['anchor']['distance_m']
            p['guard_after_m'] = min(p['guard_after_m'], max(0, round(remaining - .1, 3)))
write('junctions.json', junctions)

for name in ('shared_apron.json', 'environment.json'):
    data = read(name)
    corridors = data['corridors' if name == 'shared_apron.json' else 'route_corridors']
    for corridor in corridors:
        if corridor['loop_id'] == 'exit_aisle':
            corridor['knots'] = exit_knots
        elif corridor['loop_id'].startswith('mat_'):
            corridor['knots'] = next(v['knots'] for v in private if v['loop_id'] == corridor['loop_id'])
        if name=='environment.json' and corridor['loop_id']=='exit_aisle':
            corridor['widths_m']=[8.]*len(exit_knots)
    write(name, data)
print('30 private departures shortened by 1.5 m; original rejoin and firing points preserved.')
