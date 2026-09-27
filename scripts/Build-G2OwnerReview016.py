"""Small owner-directed corrective review. Copies unedited Unity captures only."""
from pathlib import Path
import hashlib
import html
import json
import shutil
import subprocess
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
RUN = ROOT / 'artifacts/g2-corrective-016'
OUT = ROOT / 'docs/quality/g2/owner-review-016'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

summary = read(RUN / 'full-release-tests.json')['result']['structuredContent']['result']['Summary']
assert summary['Status'] == 'Passed' and summary['TotalTests'] == summary['PassedTests'] and summary['TotalTests'] > 160
player = read(RUN / 'player-build-run.json')
assert player['passed'] and len(player['render_checks']) == 9
assert not (ROOT / 'Assets/Resources/Biathlon/RestrictedG2Hero').exists()
assert not subprocess.check_output(['git', 'ls-files', 'Assets/Resources/Biathlon/RestrictedG2Crowd'], cwd=ROOT).strip()
OUT.mkdir(parents=True, exist_ok=True)
shots = [
    ('hero-close.png', ROOT/'artifacts/g2-hero/review/hero-pose-5.png', 'Герой: прежнее простое лицо, без нового баффа'),
    ('hero-skiing.png', ROOT/'artifacts/g2-hero/review/hero-pose-2.png', 'Полный силуэт лыжника; изолированная проверка позы'),
    ('hero-benchmark.png', ROOT/'artifacts/g2-owner-review/hero-front.png', 'Лыжник в benchmark; постановочное наблюдение'),
    ('prone-benchmark.png', ROOT/'artifacts/g2-owner-review/shooting-Prone-side.png', 'Prone на существующем коврике benchmark; постановочная поза'),
    ('standing-benchmark.png', ROOT/'artifacts/g2-owner-review/shooting-Standing-side.png', 'Standing на своей дорожке benchmark; постановочная поза'),
    ('crowd-winter.png', ROOT/'artifacts/g2-crowd/review/near-8.png', 'Зимние зрители: сохранены четыре формы одежды, упрощены лица'),
]
for format in ('Sprint', 'MassStart'):
    for stance in ('Prone', 'Standing'):
        name = f'{format}-{stance}-on-mat.png'
        shots.append((name, RUN/name, f'{format} · {stance}: настоящее наблюдение гонки, без подмены симуляции'))
cards = []
for name, source, caption in shots:
    assert source.exists(), source
    shutil.copyfile(source, OUT/name)
    cards.append(f'<figure><img loading="lazy" src="{name}" alt="{html.escape(caption)}"><figcaption>{html.escape(caption)}</figcaption></figure>')
for name in ('full-release-tests.json', 'player-build-run.json', 'console-final.json', 'warnings-final.json'):
    shutil.copyfile(RUN/name, OUT/name)
for source in RUN.glob('*on-mat.txt'):
    shutil.copyfile(source, OUT/source.name)
clips = []
for name, folder, hz, count in (
    ('benchmark-chase', ROOT/'artifacts/g2-owner-review/motion-frames', 24, 48),
    ('race-chase', RUN/'race-motion', 20, 60),
):
    paths = sorted(folder.glob('*.png'))
    assert len(paths) == count
    frames = [Image.open(path) for path in paths]
    assert all(frame.size == (1280,720) for frame in frames)
    frames[0].save(OUT/(name+'.webp'), save_all=True, append_images=frames[1:], duration=round(1000/hz), loop=0, quality=88, method=4)
    clips.append(dict(id=name, sample_hz=hz, frame_count=count, source_sha256=[sha(p) for p in paths]))
page = '''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>BIATHLON — G2 corrective 016</title><style>body{background:#14212b;color:#edf4fa;font:17px/1.6 system-ui;margin:0}main{max-width:1100px;margin:auto;padding:26px}a{color:#9edee5}img{max-width:100%;display:block}figure{margin:30px 0}figcaption{padding:10px 0}.status{padding:15px;border-left:4px solid #eda887;background:#473238}</style>
<main><h1>G2 · корректирующая сборка 016</h1><p>27 сентября 2026</p>
<p class="status">Готово к решению владельца. G2 не помечен PASS; M13 не начат.</p>
<p>Возвращён герой до неудачной версии 015: прежнее лицо и траектория рук, без нового воротника. Сохранены материалы ткани. У зрителей отключена фотографическая детализация лиц; четыре формы зимней одежды и десять вариантов сохранены.</p>
<p>Таз следует опорной лыже и плавно переносится на другую во время двойной опоры. Длинная фаза скольжения, короткий возврат лыжи, парная работа палок и плавные переходы сохранены. Движение остаётся процедурным, без mocap.</p>
<p>Игровые коврики построены по физическим точкам остановки всех 30 lanes. Во время Setup визуальная модель плавно центрируется на назначенной дорожке. Лыжное боковое смещение симуляции, физическая занятость, правила и sight ray не меняются. Обзорная поза также перенесена с произвольной координаты на центр существующего коврика.</p>
<p><a href="../../../reports/G2_VISUAL_REMEDIATION_016.md">Короткий отчёт</a> · <a href="manifest.json">Manifest</a> · <a href="full-release-tests.json">Unity tests</a> · <a href="player-build-run.json">Windows checks</a></p>
<p>Запуск: <code>Builds/VisualReviewWindows/START_VISUAL_REVIEW.cmd</code>. Новый benchmark открывается первым; Sprint/Mass Start остаются на прежних greybox трассах. Перенос окружения на production venues в этот проход не входит.</p>
<h2>Лыжный ход с chase camera</h2><p>Benchmark — демонстрационная анимация по наблюдениям:</p><img src="benchmark-chase.webp" alt="Benchmark chase motion">
<p>Реальная Sprint-гонка, 60 последовательных simulation ticks / 20 Гц:</p><img src="race-chase.webp" alt="Real Sprint chase motion">
<p>Частота записи клипов не является измерением FPS. Проверки выполнялись скрыто; GPU-производительность не подтверждена.</p>
''' + ''.join(cards) + '<p>Остаются ограничения формы костюма, простой головы, процедурной анимации и статичных рукавов части зрителей. Очередь перед общим входом рубежа не исправлялась. Достаточность результата для G2 определяет владелец.</p></main></html>'
(OUT/'index.html').write_text(page, encoding='utf-8')
manifest = dict(schema_version=1,id='g2-owner-review-016',base_commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),working_tree_dirty=True,test_summary=summary,g2='OWNER_DECISION_PENDING',clips=clips,
    files=[dict(path=p.name,bytes=p.stat().st_size,sha256=sha(p)) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='manifest.json'])
(OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'016: {len(shots)} images, 2 clips; {summary["PassedTests"]} tests; Windows9/9; owner decision pending.')
