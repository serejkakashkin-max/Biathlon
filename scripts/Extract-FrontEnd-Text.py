"""Stable data bridge for approved bilingual labels; preserves translation edits."""
import hashlib,json,re
from pathlib import Path
root=Path(__file__).resolve().parents[1]
target=root/'Assets/Resources/Biathlon/UI/front_end_pairs_v2.json'
existing=json.loads(target.read_text(encoding='utf-8-sig')) if target.exists() else {'schema_version':2,'id':'front_end_pairs_v2','locales':['ru','en'],'entries':[]}
rows={(v['source_ru'],v['source_en']):v for v in existing['entries']}
pattern=re.compile(r'\bL\(\s*("(?:\\.|[^"\\])*")\s*,\s*("(?:\\.|[^"\\])*")\s*\)')
for path in sorted((root/'Assets/Scripts/Unity/Presentation').glob('FrontEnd*.cs')):
    for match in pattern.finditer(path.read_text(encoding='utf-8-sig')):
        ru,en=map(json.loads,match.groups())
        if (ru,en) in rows:continue
        key='approved_'+hashlib.sha256((ru+'\0'+en).encode()).hexdigest()[:16]
        rows[(ru,en)]={'key':key,'source_ru':ru,'source_en':en,'values':[ru if l=='ru' else en for l in existing['locales']]}
existing['entries']=sorted(rows.values(),key=lambda v:v['key'])
assert len({v['key'] for v in existing['entries']})==len(rows)
target.write_text(json.dumps(existing,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(f'{len(rows)} approved bilingual labels available as translator-editable keyed data')
