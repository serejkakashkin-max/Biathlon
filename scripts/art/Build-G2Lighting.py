"""CC0 Passendorf sky, reduced to2K HDR with source sun neutralized."""
import bpy,json,hashlib,numpy as np
from pathlib import Path
r=Path.cwd();source=r/'external_downloads/g2_visual_remediation/07_hdri/passendorf_snow/extracted/passendorf_snow_8k.hdr';folder=r/'Assets/Resources/Biathlon/Weather/G2Lighting';folder.mkdir(parents=True,exist_ok=True)
image=bpy.data.images.load(str(source),check_existing=False);image.scale(2048,1024)
pixels=np.array(image.pixels[:],dtype=np.float32).reshape(1024,2048,4)
bright=np.sum(pixels[520:,:,:3],axis=2);sy,sx=np.unravel_index(np.argmax(bright),bright.shape);sy+=520
for y in range(max(512,sy-115),min(1024,sy+116)):
 for dx in range(-140,141):
  x=(sx+dx)%2048;radius=((dx/140)**2+((y-sy)/115)**2)**.5
  if radius>=1:continue
  weight=1 if radius<.65 else (1-radius)/.35;weight=weight*weight*(3-2*weight)
  fill=(pixels[y,(sx-160)%2048,:3]+pixels[y,(sx+160)%2048,:3])*.5
  pixels[y,x,:3]=pixels[y,x,:3]*(1-weight)+fill*weight
image.pixels.foreach_set(pixels.ravel());image.update();path=folder/'PassendorfSky.hdr';image.filepath_raw=str(path);image.file_format='HDR';image.save()
out=r/'artifacts/g2-lighting';out.mkdir(parents=True,exist_ok=True)
(out/'manifest.json').write_text(json.dumps(dict(schema_version=1,id='g2-lighting-derivation-v1',asset='G2-023',author='Poly Haven',license='CC0',url='https://polyhaven.com/a/passendorf_snow',source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),output=dict(path=path.relative_to(r).as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),width=2048,height=1024),source_sun_pixel=[int(sx),int(sy)],modifications=['8K HDR resized to2K linear HDR','source sun/halo interpolated from same-elevation neighbours; runtime disc follows directional light','shader uses upper hemisphere above haze; photographed ground is not displayed','sky appearance only, not HDR image based illumination']),indent=2)+'\n')
print('2K Passendorf derivative written; source sun:',sx,sy)
