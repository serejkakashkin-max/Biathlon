"""Selected CC0 material derivatives only; run in Blender background, no add-ons."""
import bpy, hashlib, json, sys
from pathlib import Path
import numpy as np
root=Path(__file__).resolve().parents[2]
stage=root/'external_downloads/g2_visual_remediation/08_materials'
out=root/'Assets/Resources/Biathlon/Athletes/G2Hero/Textures';out.mkdir(parents=True,exist_ok=True)
records=[]
for name, source in [
    ('SuitNormal',stage/'fabric/bi_stretch/extracted/textures/bi_stretch_nor_gl_4k.jpg'),
    ('BibNormal',stage/'fabric/terlenka/extracted/textures/terlenka_nor_gl_4k.jpg'),
    ('SkinNormal',stage/'skin/blender_cc0_skin_stamps/extracted/CGC_skin-stamps_CC0/CGC_skin-pores-small.png')]:
    image=bpy.data.images.load(str(source),check_existing=False)
    image.colorspace_settings.name='Non-Color';image.scale(1024,1024)
    if name=='SkinNormal':
        pixels=np.empty(1024*1024*4,dtype=np.float32);image.pixels.foreach_get(pixels)
        height=pixels.reshape((1024,1024,4))[:,:,0]
        dx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*.7
        dy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*.7
        normal=np.stack((-dx,-dy,np.ones_like(dx)),axis=-1)
        normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
        rgba=np.ones((1024,1024,4),dtype=np.float32);rgba[:,:,:3]=normal*.5+.5
        image.pixels.foreach_set(rgba.reshape(-1))
    path=out/(name+'.png');image.filepath_raw=str(path);image.file_format='PNG';image.save()
    records.append({'output':str(path.relative_to(root)).replace('\\','/'),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'source':str(source.relative_to(root)).replace('\\','/'),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'license':'CC0-1.0','changes':'1024px runtime normal; skin height converted to a mild tangent normal' if name=='SkinNormal' else 'selected OpenGL normal resized from 4K to 1K'})
(root/'artifacts/g2-hero/material-manifest.json').write_text(json.dumps({'schema_version':1,'id':'g2-hero-materials-v1','outputs':records},indent=2))
