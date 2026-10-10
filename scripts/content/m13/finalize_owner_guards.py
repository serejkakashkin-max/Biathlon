"""Rebind measured overlaps after owner shortening, without shrinking roads/bodies."""
import argparse,json,math,os
from pathlib import Path
from bind_compact_range import OVERLAP
from refresh_woodland_guards import refresh
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--report',type=Path,required=True);p.add_argument('--resource-root',type=Path,required=True)
a=p.parse_args();report=json.loads(a.report.read_text(encoding='utf-8-sig'));same=[];distinct=[]
for issue in report['issues']:
 match=OVERLAP.search(issue.get('detail',''))
 (same if match and match[1]==match[4] else distinct).append(issue)
filtered=a.report.with_name('venue-distinct-crossings.json');filtered.write_text(json.dumps(dict(report,issues=distinct))+'\n')
receipt=refresh(a.resource_root,filtered)
path=a.resource_root/'Biathlon/Venues/woodland_basin_v1/junctions.json';d=json.loads(path.read_text(encoding='utf-8-sig'))
for issue in same:
 match=OVERLAP.search(issue['detail']);name='owner_bend_'+issue['path'].replace('/','_');ports=[]
 for i,(loop,low,high) in enumerate((match.groups()[:3],match.groups()[3:])):
  low=math.floor(float(low)*1000)-1;high=math.ceil(float(high)*1000)+1;mid=(low+high)//2
  ports.append(dict(id=name+'_'+str(i),anchor=dict(loop_id=loop,distance_m=mid/1000),guard_before_m=(mid-low)/1000,guard_after_m=(high-mid)/1000))
 d['junctions'].append(dict(id=name,ports=ports))
# An open 40m start road needs two finite guards when its full surface overlaps
# the lap road. Preserve the existing port identity used by connections.
for region in d['junctions']:
 if region['id']!='apron_body_main_start_approach':continue
 for port in list(region['ports']):
  if port['anchor']['loop_id']!='start_approach':continue
  if port['guard_before_m']+port['guard_after_m']<40:continue
  port['guard_after_m']=round(39.999-port['anchor']['distance_m'],3)
  region['ports'].append(dict(id=port['id']+'_end',anchor=dict(loop_id='start_approach',distance_m=39.999),guard_before_m=.001,guard_after_m=.001))
temp=path.with_suffix('.authoring.tmp');temp.write_text(json.dumps(d,indent=2)+'\n');os.replace(temp,path)
print(json.dumps(dict(receipt,self_bend_regions=len(same))))
