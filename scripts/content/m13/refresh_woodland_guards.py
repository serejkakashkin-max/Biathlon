"""Expand measured corridor guard coverage after authored width/finish edits.

Preserve every existing region, port ID and anchor. Coverage can only grow;
this is geometry metadata, never permission to serialize parallel athletes.
"""
import argparse,json,math
from pathlib import Path
from bind_compact_range import native_pairs,interval_ports,cropped_knots

def refresh(root,report):
    folder=root/'Biathlon/Venues/woodland_basin_v1'
    read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
    track=read(folder/'track.json');junctions=read(folder/'junctions.json')
    loops={l['id']:l for l in track['loops']}
    lengths={k:sum(round(s['length_m']*1000) for s in l['segments']) for k,l in loops.items()}
    pairs=native_pairs(read(report),lengths);lookup={j['id']:j for j in junctions['junctions']};changed=added=0
    for pair,intervals in pairs.items():
        name='apron_body_'+'_'.join(pair)
        region=lookup.get(name)
        if region is None:
            ports=[]
            for loop,(low,high) in intervals.items():ports+=interval_ports(loop,low,high,name,lengths[loop],loops[loop]['closed'])
            region=dict(id=name,ports=ports);junctions['junctions'].append(region);lookup[name]=region;added+=1
        else:
            for loop,(low,high) in intervals.items():
                ports=[p for p in region['ports'] if p['anchor']['loop_id']==loop]
                assert ports
                coveredLow=min(round((p['anchor']['distance_m']-p['guard_before_m'])*1000) for p in ports)
                coveredHigh=max(round((p['anchor']['distance_m']+p['guard_after_m'])*1000) for p in ports)
                if coveredLow<=low and coveredHigh>=high:continue
                # Preserve the reference anchor and widen its existing guard.
                p=ports[0];at=round(p['anchor']['distance_m']*1000)
                p['guard_before_m']=max(p['guard_before_m'],max(0,at-low)/1000)
                p['guard_after_m']=max(p['guard_after_m'],max(0,high-at)/1000);changed+=1
    # New measured main/mat guards use the same concurrent physical apron,
    # never the default exclusive junction gate. Preserve typed exit bindings.
    apron=read(folder/'shared_apron.json');path=read(folder/'paths.json')
    family={s['loop_id'] for lane in path['lanes'] for s in lane['slices']}
    selected=[j for j in junctions['junctions'] if len({p['anchor']['loop_id'] for p in j['ports']})==2
              and all(p['anchor']['loop_id'] in family for p in j['ports'])]
    bound={r['junction_id'] for r in apron['regions']}
    for region in selected:
        if region['id'] not in bound:apron['regions'].append({'junction_id':region['id']})
    visual={l['loop_id']:l for l in read(folder/'visual.json')['loops']}
    body=read(root/'Biathlon/occupancy_provisional_v1.json')
    half=(body['body_length_m']+body['personal_clearance_m'])/2
    for corridor in apron['corridors']:
        loop=corridor['loop_id'];ports=[p for j in selected for p in j['ports'] if p['anchor']['loop_id']==loop]
        low=min(p['anchor']['distance_m']-p['guard_before_m']-half for p in ports)
        high=max(p['anchor']['distance_m']+p['guard_after_m']+half for p in ports)
        if not loops[loop]['closed']:low=max(0,low);high=min(lengths[loop]/1000,high)
        low=min(corridor['knots'][0]['distance_m'],math.floor(low*1000)/1000)
        high=max(corridor['knots'][-1]['distance_m'],math.ceil(high*1000)/1000)
        corridor['knots']=cropped_knots(visual[loop]['knots'],low,high)
    (folder/'shared_apron.json').write_text(json.dumps(apron,indent=2)+'\n',encoding='utf-8')
    (folder/'junctions.json').write_text(json.dumps(junctions,indent=2)+'\n',encoding='utf-8')
    return dict(schema_version=1,id='woodland_guard_refresh_v1',expanded_ports=changed,added_regions=added,removed_regions=0)

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--resource-root',type=Path,required=True);p.add_argument('--report',type=Path,required=True)
    a=p.parse_args();print(json.dumps(refresh(a.resource_root,a.report)))
