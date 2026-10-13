"""Repair two existing stadium footings from the actual triangle grid, without terrain/path edits."""
import json, sys, math
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'scripts/content/m13'))
import dress_woodland_course as dressing

def prepare(environment,grid,spacing,bounds):
    stands=[p for p in environment['assets'] if p['resource']=='Biathlon/Stadium/G2Grandstand/Grandstand']
    if len(stands)!=9: raise ValueError('Expected the nine existing grandstands.')
    # The leftmost section was placed over the rising return shoulder. Its own
    # unobstructed flat snow pocket is 12m forward; preserve all section widths.
    stands[0]['position']['z']=-60
    proof=[]
    for stand in stands:
        p=stand['position'];poly=dressing.footprint(bounds,(p['x'],0,p['z']),stand['yaw_deg'])
        heights=[h for _,h in dressing.foundation_samples(grid,spacing,poly)]
        variation=max(heights)-min(heights)
        if variation>.25:raise ValueError('Uneven native foundation: '+stand['id'])
        clearance=min(dressing.segment_polygon((a['x'],a['z']),(b['x'],b['z']),poly)-max(c['widths_m'][i:i+2])/2
            for c in environment['route_corridors'] for i,(a,b) in enumerate(zip(c['knots'],c['knots'][1:])))
        if clearance<3:raise ValueError('Foundation intersects a paid course.')
        for prior in proof:
            gap=min(dressing.segment_polygon(poly[i],poly[(i+1)%4],prior['footprint']) for i in range(4))
            if gap<=1e-9:raise ValueError('Grandstand footprints overlap.')
        p['y']=max(heights)-bounds[0][1]
        proof.append(dict(id=stand['id'],footprint=poly,variation_m=variation,clearance_m=clearance,position=dict(p)))
    return proof

def clear_spectators(environment,grid,spacing):
    body=dressing.mesh_bounds(root/'Assets/Resources/Biathlon/Crowd/StandingSpectatorLOD0.asset')
    contacts=dressing.standing_contact_geometry(root);_,anchors=contacts
    bounds=dressing.merged_bounds([(tuple(body[0][i]+a[i] for i in range(3)),tuple(body[1][i]+a[i] for i in range(3))) for a in anchors])
    proof=[]
    for item in environment['assets']:
        if item['resource']!='Biathlon/Roadside/StandingCrowd16':continue
        original=dict(item['position']);p=item['position']
        for attempt in range(25):
            poly=dressing.footprint(bounds,(p['x'],0,p['z']),item['yaw_deg'])
            gap=float('inf');nearest=None
            for c in environment['route_corridors']:
                for i,(a,b) in enumerate(zip(c['knots'],c['knots'][1:])):
                    g=dressing.segment_polygon((a['x'],a['z']),(b['x'],b['z']),poly)-max(c['widths_m'][i:i+2])/2
                    if g<gap:
                        gap=g;dx=b['x']-a['x'];dz=b['z']-a['z']
                        t=max(0,min(1,((p['x']-a['x'])*dx+(p['z']-a['z'])*dz)/(dx*dx+dz*dz)))
                        nearest=(a['x']+t*dx,a['z']+t*dz)
            if gap>=3.2:break
            dx=p['x']-nearest[0];dz=p['z']-nearest[1];length=math.hypot(dx,dz)
            if length<.001:raise ValueError('Ambiguous spectator course side.')
            p['x']+=dx/length*.5;p['z']+=dz/length*.5
        else:raise ValueError('No bounded off-course spectator pocket: '+item['id'])
        # Keep both actual boots supported, not just the centre of each person.
        if dressing.standing_contact_gap(grid,spacing,(p['x'],0,p['z']),item['yaw_deg'],contacts)>.029:
            base=dict(p);found=False
            offsets=sorted(((x*.5,z*.5) for x in range(-24,25) for z in range(-24,25)),key=lambda q:q[0]*q[0]+q[1]*q[1])
            for dx,dz in offsets:
                candidate=(base['x']+dx,0,base['z']+dz)
                if dressing.standing_contact_gap(grid,spacing,candidate,item['yaw_deg'],contacts)>.029:continue
                poly=dressing.footprint(bounds,candidate,item['yaw_deg'])
                gap=min(dressing.segment_polygon((a['x'],a['z']),(b['x'],b['z']),poly)-max(c['widths_m'][i:i+2])/2
                    for c in environment['route_corridors'] for i,(a,b) in enumerate(zip(c['knots'],c['knots'][1:])))
                if gap<3.2:continue
                p['x'],p['z']=candidate[0],candidate[2];found=True;break
            if not found:raise ValueError('No supported spectator foot pocket: '+item['id'])
        if p!=original:p['y']=dressing.ground(grid,spacing,p['x'],p['z'])
        proof.append(dict(id=item['id'],original=original,position=dict(p),clearance_m=gap))
    return proof

