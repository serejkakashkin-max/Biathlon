"""Package untouched native front/side frames for the owner elbow review."""
import hashlib
import json
import statistics
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'artifacts/m15-5-elbows'
OUTPUT = ROOT / 'docs/quality/m15-5/elbow-refinement'


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
            before, after = SOURCE / 'baseline-valid' / name, SOURCE / 'final-valid' / name
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
            figures.append(f'<figure><figcaption>{name} · слева отклонённая версия / справа исправление</figcaption><video controls loop muted preload="metadata" src="{name}.mp4"></video></figure>')
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
    (OUTPUT / 'capture-hashes.json').write_text(json.dumps(dict(schema_version=1, id='m15_5_elbow_native_captures', captures=records), indent=2)+'\n')
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
    (OUTPUT / 'performance-summary.json').write_text(json.dumps(dict(schema_version=1,id='elbow_refinement_cpu',results=performance,
         method='Sequential matched Ultra native 1920x1080 URP captures, 30 athletes/2588 trees, 120 warmup + 240 measured frames; excludes HUD/swapchain. GPU timing unavailable.'),indent=2)+'\n')
    html='''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>M15.5 — локти</title><style>
    :root{color-scheme:dark}*{box-sizing:border-box}body{margin:0;background:#0d1822;color:#edf4f5;font:17px system-ui}main{max-width:1600px;padding:32px;margin:auto}h1{font-size:42px}p{max-width:1100px;line-height:1.6;color:#bacbd5}figure{margin:26px 0;background:#172a35;border-radius:12px;overflow:hidden}figcaption{padding:16px;color:#66e4d8}video{display:block;width:100%}a{color:#66e4d8}</style><main><h1>M15.5 / Анатомия локтей</h1>
    <p>Первые восемь видео — синтетические одинаковые входные наблюдения и моменты времени. Слева замороженный отклонённый билд, справа исправление. Руки показаны спереди и сбоку через полный цикл. Рендер G2 Hero проходит настоящую цепочку pose → rifle → contact → grip; корень движется. Фон лабораторный, не игровая трасса.</p>
    <p>Последние три видео — реальный масстарт из 30 спортсменов. Исходные Unity PNG сохранены без ретуши. Видео сравнения только соединяет два кадра рядом; Парное видео использует одинаковые кадры 1–89 обеих последовательностей; все исходные кадры сохранены. Это авторская процедурная анимация, без motion capture. Визуальное принятие остаётся за владельцем.</p>
    <p><a href="focused-tests.json">Выборочные тесты</a> · <a href="performance-summary.json">CPU и ограничения замера</a> · <a href="capture-hashes.json">Хэши кадров</a></p>'''+''.join(figures)+'</main></html>'
    (OUTPUT / 'review.html').write_text(html,encoding='utf-8')
    print(json.dumps(performance,indent=2))


if __name__ == '__main__':
    package()
