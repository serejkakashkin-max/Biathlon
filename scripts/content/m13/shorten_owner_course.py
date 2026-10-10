"""Owner revision: shorten the forest while preserving the complete stadium.

Paid millimetre lengths with sub-millimetre world rounding, route anchors and format branches move together. This
applies once to the previous 1200m venue; it never rescales race speed or targets.
"""
import bisect, copy, json, math, os
from pathlib import Path
from corridor_sampling import authored_corridor_widths
from snap_owner_course_seams import snap
ROOT=Path(__file__).resolve().parents[3]
V=ROOT/'Assets/Resources/Biathlon/Venues/woodland_basin_v1'
read=lambda n:json.loads((V/(n+'.json')).read_text(encoding='utf-8-sig'))
def save(path,d,compact=False):
 temp=path.with_suffix('.authoring.tmp')
 temp.write_text(json.dumps(d,ensure_ascii=False,**({'separators':(',',':')} if compact else {'indent':2}))+'\n',encoding='utf-8')
 os.replace(temp,path)
def write(n,d): save(V/(n+'.json'),d)
track,visual=read('track'),read('visual')
old={l['loop_id']:copy.deepcopy(l['knots']) for l in visual['loops']}
assert old['main'][-1]['distance_m']==1200, 'Apply once to the 1200m course'
def point(k,scale): return (k['x'],k['y']*scale,k['z'] if k['z']>=-30 else -30+(k['z']+30)*scale)
def length(scale): return sum(math.dist(point(a,scale),point(b,scale)) for a,b in zip(old['main'],old['main'][1:]))
lo,hi=0.,1.
for _ in range(60):
 mid=(lo+hi)/2
 if length(mid)<960:lo=mid
 else:hi=mid
scale=(lo+hi)/2
maps={};newloops={}
for l in visual['loops']:
 name=l['loop_id'];knots=old[name]
 if name=='penalty_150':maps[name]=[k['distance_m'] for k in knots];newloops[name]=l;continue
 segments=next(x for x in track['loops'] if x['id']==name)['segments']
 assert len(segments)==len(knots)-1
 output=[dict(distance_m=0,x=knots[0]['x'],y=point(knots[0],scale)[1],z=point(knots[0],scale)[2])];newsegments=[];stations=[0.];paid=0
 for i,(a,b,segment) in enumerate(zip(knots,knots[1:],segments)):
  pa,pb=point(a,scale),point(b,scale)
  if pa==(a['x'],a['y'],a['z']) and pb==(b['x'],b['y'],b['z']):
   points=[pa,pb];lengths=[round(segment['length_m']*1000)]
  else:
   points=[pa,pb];lengths=[round(math.dist(pa,pb)*1000)]
  for j,(p,q,mm) in enumerate(zip(points,points[1:],lengths)):
   data=copy.deepcopy(segment);data['id']=f'owner_edge_{i:03d}_{j}';data['length_m']=mm/1000
   dx,dz=q[0]-p[0],q[2]-p[2];grade=(q[1]-p[1])/math.hypot(dx,dz)
   data['start_grade']=data['end_grade']=grade
   data['curvature_per_m']=segment['curvature_per_m']*segment['length_m']/max(math.dist(pa,pb),.001)
   newsegments.append(data);paid+=mm;output.append(dict(distance_m=paid/1000,x=q[0],y=q[1],z=q[2]))
  stations.append(paid/1000)
 l['knots']=output;newloops[name]=l
 next(x for x in track['loops'] if x['id']==name)['segments']=newsegments
 maps[name]=stations
def remap(name,d):
 ks=old[name];i=min(bisect.bisect_right([k['distance_m'] for k in ks],d)-1,len(ks)-2)
 t=(d-ks[i]['distance_m'])/(ks[i+1]['distance_m']-ks[i]['distance_m'])
 return round(maps[name][i]+t*(maps[name][i+1]-maps[name][i]),3)
