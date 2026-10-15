"""Package untouched native front/side frames for the owner sprint frequency and rifle carry review."""
import hashlib
import json
import statistics
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'artifacts/m15-5-sprint-carry/contact-correction'
OUTPUT = ROOT / 'docs/quality/m15-5/sprint-carry-refinement'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def package():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    sys.path.insert(0, str(ROOT / 'artifacts/tools/ffmpeg-runtime'))
    import imageio_ffmpeg
    binary = imageio_ffmpeg.get_ffmpeg_exe()
    records, figures = [], []
    for state in ('Flat', 'Uphill', 'MaxSprint', 'Tuck'):
        for angle in ('front', 'side'):
            name = state + '-' + angle
            before, after = ROOT / 'artifacts/m15-5-arms/after' / name, SOURCE / 'after' / name
            for folder in (before, after):
                if len(list(folder.glob('*.png'))) != 90:
                    raise ValueError('Incomplete native capture: ' + str(folder))
            # Pair the same 89 retained frames in both native sequences.
            # Both capture fixtures include URP warm-up and a grade-matched plane.
            destination = OUTPUT / (name + '.mp4')
            subprocess.run([binary, '-hide_banner', '-loglevel', 'error', '-y',
                            '-framerate', '30', '-start_number', '1', '-i', str(before / '%04d.png'),
                            '-framerate', '30', '-start_number', '1', '-i', str(after / '%04d.png'),
                            '-filter_complex', '[0:v][1:v]hstack=inputs=2[v]', '-map', '[v]',
                            '-c:v', 'libx264', '-crf', '19', '-pix_fmt', 'yuv420p',
                            '-movflags', '+faststart', str(destination)], cwd=ROOT, check=True)
            (OUTPUT / (name + '-before.png')).write_bytes((before / '0008.png').read_bytes())
            (OUTPUT / (name + '-after.png')).write_bytes((after / '0008.png').read_bytes())
            record = dict(case=name, paired_frames=89, fps=30, encoded_sha256=hashlib.sha256(destination.read_bytes()).hexdigest())
            for label, folder in (('before', before), ('after', after)):
                record[label] = dict(source=str(folder.relative_to(ROOT)), frames=90,
                                     source_sequence_sha256=hashlib.sha256(b''.join(hashlib.sha256(p.read_bytes()).digest() for p in sorted(folder.glob('*.png')))).hexdigest())
            records.append(record)
            figures.append(f'<figure><figcaption>{name} · слева предыдущий билд / справа исправление</figcaption><video controls loop muted preload="metadata" src="{name}.mp4"></video></figure>')
    for state in ('Flat', 'MaxSprint', 'Tuck'):
        name=state+'-contact';before=SOURCE.parent/'after'/(state+'-side');after=SOURCE/'after'/(state+'-side')
        destination=OUTPUT/(name+'.mp4')
        subprocess.run([binary,'-hide_banner','-loglevel','error','-y','-framerate','30','-start_number','1','-i',str(before/'%04d.png'),
                        '-framerate','30','-start_number','1','-i',str(after/'%04d.png'),'-filter_complex','[0:v][1:v]hstack=inputs=2[v]',
                        '-map','[v]','-c:v','libx264','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(destination)],check=True)
        (OUTPUT/(name+'-before.png')).write_bytes((before/'0008.png').read_bytes())
        contact=dict(case=name,paired_frames=89,fps=30,encoded_sha256=hashlib.sha256(destination.read_bytes()).hexdigest())
        for label,folder in (('before',before),('after',after)):
            contact[label]=dict(source=str(folder.relative_to(ROOT)),frames=90,
                source_sequence_sha256=hashlib.sha256(b''.join(hashlib.sha256(p.read_bytes()).digest() for p in sorted(folder.glob('*.png')))).hexdigest())
        records.append(contact)
        figures.append(f'<figure><figcaption>{name} · слева отклонённое парящее крепление / справа прилегание к спине</figcaption><video controls loop muted preload="metadata" src="{name}.mp4"></video></figure>')
    for state in ('Flat', 'Uphill', 'MaxSprint', 'Tuck'):
        name=state+'-back';folder=SOURCE/'after'/name;frames=sorted(folder.glob('*.png'))
        if len(frames)!=90: raise ValueError('Incomplete native back view')
        destination=OUTPUT/(name+'.mp4')
        subprocess.run([binary,'-hide_banner','-loglevel','error','-y','-framerate','30','-i',str(folder/'%04d.png'),
                        '-c:v','libx264','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(destination)],check=True)
        (OUTPUT/(name+'.png')).write_bytes(frames[8].read_bytes())
        records.append(dict(case=name,frames=90,source=str(folder.relative_to(ROOT)),
                            source_sequence_sha256=hashlib.sha256(b''.join(hashlib.sha256(p.read_bytes()).digest() for p in frames)).hexdigest(),
                            encoded_sha256=hashlib.sha256(destination.read_bytes()).hexdigest()))
        figures.append(f'<figure><figcaption>{name} · новое крепление винтовки, вид сзади</figcaption><video controls loop muted preload="metadata" src="{name}.mp4"></video></figure>')
    field = read(SOURCE / 'native-field' / 'review.json')
    if not field['success'] or field['errors'] or field['final_tick'] < 900:
        raise ValueError('Production field review failed')
    (OUTPUT / 'native-field.json').write_text(json.dumps(field, indent=2)+'\n', encoding='utf-8')
    for name in ('start-acceleration', 'first-turn', 'uphill'):
        folder = SOURCE / 'native-field' / name
        frames = sorted(folder.glob('*.png'))
        if len(frames) != 150:
            raise ValueError('Incomplete field clip')
        destination = OUTPUT / (name + '.mp4')
        subprocess.run([binary, '-hide_banner', '-loglevel', 'error', '-y', '-framerate', '30', '-i', str(folder / '%04d.png'),
                        '-c:v', 'libx264', '-crf', '19', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(destination)], check=True)
        (OUTPUT / (name + '.png')).write_bytes(frames[20].read_bytes())
        records.append(dict(case=name, frames=150, source=str(folder.relative_to(ROOT)),
                            source_sequence_sha256=hashlib.sha256(b''.join(hashlib.sha256(p.read_bytes()).digest() for p in frames)).hexdigest(),
                            encoded_sha256=hashlib.sha256(destination.read_bytes()).hexdigest()))
        figures.append(f'<figure><figcaption>{name} · реальный масстарт, 30 спортсменов</figcaption><video controls loop muted preload="metadata" src="{name}.mp4"></video></figure>')
    (OUTPUT / 'capture-hashes.json').write_text(json.dumps(dict(schema_version=1, id='m15_5_sprint_carry_native_captures', captures=records), indent=2)+'\n')
    performance = {}
    for phase in ('baseline-perf', 'final-perf'):
        receipt = read(SOURCE / phase / 'review.json')
        if not receipt['success'] or receipt['errors'] or len(receipt['samples']) != 240:
            raise ValueError('Invalid performance receipt')
        if (receipt['width'], receipt['height'], receipt['athletes'], receipt['trees']) != (1920,1080,30,2588):
            raise ValueError('Unmatched scene')
        metrics = {}
        for key in ('cpu_main_ms','cpu_render_ms','gpu_ms','gc_bytes','draw_calls','triangles'):
            values = sorted(s[key] for s in receipt['samples'] if s[key] >= 0)
            metrics[key] = dict(mean=statistics.mean(values),p95=values[int(.95*(len(values)-1))],valid=len(values)) if values else None
        values = [n/1e6 for n in receipt['animation_ns'] if n >= 0]
        metrics['animation_ms'] = dict(mean=statistics.mean(values),maximum=max(values)) if values else None
        performance[phase] = metrics
        (OUTPUT / (phase+'.json')).write_text(json.dumps(receipt,indent=2)+'\n')
    (OUTPUT / 'performance-summary.json').write_text(json.dumps(dict(schema_version=1,id='sprint_carry_cpu',results=performance,
         method='Sequential matched Ultra native 1920x1080 URP captures, 30 athletes/2588 trees, 120 warmup + 240 measured frames; excludes HUD/swapchain. GPU timing unavailable.'),indent=2)+'\n')
    test_root=ET.parse(SOURCE/'full-playmode.xml').getroot()
    cases=test_root.findall('.//test-case')
    if test_root.get('result')!='Passed' or len(cases)!=410 or any(c.get('result')!='Passed' for c in cases):
        raise ValueError('Final full PlayMode validation did not pass')
    tests=dict(schema_version=1,id='m15_5_sprint_carry_final_tests',assembly='Biathlon.PlayModeTests',
               passed=len(cases),failed=0,skipped=0,duration_seconds=float(test_root.get('duration').replace(',','.')),
               method='Complete native assembly run. MCP request timed out; Unity TestRunner callback persisted the final NUnit XML independently.',
               cases=[dict(name=c.get('fullname'),result=c.get('result'),seconds=float(c.get('duration').replace(',','.'))) for c in cases])
    (OUTPUT/'full-playmode.xml').write_bytes((SOURCE/'full-playmode.xml').read_bytes())
    (OUTPUT/'playmode-summary.json').write_text(json.dumps(tests,indent=2)+'\n',encoding='utf-8')
    for name in ('observations.json','console-after.json'):
        (OUTPUT/name).write_bytes((SOURCE/name).read_bytes())
    for name in ('red-60fFalse.json','red-90fFalse.json','red-120fTrue.json'):
        (OUTPUT/('contact-'+name)).write_bytes((SOURCE/name).read_bytes())
    (OUTPUT/'test-tool-timeout.json').write_bytes((SOURCE/'full-playmode.json').read_bytes())
    build=ROOT/'Builds/M15_5_Rifle_Contact_Review'
    for name in ('build-summary.json','startup-smoke.json','startup-menu.png'):
        (OUTPUT/name).write_bytes((build/name).read_bytes())
    owned=('Assets/Resources/Biathlon/ski_motion_v2.json','Assets/Resources/Biathlon/athlete_rifle_v1.json',
           'Assets/Scripts/Unity/Presentation/AthleteHybridLocomotionDriver.cs','Assets/Scripts/Unity/Presentation/AthleteRiflePresentation.cs',
           'Assets/Scripts/Unity/Presentation/SkiMotionComparison.cs','Assets/Scripts/Unity/Editor/ArmMotionReviewDiagnostics.cs',
           'Assets/Scripts/Unity/Editor/PlayerBuildVisualUpgrade.cs','Assets/Tests/PlayMode/SprintCarryRefinementTests.cs','Assets/Tests/PlayMode/RifleBackContactTests.cs')
    binaries=('Biathlon.exe','UnityPlayer.dll','Biathlon_Data/Managed/Biathlon.Unity.Presentation.dll','Biathlon_Data/resources.assets')
    provenance=dict(schema_version=1,id='sprint_carry_source',source_commit='221c4ccd',cadence_commit='e5b2db84',baseline_build_source='7d4ad434',
                    rejected_hover_build_source='e5b2db84',build=str(build.relative_to(ROOT)),
                    files={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in owned},
                    binary_sha256={p:hashlib.sha256((build/p).read_bytes()).hexdigest() for p in binaries})
    (OUTPUT/'source-hashes.json').write_text(json.dumps(provenance,indent=2)+'\n',encoding='utf-8')
    html='''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>M15.5 — ускорение и винтовка</title><style>
    :root{color-scheme:dark}*{box-sizing:border-box}body{margin:0;background:#0d1822;color:#edf4f5;font:17px system-ui}main{max-width:1600px;padding:32px;margin:auto}h1{font-size:42px}p{max-width:1100px;line-height:1.6;color:#bacbd5}figure{margin:26px 0;background:#172a35;border-radius:12px;overflow:hidden}figcaption{padding:16px;color:#66e4d8}video{display:block;width:100%}a{color:#66e4d8}</style><main><h1>M15.5 / Темп ускорения и винтовка на спине</h1>
    <p>Первые восемь видео — синтетические одинаковые входные наблюдения и моменты времени. Слева предыдущий тестовый билд с одобренными ногами и туловищем, справа исправление. Руки показаны спереди и сбоку через полный цикл. Рендер G2 Hero проходит настоящую цепочку pose → rifle → contact → grip; корень движется. Фон лабораторный, не игровая трасса.</p>
    <p>Ещё три парных видео показывают исправление замеченного отрыва винтовки: слева промежуточное парящее крепление, справа итоговое прилегание к спине. Четыре дополнительных видео — крепление сзади. Последние три видео — реальный масстарт из 30 спортсменов. Исходные Unity PNG сохранены без ретуши. Видео сравнения только соединяет два кадра рядом; используются одинаковые кадры 1–89 обеих последовательностей. Сохранено свободное положение локтей; снижена частота толчков при ускорении. Шаги сохраняют прежнюю частоту, небольшой наклон корпуса согласован с толчком. Широкая сторона ложи следует изгибу спины. Это авторская процедурная анимация, без motion capture. Визуальное принятие остаётся за владельцем.</p>
    <p><a href="playmode-summary.json">Все 410 тестов PlayMode</a> · <a href="focused-tests.json">Предыдущая выборочная проверка темпа рук</a> · <a href="performance-summary.json">CPU и ограничения замера</a> · <a href="capture-hashes.json">Хэши кадров</a></p>'''+''.join(figures)+'</main></html>'
    (OUTPUT / 'review.html').write_text(html,encoding='utf-8')
    print(json.dumps(performance,indent=2))


if __name__ == '__main__':
    package()
