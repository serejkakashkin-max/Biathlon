"""Prepare short CC0 human crowd reactions offline; no runtime synthesis/downloads."""
import hashlib
import json
from pathlib import Path
import subprocess
from urllib.request import Request, urlopen
import uuid
import wave

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
BANK = ROOT / 'Assets/Resources/Biathlon/Audio'
SOURCES = ROOT / 'tools/audio/sources/m16_2'
FFMPEG = ROOT / 'artifacts/tools/ffmpeg-runtime/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe'
RECORDINGS = [
    dict(id=678546, author='mglennsound', title='AF Crowd Cheer Nat End .wav',
         page='https://freesound.org/people/mglennsound/sounds/678546/',
         download='https://cdn.freesound.org/previews/678/678546_14787385-hq.mp3',
         sha256='c52d750c4a990c75fa6cf0935a24389e1a6c0e1815d9322f40f6a21d62af9a6b'),
    dict(id=764035, author='ShangusBurger', title='CRWDAngr_Crowd Groans At Bad Joke 01_ShaneVincent_GSC24_FUMA-AmbeoVR.wav',
         page='https://freesound.org/people/ShangusBurger/sounds/764035/',
         download='https://cdn.freesound.org/previews/764/764035_11744683-hq.mp3',
         sha256='e278c40673525c3d208329ede09fc5668e9368a4d5f7ff66bbf3d385871f8fa0'),
]


def prepare():
    SOURCES.mkdir(parents=True, exist_ok=True)
    decoded = {}
    for source in RECORDINGS:
        path = SOURCES / f"{source['id']}-hq.mp3"
        if not path.exists():
            path.write_bytes(urlopen(Request(source['download'], headers={'User-Agent': 'Mozilla/5.0'}), timeout=40).read())
        assert hashlib.sha256(path.read_bytes()).hexdigest() == source['sha256'], path
        result = subprocess.run([str(FFMPEG), '-v', 'error', '-i', str(path), '-ac', '1', '-ar', '44100',
                                 '-af', 'highpass=f=110,lowpass=f=7800', '-f', 'f32le', '-'], capture_output=True, check=True)
        decoded[source['id']] = np.frombuffer(result.stdout, '<f4').astype(np.float64)
    clips = []
    for name, source_id, start, peak in [('crowd_hit_0', 678546, 1.45, .62), ('crowd_hit_1', 678546, 6.25, .62),
                                        ('crowd_miss_0', 764035, .30, .58), ('crowd_miss_1', 764035, .55, .58)]:
        rate = 44100
        audio = decoded[source_id][round(start*rate):round(start*rate)+round(1.15*rate)].copy()
        assert len(audio) == round(1.15*rate)
        attack, release = round(.04*rate), round(.25*rate)
        audio[:attack] *= np.linspace(0, 1, attack)
        audio[-release:] *= np.linspace(1, 0, release)
        audio *= peak / np.max(np.abs(audio))
        path = BANK / (name+'.wav')
        with wave.open(str(path), 'wb') as output:
            output.setnchannels(1); output.setsampwidth(2); output.setframerate(rate)
            output.writeframes(np.rint(audio*32767).astype('<i2').tobytes())
        meta = path.with_suffix('.wav.meta')
        if not meta.exists():
            template = (BANK/'cheer.wav.meta').read_text(encoding='utf-8')
            lines = [f'guid: {uuid.uuid4().hex}' if line.startswith('guid:') else line.rstrip() for line in template.splitlines()]
            meta.write_text('\n'.join(lines)+'\n', encoding='utf-8')
        clips.append(dict(id=name, source_id=source_id, start_seconds=start, seconds=1.15, sample_rate=rate,
                          channels=1, peak=peak, rms=float(np.sqrt(np.mean(audio**2))),
                          sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    manifest = dict(schema_version=1, id='m16_2_player_crowd_sources', license='CC0-1.0',
                    license_url='https://creativecommons.org/publicdomain/zero/1.0/',
                    method='Public high-quality previews of CC0 human recordings. Offline mono44.1kHz PCM16 excerpts,110Hz highpass/7.8kHz lowpass,40ms attack/250ms release, bounded peak. No broadcast audio, voice synthesis or runtime conversion.',
                    preparation='tools/audio/prepare_m162_crowd.py', sources=RECORDINGS, clips=clips)
    (BANK/'crowd_reactions_sources.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
    print(json.dumps({'clips': len(clips), 'additional_float_pcm_bytes': sum(round(c['seconds']*44100)*4 for c in clips)}, indent=2))


if __name__ == '__main__':
    prepare()
