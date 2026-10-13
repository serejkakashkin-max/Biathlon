"""Reversible, exact-path baseline-art swap; never modifies foreign workspace changes.
Run prepare then restore immediately after the native baseline build. Retain backup.
"""
import json, subprocess, sys, hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1]
backup=root/'artifacts/m15-5-baseline-asset-backup'
paths=[f'Assets/Shaders/{x}.shader' for x in ('AthleteSurface','Snow','SnowContact','SnowSpray','BenchmarkSky')]
paths += [f'Assets/Resources/Biathlon/Athletes/G2Hero/Meshes/HeroLOD{i}.asset' for i in range(3)]
paths += [f'Assets/Resources/Biathlon/Athletes/G2Hero/Materials/{x}.mat' for x in ('SuitNavy','SuitTeal','CapNavy','Skin','GloveRubber','BootComposite','GoggleLens')]
paths += [f'Assets/Resources/Biathlon/{x}.json' for x in ('g2_forest_composition_v1','snow_material_v1','terrain_blend_v1','snow_spray_v1','weather_visual_v1')]
mode=sys.argv[1]
if mode=='prepare':
    if (backup/'pending.json').exists(): raise SystemExit('Restore the pending baseline asset swap first.')
    backup.mkdir(parents=True,exist_ok=True)
    receipt=[]
    for relative in paths:
        p=root/relative;data=p.read_bytes();saved=backup/relative;saved.parent.mkdir(parents=True,exist_ok=True);saved.write_bytes(data)
        receipt.append({'path':relative,'sha256':hashlib.sha256(data).hexdigest()})
    (backup/'pending.json').write_text(json.dumps(receipt,indent=2))
    for relative in paths:
        data=subprocess.run(['git','show','d95ef065:'+relative],cwd=root,check=True,capture_output=True).stdout
        if relative.endswith('snow_spray_v1.json'):
            d=json.loads(data.decode('utf-8-sig'))
            d.update(push_min=1,edge_gain=0,attack_gain=0,fresh_gain=1,ice_gain=1,performance_draw_budget=64,high_draw_budget=256,ultra_draw_budget=512)
            data=json.dumps(d,indent=2).encode()
        (root/relative).write_bytes(data)
    print('20 own asset paths backed up; original art active for baseline build only.')
elif mode=='restore':
    pending=backup/'pending.json';receipt=json.loads(pending.read_text())
    for item in receipt:
        data=(backup/item['path']).read_bytes()
        if hashlib.sha256(data).hexdigest()!=item['sha256']:raise SystemExit('Backup checksum mismatch: '+item['path'])
        (root/item['path']).write_bytes(data)
    pending.replace(backup/'restored.json')
    print('All 20 candidate assets restored byte-for-byte.')
else:raise SystemExit('prepare|restore')
