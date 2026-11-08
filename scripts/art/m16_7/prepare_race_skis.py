"""Recolor original PULSE ski graphics using the saved career equipment palette."""
from pathlib import Path
import json, hashlib
import numpy as np
from PIL import Image

project=Path(__file__).resolve().parents[3]
source=project/'Assets/Biathlon/Characters/Biathlete_16_7/Textures/Ski_PULSE_Topsheet_BaseColor_4K.png'
catalog=project/'Assets/Resources/Biathlon/UI/ski_appearance_v1.json'
out=project/'Assets/Resources/Biathlon/Athletes/M167'
out.mkdir(parents=True,exist_ok=True)
original=Image.open(source).convert('RGB').resize((2048,2048),Image.Resampling.LANCZOS)
pixels=np.asarray(original,dtype=np.float32)/255
r,g,b=pixels[:,:,0],pixels[:,:,1],pixels[:,:,2]
mask=(r>g*1.35)&(r>b*1.25)&(r>.18)&((r-np.minimum(g,b))>.12)
value=np.clip(r/.85,0,1.15)
results=[]
for row in json.loads(catalog.read_text(encoding='utf-8-sig'))['skis']:
    color=np.array([int(row['base_color'][i:i+2],16)/255 for i in (1,3,5)])
    result=pixels.copy()
    result[mask]=np.clip(value[mask,None]*color,0,1)
    path=out/(row['id']+'.png')
    Image.fromarray(np.round(result*255).astype(np.uint8)).save(path,optimize=True)
    results.append({'id':row['id'],'path':path.relative_to(project).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
receipt={'schema_version':1,'id':'m167_saved_ski_graphics_v1','source':source.relative_to(project).as_posix(),
         'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'catalog':catalog.relative_to(project).as_posix(),
         'recolored_pixels':int(mask.sum()),'resolution':[2048,2048],'variants':results}
(out/'ski-graphics-provenance.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(f'Prepared {len(results)} shared ski textures; original lettering and carbon graphics retained.')
