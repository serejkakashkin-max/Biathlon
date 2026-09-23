"""Package actual Unity captures for owner review; requires Pillow, no synthesized imagery."""
from pathlib import Path
import hashlib, html, json, math, shutil, subprocess
from PIL import Image, ImageChops, ImageDraw

root = Path(__file__).resolve().parent.parent
source = root / 'artifacts/g2-owner-review'
output = root / 'docs/quality/g2/owner-review'
output.mkdir(parents=True, exist_ok=True)
summary = json.loads((source/'capture-test-final.json').read_text())['result']['structuredContent']['result']['Summary']
assert summary['PassedTests'] == 1 and summary['FailedTests'] == 0
assert (source/'capture-complete.txt').exists()
assert not subprocess.check_output(['git','ls-files','Assets/Resources/Biathlon/RestrictedG2Crowd'],cwd=root,text=True).strip()
images = sorted(source.glob('*.png'))
assert len(images) == 45
for image in images:
    assert Image.open(image).size == (2560,1440)
    shutil.copyfile(image,output/image.name)
    shutil.copyfile(image.with_suffix('.json'),output/image.with_suffix('.json').name)
for name in ['capture-test-final.json','rejected-chase-capture.json','console-state.json','console-warnings.json','test-results.xml']:
    shutil.copyfile(source/name,output/name)

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def record(path): return dict(path=path.relative_to(root).as_posix(),bytes=path.stat().st_size,sha256=sha(path))

frames = sorted((source/'motion-frames').glob('*.png'))
assert len(frames) == 48
motion = [Image.open(p) for p in frames]
assert all(im.size == (1280,720) and max(hi for lo,hi in im.getextrema()) > 32 for im in motion), 'Missing rendered motion frame'
motion[0].save(output/'chase-motion.webp',save_all=True,append_images=motion[1:],duration=[42,42,41]*16,loop=0,quality=90,method=4)
motion_manifest = dict(schema_version=1,id='g2-review-motion-v1',kind='detached benchmark observation, production chase camera, no HUD',source_tick=400,start_seconds=8,sample_hz=24,frames=[dict(index=i,time=8+i/24,sha256=sha(p)) for i,p in enumerate(frames)],output=record(output/'chase-motion.webp'),width=1280,height=720,encoding='animated WebP quality90; raw PNG frames retained locally')
(output/'motion-manifest.json').write_text(json.dumps(motion_manifest,indent=2)+'\n',encoding='utf-8')

before = root/'docs/quality/m12/wp165/matrix'
comparisons=[]
for path in sorted(before.glob('*.json')):
    if path.stem in ['cases','manifest']:continue
    old=json.loads(path.read_text());new=json.loads((output/path.name).read_text());c=old['camera'];p=new['camera_position'];q=new['camera_rotation']
    delta=max([abs(c[k]-p[k]) for k in 'xyz']+[abs(c['q'+k]-q[k]) for k in 'xyzw']+[abs(c['fov']-new['fov'])])
    assert delta < 1e-4 and old['time']==new['time'] and old['tier']==new['tier'] and old['weather']==new['weather']
    comparisons.append(dict(id=path.stem,camera_max_component_difference=delta,before=record(path.with_suffix('.png')),after=record(output/path.with_suffix('.png').name)))
assert len(comparisons)==30
assert ImageChops.difference(Image.open(output/'rewind-a.png'),Image.open(output/'rewind-b.png')).getbbox() is None

comparison_folder=output/'comparisons';comparison_folder.mkdir(exist_ok=True)
for name in ['balanced-clear-athlete','balanced-clear-approach','balanced-snowfall-targets']:
    pair=Image.new('RGB',(1920,574),'#15222c');draw=ImageDraw.Draw(pair)
    for x,path,label in [(0,before/(name+'.png'),'BEFORE / WP-165'),(960,output/(name+'.png'),'AFTER / G2 remediation')]:
        im=Image.open(path);im.thumbnail((960,540));pair.paste(im,(x,34));draw.text((x+12,10),label,fill='white')
    pair.save(comparison_folder/(name+'.png'))

evidence=dict(schema_version=1,id='g2-owner-review-capture-manifest-v1',git_commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),working_tree_dirty=bool(subprocess.check_output(['git','status','--porcelain'],cwd=root,text=True).strip()),capture_count=45,motion_frames=48,rewind_pixel_equal=True,camera_comparisons=comparisons,images=[record(output/p.name) for p in images],capture_harness=record(root/'Assets/Tests/PlayMode/G2OwnerReviewTests.cs'),motion_manifest=record(output/'motion-manifest.json'),runtime_build_manifest='../lighting-a/player/build-files.json',restricted_payload_tracked=False)
(output/'manifest.json').write_text(json.dumps(evidence,indent=2)+'\n',encoding='utf-8')

