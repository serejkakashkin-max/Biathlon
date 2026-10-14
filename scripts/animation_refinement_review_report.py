"""Package native animation refinement footage; never alter the source PNGs."""
import hashlib
import json
import statistics
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'artifacts/m15-5-poles'
OUTPUT = ROOT / 'docs/quality/m15-5/animation-refinement'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def metrics():
    results = {}
    for name in ('rejected-perf', 'refined-perf'):
        receipt = read(SOURCE / name / 'review.json')
        if not receipt['success'] or receipt['errors'] or len(receipt['samples']) != 240:
            raise ValueError('Invalid native performance capture')
        if (receipt['width'], receipt['height'], receipt['athletes'], receipt['trees']) != (1920, 1080, 30, 2588):
            raise ValueError('Unmatched scene/capture workload')
        result = {}
        for key in ('cpu_main_ms', 'cpu_render_ms', 'gpu_ms', 'gc_bytes', 'draw_calls', 'triangles'):
            samples = sorted(s[key] for s in receipt['samples'] if s[key] >= 0)
            result[key] = dict(mean=statistics.mean(samples), p95=samples[int(.95*(len(samples)-1))], valid=len(samples)) if samples else None
        pose = [n/1e6 for n in receipt['animation_ns'] if n >= 0]
        result['animation_ms'] = dict(mean=statistics.mean(pose), max=max(pose)) if pose else None
        results[name] = result
        (OUTPUT / (name+'.json')).write_text(json.dumps(receipt, indent=2)+'\n', encoding='utf-8')
    report = dict(schema_version=1, id='m15_5_animation_refinement_performance',
                  method='Sequential matched 1920x1080 URP offscreen captures, Ultra, 30 athletes, 2588 trees; 120 warmup + 240 measured frames, no HUD/swapchain. GPU timing unavailable.',
                  baseline='Frozen M15_5_Visual_Review build, rejected animation with approved art, 2ecb1440', results=results)
    (OUTPUT / 'performance-summary.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')


def package():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    metrics()
    sys.path.insert(0, str(ROOT / 'artifacts/tools/ffmpeg-runtime'))
    import imageio_ffmpeg
    binary = imageio_ffmpeg.get_ffmpeg_exe()
    font = 'Assets/Resources/Biathlon/UI/Fonts/Oswald.ttf'
    videos = []
    hashes = []
    field = SOURCE / 'native-field-v3'
    receipt = read(field / 'review.json')
    if not receipt['success'] or receipt['errors'] or receipt['final_tick'] < 900:
        raise ValueError('Actual production field review did not complete')
    (OUTPUT / 'native-field.json').write_text(json.dumps(receipt, indent=2)+'\n', encoding='utf-8')
    (OUTPUT / 'waiting-mass-start.png').write_bytes((field / 'waiting-mass-start.png').read_bytes())
    cases = [(field/name, name, name.upper()+' / ACTUAL MASS START / 30 ATHLETES', 150, False)
             for name in ('start-acceleration', 'first-turn', 'uphill')]
    cases += [(SOURCE/'lab-v3'/name, name, name.upper()+' / SYNTHETIC IDENTICAL INPUTS', 90, True)
              for name in ('Flat', 'Uphill', 'MaxSprint', 'CornerLeft', 'SlowStop', 'CurvedClimb', 'LaneChange')]
    for folder, name, label, expected, comparison in cases:
        frames = sorted(folder.glob('*.png'))
        if len(frames) != expected:
            raise ValueError('Wrong native frame count for '+name)
        overlay = f"drawtext=fontfile={font}:text='{label}':fontsize=23:fontcolor=white:x=25:y=670:box=1:boxcolor=0x10232E@0.85:boxborderw=8"
        if comparison:
            overlay += (f",drawtext=fontfile={font}:text='LEGACY FALLBACK':fontsize=25:fontcolor=white:x=25:y=25:box=1:boxcolor=0x10232E@0.85:boxborderw=8"
                        f",drawtext=fontfile={font}:text='REFINED MOTION':fontsize=25:fontcolor=0x5EE8DB:x=690:y=25:box=1:boxcolor=0x10232E@0.85:boxborderw=8")
        destination = OUTPUT / (name+'.mp4')
        subprocess.run([binary, '-hide_banner', '-loglevel', 'error', '-y', '-framerate', '30', '-i', str(folder/'%04d.png'),
                        '-vf', overlay, '-c:v', 'libx264', '-crf', '18', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(destination)], cwd=ROOT, check=True)
        videos.append((name, label))
        representative = frames[min(20, len(frames)-1)]
        (OUTPUT / (name+'.png')).write_bytes(representative.read_bytes())
        hashes.append(dict(case=name, frames=len(frames), source=str(folder.relative_to(ROOT)),
                           source_sequence_sha256=hashlib.sha256(b''.join(hashlib.sha256(p.read_bytes()).digest() for p in frames)).hexdigest(),
                           encoded_sha256=hashlib.sha256(destination.read_bytes()).hexdigest()))
    (OUTPUT/'capture-hashes.json').write_text(json.dumps(dict(schema_version=1, id='m15_5_animation_native_hashes', captures=hashes), indent=2)+'\n')
    clips=''.join(f'<figure><figcaption>{label}</figcaption><video controls loop muted preload="metadata" poster="{name}.png" src="{name}.mp4"></video></figure>' for name,label in videos)
    html='''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Biathlon — доработка анимации M15.5</title><style>
    :root{color-scheme:dark}*{box-sizing:border-box}body{margin:0;background:#0c1620;color:#e9f0f3;font:17px system-ui,sans-serif}main{max-width:1450px;margin:auto;padding:32px}h1{font-size:clamp(28px,4vw,52px)}p{max-width:1000px;line-height:1.55;color:#b6c6d0}figure{margin:24px 0;background:#152632;border-radius:12px;overflow:hidden}figcaption{padding:16px;color:#5ee8db}video,img{display:block;width:100%}a{color:#5ee8db}</style><main>
    <h1>M15.5 / Руки и палки</h1><p>Первые три видео — настоящая группа масстарта из 30 спортсменов: старт, первый поворот и подъём. Далее — изолированные проверки одинаковых входных наблюдений: слева прежний процедурный fallback, справа доработанная анимация. Это не сравнение с отклонённой версией M15.5 и не motion capture.</p>
    <p>Кадры Unity без ретуши. Видео добавляют только подписи. Одобренные материалы, модель, снег, погода и гонка сохранены. <a href="native-field.json">Native-проверка</a> · <a href="performance-summary.json">Замер нагрузки</a> · <a href="focused-tests.json">Тесты</a></p>
    <figure><figcaption>Ожидание старта масстарта</figcaption><img src="waiting-mass-start.png"></figure>'''+clips+'</main></html>'
    (OUTPUT/'review.html').write_text(html,encoding='utf-8')
    print('Packaged 3 actual field clips, 7 synthetic comparisons, untouched stills and matched performance evidence.')


if __name__ == '__main__':
    package()
