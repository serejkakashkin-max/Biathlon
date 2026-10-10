"""Rebind branch endpoints to the paid millimetre anchors after shortening."""
import json, math, os
from pathlib import Path

def snap(track, visual, junctions):
    loops={l['loop_id']:l['knots'] for l in visual['loops']}
    physical={l['id']:l for l in track['loops']}
    measured=[]
    for junction in junctions['junctions']:
        ports=junction['ports']
        main=[p['anchor'] for p in ports if p['anchor']['loop_id']=='main']
        branches=[p['anchor'] for p in ports if p['anchor']['loop_id'] in ('sprint_extension','pursuit_shortcut')]
        if len(main)!=1 or len(branches)!=1:continue
        anchor,branch=main[0],branches[0];knots=loops[branch['loop_id']]
        d=branch['distance_m']
        if d not in (0,knots[-1]['distance_m']):continue
        road=loops['main'];i=1
        while i<len(road)-1 and road[i]['distance_m']<=anchor['distance_m']:i+=1
        a,b=road[i-1:i+1];t=(anchor['distance_m']-a['distance_m'])/(b['distance_m']-a['distance_m'])
        point={c:a[c]+(b[c]-a[c])*t for c in ('x','y','z')}
        endpoint=knots[0 if d==0 else -1]
        error=math.dist([endpoint[c] for c in point],list(point.values()))
        assert error<.001,(junction['id'],error)
        endpoint.update(point)
        a,b=knots[:2] if d==0 else knots[-2:]
        segment=physical[branch['loop_id']]['segments'][0 if d==0 else -1]
        segment['start_grade']=segment['end_grade']=(b['y']-a['y'])/math.hypot(b['x']-a['x'],b['z']-a['z'])
        measured.append(dict(junction=junction['id'],correction_m=error))
    return measured

if __name__=='__main__':
    root=Path(__file__).resolve().parents[3]
    venue=root/'Assets/Resources/Biathlon/Venues/woodland_basin_v1'
    read=lambda n:json.loads((venue/(n+'.json')).read_text(encoding='utf-8-sig'))
    track,visual,junctions,environment=map(read,('track','visual','junctions','environment'))
    measured=snap(track,visual,junctions)
    for corridor in environment['route_corridors']:
        corridor['knots']=next(l['knots'] for l in visual['loops'] if l['loop_id']==corridor['loop_id'])
    for name,data in (('track',track),('visual',visual),('environment',environment)):
        path=venue/(name+'.json');temp=path.with_suffix('.authoring.tmp')
        temp.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');os.replace(temp,path)
    output=root/'docs/quality/m13/owner-followup-20261010/branch-seam-correction.json'
    output.write_text(json.dumps(measured,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(measured))
