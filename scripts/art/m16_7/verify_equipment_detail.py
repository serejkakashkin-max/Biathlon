"""Saved-mesh / actual bitmap gates for the isolated visual equipment."""
import argparse,sys,json,math
from pathlib import Path
from collections import deque
import bpy,numpy as np
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
p=argparse.ArgumentParser();p.add_argument('--asset',required=True);p.add_argument('--baseline',required=True);p.add_argument('--texture',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);rows=[]
def check(name,ok,**data):rows.append(dict(name=name,pass_=bool(ok),**data))
def glasses():return {o.name:np.array([list(o.matrix_world@v.co)for v in o.data.vertices]) for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith(('Panoramic_Amber_Lens','Goggle_'))}
bpy.ops.wm.open_mainfile(filepath=str(Path(a.baseline).resolve()));before=glasses()
bpy.ops.wm.open_mainfile(filepath=str(Path(a.asset).resolve()));after=glasses()
error=max(float(np.max(np.linalg.norm(before[n]-after[n],axis=1)))for n in before) if before.keys()==after.keys() else math.inf
check('original_glasses_geometry_preserved',error<.000001,maximum_vertex_error_m=error,parts=len(before))
stock=bpy.data.objects.get('PULSE_Biathlon_Walnut_Stock');check('reference_stock_is_authored',stock is not None)
if stock:
    tree=BVHTree.FromObject(stock,bpy.context.evaluated_depsgraph_get());carry=Matrix.Translation((.045,.188,.81))@Matrix.Rotation(math.radians(-4),4,'Y');transform=stock.matrix_world.inverted()@carry
    for x,z in [(.038,.316),(.036,.413)]:
        origin=transform@Vector((x,-.10,z));direction=(transform.to_3x3()@Vector((0,1,0))).normalized();hit=tree.ray_cast(origin,direction,.20)
        check('stock_external_aperture_'+str(z),hit[0] is None)
mask=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith(('Mask_Derived','Mask_PULSE'))]
check('mask_kept_separate',len(mask)>=4,parts=len(mask))
for obj in mask:
    obj.data.calc_loop_triangles();area=sum(p.area for p in obj.data.polygons);check(obj.name,area>0 and all(math.isfinite(c)for v in obj.data.vertices for c in v.co),triangles=len(obj.data.loop_triangles))
im=bpy.data.images.load(str(Path(a.texture).resolve()));pixels=np.empty(im.size[0]*im.size[1]*4,np.float32);im.pixels.foreach_get(pixels);pixels=pixels.reshape(im.size[1],im.size[0],4)
for lo,hi in [(80,1100),(2950,3700)]:
    crop=pixels[im.size[1]-hi:im.size[1]-lo,50:462,:3];mask=(crop.min(axis=2)>.65);counts=[]
    for y,x in zip(*np.where(mask.copy())):
        if not mask[y,x]:continue
        queue=deque([(y,x)]);mask[y,x]=False;count=0
        while queue:
            yy,xx=queue.popleft();count+=1
            for y2,x2 in [(yy-1,xx),(yy+1,xx),(yy,xx-1),(yy,xx+1)]:
                if 0<=y2<mask.shape[0] and 0<=x2<mask.shape[1] and mask[y2,x2]:mask[y2,x2]=False;queue.append((y2,x2))
        if count>200:counts.append(count)
    check('ski_PULSE_complete_'+str(lo),len(counts)==5,connected_glyphs=len(counts),areas=counts)
report={'schema_version':1,'id':'m167_equipment_saved_geometry_and_print','pass':all(r['pass_']for r in rows),'rows':rows,'scope':'sampled static external geometry and actual bitmap; art acceptance separate'}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Visual equipment gate failed')
