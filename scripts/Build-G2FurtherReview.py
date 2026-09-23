"""Package the 012 Unity review without overwriting the historical 008A evidence. Pillow required."""
from pathlib import Path
import hashlib, html, json, shutil, subprocess
from PIL import Image, ImageChops, ImageDraw

r=Path(__file__).resolve().parent.parent
src=r/'artifacts/g2-owner-review'; run=r/'artifacts/g2-review-012'
out=r/'docs/quality/g2/owner-review-012'; out.mkdir(exist_ok=True)
summary=json.loads((run/'full.json').read_text())['result']['structuredContent']['result']['Summary']
assert summary['Status']=='Passed' and summary['PassedTests']==summary['TotalTests']==155
assert not subprocess.check_output(['git','ls-files','Assets/Resources/Biathlon/RestrictedG2Crowd'],cwd=r,text=True).strip()
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def record(p): return dict(path=p.relative_to(r).as_posix(),bytes=p.stat().st_size,sha256=sha(p))
images=sorted(src.glob('*.png')); assert len(images)==45
parity=[]
for p in images:
    assert Image.open(p).size==(2560,1440)
    shutil.copyfile(p,out/p.name); shutil.copyfile(p.with_suffix('.json'),out/p.with_suffix('.json').name)
    old=json.loads((r/'docs/quality/g2/owner-review'/p.with_suffix('.json').name).read_text())
    new=json.loads(p.with_suffix('.json').read_text())
    diff=max(abs(old[key][c]-new[key][c]) for key in ['camera_position','camera_rotation'] for c in old[key])
    assert diff<1e-4 and old['fov']==new['fov']
    parity.append(dict(id=p.stem,camera_max_component_difference=diff))
assert ImageChops.difference(Image.open(out/'rewind-a.png'),Image.open(out/'rewind-b.png')).getbbox() is None
frames=sorted((src/'motion-frames').glob('*.png')); assert len(frames)==48
motion=[Image.open(p) for p in frames]
assert all(im.size==(1280,720) and max(hi for lo,hi in im.getextrema())>32 for im in motion)
motion[0].save(out/'chase-motion.webp',save_all=True,append_images=motion[1:],duration=[42,42,41]*16,loop=0,quality=90,method=4)
(out/'motion-manifest.json').write_text(json.dumps(dict(schema_version=1,id='g2-review-012-motion',sample_hz=24,source_tick=400,start_seconds=8,frames=[dict(index=i,time=8+i/24,sha256=sha(p)) for i,p in enumerate(frames)]),indent=2)+'\n')
for name in ['full.json','console.json','warnings.json','player.json','profile.json']:
    shutil.copyfile(run/name,out/name)
shutil.copyfile(run/'capture.json',out/'capture-before-harness.json')
for folder,source in [('lighting',r/'artifacts/g2-lighting/review'),('crowd',r/'artifacts/g2-crowd/review'),('composition',r/'artifacts/g2-composition/review')]:
    dest=out/folder;dest.mkdir(exist_ok=True)
    for p in source.glob('*.png'):shutil.copyfile(p,dest/p.name)
profile=r/'artifacts/visual-profile/g2-review-012-final-20260923'
dest=out/'profile';dest.mkdir(exist_ok=True)
for p in profile.glob('*.json'):shutil.copyfile(p,dest/p.name)
analysis=json.loads((profile/'analysis.json').read_text())
assert not any(v['status']=='FAIL' for c in analysis['cases'] for v in c['budget_checks'].values())
player=json.loads((run/'player.json').read_text());assert player['passed'] and len(player['render_checks'])==9
dest=out/'player';dest.mkdir(exist_ok=True)
for p in Path(player['log']).parent.glob('*.json'):shutil.copyfile(p,dest/p.name)
for name in ['build-summary.json','THIRD_PARTY_CREDITS.txt']:shutil.copyfile(r/'Builds/WindowsDevelopment'/name,dest/name)
cards=[]
for p in images:
    name=p.stem
    cards.append(f'<article><h3>{html.escape(name)}</h3><div class="pair"><figure><img loading="lazy" src="../owner-review/{p.name}" alt="008A {name}"><figcaption>008A · до доработок</figcaption></figure><figure><img loading="lazy" src="{p.name}" alt="012 {name}"><figcaption>012 · после доработок · <a href="{name}.json">условия</a></figcaption></figure></div></article>')
page='''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>BIATHLON · G2 review 012</title>
<style>body{margin:0;background:#14212b;color:#e4eef5;font:16px/1.55 system-ui}main{max-width:1500px;margin:auto;padding:28px}a{color:#9edee5}h1{font-size:34px}p{max-width:1100px}.status{padding:16px;background:#493035;border-left:4px solid #eaa892}.pair{display:grid;grid-template-columns:1fr 1fr;gap:14px}figure{margin:0}img{display:block;width:100%;height:auto}article{border-top:1px solid #415362;margin-top:26px;padding-top:12px}nav{display:flex;gap:24px;flex-wrap:wrap}.clip{max-width:1000px}@media(max-width:750px){.pair{grid-template-columns:1fr}main{padding:15px}}</style>
<main><h1>BIATHLON · повторный G2 review</h1><p>23 сентября 2026 · checkpoints009–011 →012 · Windows Development</p>
<p class="status"><strong>G2 NOT APPROVED. Рекомендация: продолжить доработку визуала.</strong><br>Пакет проверок готов; художественный production target не заявляется достигнутым. Решение G2 принимает владелец. M13/WP167 заблокированы.</p>
<nav><a href="../../../reports/G2_REVIEW_012.md">Отчёт и ограничения</a><a href="../../../reports/G2_FURTHER_REMEDIATION_REPORT.md">Что изменено</a><a href="manifest.json">Манифест</a><a href="profile/analysis.json">Профиль</a></nav>
<p>Слева008A, справа012. Все45 камер/FOV совпадают; свет и окружение изменены намеренно. Captures из Unity, без дорисовки. Кадры2560×1440; движущийся клип1280×720,48samples за2секунды.24fps — частота сохранения, не замер FPS. Benchmark —10detached pose fixtures tick400; реальные гонки отдельно проверены Windows smoke. HUD в offscreen изображениях отсутствует.</p>
<h2>Движение и близкие планы</h2><img class="clip" src="chase-motion.webp" alt="Новая лыжная анимация"><p><a href="../owner-review/chase-motion.webp">Прежняя анимация008A</a> · <a href="crowd/near-8.png">Толпа вблизи</a> · <a href="lighting/dawn-approach.png">Рассвет</a> · <a href="lighting/late_day-approach.png">Вечер</a></p>
'''+''.join(cards)+'</main></html>'
(out/'index.html').write_text(page,encoding='utf-8')
manifest=dict(schema_version=1,id='g2-review-012',git_commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=r,text=True).strip(),capture_count=45,motion_frames=48,camera_parity=parity,rewind_pixel_equal=True,restricted_payload_tracked=False,test_summary=summary,images=[record(out/p.name) for p in images],harness=record(r/'Assets/Tests/PlayMode/G2OwnerReviewTests.cs'),full_result=record(out/'full.json'),motion=record(out/'chase-motion.webp'))
(out/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps(dict(images=45,motion_frames=48,camera_parity=len(parity),rewind_equal=True,tests=summary['PassedTests'])))
