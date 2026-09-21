"""Ablation evidence only. --assert-budget is the red/green triangle repro loop."""
import json,sys,math
from pathlib import Path

def analyze(root):
    execution=json.loads((root/'execution.json').read_text(encoding='utf-8-sig'))
    assert execution['passed'] and execution['diagnostic_only']
    case=json.loads((root/(execution['case_id']+'.json')).read_text(encoding='utf-8-sig'))
    assert case['diagnostic_only'] and case['diagnostic_probe']==execution['probe']
    samples=case['samples'];definition=json.loads((root/'definition.json').read_text(encoding='utf-8-sig'))
    assert len(samples)==definition['sample_frames'] and [s['index'] for s in samples]==list(range(len(samples)))
    assert all(s['draw_calls']>0 and s['triangles']>0 for s in samples)
    triangles=max(s['triangles'] for s in samples)
    main=sorted(s['cpu_main_ms'] for s in samples if s['cpu_main_ms']>=0)
    result=dict(schema_version=1,id='visual_profile_probe_analysis_v1',diagnostic_only=True,case_id=execution['case_id'],probe=execution['probe'],
        maximum_triangles=triangles,maximum_draws=max(s['draw_calls'] for s in samples),cpu_main_p95_ms=main[math.ceil(len(main)*.95)-1] if main else None,
        triangle_budget=execution['triangle_budget'],triangle_status='PASS' if triangles<=execution['triangle_budget'] else 'FAIL',
        qualification_status='NOT_APPLICABLE_DIAGNOSTIC',note='Ablation or single-case run; never production performance qualification.')
    (root/'analysis.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result));return result

if __name__=='__main__':
    result=analyze(Path(sys.argv[1]))
    if '--assert-budget' in sys.argv and result['triangle_status']!='PASS':sys.exit(2)
