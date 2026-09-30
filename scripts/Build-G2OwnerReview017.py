"""Focused motion review: preserve historical016 and copy unedited Unity captures."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
RUN = ROOT / 'artifacts/g2-motion-017'
OUT = ROOT / 'docs/quality/g2/owner-review-017'
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

summary = read(RUN/'full-release-tests.json')['result']['structuredContent']['result']['Summary']
assert summary['Status'] == 'Passed' and summary['TotalTests'] == summary['PassedTests'] == 164
player = read(RUN/'player-build-run.json')
assert player['passed'] and len(player['render_checks']) == 9
build = ROOT/'Builds/VisualReviewWindows'
build_files = read(Path(player['log']).parent/'build-files.json')
assert all(sha(build/entry['path']) == entry['sha256'] for entry in build_files)
OUT.mkdir(parents=True, exist_ok=True)
for name in ('full-release-tests.json','motion-tests.json','exertion-tests.json','full-initial-failed.json','exertion-initial-failed.json','player-build-run.json','console-final.json','warnings-final.json'):
    shutil.copyfile(RUN/name, OUT/name)
for name in ('build-summary.json','THIRD_PARTY_CREDITS.txt','READ_ME_RU.txt'):
    shutil.copyfile(build/name, OUT/name)
shutil.copyfile(Path(player['log']).parent/'build-files.json', OUT/'build-files.json')
clips = []
for name, folder, hz, count in (
    ('benchmark-chase', ROOT/'artifacts/g2-owner-review/motion-frames',24,48),
    ('race-chase', ROOT/'artifacts/g2-corrective-016/race-motion',20,60),
):
    paths = sorted(folder.glob('*.png'))
    assert len(paths) == count
    frames = [Image.open(p) for p in paths]
    assert all(f.size == (1280,720) for f in frames)
    frames[0].save(OUT/(name+'.webp'),save_all=True,append_images=frames[1:],duration=round(1000/hz),loop=0,quality=88,method=4)
    clips.append(dict(id=name,sample_hz=hz,frame_count=count,source_sha256=[sha(p) for p in paths]))
    for f in frames: f.close()
shutil.copyfile(ROOT/'artifacts/g2-hero/review/hero-pose-2.png',OUT/'hero-skiing.png')
page = '''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>BIATHLON — движение 017</title><style>body{background:#14212b;color:#edf4fa;font:17px/1.6 system-ui;margin:0}main{max-width:1200px;margin:auto;padding:26px}a{color:#9edee5}img{max-width:100%;display:block}.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0}figcaption{padding:10px 0}.status{padding:15px;border-left:4px solid #eda887;background:#473238}@media(max-width:800px){.pair{grid-template-columns:1fr}}</style>
<main><h1>G2 · доработка движения 017</h1><p>30 сентября 2026. Руки и корпус по отзыву владельца.</p>
<p class="status">Упрощённая толпа согласована. G2 в целом ожидает решения владельца; M13 не начат.</p>
<p>На равнине и при ускорении руки теперь выполняют один парный толчок на два шага вместо двух. Корпус мягко наклоняется в толчок и возвращается к базовому положению. Уменьшено вертикальное качание таза. Темп ног, скольжение, возврат лыжи и боковой перенос веса сохранены.</p>
<p>Запуск обновлённой сборки: <code>Builds/VisualReviewWindows/START_VISUAL_REVIEW.cmd</code>. Сначала benchmark; Sprint / Mass Start — прежние игровые greybox-трассы.</p>
<p><a href="../../../reports/G2_MOTION_REFINEMENT_017.md">Отчёт</a> · <a href="manifest.json">Manifest</a> · <a href="full-release-tests.json">164 Unity tests</a> · <a href="player-build-run.json">Windows checks</a></p>
<h2>Одинаковые ракурсы: 016 → 017</h2><p>Benchmark: демонстрация по наблюдениям, 48 кадров / 24 Гц.</p>
<div class="pair"><figure><img src="../owner-review-016/benchmark-chase.webp" alt="Benchmark016"><figcaption>016 · до</figcaption></figure><figure><img src="benchmark-chase.webp" alt="Benchmark017"><figcaption>017 · после</figcaption></figure></div>
<p>Настоящая Sprint-гонка: 60 последовательных ticks / 20 Гц. Это не постановочная модель.</p>
<div class="pair"><figure><img src="../owner-review-016/race-chase.webp" alt="Sprint016"><figcaption>016 · до</figcaption></figure><figure><img src="race-chase.webp" alt="Sprint017"><figcaption>017 · после</figcaption></figure></div>
<p>Клипы зациклены; короткий сброс на границе записи не является скачком анимации в игре. Частота записи не измеряет FPS. Скрытые проверки не подтверждают GPU-производительность.</p>
<h2>Поза полного силуэта</h2><img src="hero-skiing.png" alt="Изолированная проверка лыжной позы">
<p>Анимация остаётся процедурной, без mocap. Более спокойный ритм — вариант для hands-on оценки, не утверждение финальной биомеханической точности. Очередь на тестовой карте принята владельцем как ограничение; для production venues записаны отдельные требования к распределению плотной группы. Физика гонки и стрельбы не изменена.</p></main></html>'''
(OUT/'index.html').write_text(page,encoding='utf-8')
manifest = dict(schema_version=1,id='g2-owner-review-017',review_date='2026-09-30',base_commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),working_tree_dirty=True,test_summary=summary,g2='OWNER_DECISION_PENDING',crowd='OWNER_APPROVED_2026-09-30',build_files_verified=len(build_files),clips=clips,
    historical_comparison=[dict(path='../owner-review-016/'+name,sha256=sha(OUT.parent/'owner-review-016'/name)) for name in ('benchmark-chase.webp','race-chase.webp')],
    files=[dict(path=p.name,bytes=p.stat().st_size,sha256=sha(p)) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='manifest.json'])
(OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'017: 164 tests PASS; Windows9/9; {len(build_files)} build hashes verified; 2 comparison clips; owner decision pending.')
