"""Original, reproducible Foley/sound-design sources. No external recordings.
Offline only: numpy 2.x; 48 kHz PCM16. Runtime never synthesizes waveforms.
"""
from pathlib import Path
import json, wave, hashlib
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Resources/Biathlon/Audio'
OUT.mkdir(parents=True, exist_ok=True)
RATE = 48000
rng = np.random.default_rng(198205)
receipts = []

def band(n, low, high):
    frequencies = np.fft.rfftfreq(n, 1 / RATE)
    spectrum = np.fft.rfft(rng.normal(size=n))
    shape = np.minimum(1, frequencies / max(1, low)) ** 3 / (1 + (frequencies / high) ** 6)
    y = np.fft.irfft(spectrum * shape, n)
    return y / max(.001, np.sqrt(np.mean(y * y)))

def save(name, y, peak=.65, loop=False):
    y = np.asarray(y)
    if not loop:
        fade = min(int(.006 * RATE), len(y) // 4)
        y[:fade] *= np.linspace(0, 1, fade)
        y[-fade:] *= np.linspace(1, 0, fade)
    else:
        # Equal-amplitude crossfade wrap; exact endpoints prevent seam clicks.
        k = min(RATE // 2, len(y) // 8)
        cross = np.linspace(0, 1, k)
        y[:k] = y[-k:] * (1-cross) + y[:k] * cross
        y = y[:-k]
        y[0] = y[-1]
    y *= peak / max(.001, np.max(np.abs(y)))
    path = OUT / (name + '.wav')
    with wave.open(str(path), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes((y * 32767).astype('<i2').tobytes())
    receipts.append(dict(id=name, seconds=len(y)/RATE, peak=float(np.max(np.abs(y))),
                         rms=float(np.sqrt(np.mean(y*y))), loop=loop,
                         sha256=hashlib.sha256(path.read_bytes()).hexdigest()))

for name, low, high in [('ski_packed',700,4200),('ski_soft',180,1700),('ski_ice',1600,7600)]:
    n=RATE*8; t=np.arange(n)/RATE
    y=band(n,low,high)*(.72+.12*np.sin(2*np.pi*1.7*t)+.06*np.sin(2*np.pi*3.3*t))
    save(name,y,.5,True)
for variant in range(3):
    t=np.arange(int(RATE*.3))/RATE
    # Soft snow compression + short brushed withdrawal. No hard attack/crack.
    # Keep the same two noise draws so unrelated source waveforms remain identical.
    envelope=np.sin(np.pi*np.clip(t/.3,0,1))**1.6*np.exp(-t*5)
    y=(band(len(t),140,1500)+.35*band(len(t),100,650))*envelope
    save('pole_'+str(variant),y,.32)
for variant in range(3):
    t=np.arange(int(RATE*1.7))/RATE
    y=band(len(t),900,8500)*np.exp(-t*110)+.65*band(len(t),120,2200)*np.exp(-t*32)
    y+=.32*np.sin(2*np.pi*(150*t-25*t*t))*np.exp(-t*48)
    # Distant forest reflection is a diffuse tail, not a second rifle report.
    tail=band(len(t),220,1500)*np.maximum(0,1-np.exp(-t*18))*np.exp(-t*4)
    y+=.12*tail
    save('rifle_'+str(variant),y,.76)
for name, base in [('target_hit',1350),('target_miss',310)]:
    t=np.arange(int(RATE*.7))/RATE
    y=sum(np.sin(2*np.pi*base*r*t)*np.exp(-t*(13+i*8))/(i+1) for i,r in enumerate([1,1.43,2.71,3.92]))
    y+=band(len(t),500,6000)*np.exp(-t*65)*.65
    save(name,y,.65)
t=np.arange(int(RATE*.45))/RATE
bolt=np.zeros_like(t)
for delay,gain in [(0,1),(.11,.6),(.27,.9)]:
    x=np.maximum(0,t-delay); bolt+=gain*band(len(t),450,4200)*np.exp(-x*55)*(t>=delay)
save('bolt',bolt,.6)
t=np.arange(int(RATE*2.8))/RATE
env=np.maximum(0,np.sin(np.pi*t/1.15))*(t<1.15)+.72*np.maximum(0,np.sin(np.pi*(t-1.4)/1.4))*(t>=1.4)
save('breath',band(len(t),280,2200)*env,.48)
t=np.arange(int(RATE*.32))/RATE
save('heart',np.sin(2*np.pi*66*t)*np.exp(-t*32)+.55*np.sin(2*np.pi*52*(t-.13))*np.exp(-np.maximum(0,t-.13)*38)*(t>.13),.55)
for name,high in [('wind',950),('snowfall',2100)]:
    t=np.arange(RATE*12)/RATE
    save(name,band(len(t),45,high)*(.65+.22*np.sin(2*np.pi*t/5.1)+.1*np.sin(2*np.pi*t/2.7)),.48,True)
# Many soft formant-like voices, handclaps and muffled cowbells, no spoken words.
for name,cheer in [('crowd',False),('cheer',True)]:
    t=np.arange(int(RATE*(4 if cheer else 12)))/RATE; y=band(len(t),150,950)*.14
    for person in range(28):
        fundamental=rng.uniform(110,265); phase=rng.uniform(0,6.28)
        envelope=(.5+.5*np.sin(2*np.pi*rng.uniform(.25,.9)*t+phase))**3
        voice=sum(np.sin(2*np.pi*fundamental*k*t+phase)/(k*k) for k in range(1,7))
        y+=voice*envelope*.06
    for at in rng.uniform(0,t[-1],70 if cheer else 40):
        d=np.maximum(0,t-at); y+=band(len(t),900,7000)*np.exp(-d*90)*(t>=at)*.08
    if cheer:y*=np.sin(np.pi*t/t[-1])**.6
    save(name,y,.55,not cheer)
for name,freq,duration in [('ui_focus',600,.045),('ui_confirm',750,.12),('ui_back',440,.1),('countdown',720,.14),('start',1080,.28)]:
    t=np.arange(int(RATE*duration))/RATE
    save(name,(np.sin(2*np.pi*freq*t)+.22*np.sin(2*np.pi*freq*2*t))*np.sin(np.pi*t/duration)**2,.38)
# Original instrumental miniature "Snowlight": 8 bars, 60 BPM, D major.
# Warm soft keys, a slowly moving pad and a restrained melody; no percussion.
duration=32.5  # save() folds the final half-second into a seamless 32s period.
t=np.arange(int(RATE*duration))/RATE; y=np.zeros_like(t)
def note(at, midi, gain, sustain=2.6):
    freq=440*2**((midi-69)/12)
    x=t-at; active=x>=0; age=np.maximum(0,x)
    attack=(1-np.exp(-age/0.045))**2
    decay=np.exp(-age/sustain)
    tone=(np.sin(2*np.pi*freq*age)+.22*np.sin(2*np.pi*freq*2*age)
          +.07*np.sin(2*np.pi*freq*3*age)+.018*np.sin(2*np.pi*freq*4*age))
    y[:]+=gain*tone*attack*decay*active
chords=[(50,54,57,61,64),(47,54,57,62,66),(43,50,54,59,62),(45,52,57,59,62)]
for bar,chord in enumerate(chords):
    at=bar*8
    note(at,chord[0]-12,.14,3.7);note(at+4,chord[0]-12,.1,3.3)
    for offset in range(8):note(at+offset,chord[1+offset%4],.115 if offset%2==0 else .075,2.2)
    for midi in chord[1:]:
        freq=440*2**((midi-69)/12);phase=(t-at)/9
        env=np.where((phase>=0)&(phase<=1),np.sin(np.pi*np.clip(phase,0,1))**2,0)
        y+=.04*np.sin(2*np.pi*freq*t)*env
for at,midi in [(1,78),(3,76),(5,73),(6,74),(9,78),(11,81),(13,78),(14,76),
                (17,78),(19,76),(21,74),(22,73),(25,76),(27,74),(29,73),(30,69)]:
    note(at,midi,.13,1.9)
dry=y.copy()
for delay,gain in [(.19,.16),(.37,.12),(.61,.075)]:
    shift=int(RATE*delay);y[shift:]+=dry[:-shift]*gain
save('menu_air',y,.55,True)
(OUT/'sources.json').write_text(json.dumps(dict(schema_version=1,id='m15_original_soundbank',
    generator='tools/audio/generate_m15_audio.py',seed=198205,license='Original project-authored procedural sound design and composition; no third-party samples.',
    music=dict(id='snowlight',title='Snowlight',composer='Original Biathlon project composition',bpm=60,bars=8,key='D major',instruments=['soft keys','warm pad','bass']),clips=receipts),indent=2)+'\n')
print(f'Generated {len(receipts)} original clips; {sum(x["seconds"] for x in receipts):.1f}s mono PCM.')
