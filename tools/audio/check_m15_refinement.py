"""Objective guard for softened pole onsets, menu duration and unchanged bank budget.
This deliberately does not claim a subjective musical-quality test.
"""
from pathlib import Path
import json, wave
import numpy as np

root = Path(__file__).resolve().parents[2]
bank = root / 'Assets/Resources/Biathlon/Audio'
checks = []
for name in ['pole_0', 'pole_1', 'pole_2', 'menu_air']:
    with wave.open(str(bank / (name + '.wav'))) as source:
        rate = source.getframerate()
        samples = np.frombuffer(source.readframes(source.getnframes()), '<i2').astype(float) / 32767
    record = dict(id=name, seconds=len(samples)/rate, peak=float(np.max(np.abs(samples))))
    if name.startswith('pole'):
        window = int(rate * .012)
        envelope = np.sqrt(np.mean(samples[:len(samples)//window*window].reshape(-1, window)**2, axis=1))
        record['first_12ms_to_max_rms'] = float(envelope[0] / max(envelope))
        record['pass'] = record['first_12ms_to_max_rms'] < .25 and record['peak'] <= .36
    else:
        record['pass'] = bool(30 <= record['seconds'] <= 36 and abs(samples[0]-samples[-1]) < .0001)
    checks.append(record)
print(json.dumps(dict(schema_version=1, id='m15_audio_refinement', checks=checks), indent=2))
assert all(c['pass'] for c in checks), 'Pole onset/menu duration refinement not present'
