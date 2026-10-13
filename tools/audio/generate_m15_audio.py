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
    y=band(len(t),600,4800)*np.exp(-t*35)+.35*band(len(t),100,850)*np.exp(-t*17)
    save('pole_'+str(variant),y,.6)
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
# Sparse harmonic menu air, seamless sustained D/A/E voicing.
t=np.arange(RATE*16)/RATE
y=sum(np.sin(2*np.pi*f*t+p)*(.65+.15*np.sin(2*np.pi*t/8+p))*g for f,p,g in [(146.875,0,.3),(220,1,.2),(293.75,2,.14),(330,3,.07)])
save('menu_air',y,.28,True)
(OUT/'sources.json').write_text(json.dumps(dict(schema_version=1,id='m15_original_soundbank',
    generator='tools/audio/generate_m15_audio.py',seed=198205,license='Original project-authored procedural sound design; no third-party samples.',clips=receipts),indent=2)+'\n')
print(f'Generated {len(receipts)} original clips; {sum(x["seconds"] for x in receipts):.1f}s mono PCM.')
