"""Encode real Unity capture frames. The encoder is a local tool, never a game asset."""
import json
from pathlib import Path
import subprocess
import sys

root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'artifacts/m15-6/video-tools'))
import imageio_ffmpeg
frames=root/'artifacts/m15-6/demo-frames'
paths=[frames/f'frame-{i:03}.png' for i in range(88)]
if not all(p.exists() for p in paths):raise RuntimeError('All88 actual Unity frames are required.')
output=root/'docs/quality/m15-6/creator-demo.mp4'
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y',
                '-framerate','8','-i',str(frames/'frame-%03d.png'),'-frames:v','88',
                '-c:v','libx264','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(output)],check=True)
(output.parent/'creator-demo.json').write_text(json.dumps({'schema_version':1,'id':'m15_6_native_creator_demo',
 'source':'88 real sequential native Unity UI Toolkit/character RenderTexture captures',
 'capture_fixture':'CareerCreatorDemoCaptureTests.NativeWizardAndRotatingAthleteProduceVisibleDemoFrames',
 'presentation_fps':8,'duration_seconds':11,'generated_images':False,'gameplay_performance_claim':False,
 'frames':len(paths),'bytes':output.stat().st_size},indent=2)+'\n',encoding='utf-8')
print(output)
