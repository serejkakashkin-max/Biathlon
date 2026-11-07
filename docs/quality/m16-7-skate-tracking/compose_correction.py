from pathlib import Path
import json, subprocess, hashlib
import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFont

before = Path('artifacts/m16-7-skate-rhythm/native')
after = Path('artifacts/m16-7-skate-tracking/native')
out = Path('docs/quality/m16-7-skate-tracking')
out.mkdir(parents=True, exist_ok=True)
shots = [('skate', 'V2 · сбоку'), ('skate_front','V2 · спереди'),
         ('acceleration','Разгон · сбоку'), ('acceleration_front','Разгон · спереди'),
         ('uphill','Подъём V1 · сбоку'), ('uphill_front','Подъём V1 · спереди')]
fps, count = 24, 96
size = (1512,900)
fonts = Path('C:/Windows/Fonts')
title = ImageFont.truetype(str(fonts/'seguisb.ttf'),36)
label = ImageFont.truetype(str(fonts/'seguisb.ttf'),21)
small = ImageFont.truetype(str(fonts/'segoeui.ttf'),20)
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
cmd = [ffmpeg,'-hide_banner','-loglevel','error','-y','-f','rawvideo','-pix_fmt','rgb24',
       '-video_size','1512x900','-framerate',str(fps),'-i','-','-an','-c:v','libx264',
       '-preset','fast','-crf','17','-pix_fmt','yuv420p','-movflags','+faststart',str(out/'before-after.mp4')]
p = subprocess.Popen(cmd,stdin=subprocess.PIPE)
manifest = []
for chapter,(name,text) in enumerate(shots):
    assert len(list((before/name).glob('hero-*.png'))) == count
    assert len(list((after/name).glob('hero-*.png'))) == count
    hashes = [hashlib.sha256(),hashlib.sha256()]
    record = {'id':name,'title_ru':text,'start_seconds':chapter*4,'frames':count}
    manifest.append(record)
    for n in range(count):
        canvas = Image.new('RGB',size,(9,17,26)); d=ImageDraw.Draw(canvas)
        d.text((24,16),'BIATHLON: PULSE  /  М16.7',font=small,fill=(123,222,224))
        d.text((24,47),text,font=title,fill=(231,242,245))
        for index,(x,source,caption) in enumerate([(24,before,'ПРЕЖНИЙ М16.7'),(768,after,'ИСПРАВЛЕННЫЙ М16.7')]):
            d.rounded_rectangle((x,88,x+720,110),3,fill=(24,49,65))
            d.text((x+8,86),caption,font=label,fill=(200,230,240))
            source_frame = source/name/f'hero-{n:04}.png'
            hashes[index].update(source_frame.read_bytes())
            with Image.open(source_frame) as image:
                canvas.paste(image.convert('RGB'),(x,110))
        d.text((24,850),'Одна камера и траектория · обычная скорость · реальный риг Unity',font=small,fill=(231,242,245))
        d.rectangle((24,887,1488,890),fill=(49,82,103))
        d.rectangle((24,887,24+1464*(chapter*count+n+1)/(count*len(shots)),890),fill=(123,222,224))
        p.stdin.write(canvas.tobytes())
        if name=='skate_front' and n==36: canvas.save(out/'correction-preview.jpg',quality=94)
    record['before_frames_sha256'] = hashes[0].hexdigest()
    record['after_frames_sha256'] = hashes[1].hexdigest()
p.stdin.close()
assert p.wait()==0
(out/'correction-chapters.json').write_text(json.dumps({'schema_version':1,'id':'m167_skate_tracking_before_after',
    'fps':fps,'frames':count*len(shots),'seconds':24,'before_commit':'672018b4e0d50bb35199929dca4adb93dc60f716',
    'before_capture':before.as_posix(),'after_capture':after.as_posix(),'chapters':manifest},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
