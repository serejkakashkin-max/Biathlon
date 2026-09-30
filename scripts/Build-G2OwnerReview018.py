"""Package unedited target-camera captures and verified hidden Windows build."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess

ROOT=Path(__file__).resolve().parent.parent
RUN=ROOT/'artifacts/g2-target-018'
OUT=ROOT/'docs/quality/g2/owner-review-018'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
summary=read(RUN/'full-release-tests.json')['result']['structuredContent']['result']['Summary']
assert summary['Status']=='Passed' and summary['PassedTests']==summary['TotalTests']==166
player=read(RUN/'player-build-run.json');assert player['passed'] and len(player['render_checks'])==9
build=ROOT/'Builds/VisualReviewWindows';build_files=read(Path(player['log']).parent/'build-files.json')
assert all(sha(build/f['path'])==f['sha256'] for f in build_files)
OUT.mkdir(parents=True,exist_ok=True)
for name in ('prone-aim.png','standing-aim.png','closed-aim.png','target-tests.json','full-release-tests.json','geometry-tests.json','player-build-run.json','console-final.json','warnings-final.json'):
    shutil.copyfile(RUN/name,OUT/name)
for name in ('build-summary.json','THIRD_PARTY_CREDITS.txt','READ_ME_RU.txt'):shutil.copyfile(build/name,OUT/name)
shutil.copyfile(Path(player['log']).parent/'build-files.json',OUT/'build-files.json')
page='''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>BIATHLON — мишень018</title><style>body{background:#14212b;color:#edf4fa;font:17px/1.6 system-ui;margin:0}main{max-width:1150px;margin:auto;padding:26px}a{color:#9edee5}img{max-width:100%;display:block}figure{margin:24px 0}figcaption{padding:10px 0}</style>
<main><h1>G2 · мишень018</h1><p>30 сентября2026. Последнее исправление по отзыву владельца после017.</p>
<p>Лёжа: внутри чёрного круга115мм обозначена граница зачётного отверстия45мм. Тонкий серый ободок расположен снаружи зачётной области. Стоя сохраняется большой круг. Белое закрытие скрывает внутреннюю отметку.</p>
<p>Исходные1920×1080 кадры Unity, production aim camera при50м с тем же FOV, без HUD; камера в тесте смотрит точно в центр. Рендер-проверка измеряет различимость границы. Правила попадания не изменены.</p>
<figure><img src="prone-aim.png" alt="Лёжа: внутренний круг45мм"><figcaption>Лёжа — малый зачётный круг внутри большого</figcaption></figure>
<figure><img src="standing-aim.png" alt="Стоя: большой круг115мм"><figcaption>Стоя — большой круг, без внутренней отметки</figcaption></figure>
<figure><img src="closed-aim.png" alt="Закрытые мишени"><figcaption>Закрытие — сплошные светлые диски</figcaption></figure>
<p><a href="../../../reports/G2_TARGET_CORRECTION_018.md">Отчёт</a> · <a href="manifest.json">Manifest</a> · <a href="full-release-tests.json">166 Unity tests</a> · <a href="player-build-run.json">Windows9/9</a> · <a href="../owner-review-017/index.html">Сохранённая анимация017</a></p>
<p>Обновлённая сборка: <code>Builds/VisualReviewWindows/START_VISUAL_REVIEW.cmd</code>. Владелец выразил готовность согласовать G2 после исправления; окончательное APPROVED ещё не записано. Production-трассы после G2 обязаны иметь боковой подъезд и проверенный поток к дорожкам. Тестовая карта сохранена. GPU/FPS-производительность скрытыми проверками не подтверждается.</p></main></html>'''
(OUT/'index.html').write_text(page,encoding='utf-8')
manifest=dict(schema_version=1,id='g2-owner-review-018',review_date='2026-09-30',base_commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),working_tree_dirty=True,test_summary=summary,build_files_verified=len(build_files),g2='OWNER_READY_TO_APPROVE_AFTER_TARGET_CORRECTION',files=[dict(path=p.name,bytes=p.stat().st_size,sha256=sha(p)) for p in sorted(OUT.iterdir()) if p.is_file() and p.name!='manifest.json'])
(OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'018: 166 PlayMode tests; Windows9/9; {len(build_files)} build hashes verified; 3 target captures.')
