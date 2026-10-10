"""Owner range-flow dressing: wider penalty road, clear stand sightlines, snow mounds.
All changes are cold authored data, never per-frame work. Run once per input revision.
"""
import json,math,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];RES=ROOT/'Assets/Resources';VENUE=RES/'Biathlon/Venues/woodland_basin_v1'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
write=lambda p,d:p.write_text(json.dumps(d,indent=2) +'\n' if p.name in ('track.json','environment.json','sprint_fork_snow_shoulders.json') else json.dumps(d,separators=(',',':'))+'\n',encoding='utf-8')
d=read(Path(__file__).with_name('woodland_range_flow_dressing_v1.json'))
track=read(VENUE/'track.json');visual=read(VENUE/'visual.json');env=read(VENUE/'environment.json')
for loop in track['loops']:
    if loop['id']=='penalty_150':
        for segment in loop['segments']:segment['width_m']=d['penalty_width_m']
write(VENUE/'track.json',track)
for c in env['route_corridors']:
    if c['loop_id']=='penalty_150':c['widths_m']=[d['penalty_width_m']]*len(c['knots'])
write(VENUE/'environment.json',env)
shoulders=read(VENUE/'sprint_fork_snow_shoulders.json')
shoulders['additional_windows']=[{'loop_id':'main','start_m':0,'end_m':d['main_snow_bank_end_m']},
    {'loop_id':'penalty_150','start_m':0,'end_m':150}]
write(VENUE/'sprint_fork_snow_shoulders.json',shoulders)
main=next(c for c in env['route_corridors'] if c['loop_id']=='main');mounds=[]
for station in d['mound_main_stations_m']:
    for i,(a,b) in enumerate(zip(main['knots'],main['knots'][1:])):
        if a['distance_m']<=station<b['distance_m']:
            t=(station-a['distance_m'])/(b['distance_m']-a['distance_m']);dx=b['x']-a['x'];dz=b['z']-a['z'];length=math.hypot(dx,dz)
            for side in (-1,1):
                offset=side*(main['widths_m'][i]/2+d['mound_outside_edge_m'])
                mounds.append((a['x']+t*dx+dz/length*offset,a['z']+t*dz-dx/length*offset,dx/length,dz/length))
            break
grid={};changed=0
for tile in env['terrain']['tiles']:
    path=RES/(tile['height_resource']+'.json');data=read(path);spacing=env['terrain']['grid_spacing_m']
    x0=tile['center']['x']-tile['size']['x']/2;z0=tile['center']['z']-tile['size']['z']/2
    for row in range(data['rows']):
        for col in range(data['columns']):
            x=x0+col*spacing;z=z0+row*spacing;key=(x,z);idx=row*data['columns']+col
            if key not in grid:
                lift=0
                for mx,mz,fx,fz in mounds:
                    along=((x-mx)*fx+(z-mz)*fz)/d['mound_length_m'];side=((x-mx)*fz-(z-mz)*fx)/d['mound_width_m']
                    r=along*along+side*side
                    if r<1:lift=max(lift,d['mound_height_m']*(1-r)**2)
                grid[key]=round(data['heights_m'][idx]+lift,6)
            changed+=data['heights_m'][idx]!=grid[key];data['heights_m'][idx]=grid[key]
    write(path,data)
cleared=thinned=0;r=d['grandstand_clearance']
segments=[(a,b,max(c['widths_m'])/2) for c in env['route_corridors'] for a,b in zip(c['knots'],c['knots'][1:])]
def distance(x,z):
    result=float('inf')
    for a,b,w in segments:
        dx=b['x']-a['x'];dz=b['z']-a['z'];l2=dx*dx+dz*dz
        if not l2:continue
        t=max(0,min(1,((x-a['x'])*dx+(z-a['z'])*dz)/l2))
        result=min(result,math.hypot(x-a['x']-t*dx,z-a['z']-t*dz)-w)
    return result
for group in env['forest']['groups']:
    path=RES/(group['instances_resource']+'.json');data=read(path);kept=[]
    for tree in data['instances']:
        x=tree['position']['x'];z=tree['position']['z']
        if r['min_x']<=x<=r['max_x'] and r['min_z']<=z<=r['max_z']:cleared+=1;continue
        if distance(x,z)>d['distant_tree_distance_m'] and tree['density_rank']>d['distant_tree_keep_fraction']:thinned+=1;continue
        kept.append(tree)
    if not kept:raise ValueError('Empty grove needs explicit recipe removal')
    data['instances']=kept;group['count']=len(kept);write(path,data)
write(VENUE/'environment.json',env)
subprocess.run([sys.executable,str(Path(__file__).with_name('refresh_woodland_ground.py'))],check=True)
print(json.dumps({'changed_snow_samples':changed,'mounds':len(mounds),'trees_cleared_for_stands':cleared,'distant_trees_thinned':thinned,'penalty_width_m':d['penalty_width_m']}))
