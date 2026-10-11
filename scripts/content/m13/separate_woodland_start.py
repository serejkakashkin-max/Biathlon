"""Cold owner-directed start and fence correction. Main/finish race geometry is retained.
Run against the input revision once; the generated source of truth lives in Resources.
"""
import copy,json,math,subprocess,sys
from pathlib import Path
from dress_woodland_course import terrain_grid,ground

ROOT=Path(__file__).resolve().parents[3]; RES=ROOT/'Assets/Resources'; V=RES/'Biathlon/Venues/woodland_basin_v1'
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8')
d=read(Path(__file__).with_name('woodland_start_corner_dressing_v1.json'))
t=read(V/'track.json');v=read(V/'visual.json');e=read(V/'environment.json');j=read(V/'junctions.json');sc=read(V/'start_continuation.json')
loops={l['loop_id']:l['knots'] for l in v['loops']}; main=loops['main']; end=main[0]
x=end['x']; z=end['z']; radius=d['merge_radius_m']; merge=x-d['merge_straight_m']; start=merge-radius; top=z+radius+d['straight_m']
points=[(start,0,top),(start,0,z+radius)]
for i in range(1,d['merge_samples']+1):
 angle=math.pi+i/d['merge_samples']*math.pi/2
 points.append((merge+radius*math.cos(angle),0,z+radius+radius*math.sin(angle)))
points[-1]=(merge,0,z);points.append((x,0,z));knots=[dict(distance_m=0,x=points[0][0],y=0,z=points[0][2])]; segments=[]
template=copy.deepcopy(next(l for l in t['loops'] if l['id']=='start_approach')['segments'][0]); paid=0
directions=[math.atan2(b[2]-a[2],b[0]-a[0]) for a,b in zip(points,points[1:])]
for i,(a,b) in enumerate(zip(points,points[1:])):
 length=round(math.dist(a,b)*1000)/1000; paid=round(paid+length,3)
 knots.append(dict(distance_m=paid,x=b[0],y=0,z=b[2])); s=copy.deepcopy(template)
 turn=math.atan2(math.sin(directions[min(i+1,len(directions)-1)]-directions[i]),math.cos(directions[min(i+1,len(directions)-1)]-directions[i]))
 s.update(id=f'separate_start_{i:03d}',length_m=length,width_m=d['start_width_m'],curvature_per_m=0 if i==0 or i==len(directions)-1 else turn/length)
 segments.append(s)
next(l for l in t['loops'] if l['id']=='start_approach')['segments']=segments
next(l for l in v['loops'] if l['loop_id']=='start_approach')['knots']=knots
next(c for c in e['route_corridors'] if c['loop_id']=='start_approach').update(knots=knots,widths_m=[d['start_width_m']]*len(knots))
sc['corridors'][0]['knots']=knots
for region in j['junctions']:
 for p in region['ports']:
  if p['anchor']['loop_id']=='start_approach':
   if p['anchor']['distance_m'] in (40,loops['start_approach'][-1]['distance_m']):p['anchor']['distance_m']=paid
   elif p['anchor']['distance_m']>paid:p['anchor']['distance_m']=paid
   p['guard_before_m']=min(p['guard_before_m'],p['anchor']['distance_m'])
   p['guard_after_m']=min(p['guard_after_m'],paid-p['anchor']['distance_m'])
approach=read(V/'start_approach.json');approach['launch_distance_m']=d['launch_distance_m']
# Preserve the old portal as the stadium timing split; put the launch portal on the new straight.
if not any(a['id'].startswith('timing_gantry_') for a in e['mesh_assets']):
 for a in list(e['mesh_assets']):
  if a['id'].startswith('start_gantry_'):
   old=copy.deepcopy(a);old['id']=a['id'].replace('start_','timing_',1);e['mesh_assets'].append(old)
for a in e['mesh_assets']:
 if a['id'].startswith('start_gantry_'):a['position'].update(x=start,y=0,z=top-d['launch_distance_m']);a['yaw_deg']=180