cards=[]
for image in images:
    name=image.stem;metadata=json.loads((source/(name+'.json')).read_text());old=before/image.name
    if old.exists():
        pair='<div class="pair"><figure><a href="../../m12/wp165/matrix/'+image.name+'"><img loading="lazy" src="../../m12/wp165/matrix/'+image.name+'" alt="До '+name+'"></a><figcaption>До · WP-165</figcaption></figure><figure><a href="'+image.name+'"><img loading="lazy" src="'+image.name+'" alt="После '+name+'"></a><figcaption>После · G2 remediation</figcaption></figure></div>'
    else:
        pair='<figure><a href="'+image.name+'"><img loading="lazy" src="'+image.name+'" alt="'+name+'"></a></figure>'
    cards.append('<article><h3>'+html.escape(name)+'</h3><p>'+html.escape(metadata['kind'])+' · <a href="'+name+'.json">камера / условия</a></p>'+pair+'</article>')
page='''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>BIATHLON · G2 owner review</title>
<style>body{margin:0;background:#111c25;color:#e5edf3;font:16px/1.55 system-ui}main{max-width:1500px;margin:auto;padding:32px}h1{font-size:32px;margin-bottom:8px}h2{margin-top:36px}p{max-width:1080px}a{color:#a8dce5}article{border-top:1px solid #344552;margin-top:26px;padding-top:12px}figure{margin:0}img{max-width:100%;height:auto;display:block}figcaption{padding:6px 0}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}.status{background:#482a2b;padding:16px;border-left:4px solid #e99c87}.clip{max-width:1000px}nav{display:flex;flex-wrap:wrap;gap:18px}@media(max-width:800px){.pair{grid-template-columns:1fr}main{padding:18px}}</style>
<main><h1>BIATHLON · G2 owner review</h1><p>23 сентября 2026 · post-WP166 / pre-G2 · Windows Development</p>
<p class="status"><strong>Рекомендация: G2 FAIL / требуется дальнейшая доработка.</strong><br>Технический этап проверен; целевой визуальный уровень не достигнут. G2 не одобрен, M13/WP167 заблокированы. Решение принадлежит владельцу проекта.</p>
<nav><a href="../../../reports/G2_VISUAL_BENCHMARK_REVIEW.md">Итоговый отчёт</a><a href="../../../reports/G2_OPEN_ITEMS.md">Открытые проблемы</a><a href="manifest.json">Манифест</a><a href="../lighting-a/profile/analysis.json">Профиль</a><a href="../lighting-a/player/verification.json">Windows smoke</a></nav>
<p>45 кадров 2560×1440. У 30 сравнений совпадают позиции, ориентации, FOV, время и quality/weather; различия компонентов камеры меньше 0,000002. Слева прежний WP-165, справа текущее состояние. Свет/материалы намеренно изменены. Старый Game View и новый offscreen output различаются путём вывода; это качественное сравнение, не pixel regression между версиями. Повторный rewind текущей версии пиксельно идентичен.</p>
<p>Benchmark содержит 10 detached pose fixtures из tick400, а не текущую полноценную гонку. Отдельные стрелковые позы — постановочные fixtures. Dedicated aim берёт реальные замороженные AI bouts. Offscreen-кадры не включают HUD/crosshair и не заменяют hands-on оценку стрельбы. Playable tracks остаются greybox; туда окружение benchmark не переносилось.</p>
<h2>Движение с production chase camera</h2><p>2 секунды, 48 кадров, 24 fps,1280×720; WebP quality90. Это частота сохранения, не измеренный FPS игры. По клипу видны механический шаг и недостаточный перенос веса.</p><img class="clip" src="chase-motion.webp" alt="Движение benchmark athlete с chase camera"><p><a href="motion-manifest.json">Время и хеши исходных кадров</a> · <a href="../lighting-a/clear_cold-athlete.png">Clear lighting</a> · <a href="../lighting-a/dawn-athlete.png">Dawn</a> · <a href="../lighting-a/late_day-athlete.png">Late day</a> · <a href="../lighting-a/crowd-near.png">Crowd close</a></p>
<h2>Матрица и близкие планы</h2>'''+''.join(cards)+'</main></html>'
(output/'index.html').write_text(page,encoding='utf-8')
print('Packaged45 stills,48 motion frames,30 matching camera comparisons; rewind exact.')
