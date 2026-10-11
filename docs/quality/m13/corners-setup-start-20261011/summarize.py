"""Summarize the bounded native captures; run from the repository root."""
import csv,json,statistics
from pathlib import Path
p=Path(__file__).resolve().parent
result={"schema_version":1,"id":"m13_corner_setup_native_summary_20261011",
        "scope":"One all-AI Mass30 first-lap Editor capture per revision, RTX5080. One tick/frame around first bend and setup; four elsewhere. Changed start and AI decisions also change pack states. Not a controlled FPS benchmark or a complete race."}
for name in ("before","final"):
 rows=list(csv.DictReader((p/f"live-{name}.csv").open()))
 result[name]={}
 for phase in ("first_bend","setup"):
  selected=[r for r in rows if (r['loop']=='main' and 55<=float(r['arc_m'])<=140 if phase=='first_bend' else r['phase']=='Setup')]
  out={"samples":len(selected)}
  for key in ("sim_ms","pose_ms","camera_ms","draws","frame_ms"):
   values=sorted(float(r[key])/(int(r['steps']) if key=='sim_ms' else 1) for r in selected)
   out[key]={"mean":statistics.mean(values),"p95":values[min(len(values)-1,int(len(values)*.95))],"max":max(values)}
  result[name][phase]=out
(p/'native-summary.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2))
