"""Encode actual Unity capture frames; does not render or synthesize an athlete.

Blender 4.5: --background --python-exit-code 2 --python this.py --
    --capture artifacts/.../motion --output artifacts/.../motion-review
The capture's cycle durations drive playback; a second movie runs at 0.25x.
"""
import argparse,json,sys
from pathlib import Path
import bpy

p=argparse.ArgumentParser();p.add_argument('--capture',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);source=Path(a.capture).resolve();out=Path(a.output).resolve();out.mkdir(parents=True,exist_ok=True)
receipt=json.loads((source/'complete.json').read_text());videos=[]
for label,speed,repeats in [('normal',1,2),('slow',.25,1)]:
    bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene;scene.sequence_editor_create()
    indices=[];timeline=[];start=0
    for cycle in receipt['cycles']:
        count=round(cycle['cycle_seconds']/speed*30)
        timeline.append({'state':cycle['state'],'starts_at_seconds':len(indices)/30,'duration_seconds':count*repeats/30})
        for _ in range(repeats):indices.extend(start+min(cycle['frames']-1,int(i*cycle['frames']/count)) for i in range(count))
        start+=cycle['frames']
    strip=scene.sequence_editor.strips.new_image('Actual Unity athlete',str(source/f'frame-{indices[0]:04d}.png'),channel=1,frame_start=1)
    for index in indices[1:]:strip.elements.append(f'frame-{index:04d}.png')
    strip.frame_final_duration=len(indices)
    scene.render.resolution_x=receipt['width'];scene.render.resolution_y=receipt['height'];scene.render.resolution_percentage=100
    scene.render.fps=30;scene.render.fps_base=1;scene.frame_start=1;scene.frame_end=len(indices)
    scene.view_settings.view_transform='Standard';scene.view_settings.look='None';scene.view_settings.exposure=0;scene.view_settings.gamma=1
    scene.render.image_settings.file_format='FFMPEG';scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264'
    scene.render.ffmpeg.constant_rate_factor='MEDIUM';scene.render.ffmpeg.ffmpeg_preset='GOOD';scene.render.ffmpeg.audio_codec='NONE'
    scene.render.filepath=str(out/f'motion-{label}.mp4');bpy.ops.render.render(animation=True)
    videos.append({'file':f'motion-{label}.mp4','playback_speed':speed,'fps':30,'frame_count':len(indices),'timeline':timeline})
(out/'videos.json').write_text(json.dumps({'schema_version':1,'id':'m167_actual_motion_video_review','source_capture':receipt,'videos':videos,'method':'Actual Unity PNG frames resampled to 30fps using recorded cycle durations; no interpolated or generated athlete frames.'},indent=2)+'\n')
