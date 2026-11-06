"""Cross-check offline subdivision stencils against real Blender subdivision."""
import argparse,json,sys,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy,numpy as np
from mathutils import Matrix,Vector
from hand_surface import HandSurface
p=argparse.ArgumentParser();p.add_argument('--anatomy',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);meta=json.loads(Path(a.anatomy).with_suffix('.json').read_text());rows=[]
bpy.ops.wm.read_factory_settings(use_empty=True)
for side,prefix in [(1,'Left'),(-1,'Right')]:
    cage=meta['hand_cages'][prefix];suffix='.L' if side>0 else '.R'
    names=[name for name in meta['finger_bones'] if name.endswith(suffix)]
    surface=HandSurface(meta,side,3,names)
    for phase in range(3):
        matrices=[]
        for index,name in enumerate(names):
            head=Vector(meta['finger_bones'][name]['head'])
            angle=0 if phase==0 else math.radians(((index*7)%35-17)*phase)
            matrices.append(Matrix.Translation(head)@Matrix.Rotation(angle,4,Vector((.3,.5,.7)).normalized())@Matrix.Translation(-head))
        posed=surface.rest*surface.remainder[:,None]
        for index,matrix in enumerate(matrices):
            matrix=np.asarray(matrix);posed+=(surface.rest@matrix[:3,:3].T+matrix[:3,3])*surface.weights[:,index,None]
        calculated_surface=(posed[surface.indices]*surface.coefficients[:,:,None]).sum(axis=1)
        calculated=calculated_surface[:len(posed)]
        mesh=bpy.data.meshes.new('Stencil comparison');mesh.from_pydata(posed.tolist(),[],cage['faces']);mesh.update()
        obj=bpy.data.objects.new('Stencil comparison',mesh);bpy.context.collection.objects.link(obj);bpy.context.view_layer.objects.active=obj
        modifier=obj.modifiers.new('Actual Catmull-Clark','SUBSURF');modifier.levels=1;bpy.ops.object.modifier_apply(modifier=modifier.name)
        actual=np.asarray([tuple(v.co) for v in obj.data.vertices[:len(posed)]])
        error=float(np.linalg.norm(actual-calculated,axis=1).max())
        expected_normal=np.asarray([tuple(obj.data.vertices[k].normal) for k in surface.pad]).mean(axis=0);expected_normal/=np.linalg.norm(expected_normal)
        fitted_normal=surface.sample_surface(calculated_surface,accurate_normal=True)[1]
        normal_error=math.degrees(math.acos(float(np.clip(expected_normal@fitted_normal,-1,1))))
        rows.append({'hand':prefix,'phase':phase,'source_corners':len(posed),'maximum_corner_difference_m':error,'palmar_normal_difference_degrees':normal_error,'pass':error<.00006 and normal_error<2})
        bpy.data.objects.remove(obj,do_unlink=True)
report={'schema_version':1,'id':'m167_hand_surface_stencil_blender_parity','cases':rows,'pass':all(r['pass'] for r in rows),'art_acceptance':False}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Offline stencil does not represent the actual Blender hand surface')
