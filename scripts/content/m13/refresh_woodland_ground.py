"""Refresh existing terrain below current ribbons; retain dressing/tree X/Z.

Uses the authoring generator's exact triangle overlay rather than relaxing
runtime snow/ground validation. Neighboring tile seams use one world grid.
"""
import argparse, ast, bisect, json, math, struct
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--resource-root',type=Path,default=ROOT/'Assets/Resources')
RES=parser.parse_args().resource_root.resolve()
VENUE=RES/'Biathlon/Venues/woodland_basin_v1'
source=Path(__file__).with_name('generate_environment.py')
functions=ast.parse(source.read_text(encoding='utf-8'))
namespace={'math':math,'struct':struct,'bisect':bisect}
names={'float32','ribbon_triangles','cross_xz','clipped_polygon','triangle_height','cap_terrain_below_ribbons'}
exec(compile(ast.Module(body=[n for n in functions.body if isinstance(n,ast.FunctionDef) and n.name in names],type_ignores=[]),str(source),'exec'),namespace)
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
env=read(VENUE/'environment.json');track=read(VENUE/'track.json');visual=read(VENUE/'visual.json')
spacing=round(env['terrain']['grid_spacing_m']);grid={};documents=[]
for tile in env['terrain']['tiles']:
    p=RES/(tile['height_resource']+'.json');d=read(p)
    x0=round(tile['center']['x']-tile['size']['x']/2);z0=round(tile['center']['z']-tile['size']['z']/2)
    for z in range(d['rows']):
        for x in range(d['columns']):grid[x0+x*spacing,z0+z*spacing]=d['heights_m'][z*d['columns']+x]
    documents.append((p,d,x0,z0))
audit=namespace['cap_terrain_below_ribbons'](grid,spacing,namespace['ribbon_triangles'](track,visual),.03)
changed=0
for p,d,x0,z0 in documents:
    heights=[grid[x0+x*spacing,z0+z*spacing] for z in range(d['rows']) for x in range(d['columns'])]
    if heights!=d['heights_m']:
        d['heights_m']=heights;p.write_text(json.dumps(d,separators=(',',':'))+'\n',encoding='utf-8');changed+=1
def ground(x,z):
    x0=math.floor(x/spacing)*spacing;z0=math.floor(z/spacing)*spacing
    fx=(x-x0)/spacing;fz=(z-z0)/spacing
    a,b,c,d=(grid.get(k) for k in ((x0,z0),(x0+spacing,z0),(x0,z0+spacing),(x0+spacing,z0+spacing)))
    if a is None or b is None or c is None or d is None:return None
    return a+(b-a)*fx+(c-a)*fz if fx+fz<=1 else d+(c-d)*(1-fx)+(b-d)*(1-fz)
trees=0
for group in env['forest']['groups']:
    p=RES/(group['instances_resource']+'.json');d=read(p);edited=False
    for item in d['instances']:
        pos=item['position'];height=ground(pos['x'],pos['z'])
        if height is not None and abs(pos['y']-height)>1e-5:
            pos['y']=round(height,6);edited=True;trees+=1
    if edited:p.write_text(json.dumps(d,separators=(',',':'))+'\n',encoding='utf-8')
audit.update(changed_tiles=changed,corrected_tree_heights=trees)
print(json.dumps(audit))
