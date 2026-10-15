"""Package native player portraits, invariants and matched face-diversity profiling."""
import hashlib
import html
import json
import statistics
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'artifacts/m15-5-faces'
OUTPUT = ROOT / 'docs/quality/m15-5/faces'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def save(name, value):
    (OUTPUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')


def package():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    review = read(SOURCE / 'portraits-seam-fixed/review.json')
    assert review['success'] and not review['errors'] and len(review['portraits']) == 14
    records = []
    for portrait in review['portraits']:
        name = portrait['file']
        assert Path(name).name == name
        content = (SOURCE / 'portraits-seam-fixed' / name).read_bytes()
        (OUTPUT / name).write_bytes(content)
        records.append(dict(**portrait, sha256=hashlib.sha256(content).hexdigest()))
    save('native-portraits.json', dict(schema_version=1, id='face_diversity_native_portraits',
         method='Native Unity player 640x800; identical studio lighting and neutral existing build. Six front profiles share skin tone1. Originals unretouched.', portraits=records))

    # Contact-sheet layout only: resize native frames, add captions; retain each original.
    font = ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf', 22)
    sheet = Image.new('RGB', (1280, 864), '#142530')
    draw = ImageDraw.Draw(sheet)
    for i in range(8):
        with Image.open(OUTPUT / f'skin-{i}.png') as original:
            sheet.paste(original.resize((320, 400), Image.Resampling.LANCZOS), ((i % 4)*320, (i//4)*432))
        draw.text(((i % 4)*320+12, (i//4)*432+400), f'Тон {i+1}', font=font, fill='#E7F2F5')
    sheet.save(OUTPUT / 'skin-overview.jpg', quality=94)

    selections = ('face-authoring', 'AthleteFaceTests', 'G2AppearanceTests', 'CareerRaceAppearanceTests',
                  'VisualUpgradeIntegrationTests', 'G2HeroTests', 'G2JointVolumeTests', 'G2MotionTests',
                  'HybridSkiMotionTests', 'SprintCarryRefinementTests', 'AthleteShootingAlignmentTests',
                  'AthleteStandingTests', 'AthleteProneTests', 'rifle-contact-13', 'rifle-contact-14', 'rifle-contact-15')
    tests, names = [], set()
    for selection in selections:
        data = read(SOURCE / f'tests-{selection}.json')['result']['structuredContent']['result']
        assert data['Summary']['Status'] == 'Passed' and not data['Summary']['FailedTests'] and not data['Summary']['SkippedTests']
        for result in data['Results']:
            assert result['Status'] == 'Passed' and result['Name'] not in names
            names.add(result['Name'])
        tests.append(dict(selection=selection, mode='EditMode' if selection == 'face-authoring' else 'PlayMode', data=data))
    assert len(names) == 71
    save('tests.json', dict(schema_version=1, id='athlete_faces_native_tests', passed=71, failed=0, skipped=0,
         scope='69 selected PlayMode cases +2 EditMode cases,14 fixtures. Not a full420-case PlayMode rerun. Repeated passes are not added.', results=tests))
    (OUTPUT / 'first-joint-fixture.json').write_bytes((SOURCE / 'tests-G2JointVolumeTests-first.json').read_bytes())
    (OUTPUT / 'midline-red.json').write_bytes((SOURCE / 'face-midline-red-all-lods.json').read_bytes())
    (OUTPUT / 'narrow-before-fix.png').write_bytes((SOURCE / 'portraits-verified/face-narrow.png').read_bytes())

    before = read(ROOT / 'docs/quality/m15-5/sprint-carry-refinement/observations.json')
    after = read(SOURCE / 'observations.json')
    assert len(before['cases']) == len(after['cases']) == 6
    unchanged = []
    for a, b in zip(before['cases'], after['cases']):
        assert a['id'] == b['id'] and a['body_sha256'] == b['body_sha256']
        unchanged.append(dict(case=a['id'], before_sha256=a['body_sha256'], after_sha256=b['body_sha256']))
    save('motion-preservation.json', dict(schema_version=1, id='faces_approved_motion_preservation',
         method='Same6 moving-root observations,360 frames at120Hz. Exact body/leg transform stream hashes; scoped preservation check.', cases=unchanged))
    save('observations.json', after)

    metrics, texture_counts = {}, []
    for phase in ('baseline-perf', 'final-perf'):
        receipt = read(SOURCE / phase / 'review.json')
        assert receipt['success'] and not receipt['errors'] and len(receipt['samples']) == 240
        assert (receipt['width'], receipt['height'], receipt['athletes'], receipt['trees']) == (1920, 1080, 30, 2588)
        texture_counts.append(receipt['texture_bytes'])
        values = {}
        for key in ('cpu_main_ms', 'cpu_render_ms', 'gpu_ms', 'gc_bytes', 'draw_calls', 'triangles'):
            samples = sorted(s[key] for s in receipt['samples'] if s[key] >= 0)
            values[key] = dict(mean=statistics.mean(samples), p95=samples[int(.95*(len(samples)-1))], samples=len(samples)) if samples else None
        poses = [x/1e6 for x in receipt['animation_ns'] if x >= 0]
        values['animation_ms'] = dict(mean=statistics.mean(poses), maximum=max(poses))
        metrics[phase] = values
        save(phase+'.json', receipt)
    assert texture_counts[0] == texture_counts[1]
    save('performance.json', dict(schema_version=1, id='face_diversity_performance', texture_bytes=texture_counts[0], results=metrics,
         method='Sequential matched Ultra1920x1080,30 athletes/2588 trees;120 warmup+240 sampled frames. No HUD/swapchain cost; GPU/render-thread timing unavailable. One scoped pair, not a general speedup/60FPS claim.'))
    field = read(SOURCE / 'native-field-seam-fixed/review.json')
    assert field['success'] and not field['errors'] and field['final_tick'] == 1000
    save('native-field.json', field)
    for name in ('start-acceleration', 'first-turn', 'uphill'):
        (OUTPUT / (name+'.png')).write_bytes((SOURCE / 'native-field-seam-fixed' / name / '0020.png').read_bytes())
    for kind in ('Error', 'Exception', 'Assert'):
        data = read(SOURCE / f'console-{kind}-idle.json')
        assert not data['result']['structuredContent']['result']
        save('console-'+kind+'.json', data)
    detail=read(SOURCE/'console-exception-detail.json')
    entries=detail['result']['structuredContent']['result']
    assert entries and all('com.IvanMurzak.Unity.MCP.Runtime.Utils.UnityMainThread' in e['StackTrace'] for e in entries)
    save('editor-tooling-exceptions.json', detail)
    build = ROOT / 'Builds/M15_5_Faces_Review'
    summary, smoke = read(build / 'build-summary.json'), read(build / 'startup-smoke.json')
    assert summary['result'] == 'Succeeded' and summary['errors'] == 0 and smoke['success'] and smoke['fullscreen_mode'] == 'FullScreenWindow'
    save('build-summary.json', summary)
    save('startup-smoke.json', smoke)
    files = ('Assets/Resources/Biathlon/athlete_faces_v1.json',
             'Assets/Scripts/Unity/Presentation/AthleteFaceCatalog.cs', 'Assets/Scripts/Unity/Presentation/AthleteAppearancePresentation.cs',
             'Assets/Scripts/Unity/Presentation/AthleteFaceReview.cs', 'Assets/Scripts/Unity/Editor/AthleteFaceAuthoring.cs',
             'Assets/Scripts/Unity/Editor/VisualUpgradeHeroAuthoring.cs', 'Assets/Scripts/Unity/Editor/PlayerBuildVisualUpgrade.cs',
             'Assets/Tests/PlayMode/AthleteFaceTests.cs', 'Assets/Tests/Editor/AthleteFaceAuthoringTests.cs') + tuple(
                 f'Assets/Resources/Biathlon/Athletes/G2Hero/Meshes/HeroLOD{i}.asset' for i in range(3))
    binaries = ('Biathlon.exe', 'Biathlon_Data/Managed/Biathlon.Unity.Presentation.dll', 'Biathlon_Data/resources.assets')
    save('source-hashes.json', dict(schema_version=1, id='athlete_faces_source', source_commit='74baab00', feature_commit='e5c65d68', baseline_commit='221c4ccd',
         source_sha256={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in files},
         binary_sha256={p:hashlib.sha256((build/p).read_bytes()).hexdigest() for p in binaries}))

    titles = dict(oval='Овальное', angular='Угловатое', round='Округлое', narrow='Узкое', broad='Широкое', defined_chin='Выраженный подбородок')
    def card(p):
        title = f'Тон {int(p["file"][5])+1}' if p['file'].startswith('skin-') else titles[p['face']]
        return f'<figure><img loading="lazy" src="{html.escape(p["file"])}" alt="{title}"><figcaption>{title}</figcaption></figure>'
    cards = ''.join(card(p) for p in records[:8])
    faces = ''.join(card(p) for p in records[8:])
    page = '''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>BIATHLON — лица спортсменов</title><style>
    :root{color-scheme:dark}*{box-sizing:border-box}body{margin:0;background:#0b1821;color:#edf5f6;font:17px system-ui}main{max-width:1400px;margin:auto;padding:32px}h1{font-size:40px;margin:8px 0 20px}h2{margin-top:42px;font-size:26px}p{color:#c1d1dc;max-width:1050px;line-height:1.6}a{color:#70e4d5}.grid{display:grid;grid-template-columns:repeat(4,1fr);gap:16px}figure{margin:0;background:#182d38;border-radius:12px;overflow:hidden}img{display:block;width:100%;height:auto}figcaption{padding:12px}footer{margin-top:40px;color:#a6bfce}@media(max-width:800px){.grid{grid-template-columns:repeat(2,1fr)}}
    </style><main><p>BIATHLON / M15.5</p><h1>У каждого спортсмена — постоянная внешность</h1>
    <p>Восемь оттенков кожи и шесть форм лица на существующей модели G2. Внешность определяется ID спортсмена, цвет комбинезона — сборной. Одобренное движение и положение винтовки сохранены. Это настоящие кадры тестового билда, без ретуши.</p>
    <h2>Оттенки кожи</h2><div class="grid">'''+cards+'''</div><h2>Формы лица</h2><p>Одинаковый оттенок кожи, освещение и прежний нейтральный вариант телосложения. Показаны различия щёк, челюсти, подбородка и носа.</p><div class="grid">'''+faces+'''</div>
    <footer><a href="tests.json">71 проверка</a> · <a href="motion-preservation.json">Сохранение движения</a> · <a href="performance.json">Нагрузка и границы замера</a> · <a href="source-hashes.json">Версия и хэши</a></footer></main></html>'''
    (OUTPUT / 'review.html').write_text(page, encoding='utf-8')
    print(json.dumps(dict(tests=71, native_portraits=14, motion_streams_unchanged=6, performance=metrics), indent=2))


if __name__ == '__main__':
    package()