def walk(d):
 if isinstance(d,list):
  for item in d:walk(item)
 elif isinstance(d,dict):
  name=d.get('loop_id')
  if name in maps:
   for key in ('distance_m','start_m','end_m','start_distance_m','end_distance_m'):
    if key in d:d[key]=remap(name,d[key])
   if 'knots' in d:
    for knot in d['knots']:
     knot['distance_m']=remap(name,knot['distance_m']);knot['y']=point(knot,scale)[1];knot['z']=point(knot,scale)[2]
   if 'windows' in d:
    for window in d['windows']:
     for key in ('start_m','end_m'):window[key]=remap(name,window[key])
  for value in d.values():walk(value)
names=['manifest','paths','junctions','connections','mass_start_route','sprint_route','pursuit_route','start_approach','start_continuation','shared_apron','pursuit_directed_passages','sprint_fork_snow_shoulders','timing']
for name in names:
 if not (V/(name+'.json')).exists():continue
 d=read(name)
 if name=='paths':
  for key in ('entry_distance_m','exit_distance_m'):d[key]=remap('main',d[key])
 walk(d)
 if name=='junctions':
  for junction in d['junctions']:
   for port in junction['ports']:
    loop=port['anchor']['loop_id'];ratio=maps[loop][-1]/old[loop][-1]['distance_m']
    for key in ('guard_before_m','guard_after_m'):port[key]=round(port[key]*ratio,3)
 write(name,d)
for name in ('mass_start','sprint','pursuit'):
 route=read(name+'_route');profile=read(name+'_profile')
 profile['distance_m']=round(sum(s['end_m']-s['start_m'] for lap in route['laps'] for s in lap['slices']),3)
 write(name+'_profile',profile)
snap(track,visual,read('junctions'))
env=read('environment')
env['route_corridors']=[dict(loop_id=l['loop_id'],source_visual_resource='Biathlon/Venues/woodland_basin_v1/visual',knots=l['knots'],
 widths_m=authored_corridor_widths(next(t for t in track['loops'] if t['id']==l['loop_id']),l)) for l in visual['loops']]
def move(pos):
 pos['y']*=scale
 if pos['z']<-30:pos['z']=-30+(pos['z']+30)*scale
for collection in ('assets','mesh_assets'):
 for item in env[collection]:
  if not item['id'].startswith(('grandstand','finish_gantry','start_gantry','event_screen','range_')):move(item['position'])
# Forest placement is compressed with its roadside landmarks. Remove canopies
# now crossing any ribbon and retain only the already authored visible band.
edges=[]
for road in env['route_corridors']:
 for a,b,wa,wb in zip(road['knots'],road['knots'][1:],road['widths_m'],road['widths_m'][1:]):edges.append((a,b,max(wa,wb)/2))
guard=env['forest']['additional_canopy_corridor_clearance_m']+env['forest']['conservative_all_species_canopy_radius_m'];removed=0
for group in env['forest']['groups']:
 path=ROOT/'Assets/Resources'/(group['instances_resource']+'.json');d=json.loads(path.read_text(encoding='utf-8-sig'));kept=[]
 for tree in d['instances']:
  p=tree['position'];move(p);nearest=math.inf;clear=True
  for a,b,w in edges:
   dx,dz=b['x']-a['x'],b['z']-a['z'];t=max(0,min(1,((p['x']-a['x'])*dx+(p['z']-a['z'])*dz)/(dx*dx+dz*dz)))
   distance=math.hypot(p['x']-a['x']-t*dx,p['z']-a['z']-t*dz);nearest=min(nearest,distance)
   if distance<w+guard:clear=False;break
  if clear and nearest<=75:kept.append(tree)
 removed+=len(d['instances'])-len(kept);d['instances']=kept;group['count']=len(kept)
 save(path,d,True)
env['forest']['groups']=[g for g in env['forest']['groups'] if g['count']>0]
walk(track['range']);walk(track['penalty'])
write('track',track);write('visual',visual);write('environment',env)
receipt=dict(schema_version=1,id='woodland_owner_shortening_v3',forest_depth_and_height_scale=scale,old_main_m=1200,new_main_m=maps['main'][-1],removed_trees=removed,
 format_distances_m={n:read(n+'_profile')['distance_m'] for n in ('mass_start','sprint','pursuit')},stadium_unchanged=True)
(ROOT/'docs/quality/m13/owner-followup-20261010/course-shortening.json').write_text(json.dumps(receipt,indent=2)+'\n')
print(json.dumps(receipt))
