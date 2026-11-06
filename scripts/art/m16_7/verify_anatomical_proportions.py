"""Detect the legacy jaw/neck warp against the actual morphed CC0 source mesh.

Run under Blender: --python-exit-code 2 --python this.py -- --body path --source path
This checks preservation of source anatomy, not subjective final art acceptance.
"""
import argparse,json,sys
from pathlib import Path
import bpy,numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parent))
from anatomy_prepare import parse_obj

p=argparse.ArgumentParser();p.add_argument('--body',required=True);p.add_argument('--source',required=True);p.add_argument('--output',required=True)
args=p.parse_args(sys.argv[sys.argv.index('--')+1:]);body_path=Path(args.body).resolve();source=Path(args.source).resolve()
meta=json.loads(body_path.with_suffix('.json').read_text(encoding='utf-8'))
raw,*_=parse_obj(source/'base.obj')
for filename,weight in meta['morph_targets'].items():
    for line in (source/filename).read_text(encoding='utf-8').splitlines():
        if line.strip() and not line.startswith('#'):
            index,x,y,z=line.split();raw[int(index)]+=np.array([float(x),float(y),float(z)])*weight
ids=meta['source_vertex_ids'];selected=raw[ids];scale=1.8/(selected[:,1].max()-selected[:,1].min())
expected=np.column_stack((selected[:,0],-selected[:,2],selected[:,1]-selected[:,1].min()))*scale
bpy.ops.wm.open_mainfile(filepath=str(body_path));mesh=bpy.data.objects['MakeHuman_Athletic_Fitted'].data
actual=np.array([tuple(v.co) for v in mesh.vertices]);actual[:,1]-=.09
error=np.linalg.norm(actual-expected,axis=1);neck=expected[:,2]>1.48
# Remove rigid placement/rotation before judging deformation of the head/neck.
# A changed rest position or A-pose is not itself an anatomical distortion.
reference=expected[neck]-expected[neck].mean(axis=0);candidate=actual[neck]-actual[neck].mean(axis=0)
u,_,vt=np.linalg.svd(candidate.T@reference);rotation=u@vt
if np.linalg.det(rotation)<0:u[:,-1]*=-1;rotation=u@vt
shape_error=np.linalg.norm(candidate@rotation-reference,axis=1)
report={'schema_version':1,'id':'m167_anatomical_proportion_preservation','pass':bool(shape_error.max()<.0005),'vertices':len(ids),
    'max_coordinate_difference_m':float(error.max()),'max_head_neck_warp_after_rigid_alignment_m':float(shape_error.max()),
    'basis':'official CC0 male morphology, uniform 1.80m scale and rigid translation; no bone-fit stretching',
    'art_acceptance':False}
Path(args.output).write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report))
if not report['pass']:raise RuntimeError('The source anatomical proportions have been warped')
