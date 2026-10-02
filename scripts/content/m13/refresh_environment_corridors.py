"""Refresh detached corridor data without regenerating unchanged terrain/trees."""
from pathlib import Path
import argparse,hashlib,json
from corridor_sampling import authored_corridor_widths

SCRIPT=Path(__file__).resolve().parent
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--project-root',type=Path,default=SCRIPT.parents[2])
parser.add_argument('--output-root',type=Path,default=None)
parser.add_argument('--venue-id',default='woodland_basin_v1')
args=parser.parse_args()
OUT=(args.output_root or args.project_root/'artifacts/wp169i/draft').resolve()
PREFIX='Biathlon/Venues/'+args.venue_id
def read(name):return json.loads((OUT/'resources'/PREFIX/(name+'.json')).read_text(encoding='utf-8-sig'))
track=read('track');visual=read('visual');environment=read('environment')
loops={loop['id']:loop for loop in track['loops']}
environment['route_corridors']=[{'loop_id':loop['loop_id'],'source_visual_resource':PREFIX+'/visual','knots':loop['knots'],
    'widths_m':authored_corridor_widths(loops[loop['loop_id']],loop)} for loop in visual['loops']]
resource=OUT/'resources'/PREFIX/'environment.json'
resource.write_text(json.dumps(environment,allow_nan=False,separators=(',',':'))+'\n',encoding='utf-8')
(OUT/'environment.json').write_text(json.dumps(environment,indent=2)+'\n',encoding='utf-8')
audit_path=OUT/'environment-audit.json';audit=json.loads(audit_path.read_text(encoding='utf-8-sig'))
audit['environment_resource_sha256']=hashlib.sha256(resource.read_bytes()).hexdigest()
audit['corridor_width_sample_count']=sum(len(c['widths_m']) for c in environment['route_corridors'])
audit['corridor_width_sampling']='TrackSampler Reject mode: outgoing segment at interior boundaries, last segment at terminal knot'
audit_path.write_text(json.dumps(audit,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'corridor_count':len(environment['route_corridors']),'authored_width_sample_count':audit['corridor_width_sample_count'],
    'environment_sha256':audit['environment_resource_sha256']}))