write(V/'track.json',t);write(V/'visual.json',v);write(V/'junctions.json',j);write(V/'start_continuation.json',sc);write(V/'start_approach.json',approach);write(V/'environment.json',e)
# Fit only the new corridor's shoulder locally. Preserve prior decorative snow humps elsewhere.
grid,spacing=terrain_grid(ROOT,e); startCorr=next(c for c in e['route_corridors'] if c['loop_id']=='start_approach')
def nearest(c,x,z):
 best=None
 for k,(a,b) in enumerate(zip(c['knots'],c['knots'][1:])):
  dx=b['x']-a['x'];dz=b['z']-a['z'];l2=dx*dx+dz*dz
  if not l2:continue
  u=max(0,min(1,((x-a['x'])*dx+(z-a['z'])*dz)/l2)); px=a['x']+u*dx;pz=a['z']+u*dz
  r=math.hypot(x-px,z-pz); item=(r,px,pz,dx/math.sqrt(l2),dz/math.sqrt(l2),(x-px)*dz-(z-pz)*dx,c['widths_m'][k])
  if best is None or r<best[0]:best=item
 return best
for key,value in list(grid.items()):
 n=nearest(startCorr,*key); edge=max(0,n[0]-n[6]/2)
 if edge<8:grid[key]=round(max(-.03-max(0,edge-2)*.35,min(-.03+max(0,edge-2)*.35,value)),6)
for tile in e['terrain']['tiles']:
 path=RES/(tile['height_resource']+'.json');h=read(path);x0=tile['center']['x']-tile['size']['x']/2;z0=tile['center']['z']-tile['size']['z']/2
 h['heights_m']=[grid[x0+col*spacing,z0+row*spacing] for row in range(h['rows']) for col in range(h['columns'])]
 path.write_text(json.dumps(h,separators=(',',':'))+'\n',encoding='utf-8')
# Rebind each retained tape to its own route edge, away from the broad snow shoulder.
kept=[];removed=0;moved=0
for a in e['assets']:
 if a['id'].startswith('start_tape_'):continue
 if not a['resource'].endswith('/RibbonTape2m'):kept.append(a);continue
 loop=next((name for name in loops if a['id'].startswith('tape_'+name+'_')),None)
 if loop is None:kept.append(a);continue
 c=next(c for c in e['route_corridors'] if c['loop_id']==loop);p=a['position'];n=nearest(c,p['x'],p['z']);side=1 if n[5]>=0 else -1
 off=side*(n[6]/2+d['tape_edge_offset_m']);px=n[1]+n[4]*off;pz=n[2]-n[3]*off
 if any(nearest(other,px,pz)[0]-nearest(other,px,pz)[6]/2<d['other_road_clearance_m'] for other in e['route_corridors'] if other['loop_id']!=loop):removed+=1;continue
 p.update(x=px,y=ground(grid,spacing,px,pz),z=pz);a['yaw_deg']=math.degrees(math.atan2(n[3],n[4]))+90;kept.append(a);moved+=1
e['assets']=kept
for i in range(math.floor(d['straight_m']/d['tape_step_m'])+1):
 for side in (-1,1):
  px=start+side*(d['start_width_m']/2+d['tape_edge_offset_m']);pz=top-i*d['tape_step_m']
  e['assets'].append(dict(id=f'start_tape_{i:03d}_{side}',resource='Biathlon/Roadside/RibbonTape2m',position=dict(x=px,y=ground(grid,spacing,px,pz),z=pz),yaw_deg=270,scale=1))
# Clear the actual new start corridor, never reduce the texture or species detail.
cleared=0
for group in e['forest']['groups']:
 path=RES/(group['instances_resource']+'.json');f=read(path);before=len(f['instances'])
 f['instances']=[tree for tree in f['instances'] if nearest(startCorr,tree['position']['x'],tree['position']['z'])[0]>d['start_width_m']/2+5]
 cleared+=before-len(f['instances']);group['count']=len(f['instances']);path.write_text(json.dumps(f,separators=(',',':'))+'\n',encoding='utf-8')
write(V/'environment.json',e)
subprocess.run([sys.executable,str(Path(__file__).with_name('refresh_woodland_ground.py'))],check=True)
print(json.dumps(dict(start_length_m=paid,launch_distance_m=d['launch_distance_m'],straight_m=d['straight_m'],tapes_moved=moved,tapes_removed_from_other_roads=removed,trees_cleared=cleared)))
