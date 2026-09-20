"""Render original bib typography; no font file is copied or distributed."""
from pathlib import Path
import hashlib,json
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[2]
font=Path('C:/Windows/Fonts/arialbd.ttf')
if not font.is_file():raise RuntimeError('Requires the locally installed Arial Bold font for offline rasterization.')
image=Image.new('RGB',(1280,768),'white');draw=ImageDraw.Draw(image)
digits=ImageFont.truetype(str(font),230)
for digit in range(10):
    draw.text(((digit%5)*256+128,(digit//5)*256+125),str(digit),font=digits,fill='black',anchor='mm',stroke_width=0)
caption=ImageFont.truetype(str(font),150)
draw.text((640,640),'BIATHLON',font=caption,fill='black',anchor='mm')
path=root/'Assets/Resources/Biathlon/Athletes/G2Hero/Textures/BibAtlas.png';image.save(path)
(root/'artifacts/g2-hero/bib-atlas-manifest.json').write_text(json.dumps({'schema_version':1,'id':'g2-original-bib-atlas-v1','path':path.relative_to(root).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'origin':'Original BIATHLON layout and ordinary decimal glyph rasterization. Uses locally installed Arial Bold for offline rasterization only; font data is not included in the output or runtime.'},indent=2))
