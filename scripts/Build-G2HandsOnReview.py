"""Package captured Unity frames for hands-on checkpoint013; never modify historical reviews."""
from pathlib import Path
import hashlib, html, json, shutil, subprocess
from PIL import Image, ImageChops
root=Path(__file__).resolve().parent.parent
source=root/'artifacts/g2-owner-review';run=root/'artifacts/g2-owner-feedback'
out=root/'docs/quality/g2/owner-review-013';out.mkdir(parents=True,exist_ok=True)
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def record(p):return dict(path=p.relative_to(root).as_posix(),bytes=p.stat().st_size,sha256=sha(p))
summary=read(run/'full-playmode-final.json')['result']['structuredContent']['result']['Summary']
assert summary['Status']=='Passed' and summary['PassedTests']==summary['TotalTests']==158
assert not subprocess.check_output(['git','ls-files','Assets/Resources/Biathlon/RestrictedG2Crowd'],cwd=root,text=True).strip()
images=sorted(source.glob('*.png'));assert len(images)==45 and (source/'capture-complete.txt').exists()
parity=[]
for p in images:
 assert Image.open(p).size==(2560,1440)
 shutil.copyfile(p,out/p.name);shutil.copyfile(p.with_suffix('.json'),out/p.with_suffix('.json').name)
 old=read(root/'docs/quality/g2/owner-review-012'/p.with_suffix('.json').name);new=read(p.with_suffix('.json'))
 diff=max(abs(old[k][c]-new[k][c]) for k in ('camera_position','camera_rotation') for c in old[k])
 assert diff<1e-4 and old['fov']==new['fov'];parity.append(dict(id=p.stem,camera_max_component_difference=diff))
assert ImageChops.difference(Image.open(out/'rewind-a.png'),Image.open(out/'rewind-b.png')).getbbox() is None
frames=sorted((source/'motion-frames').glob('*.png'));assert len(frames)==48
motion=[Image.open(p) for p in frames];assert all(im.size==(1280,720) for im in motion)
motion[0].save(out/'chase-motion.webp',save_all=True,append_images=motion[1:],duration=[42,42,41]*16,loop=0,quality=90,method=4)
(out/'motion-manifest.json').write_text(json.dumps(dict(schema_version=1,id='g2-review-013-motion',sample_hz=24,source_tick=400,start_seconds=8,frames=[dict(index=i,time=8+i/24,sha256=sha(p)) for i,p in enumerate(frames)]),indent=2),encoding='utf-8')
for name in ('full-playmode-final.json','device-unity.json','feedback.trx','mass-range.json','console-final.json','warnings-final.json','player-build-run.json'):
 shutil.copyfile(run/name,out/name)
if (run/'full-core.trx').exists():shutil.copyfile(run/'full-core.trx',out/'full-core.trx')
profile=root/'artifacts/visual-profile/g2-hands-on-013-20260924';(out/'profile').mkdir(exist_ok=True)
for p in profile.glob('*.json'):shutil.copyfile(p,out/'profile'/p.name)
player=read(run/'player-build-run.json');assert player['passed'] and len(player['render_checks'])==9
(out/'player').mkdir(exist_ok=True)
for p in Path(player['log']).parent.glob('*.json'):shutil.copyfile(p,out/'player'/p.name)
for name in ('build-summary.json','THIRD_PARTY_CREDITS.txt'):shutil.copyfile(root/'Builds/VisualReviewWindows'/name,out/'player'/name)
shutil.copyfile(profile/'high-athlete.png',out/'player/high-athlete.png')
shutil.copyfile(root/'artifacts/g2-crowd/review/near-8.png',out/'crowd-near.png')
cards=[]
for p in images:
 name=html.escape(p.stem)
 cards.append(f'<article><h3>{name}</h3><div class="pair"><figure><img loading="lazy" src="../owner-review-012/{p.name}" alt="012 {name}"><figcaption>012 — предыдущая версия</figcaption></figure><figure><img loading="lazy" src="{p.name}" alt="013 {name}"><figcaption>013 — текущая версия · <a href="{p.stem}.json">условия кадра</a></figcaption></figure></div></article>')
page='''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>BIATHLON — доработки 013</title>
<style>body{margin:0;background:#14212b;color:#e4eef5;font:16px/1.55 system-ui}main{max-width:1500px;margin:auto;padding:28px}a{color:#9edee5}.status{padding:16px;background:#493035;border-left:4px solid #eaa892}.pair{display:grid;grid-template-columns:1fr 1fr;gap:14px}figure{margin:0}img{display:block;width:100%;height:auto}article{border-top:1px solid #415362;margin-top:26px;padding-top:12px}.clip{max-width:1000px}@media(max-width:750px){.pair{grid-template-columns:1fr}}</style>
<main><h1>BIATHLON — доработки после игрового теста</h1><p>24 сентября 2026 · checkpoint013</p>
<p class="status"><strong>G2 не принят. Визуальная цель ещё не достигнута.</strong><br>Исправлены возврат в центр при отпускании A/D и видимый размер мишеней. Добавлены четыре фасона одежды, изменена фаза скольжения и восстановлены подробные тени High. Посадка одежды, реалистичность движений и въезд на рубеж требуют дальнейшей работы.</p>
<p><a href="../../../reports/G2_HANDSON_REMEDIATION_013.md">Результаты и ограничения</a> · <a href="../../../reports/G2_OPEN_ITEMS.md">Открытые проблемы</a> · <a href="profile/analysis.json">Замеры графики</a> · <a href="manifest.json">Состав пакета</a></p>
<p>45 кадров из Unity. Камеры совпадают с 012. Движущийся клип — 48 кадров за 2 секунды, это частота записи, не замер скорости игры. Новое окружение пока доступно как демонстрационная площадка; Sprint и Mass Start используют прежние игровые трассы.</p>
<h2>Лыжный цикл</h2><img class="clip" src="chase-motion.webp" alt="Текущий лыжный цикл"><p>Удлинено скольжение на опорной лыже. Анимация остаётся процедурной; художественная приёмка не заявляется.</p>
<h2>Зимняя одежда зрителей</h2><img src="crowd-near.png" alt="Матовый и глянцевый пуховики, куртка и пальто"><p>Все локальные фасоны включены. На крупном плане ещё видны упрощённые материалы, посадка манжет и повторяемость лиц.</p>
'''+''.join(cards)+'</main></html>'
(out/'index.html').write_text(page,encoding='utf-8')
manifest=dict(schema_version=1,id='g2-owner-review-013',git_commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),working_tree_dirty=True,capture_count=45,motion_frames=48,camera_parity=parity,rewind_pixel_equal=True,restricted_payload_tracked=False,test_summary=summary,images=[record(out/p.name) for p in images],motion=record(out/'chase-motion.webp'))
(out/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('Packaged 45 captures, 48 motion frames; no G2 approval implied.')
