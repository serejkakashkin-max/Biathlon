"""Package unedited Unity evidence and the matching Windows build for checkpoint 015."""
from pathlib import Path
import hashlib
import html
import json
import shutil
import subprocess
from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / 'artifacts/g2-owner-review'
RUN = ROOT / 'artifacts/g2-hero-face'
OUT = ROOT / 'docs/quality/g2/owner-review-015'
PROFILE = ROOT / 'artifacts/visual-profile/g2-review-015-20260927'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def record(path):
    return dict(path=path.relative_to(ROOT).as_posix(), bytes=path.stat().st_size, sha256=digest(path))

summary = read(RUN / 'full-release-tests.json')['result']['structuredContent']['result']['Summary']
assert summary['Status'] == 'Passed' and summary['PassedTests'] == summary['TotalTests'] == 164
for restricted in ('RestrictedG2Hero', 'RestrictedG2Crowd'):
    assert not subprocess.check_output(['git', 'ls-files', f'Assets/Resources/Biathlon/{restricted}'], cwd=ROOT, text=True).strip()
player = read(RUN / 'player-build-run.json')
assert player['passed'] and len(player['render_checks']) == 9 and player['visual_review_startup']
assert read(PROFILE / 'execution.json')['passed']
images = sorted(SOURCE.glob('*.png'))
assert len(images) == 45 and (SOURCE / 'capture-complete.txt').exists()
OUT.mkdir(parents=True, exist_ok=True)
parity = []
for path in images:
    assert Image.open(path).size == (2560, 1440)
    for item in (path, path.with_suffix('.json')):
        shutil.copyfile(item, OUT / item.name)
    old = read(ROOT / 'docs/quality/g2/owner-review-013' / path.with_suffix('.json').name)
    new = read(path.with_suffix('.json'))
    delta = max(abs(old[key][component] - new[key][component]) for key in ('camera_position', 'camera_rotation') for component in old[key])
    assert delta < 1e-4 and old['fov'] == new['fov']
    parity.append(dict(id=path.stem, camera_max_component_difference=delta))
assert ImageChops.difference(Image.open(OUT / 'rewind-a.png'), Image.open(OUT / 'rewind-b.png')).getbbox() is None
frames = sorted((SOURCE / 'motion-frames').glob('*.png'))
assert len(frames) == 48
motion = [Image.open(path) for path in frames]
assert all(frame.size == (1280, 720) for frame in motion)
motion[0].save(OUT / 'chase-motion.webp', save_all=True, append_images=motion[1:], duration=[42, 42, 41] * 16, loop=0, quality=90, method=4)
(OUT / 'motion-manifest.json').write_text(json.dumps(dict(schema_version=1, id='g2-review-015-motion', sample_hz=24, source_tick=400, frames=[dict(time=8+i/24, sha256=digest(p)) for i,p in enumerate(frames)]), indent=2), encoding='utf-8')
for name in ('full-release-tests.json', 'player-build-run.json', 'console-final.json', 'warnings-final.json'):
    shutil.copyfile(RUN / name, OUT / name)
for pose in (3, 4, 5, 6):
    shutil.copyfile(ROOT / f'artifacts/g2-hero/review/hero-pose-{pose}.png', OUT / f'hero-pose-{pose}.png')
for folder in ('profile', 'player'):
    (OUT / folder).mkdir(exist_ok=True)
for path in PROFILE.glob('*.json'):
    shutil.copyfile(path, OUT / 'profile' / path.name)
for path in Path(player['log']).parent.glob('*.json'):
    shutil.copyfile(path, OUT / 'player' / path.name)
for name in ('build-summary.json', 'THIRD_PARTY_CREDITS.txt'):
    shutil.copyfile(ROOT / 'Builds/VisualReviewWindows' / name, OUT / 'player' / name)
shutil.copyfile(PROFILE / 'high-athlete.png', OUT / 'player/high-athlete.png')
cards = []
for path in images:
    title = html.escape(path.stem)
    cards.append(f'<article><h3>{title}</h3><div class="pair"><figure><img loading="lazy" src="../owner-review-013/{path.name}" alt="013 {title}"><figcaption>013 — предыдущая сборка</figcaption></figure><figure><img loading="lazy" src="{path.name}" alt="015 {title}"><figcaption>015 — текущая сборка · <a href="{path.stem}.json">условия кадра</a></figcaption></figure></div></article>')
page = '''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>BIATHLON — review 015</title>
<style>body{margin:0;background:#14212b;color:#e4eef5;font:16px/1.55 system-ui}main{max-width:1500px;margin:auto;padding:28px}a{color:#9edee5}.status{padding:16px;background:#493035;border-left:4px solid #eaa892}.pair{display:grid;grid-template-columns:1fr 1fr;gap:14px}figure{margin:0}img{display:block;width:100%;height:auto}article{border-top:1px solid #415362;margin-top:26px;padding-top:12px}.clip{max-width:1000px}@media(max-width:750px){.pair{grid-template-columns:1fr}}</style>
<main><h1>BIATHLON — герой и посадка экипировки</h1><p>27 сентября 2026 · checkpoint 015</p>
<p class="status"><strong>G2 не одобрен. M13 заблокирован.</strong><br>Этот пакет показывает проверенную текущую версию и оставшиеся ограничения. Техническое прохождение тестов не заменяет решение владельца о визуальном качестве.</p>
<p>Изменения с 013: более подробное лицо, новая прилегающая шапка, стык шеи, уточнённая траектория рук; нормали и roughness ткани, отражение окружения, более плотный дальний лес. Замечание владельца о слишком большой шапке включено в эту версию.</p>
<p><a href="../../../reports/G2_VISUAL_REMEDIATION_015.md">Отчёт и ограничения</a> · <a href="../../../reports/G2_OPEN_ITEMS.md">Открытые пункты</a> · <a href="profile/analysis.json">Скрытые замеры</a> · <a href="manifest.json">Состав пакета</a></p>
<p>Windows: Builds/VisualReviewWindows/START_VISUAL_REVIEW.cmd. Стартует новая демонстрационная площадка. Sprint и Mass Start всё ещё используют прежние игровые трассы: переход к production venues требует G2. Стрельба и результаты симуляции не перенастраивались.</p>
<p>164/164 PlayMode, 9/9 Windows scene×tier проверок. 45 кадров из Unity с теми же камерами, что в 013. Клип: 48 кадров за 2 секунды; частота записи не является измерением FPS игры. GPU и render-thread counters в скрытой проверке недоступны.</p>
<h2>Посадка шапки и воротника</h2><div class="pair"><img src="hero-pose-5.png" alt="Посадка спереди"><img src="hero-pose-6.png" alt="Посадка сзади"></div>
<h2>Лыжный цикл</h2><img class="clip" src="chase-motion.webp" alt="Лыжный цикл текущей версии"><p>Процедурная анимация. Лицо пока одно; форма костюма, движения, одежда толпы и композиция окружения сохраняют ограничения из отчёта.</p>
''' + ''.join(cards) + '</main></html>'
(OUT / 'index.html').write_text(page, encoding='utf-8')
manifest = dict(schema_version=1, id='g2-owner-review-015', git_commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(), working_tree_dirty=True, capture_count=45, motion_frames=48, camera_parity=parity, rewind_pixel_equal=True, restricted_payload_tracked=False, test_summary=summary, images=[record(OUT/p.name) for p in images], motion=record(OUT/'chase-motion.webp'))
(OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
print('015: 45 captures, 48 motion frames, verified tests/build/profile; G2 is owner-only.')
