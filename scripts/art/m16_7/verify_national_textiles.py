"""Read actual packed albedos/PBR surfaces, and compare against the canonical palette."""
import bpy,numpy as np,json,sys,argparse,hashlib
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--asset',required=True);p.add_argument('--output',required=True);p.add_argument('--textures-dir')
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);bpy.ops.wm.open_mainfile(filepath=str(Path(a.asset).resolve()))
root=Path(__file__).resolve().parents[3];palette_path=root/'Assets/Resources/Biathlon/athlete_appearance_v1.json';palette=json.loads(palette_path.read_text());rows=[]
def pixels(image):
    data=np.empty(image.size[0]*image.size[1]*4,np.float32);image.pixels.foreach_get(data);return data.reshape(image.size[1],image.size[0],4)
def colour(hex):
    c=np.array([int(hex[i:i+2],16)/255 for i in [1,3,5]],np.float32);return np.where(c<=.04045,c/12.92,((c+.055)/1.055)**2.4)
mask=pixels(bpy.data.images['Natural_Kit_Semantic_Mask'])[::8,::8,:3]
for i,nation in enumerate(palette['nations']):
    name='Natural_Kit_'+nation+'_BaseColor_4K'
    image=bpy.data.images.load(str(Path(a.textures_dir)/(name+'.png')),check_existing=False)if a.textures_dir else bpy.data.images.get(name) or bpy.data.images.load(str(Path(a.asset).resolve().parents[2]/'Textures'/(name+'.png')))
    data=pixels(image)[::8,::8,:3];data=np.where(data<=.04045,data/12.92,((data+.055)/1.055)**2.4);errors={}
    for channel,key in enumerate(['kits','trims','accents']):
        selected=mask[:,:,channel]>.995;expected=colour(palette[key][i]);error=float(np.max(np.abs(data[selected]-expected)))if np.any(selected)else 1.
        errors[key]={'samples':int(selected.sum()),'maximum_linear_colour_error':error}
    rows.append({'nation':nation,'resolution':list(image.size),'channels':errors,'pass':list(image.size)==[4096,4096]and all(v['samples']>20 and v['maximum_linear_colour_error']<.025 for v in errors.values())})
for label in ['Suit','Bib']:
    resolution='4K'if label=='Suit'else'2K';packed=pixels(bpy.data.images['Natural_'+label+'_MetallicSmoothness_'+resolution]);normal=pixels(bpy.data.images['Natural_'+label+'_Knit_Normal_'+resolution])
    metal=float(np.max(np.abs(packed[:,:,:3])));smooth=[float(packed[:,:,3].min()),float(packed[:,:,3].max())];variance=float(np.std(normal[:,:,:2]))
    rows.append({'fabric':label,'metallic_maximum':metal,'smoothness_range':smooth,'tangent_normal_variation':variance,'pass':metal<1e-6 and .04<smooth[0]<smooth[1]<.5 and variance>.005})
report={'schema_version':1,'id':'m167_actual_national_pbr_textile_gate','pass':all(r['pass']for r in rows),'palette_sha256':hashlib.sha256(palette_path.read_bytes()).hexdigest(),'rows':rows}
Path(a.output).write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if not report['pass']:raise RuntimeError('National textile palette or PBR maps differ from definition')