def ground_tapes(environment,grid,spacing):
    bounds=dressing.merged_bounds([dressing.mesh_bounds(root/('Assets/Resources/Biathlon/Roadside/'+name+'.asset')) for name in ('TapePosts','TapeTeal','TapeWhite')])
    proof=[]
    offsets=sorted(((x*.25,z*.25) for x in range(-16,17) for z in range(-16,17)),key=lambda q:q[0]*q[0]+q[1]*q[1])
    for item in environment['assets']:
        if item['resource']!='Biathlon/Roadside/RibbonTape2m':continue
        p=item['position'];original=dict(p);found=False
        for dx,dz in offsets:
            candidate=(original['x']+dx,0,original['z']+dz)
            poly=dressing.footprint(bounds,candidate,item['yaw_deg'])
            heights=[h for _,h in dressing.foundation_samples(grid,spacing,poly)];lo,hi=min(heights),max(heights)
            if hi-lo>.419:continue
            gap=min(dressing.segment_polygon((a['x'],a['z']),(b['x'],b['z']),poly)-max(c['widths_m'][i:i+2])/2
                for c in environment['route_corridors'] for i,(a,b) in enumerate(zip(c['knots'],c['knots'][1:])))
            if gap<3.01:continue
            # At most 2cm above the lowest witness; at most 39.9cm embedded.
            bottom=max(lo,hi-.399)
            if dx==0 and dz==0 and original['y']+bounds[0][1]>=hi-.399 and original['y']+bounds[0][1]<=lo+.02:
                bottom=original['y']+bounds[0][1]
            p.update(x=candidate[0],z=candidate[2],y=bottom-bounds[0][1]);found=True
            if p!=original:proof.append(dict(id=item['id'],original=original,position=dict(p),clearance_m=gap,embedding_m=hi-bottom,bottom_gap_m=bottom-lo))
            break
        if not found:raise ValueError('No bounded tape foundation: '+item['id'])
    return proof

def deduplicate_tapes(environment):
    retained=[];seen=[];aliases=[]
    for item in environment['assets']:
        if item['resource']=='Biathlon/Roadside/RibbonTape2m':
            p=item['position'];duplicate=None
            for prior in seen:
                q=prior['position'];angle=abs((item['yaw_deg']-prior['yaw_deg']+180)%360-180)
                if sum((p[a]-q[a])**2 for a in ('x','y','z'))<1e-8 and angle<.001:
                    duplicate=prior;break
            if duplicate:
                aliases.append(dict(removed_id=item['id'],retained_id=duplicate['id']));continue
            seen.append(item)
        retained.append(item)
    environment['assets']=retained
    return aliases

if __name__=='__main__':
    path=root/'Assets/Resources/Biathlon/Venues/woodland_basin_v1/environment.json'
    env=json.loads(path.read_text(encoding='utf-8-sig'));grid={};spacing=env['terrain']['grid_spacing_m']
    for tile in env['terrain']['tiles']:
        field=json.loads((root/'Assets/Resources'/(tile['height_resource']+'.json')).read_text())
        for z in range(tile['rows']):
            for x in range(tile['columns']):
                key=(tile['center']['x']-tile['size']['x']/2+x*spacing,tile['center']['z']-tile['size']['z']/2+z*spacing)
                grid[key]=field['heights_m'][z*tile['columns']+x]
    bounds=dressing.merged_bounds([dressing.mesh_bounds(p) for p in (root/'Assets/Resources/Biathlon/Stadium/G2Grandstand').glob('*.asset')])
    proof=prepare(env,grid,spacing,bounds)
    crowds=clear_spectators(env,grid,spacing)
    tapes=ground_tapes(env,grid,spacing)
    aliases=deduplicate_tapes(env)
    path.write_text(json.dumps(env,indent=2)+'\n')
    out=root/'docs/quality/m15-5/stadium-grounding.json'
    out.write_text(json.dumps(dict(schema_version=1,id='m15_5_stadium_grounding',terrain_changed=False,course_changed=False,witnesses=proof,spectators=crowds,tapes=tapes,duplicate_fences=aliases),indent=2)+'\n')
    print('Nine foundations and '+str(len(crowds))+' crowd pockets checked; '+str(len(aliases))+' exact duplicate fences removed. Terrain and paths retained.')
