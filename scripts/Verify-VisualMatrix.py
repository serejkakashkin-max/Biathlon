"""Check the WP165 capture contract and pixel rewind; uses Pillow and NumPy."""
import hashlib
import json
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

root = Path(sys.argv[1] if len(sys.argv) > 1 else 'docs/quality/m12/wp165/matrix')
cases = json.loads((root / 'cases.json').read_text(encoding='utf-8-sig'))
manifest = json.loads((root / 'manifest.json').read_text(encoding='utf-8-sig'))
assert len(cases) == 30 and len({c['id'] for c in cases}) == 30
expected = {(t, w, s) for t in ('low', 'balanced', 'high') for w in ('clear', 'overcast', 'snowfall') for s in ('approach', 'athlete', 'targets')}
assert {(c['tier'], c['weather'], c['shot']) for c in cases[:27]} == expected
hashes = {v['id']: v['sha256'] for v in manifest['images']}
for case in cases:
    path = root / (case['id'] + '.png')
    with Image.open(path) as image:
        assert image.size == (2560, 1440), (case['id'], image.size)
    assert hashlib.sha256(path.read_bytes()).hexdigest() == hashes[case['id']]
    meta = json.loads((root / (case['id'] + '.json')).read_text(encoding='utf-8-sig'))
    assert meta['source_tick'] == 400 and meta['athletes'] == 10
    for field in ('tier', 'shot', 'weather', 'lighting', 'time'):
        assert meta[field] == case[field], (case['id'], field)
    assert (meta['width'], meta['height']) == (2560, 1440)

def pixels(name):
    with Image.open(root / (name + '.png')) as image:
        return np.array(image.convert('RGB'), dtype=np.int16)

a, away, b = [pixels(name) for name in ('rewind-a', 'rewind-away', 'rewind-b')]
rewind = np.abs(a - b)
result = dict(schema_version=1, id='wp165_visual_matrix_verification_v1', matrix_cases=27,
              rewind_cases=3, width=2560, height=1440,
              motion_changed_pixels=int(np.count_nonzero(np.any(a != away, axis=2))),
              rewind_changed_pixels=int(np.count_nonzero(np.any(rewind != 0, axis=2))),
              rewind_max_channel_delta=int(rewind.max()))
(root.parent / 'verification.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result, indent=2))
assert result['motion_changed_pixels'] > 100
assert result['rewind_changed_pixels'] == 0, 'Explicit-time rewind is not pixel exact on this configuration.'

# Contact sheets aid review without replacing the full-resolution evidence.
for tier in ('low', 'balanced', 'high'):
    sheet = Image.new('RGB', (1536, 960), '#101a24')
    draw = ImageDraw.Draw(sheet)
    for row, weather in enumerate(('clear', 'overcast', 'snowfall')):
        for col, shot in enumerate(('approach', 'athlete', 'targets')):
            name = f'{tier}-{weather}-{shot}'
            with Image.open(root / (name + '.png')) as image:
                sheet.paste(image.resize((512, 288)), (col * 512, row * 320 + 26))
            draw.text((col * 512 + 8, row * 320 + 5), name, fill='white')
    sheet.save(root.parent / (tier + '-overview.png'))
