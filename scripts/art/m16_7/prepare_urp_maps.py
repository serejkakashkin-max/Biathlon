"""Lossless PBR channel packing only; no beauty-image generation."""
from pathlib import Path
import bpy,numpy as np
import argparse,sys
p=argparse.ArgumentParser();p.add_argument('--textures',required=True);a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);root=Path(a.textures).resolve()
for roughness,out in [('Textile_Roughness_4K','Textile_MetallicSmoothness_4K'),('Skin_ORM_2K','Skin_MetallicSmoothness_2K')]:
    image=bpy.data.images.load(str(root/(roughness+'.png')));image.colorspace_settings.name='Non-Color'
    values=np.empty(image.size[0]*image.size[1]*4,dtype=np.float32);image.pixels.foreach_get(values);values=values.reshape((-1,4));rough=values[:,1].copy();values[:]=0;values[:,3]=1-rough
    packed=bpy.data.images.new(out,width=image.size[0],height=image.size[1],alpha=True);packed.colorspace_settings.name='Non-Color';packed.pixels.foreach_set(values.ravel());packed.filepath_raw=str(root/(out+'.png'));packed.file_format='PNG';packed.save()
    print(out,list(image.size))
