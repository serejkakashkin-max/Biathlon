"""Author a last-lap-only finish spur with a separate braking corridor.

The existing shared laps, range paths and start grid retain their coordinates.
All race formats use the same explicit directed finish fork. Apply after format
and dressing generation; the operation is idempotent on current venue data.
"""
import argparse, bisect, copy, json, math
from pathlib import Path
from corridor_sampling import authored_corridor_widths

ROOT=Path(__file__).resolve().parents[3]
RECIPE=Path(__file__).with_name('recipes')/'woodland_separate_finish_v1.json'

def author(resource_root, recipe_path=RECIPE):
    recipe=json.loads(recipe_path.read_text(encoding='utf-8-sig'))
    assert recipe['schema_version']==1
    venue=resource_root/'Biathlon/Venues/woodland_basin_v1'
    def read(name):return json.loads((venue/(name+'.json')).read_text(encoding='utf-8-sig'))
    def write(name,data):(venue/(name+'.json')).write_text(json.dumps(data,indent=2,allow_nan=False)+'\n',encoding='utf-8')
    track,visual=read('track'),read('visual')
    main=next(l for l in visual['loops'] if l['loop_id']=='main')
    depart=recipe['main_depart_m']; distances=[k['distance_m'] for k in main['knots']]
    i=min(bisect.bisect_right(distances,depart)-1,len(distances)-2)
    a,b=main['knots'][i:i+2]; f=(depart-a['distance_m'])/(b['distance_m']-a['distance_m'])
    start={k:a[k]+f*(b[k]-a[k]) for k in ('x','y','z')}
    dx,dz=b['x']-a['x'],b['z']-a['z']; norm=math.hypot(dx,dz); fx,fz=dx/norm,dz/norm
    # The final-lap branch leaves before the range, on its outside left.
    # The old right-side branch cut directly through the penalty oval.
    sign=-1 if recipe.get('turn_side','right')=='left' else 1
    sx,sz=sign*fz,-sign*fx; q=recipe['quarter_segments']; chord=recipe['arc_chord_m']
    radius=chord/(2*math.sin(math.pi/(4*q)))
    knots=[dict(distance_m=0,**start)];segments=[];paid=0
    def point(forward,side):return dict(x=start['x']+fx*forward+sx*side,y=start['y'],z=start['z']+fz*forward+sz*side)
    def edge(p,length,curvature=0):
        nonlocal paid
        previous=knots[-1]
        assert abs(math.dist(tuple(previous[k] for k in ('x','y','z')),tuple(p[k] for k in ('x','y','z')))-length)<1e-8
        paid+=round(length*1000)
        segments.append(dict(id=f'edge_{len(segments):03d}',length_m=length,start_grade=0,end_grade=0,
            curvature_per_m=curvature,width_m=recipe['width_m'],snow_profile_id='snow_test_packed',
            wind_exposure=.6,terrain_muscle_factor=1,draft_allowed=False,overtake_allowed=True,
            risk_level=.05,semantic_zone='stadium'))
        knots.append(dict(distance_m=paid/1000,**p))
    lead=recipe['aligned_lead_m'];edge(point(lead,0),lead)
    for n in range(1,q+1):
        angle=n*math.pi/(2*q)
        edge(point(lead+radius*math.sin(angle),radius*(1-math.cos(angle))),chord,-sign/radius)
    finish=recipe['finish_distance_m'];tail=finish-paid/1000
    edge(point(lead+radius,radius+tail),tail)
    finish_point=copy.deepcopy(knots[-1]);runout=recipe['runout_distance_m']
    edge(point(lead+radius,radius+tail+runout),runout)
    loop_id=recipe['loop_id']
    track['loops']=[l for l in track['loops'] if l['id']!=loop_id]+[dict(id=loop_id,kind='race',closed=False,segments=segments)]
    visual['loops']=[l for l in visual['loops'] if l['loop_id']!=loop_id]+[dict(loop_id=loop_id,knots=knots)]
    junctions,connections=read('junctions'),read('connections')
    # Reapplying a changed recipe discards only this spur's derived guards.
    junctions['junctions']=[j for j in junctions['junctions'] if not j['id'].startswith('apron_body_')
        or not any(p['anchor']['loop_id']==recipe['loop_id'] for p in j['ports'])]
    fork=recipe['connection_id']; junction_id=recipe['junction_id']; guard=recipe['fork_guard_m']
    junctions['junctions']=[j for j in junctions['junctions'] if j['id']!=junction_id]+[
        dict(id=junction_id,ports=[dict(id='finish_main',anchor=dict(loop_id='main',distance_m=depart),guard_before_m=guard,guard_after_m=guard),
            dict(id='finish_spur',anchor=dict(loop_id=loop_id,distance_m=0),guard_before_m=0,guard_after_m=guard)])]
    connections['connections']=[c for c in connections['connections'] if c['id']!=fork]+[
        dict(id=fork,junction_id=junction_id,from_port_id='finish_main',to_port_id='finish_spur',kind='coincident_seam')]
    for format in ('mass_start','sprint','pursuit'):
        route,profile=read(format+'_route'),read(format+'_profile')
        for lap in route['laps']:
            if 'loop_ids' in lap:
                lap['slices']=[dict(loop_id=l,start_m=0,end_m=next(v for v in visual['loops'] if v['loop_id']==l)['knots'][-1]['distance_m']) for l in lap.pop('loop_ids')]
        last=route['laps'][-1]; slices=last['slices']
        if slices[-1]['loop_id']==loop_id:slices.pop()
        # Remove the historical 1mm Main continuation used at the closed finish.
        if len(slices)>1 and slices[-1]['loop_id']=='main' and slices[-1]['start_m']==0 and slices[-1]['end_m']<=.001:slices.pop()
        assert slices[-1]['loop_id']=='main' and slices[-1]['start_m']<depart
        slices[-1]['end_m']=depart
        slices.append(dict(loop_id=loop_id,start_m=0,end_m=finish,enter_connection_id=fork))
        route['schema_version']=2
        profile['distance_m']=round(sum(s['end_m']-s['start_m'] for lap in route['laps'] for s in lap['slices']),3)
        write(format+'_route',route);write(format+'_profile',profile)
    write('track',track);write('visual',visual);write('junctions',junctions);write('connections',connections)
    if (venue/'environment.json').exists():
        env=read('environment');definitions={l['id']:l for l in track['loops']}
        env['route_corridors']=[dict(loop_id=l['loop_id'],source_visual_resource='Biathlon/Venues/woodland_basin_v1/visual',
            knots=l['knots'],widths_m=authored_corridor_widths(definitions[l['loop_id']],l)) for l in visual['loops']]
        for asset in env['mesh_assets']:
            if asset['id'].startswith('finish_gantry_'):
                asset['position']={k:finish_point[k] for k in ('x','y','z')};asset['yaw_deg']=0
            if asset['id'].startswith('event_screen_'):
                asset['position']=recipe['screen_position']
        # Actual canopy radius plus the existing corridor guard, not trunk-only pruning.
        guard=recipe['width_m']/2+env['forest']['additional_canopy_corridor_clearance_m']+env['forest']['conservative_all_species_canopy_radius_m']
        removed=0
        for group in env['forest']['groups']:
            p=resource_root/(group['instances_resource']+'.json');data=json.loads(p.read_text(encoding='utf-8-sig'))
            def clear(tree):
                x,z=tree['position']['x'],tree['position']['z']
                for a,b in zip(knots,knots[1:]):
                    dx,dz=b['x']-a['x'],b['z']-a['z'];t=max(0,min(1,((x-a['x'])*dx+(z-a['z'])*dz)/(dx*dx+dz*dz)))
                    if math.hypot(x-a['x']-t*dx,z-a['z']-t*dz)<guard:return False
                return True
            kept=[tree for tree in data['instances'] if clear(tree)]
            group['count']=len(kept)
            if len(kept)!=len(data['instances']):
                removed+=len(data['instances'])-len(kept);data['instances']=kept;group['count']=len(kept)
                p.write_text(json.dumps(data,separators=(',',':'))+'\n',encoding='utf-8')
        write('environment',env)
    else:removed=0
    separation=math.hypot(finish_point['x']-main['knots'][0]['x'],finish_point['z']-main['knots'][0]['z'])
    assert separation>=recipe['minimum_start_finish_separation_m']
    return dict(schema_version=1,id=recipe['id'],finish_point=finish_point,start_point=main['knots'][0],
        start_finish_separation_m=separation,final_lap_only=True,removed_canopies=removed,
        distance_delta_m=depart+finish-main['knots'][-1]['distance_m'],runout_m=runout)

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--resource-root',type=Path,default=ROOT/'Assets/Resources')
    parser.add_argument('--report',type=Path)
    args=parser.parse_args();result=author(args.resource_root.resolve())
    if args.report:args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(result))
