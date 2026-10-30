"""Package original native player frames; no generated UI or modified shot data."""
import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'artifacts/m16-2'
OUT = ROOT / 'docs/quality/m16-2'
FFMPEG = ROOT / 'artifacts/tools/ffmpeg-runtime/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def package():
    evidence = {'schema_version': 1, 'id': 'm16_2_native_video_evidence',
                'method': 'Unretouched native PNG frames, one accepted20Hz tick per frame, encoded20fps H264. World and native UI targets composited in the player. These are QA scenarios, not human performance or FPS measurements.', 'videos': []}
    for name in ('main', 'miss'):
        folder = SOURCE / ('player-' + name)
        receipt = read(folder / 'review.json')
        if not receipt['success'] or receipt['errors']:
            raise ValueError('Native player probe incomplete: ' + name)
        destination = OUT / ('player-' + name)
        destination.mkdir(parents=True, exist_ok=True)
        (destination / 'review.json').write_bytes((folder / 'review.json').read_bytes())
        for image in folder.glob('*.png'):
            (destination / image.name).write_bytes(image.read_bytes())
        frames = sorted((folder / 'frames').glob('*.png'))
        if len(frames) not in (300, 620) or any(p.name != f'{i:05d}.png' for i, p in enumerate(frames)):
            raise ValueError('Missing/nonsequential native video frames: ' + name)
        output = OUT / ('live-shooting-' + name + '.mp4')
        subprocess.run([str(FFMPEG), '-v', 'error', '-y', '-framerate', '20', '-i', str(folder / 'frames/%05d.png'),
                        '-an', '-c:v', 'libx264', '-preset', 'medium', '-crf', '20', '-pix_fmt', 'yuv420p',
                        '-movflags', '+faststart', str(output)], check=True)
        subprocess.run([str(FFMPEG), '-v', 'error', '-i', str(output), '-f', 'null', '-'], check=True)
        hashes = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in frames}
        (destination / 'frame-hashes.json').write_text(json.dumps(hashes, indent=2) + '\n', encoding='utf-8')
        evidence['videos'].append({'case': name, 'file': output.name, 'fps': 20, 'frames': len(frames),
                                   'seconds': len(frames)/20, 'width': 1920, 'height': 1080, 'audio': False,
                                   'sha256': hashlib.sha256(output.read_bytes()).hexdigest(), 'decode': 'PASS'})
    (OUT / 'video-evidence.json').write_text(json.dumps(evidence, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(evidence, indent=2))


if __name__ == '__main__':
    package()
