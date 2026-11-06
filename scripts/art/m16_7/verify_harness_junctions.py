"""Measure ribbon width and complete endpoint seams, not endpoint centres alone."""
import bpy,argparse,json,sys,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
p=argparse.ArgumentParser();p.add_argument('--asset',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.open_mainfile(filepath=str(Path(a.asset).resolve()))
def tree(items):
    points=[];faces=[]
    for o in items:
        start=len(points);points.extend(o.matrix_world@v.co for v in o.data.vertices);faces.extend(tuple(i+start for i in f.vertices)for f in o.data.polygons)
    return BVHTree.FromPolygons(points,faces)
straps=tree([o for o in bpy.context.scene.objects if o.name.endswith('_Rifle_Harness_Webbing')]);stock=tree([bpy.data.objects['PULSE_Biathlon_Walnut_Stock']]);rows=[]
cloth=tree([bpy.data.objects['Woven_Bib_27'],bpy.data.objects['RaceSuit_Continuous_Tailored_Shell']])
for o in bpy.context.scene.objects:
    if o.type!='MESH'or '_Rifle_Suspension_'not in o.name:continue
    v=[o.matrix_world@v.co for v in o.data.vertices]
    rings=[v[i:i+4]for i in range(0,len(v),4)];centres=[sum(r,Vector())/4 for r in rings]
    widths=[];ratios=[]
    for i,ring in enumerate(rings):
        tangent=(centres[min(i+1,len(rings)-1)]-centres[max(0,i-1)]).normalized()
        across=ring[1]-ring[0];widths.append((across-tangent*across.dot(tangent)).length);ratios.append(widths[-1]/across.length)
    start_gap=max(straps.find_nearest(v)[3]for v in rings[0]);end_gap=max(stock.find_nearest(v)[3]for v in rings[-1])
    ratio=min(ratios);buried=[]
    for ring in rings:
        for point in ring:
            hit=cloth.ray_cast(Vector((point.x,.65,point.z)),Vector((0,-1,0)),1.0)
            if hit[0]is not None and hit[0].y-point.y>.0003:buried.append(hit[0].y-point.y)
    rows.append({'object':o.name,'minimum_effective_width_ratio':ratio,'minimum_effective_width_m':min(widths),'maximum_shoulder_seam_gap_m':start_gap,'maximum_stock_seam_gap_m':end_gap,'buried_rear_ribbon_vertices':len(buried),'maximum_garment_occlusion_m':max(buried,default=0),'pass':ratio>.85 and min(widths)>.008 and start_gap<.003 and end_gap<.003 and not buried})
front=[]
for o in bpy.context.scene.objects:
    if not o.name.endswith('_Rifle_Harness_Webbing'):continue
    columns=int(o.get('route_columns',5));count=int(o.get('route_rows',len(o.data.vertices)//2//columns))
    centres=[sum((o.matrix_world@o.data.vertices[i*columns+j].co for j in range(columns)),Vector())/columns for i in range(count)]
    turns=[]
    for i,c in enumerate(centres):
        if c.y<-.045 and 1.30<c.z<1.46:
            before=c-centres[(i-1)%count];after=centres[(i+1)%count]-c
            if min(before.length,after.length)>1e-7:turns.append(math.degrees(before.angle(after)))
    maximum=max(turns,default=180)
    front.append({'object':o.name,'front_rows':len(turns),'maximum_adjacent_route_turn_degrees':maximum,'pass':len(turns)>12 and maximum<8})
report={'schema_version':1,'id':'m167_full_width_harness_junctions','pass':len(rows)==4 and all(r['pass']for r in rows)and len(front)==2 and all(r['pass']for r in front),'rows':rows,'front_smoothness':front}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Harness has tapered effective widths or unattached endpoint edges')
