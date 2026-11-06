"""Detect torn glyph contours in the actual two-page bib bitmap."""
import argparse,json,sys
from pathlib import Path
from collections import deque
import bpy,numpy as np
p=argparse.ArgumentParser();p.add_argument('--texture',required=True);p.add_argument('--definition',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);cfg=json.loads(Path(a.definition).read_text())['bib_print'];image=bpy.data.images.load(str(Path(a.texture).resolve()));pixels=np.empty(image.size[0]*image.size[1]*4,np.float32);image.pixels.foreach_get(pixels);pixels=pixels.reshape(image.size[1],image.size[0],4)
rows=[];layout=json.loads(Path(a.texture+'.layout.json').read_text())
for item in layout:
        x0=max(0,int(item['x'])-2);x1=min(image.size[0],int(item['x']+item['width'])+3)
        y0=max(0,image.size[1]-int(item['y']+item['height'])-3);y1=min(image.size[1],image.size[1]-int(item['y'])+2)
        crop=pixels[y0:y1,x0:x1,:3]
        mask=(crop.max(axis=2)<.2);counts=[]
        for y,x in zip(*np.where(mask.copy())):
            if not mask[y,x]:continue
            queue=deque([(y,x)]);mask[y,x]=False;count=0
            while queue:
                yy,xx=queue.popleft();count+=1
                for ny,nx in [(yy-1,xx),(yy+1,xx),(yy,xx-1),(yy,xx+1)]:
                    if 0<=ny<mask.shape[0] and 0<=nx<mask.shape[1] and mask[ny,nx]:mask[ny,nx]=False;queue.append((ny,nx))
            if count>=8:counts.append(count)
        expected=len(item['text'].replace(' ',''));rows.append({'page':item['page'],'text':item['text'],'expected_contiguous_glyphs':expected,'observed_contiguous_glyphs':len(counts),'component_pixels':counts,'pass':len(counts)==expected})
report={'schema_version':1,'id':'m167_actual_bib_print_glyph_continuity','texture':a.texture,'size':list(image.size),'rows':rows,'pass':list(image.size)==[cfg['resolution']]*2 and all(row['pass'] for row in rows),'art_acceptance':False}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('Bib bitmap contains broken or missing glyph contours')
