"""Controlled CC0 Poly Haven Snow02 texture derivatives; run in hidden Blender."""
import bpy,json,hashlib
from pathlib import Path
root=Path.cwd();source=root/'external_downloads/g2_visual_remediation/05_snow_terrain/materials/snow_02/extracted/textures'
out=root/'Assets/Resources/Biathlon/Terrain/G2Snow/Textures';out.mkdir(parents=True,exist_ok=True)
outputs=[]
for suffix,name,limit in [('diff','SnowAlbedo',2048),('nor_gl','SnowNormal',2048),('rough','SnowRoughness',1024)]:
    src=source/('snow_02_'+suffix+'_4k.jpg');image=bpy.data.images.load(str(src),check_existing=False)
    image.scale(limit,limit);path=out/(name+'.png');image.filepath_raw=str(path);image.file_format='PNG';image.save()
    outputs.append(dict(path=path.relative_to(root).as_posix(),source_file=src.name,source_sha256=hashlib.sha256(src.read_bytes()).hexdigest(),dimensions=list(image.size),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
manifest=dict(schema_version=1,id='g2-snow-derivation-v1',asset='G2-017',author='Poly Haven',license='CC0-1.0',source='https://polyhaven.com/a/snow_02',modifications=['selected diffuse/OpenGL normal/roughness only; resized to2K/2K/1K','new world-metre blended material preserving original terrain layer weights; no simulation surface change'],outputs=outputs)
folder=root/'artifacts/g2-snow';folder.mkdir(parents=True,exist_ok=True);(folder/'manifest.json').write_text(json.dumps(manifest,indent=2));print(json.dumps(outputs))
