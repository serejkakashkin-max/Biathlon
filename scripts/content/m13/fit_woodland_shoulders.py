"""Fit visual snow to the current road, then use the exact ribbon clearance cap.

One shared world grid preserves tile seams. Does not change race geometry.
"""
import json, math, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
RES = ROOT / 'Assets/Resources'
VENUE = RES / 'Biathlon/Venues/woodland_basin_v1'
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
settings = read(Path(__file__).with_name('woodland_shoulders_v1.json'))
env = read(VENUE / 'environment.json')
cell = settings['index_cell_m']
reach = settings['influence_m']
segments = []
bins = {}
for corridor in env['route_corridors']:
    knots = corridor['knots']; widths = corridor['widths_m']
    for i, (a, b) in enumerate(zip(knots, knots[1:])):
        ax, az, bx, bz = a['x'], a['z'], b['x'], b['z']
        length2 = (bx-ax)**2+(bz-az)**2
        if not length2: continue
        n = len(segments)
        segments.append((ax, az, bx-ax, bz-az, length2, a['y'], b['y']-a['y'], widths[i]/2, widths[i+1]/2-widths[i]/2))
        for x in range(math.floor((min(ax,bx)-reach)/cell),math.floor((max(ax,bx)+reach)/cell)+1):
            for z in range(math.floor((min(az,bz)-reach)/cell),math.floor((max(az,bz)+reach)/cell)+1):
                bins.setdefault((x,z),[]).append(n)

grid = {}; changed = 0; maximum = 0
spacing = env['terrain']['grid_spacing_m']
for tile in env['terrain']['tiles']:
    path = RES / (tile['height_resource']+'.json'); data = read(path)
    x0=tile['center']['x']-tile['size']['x']/2; z0=tile['center']['z']-tile['size']['z']/2
    for row in range(data['rows']):
        for col in range(data['columns']):
            x=x0+col*spacing; z=z0+row*spacing; key=(x,z)
            if key not in grid:
                original=data['heights_m'][row*data['columns']+col]
                best=None
                for n in bins.get((math.floor(x/cell),math.floor(z/cell)),[]):
                    ax,az,dx,dz,l2,y,dy,w,dw=segments[n]
                    t=max(0,min(1,((x-ax)*dx+(z-az)*dz)/l2))
                    distance=math.hypot(x-ax-t*dx,z-az-t*dz)
                    edge=max(0,distance-w-t*dw)
                    if best is None or edge<best[0]: best=(edge,y+t*dy)
                value=original
                if best is not None and best[0]<reach:
                    edge,height=best; base=height-settings['clearance_m']
                    beyond=max(0,edge-settings['flat_shoulder_m'])
                    limit=beyond*settings['maximum_bank_grade']
                    value=max(base-limit,min(base+limit,original))
                grid[key]=round(value,6)
                maximum=max(maximum,abs(value-original))
            idx=row*data['columns']+col
            if data['heights_m'][idx]!=grid[key]: changed+=1
            data['heights_m'][idx]=grid[key]
    path.write_text(json.dumps(data,separators=(',',':'))+'\n',encoding='utf-8')
print(json.dumps({'changed_samples':changed,'maximum_adjustment_m':maximum}))
subprocess.run([sys.executable,str(Path(__file__).with_name('refresh_woodland_ground.py'))],check=True)
