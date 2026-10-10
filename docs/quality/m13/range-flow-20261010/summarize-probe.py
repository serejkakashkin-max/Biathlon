"""Summarize captured invocation costs; these CSVs do not establish frame FPS."""
import csv,json,statistics
from pathlib import Path
root=Path(__file__).parent
result={'schema_version':1,'id':'m13_range_flow_cpu_probe_20261010',
    'limitation':'Editor callbacks may have run more than once per frame. Frame/GPU/wall columns are not a comparable FPS benchmark. Per-invocation Stopwatch CPU costs remain useful; phases are observed at the invocation end.'}
for name in ('before','after'):
    rows=list(csv.DictReader((root/f'live-{name}.csv').open(encoding='utf-8-sig')))
    phases={}
    for phase in sorted({r['phase'] for r in rows}):
        samples=[float(r['simulation_ms'])/int(r['steps']) for r in rows if r['phase']==phase]
        singles=[float(r['simulation_ms']) for r in rows if r['phase']==phase and int(r['steps'])==1]
        phases[phase]={'samples':len(samples),'mean_ms_per_step':statistics.fmean(samples),'max_ms_per_step':max(samples),
            'single_step_samples':len(singles),'single_step_mean_ms':statistics.fmean(singles) if singles else None}
    result[name]=phases
(root/'cpu-summary.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({name:result[name].get('Setup') for name in ('before','after')}))
