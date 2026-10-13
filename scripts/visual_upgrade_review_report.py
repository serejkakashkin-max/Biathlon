"""Encode native motion frames and package unmodified before/after gallery evidence."""
import hashlib,json,statistics,subprocess,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
quality=root/'docs/quality/m15-5'

def metrics():
    result={}
    for phase in ('before-verified','after-final'):
        receipt=json.loads((quality/phase/'review.json').read_text())
        if not receipt['success'] or receipt['errors']:raise ValueError('Invalid native capture receipt')
        values={}
        for key in ('wall_ms','cpu_main_ms','cpu_render_ms','gpu_ms','draw_calls','setpass_calls','triangles','gc_bytes','used_memory_bytes'):
            samples=sorted(s[key] for s in receipt['samples'] if s[key]>=0)
            values[key]=dict(mean=statistics.mean(samples),p95=samples[int(.95*(len(samples)-1))],max=max(samples),valid=len(samples)) if samples else None
        ns=[n/1e6 for n in receipt['animation_ns'] if n>=0]
        values['animation_ms']=dict(mean=statistics.mean(ns),max=max(ns)) if ns else None
        values['texture_bytes']=receipt['texture_bytes'];result[phase]=values
    (quality/'performance-summary.json').write_text(json.dumps(dict(schema_version=1,id='m15_5_render_comparison',method='matched1920x1080 URP SingleCameraRequest,30athletes,2588trees,Ultra,noHUDorSwapchain',results=result),indent=2)+'\n')
    return result

def encode():
    sys.path.insert(0,str(root/'artifacts/tools/ffmpeg-runtime'))
    import imageio_ffmpeg
    binary=imageio_ffmpeg.get_ffmpeg_exe();folder=quality/'motion-final'
    font='Assets/Resources/Biathlon/UI/Fonts/Oswald.ttf'
    for state in ('Flat','Uphill','MaxSprint'):
        frames=sorted((folder/state).glob('*.png'))
        if len(frames)!=90:raise ValueError('Expected ninety native frames per state')
        overlay=(f"drawtext=fontfile={font}:text='ORIGINAL':fontsize=30:fontcolor=white:x=35:y=25:box=1:boxcolor=0x10232E@0.85:boxborderw=10,"
            f"drawtext=fontfile={font}:text='AUTHORED HYBRID':fontsize=30:fontcolor=0x5EE8DB:x=690:y=25:box=1:boxcolor=0x10232E@0.85:boxborderw=10,"
            f"drawtext=fontfile={font}:text='{state} / SAME INPUTS / NO MOCAP':fontsize=23:fontcolor=white:x=35:y=665:box=1:boxcolor=0x10232E@0.85:boxborderw=10")
        subprocess.run([binary,'-hide_banner','-loglevel','error','-y','-framerate','30','-i',str(folder/state/'%04d.png'),'-vf',overlay,'-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(folder/(state+'.mp4'))],cwd=root,check=True)

def gallery():
    names=('01-hero-front','02-hero-side','03-hero-back','04-flat-skate','05-uphill-skate','06-fast-corner','07-finish-sprint','08-skis-and-tracks','09-forest-course','10-race-environment')
    sections=[];hashes=[]
    for name in names:
        images=[]
        for phase,label in (('before-verified','До — исходные материалы и движение'),('after-final','После — M15.5')):
            path=quality/phase/(name+'.png')
            if not path.is_file():raise FileNotFoundError(path)
            hashes.append(dict(path=path.relative_to(root).as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
            images.append(f'<figure><figcaption>{label}</figcaption><a href="{phase}/{name}.png"><img loading="lazy" src="{phase}/{name}.png" alt="{label}: {name}"></a></figure>')
        sections.append(f'<section><h2>{name}</h2><div class="pair">'+''.join(images)+'</div></section>')
    weather=''.join(f'<figure><figcaption>{a}</figcaption><a href="after-final/atmosphere-{a}.png"><img loading="lazy" src="after-final/atmosphere-{a}.png" alt="{a}"></a></figure>' for a in ('clear','overcast','light_snow','snowfall','winter_haze'))
    videos=''.join(f'<figure><figcaption>{s} · слева прежнее / справа новое</figcaption><video controls loop muted preload="metadata" src="motion-final/{s}.mp4"></video></figure>' for s in ('Flat','Uphill','MaxSprint'))
    page='''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Biathlon M15.5 — native visual review</title><style>
    :root{color-scheme:dark}*{box-sizing:border-box}body{margin:0;background:#0c1620;color:#e9f0f3;font:17px system-ui,sans-serif}main{max-width:1700px;margin:auto;padding:32px}h1{font-size:clamp(30px,4vw,58px);margin:0 0 20px}h2{font-size:22px;color:#5ee8db}p{max-width:1000px;line-height:1.55;color:#aebfc9}section{margin:48px 0}.pair,.weather{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px}figure{margin:0;background:#152632;border-radius:10px;overflow:hidden}figcaption{padding:14px;color:#dfe8ee}img,video{display:block;width:100%;height:auto}a{color:#5ee8db}@media(max-width:800px){.pair,.weather{grid-template-columns:1fr}main{padding:18px}}</style><main>
    <h1>BIATHLON / M15.5</h1><p>Реальные кадры Unity, без ретуши. Парные кадры используют одинаковую камеру, наблюдения и базовый свет. В варианте «до» исходные материалы d95ef065 и прежний драйвер движения; измерительный адаптер и исправленная посадка декора общие. Это не две полностью идентичные старые/новые версии игры.</p>
    <p>Авторские движения и процедурная адаптация — без motion capture. Видео содержат только подписи поверх исходной последовательности кадров. <a href="performance-summary.json">Измерения</a> · <a href="after-final/review.json">Результат native review</a></p>
    '''+''.join(sections)+'<section><h2>Пять зимних атмосфер</h2><div class="weather">'+weather+'</div></section><section><h2>Движение</h2>'+videos+'</section></main></html>'
    (quality/'review.html').write_text(page,encoding='utf-8')
    (quality/'capture-hashes.json').write_text(json.dumps(dict(schema_version=1,id='m15_5_native_capture_hashes',captures=hashes),indent=2)+'\n')

if __name__=='__main__':
    metrics();encode();gallery();print('Matched ten-case gallery, five atmospheres and three native comparison videos packaged.')
