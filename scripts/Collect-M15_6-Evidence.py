"""Assemble selected actual test verdicts; Unity inventory is never an executed count."""
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/quality/m15-6'
audit=json.loads((OUT/'evidence-audit-checkpoint.json').read_text(encoding='utf-8-sig'))
cases={c['name']:dict(c) for c in audit['cases']}
def results(value):
    if isinstance(value,dict):
        if isinstance(value.get('Results'),list):
            yield from value['Results'];return
        if value.get('type')=='text':
            try:yield from results(json.loads(value['text']))
            except (ValueError,KeyError):pass
            return
        for key in ['structuredContent','result','content']:
            if key in value:
                found=list(results(value[key]))
                if found:yield from found;return
    elif isinstance(value,list):
        for item in value:yield from results(item)

files=['rifle-height-final.json','appearance-final.json','appearance-authoring-final.json',
       'arms-final.json','joints-final.json','integration-final.json','studio-expanded-final.json',
       'creator-expanded-final.json','creator-compact-final.json','face-expanded-final.json',
       'demo-final.json','replay-compat/native-final.json']
for name in files:
    path=OUT/name
    if not path.exists():continue
    for r in results(json.loads(path.read_text(encoding='utf-8-sig'))):
        cases[r['Name']]={'name':r['Name'],'status':r['Status'],'evidence':name}
counts={mode:{status:0 for status in ['Passed','Failed','Skipped']} for mode in ['EditMode','PlayMode']}
for c in cases.values():
    mode='PlayMode' if '.PlayModeTests.' in c['name'] else 'EditMode'
    counts[mode][c['status']]+=1
core=ET.parse(OUT/'final-career-core.trx').getroot()
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
counters=core.find('.//t:Counters',ns).attrib
native_player=OUT/'standalone/review.json'
build=ROOT/'Builds/M15_6_Career_Creator_Review/build-summary.json'
receipt=json.loads(build.read_text(encoding='utf-8-sig')) if build.exists() else None
smoke=json.loads(native_player.read_text(encoding='utf-8-sig')) if native_player.exists() else None
verification={'schema_version':1,'id':'m15_6_delivery_verification',
 'milestone_status':'owner_review' if receipt and receipt['result']=='Succeeded' and smoke and smoke['success'] else 'integration_in_progress',
 'scope':'Cumulative selected native checkpoints, with latest per-case replacement; not one full final-source suite. Historical red evidence remains retained separately.',
 'native_unique_cases':len(cases),'native':counts,'cases':sorted(cases.values(),key=lambda c:c['name']),
 'excluded_obsolete_counter_case':audit['excluded_obsolete_counter_case'],
 'coreclr_final_career':{'total':int(counters['total']),'passed':int(counters['passed']),'failed':int(counters['failed']),'evidence':'final-career-core.trx'},
 'focused_replay_and_course':'replay-compat/replay-policy-regression.trx',
 'authoritative_races':json.loads((OUT/'authoritative-races/final-validation.json').read_text(encoding='utf-8-sig')),
 'development_scenarios':{s:json.loads((OUT/f'development-study/{s}/summary.json').read_text(encoding='utf-8-sig')) for s in ['control','junior','national','world_cup','catalog_low','catalog_high']},
 'build':receipt,'standalone':smoke,'demo_video':'creator-demo.mp4' if (OUT/'creator-demo.mp4').exists() else None,
 'limitations':['Real athlete identities/media are not bundled; authentic fictional catalog and reviewed pack contract are shipped.',
 'Development lab uses real shared training and aggregate race load, not thousands of authoritative races; controlled entrants freeze after individual retirement.',
 'Three fixed race seeds are regression samples, not universal preset win rates. Old recorded v25 races intentionally retain old physics.',
 'Hair is limited to two short styles; facial hair is not advertised. Final owner testing determines milestone closure.']}
(OUT/'verification.json').write_text(json.dumps(verification,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'native':counts,'unique':len(cases),'status':verification['milestone_status']},ensure_ascii=False))
