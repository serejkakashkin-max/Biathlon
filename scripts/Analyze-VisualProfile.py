"""Summarize raw player counters without substituting missing values with zero."""
import json
import math
import sys
from pathlib import Path

METRICS = ('wall_ms', 'cpu_frame_ms', 'cpu_main_ms', 'cpu_render_ms', 'cpu_wait_ms',
           'gpu_ms', 'gpu_camera_ms', 'draw_calls', 'batches', 'setpass_calls', 'triangles', 'gc_bytes', 'used_memory_bytes')

def summarize(values):
    if any(not math.isfinite(v) or v < -1 for v in values):
        raise ValueError('Invalid sample; only -1 is an unavailable value')
    if any(-1 < v < 0 for v in values):
        raise ValueError('Invalid negative sample')
    valid = sorted(v for v in values if v >= 0)
    if not valid:
        return dict(available=0, unavailable=len(values), mean=None, p50=None, p95=None, maximum=None)
    return dict(available=len(valid), unavailable=len(values)-len(valid), mean=sum(valid)/len(valid),
                p50=valid[math.ceil(.50*len(valid))-1], p95=valid[math.ceil(.95*len(valid))-1], maximum=valid[-1])

def analyze(root):
    definition = json.loads((root/'definition.json').read_text(encoding='utf-8-sig'))
    execution = json.loads((root/'execution.json').read_text(encoding='utf-8-sig'))
    assert execution['passed'] and len(execution['case_ids']) == len(definition['cases'])
    budget_map = {'wall_ms':'frame_budget_ms', 'cpu_main_ms':'cpu_budget_ms', 'cpu_render_ms':'cpu_budget_ms',
                  'gpu_ms':'gpu_budget_ms', 'draw_calls':'draw_call_budget', 'batches':'batch_budget',
                  'setpass_calls':'setpass_budget', 'triangles':'triangle_budget'}
    rows = []
    for case in definition['cases']:
        evidence = json.loads((root/(case['id']+'.json')).read_text(encoding='utf-8-sig'))
        samples = evidence['samples']
        assert len(samples) == definition['sample_frames']
        assert [s['index'] for s in samples] == list(range(len(samples)))
        assert all(s['draw_calls'] > 0 and s['triangles'] > 0 for s in samples), 'No rendered scene: timing is not a graphics benchmark'
        assert evidence['source_tick'] == 400 and evidence['athletes'] == 10
        assert evidence['width'] == definition['width'] and evidence['height'] == definition['height']
        stamps = [s['timing_timestamp'] for s in samples if s['timing_timestamp']]
        assert len(stamps) == len(set(stamps)), 'Repeated GPU timing presented as independent sample'
        metrics = {m:summarize([s[m] for s in samples]) for m in METRICS}
        checks = {}
        for metric, setting in budget_map.items():
            stats = metrics[metric]
            value = stats['p95'] if metric.endswith('_ms') else stats['maximum']
            checks[metric] = dict(budget=definition[setting], statistic='p95' if metric.endswith('_ms') else 'maximum',
                                  status='UNAVAILABLE' if stats['available'] < len(samples)*.95 else 'PASS' if value <= definition[setting] else 'FAIL')
        rows.append(dict(case_id=case['id'],metrics=metrics,budget_checks=checks))
    result = dict(schema_version=1,id='visual_profile_analysis_v1',cases=rows,
                  limitations='One high-end Windows Development player; detached ten-athlete fixture. Not release/minimum hardware or full Mass Start qualification. FrameTimingManager is delayed; timings include waits.',
                  budget_status='PASS' if all(c['status']=='PASS' for r in rows for c in r['budget_checks'].values()) else 'ACTION_REQUIRED')
    (root/'analysis.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print('case | frame p95 | main p95 | render p95 | GPU p95 | max draws | max batches | checks')
    for row in rows:
        m=row['metrics']
        values=[m[k]['p95'] for k in ('wall_ms','cpu_main_ms','cpu_render_ms','gpu_ms')]+[m[k]['maximum'] for k in ('draw_calls','batches')]
        failures=','.join(k+':'+v['status'] for k,v in row['budget_checks'].items() if v['status']!='PASS') or 'PASS'
        print(row['case_id']+' | '+' | '.join('N/A' if v is None else f'{v:.3f}' for v in values)+' | '+failures)
    return result

if __name__ == '__main__':
    analyze(Path(sys.argv[1]))
