"""Original layered textiles and kits from the game's one national palette."""
import bpy,numpy as np,math,json,hashlib
from pathlib import Path
from refine_garments import analytic_panel_material,GARMENT_SETTINGS
ROOT=Path(__file__).resolve().parents[3]
def save_image(name,rgb,out,linear=False):
    h,w=rgb.shape[:2];im=bpy.data.images.new(name,width=w,height=h,alpha=True);im.colorspace_settings.name='Non-Color'if linear else'sRGB'
    # Blender's byte-image pixel buffer stores encoded colour values. Encode
    # linear palette values exactly once before PNG export; Unity decodes sRGB.
    if not linear:rgb=np.where(rgb<=.0031308,rgb*12.92,1.055*np.maximum(rgb,0)**(1/2.4)-.055)
    pixels=np.ones((h,w,4),np.float32);pixels[:,:,:rgb.shape[2]]=rgb;im.pixels.foreach_set(pixels.ravel());im.filepath_raw=str(out/'Textures'/(name+'.png'));im.file_format='PNG';im.save();return im
def textile_maps(out):
    rows={}
    for label,n,threads,amplitude,rough in [('Suit',4096,480,.11,.77),('Bib',2048,290,.16,.85)]:
        u=np.arange(n,dtype=np.float32)[None,:]/n;v=np.arange(n,dtype=np.float32)[:,None]/n
        warp=np.cos(u*math.tau*threads);weft=np.cos(v*math.tau*threads);alternation=np.sin((u+v)*math.tau*threads*.5)
        height=(warp+weft)*amplitude*.4+alternation*amplitude*.2
        dx=np.gradient(height,axis=1)*3;dy=np.gradient(height,axis=0)*3;normal=np.stack([-dx,-dy,np.ones_like(height)],axis=2);normal/=np.linalg.norm(normal,axis=2,keepdims=True)
        normalmap=save_image('Natural_'+label+'_Knit_Normal_'+('4K'if label=='Suit'else'2K'),normal*.5+.5,out,True)
        packed=np.zeros((n,n,4),np.float32);packed[:,:,3]=1-np.clip(rough+height*.35,.55,.94)
        surface=save_image('Natural_'+label+'_MetallicSmoothness_'+('4K'if label=='Suit'else'2K'),packed,out,True);rows[label]=(normalmap,surface)
    return rows
def finish_suit(suit,mat,red,black,out):
    palette_path=ROOT/'Assets/Resources/Biathlon/athlete_appearance_v1.json';palette=json.loads(palette_path.read_text());nodes=mat.node_tree.nodes;links=mat.node_tree.links;output=nodes.get('Material Output');principal=nodes.get('Principled BSDF')
    emission=nodes.new('ShaderNodeEmission');links.new(analytic_panel_material(mat,red,black,True),emission.inputs[0]);links.new(emission.outputs[0],output.inputs['Surface'])
    mask=bpy.data.images.new('Natural_Kit_Semantic_Mask',width=4096,height=4096,alpha=False);mask.colorspace_settings.name='Non-Color';target=nodes.new('ShaderNodeTexImage');target.image=mask;nodes.active=target
    bpy.ops.object.select_all(action='DESELECT');suit.select_set(True);bpy.context.view_layer.objects.active=suit;bpy.ops.object.bake(type='EMIT')
    source=out/'Models/Source~';source.mkdir(parents=True,exist_ok=True);mask.filepath_raw=str(source/'Natural_Kit_Semantic_Mask.png');mask.file_format='PNG';mask.save()
    values=np.empty(4096*4096*4,np.float32);mask.pixels.foreach_get(values);channels=np.clip(values.reshape(4096,4096,4)[:,:,:3],0,1);total=channels.sum(axis=2);channels/=np.maximum(total,1)[:,:,None]
    graphite=np.array([.016,.021,.029],np.float32);u=np.arange(4096,dtype=np.float32)[None,:];v=np.arange(4096,dtype=np.float32)[:,None];yarn=1+.012*np.cos((u+v)*math.tau/13)
    def colour(hex):
        rgb=np.array([int(hex[i:i+2],16)/255 for i in [1,3,5]],np.float32);return np.where(rgb<=.04045,rgb/12.92,((rgb+.055)/1.055)**2.4)
    kits=[];default=None
    for i,nation in enumerate(palette['nations']):
        rgb=graphite*(1-channels.sum(axis=2))[:,:,None]
        for k,key in enumerate(['kits','trims','accents']):rgb+=channels[:,:,k,None]*colour(palette[key][i])
        image=save_image('Natural_Kit_'+nation+'_BaseColor_4K',np.clip(rgb*yarn[:,:,None],0,1),out)
        if i==0:default=image
        kits.append({'nation':nation,'texture':image.name,'primary':palette['kits'][i],'trim':palette['trims'][i],'accent':palette['accents'][i]})
    links.new(principal.outputs[0],output.inputs['Surface']);node=nodes.new('ShaderNodeTexImage');node.image=default;links.new(node.outputs['Color'],principal.inputs['Base Color'])
    maps=textile_maps(out)
    n=nodes.new('ShaderNodeTexImage');n.image=maps['Suit'][0];normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=GARMENT_SETTINGS['textile_normal_strength'];links.new(n.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs[0],principal.inputs['Normal'])
    s=nodes.new('ShaderNodeTexImage');s.image=maps['Suit'][1];invert=nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1;links.new(s.outputs['Alpha'],invert.inputs[1]);links.new(invert.outputs[0],principal.inputs['Roughness']);principal.inputs['Metallic'].default_value=0;principal.inputs['Sheen Weight'].default_value=.24
    return {'schema_version':1,'id':'m167_national_textiles_v1','palette_path':palette_path.relative_to(ROOT).as_posix(),'palette_sha256':hashlib.sha256(palette_path.read_bytes()).hexdigest(),'kits':kits,'normal_maps':'original paired warp/weft tangent normals; separate fine suit and coarser bib knit','surface_maps':'zero metallic RGB, variable smoothness in alpha','default_nation':palette['nations'][0]}
def finish_bib(mat,out):
    nodes=mat.node_tree.nodes;links=mat.node_tree.links;p=nodes.get('Principled BSDF');p.inputs['Metallic'].default_value=0;p.inputs['Sheen Weight'].default_value=.20
    node=nodes.new('ShaderNodeTexImage');node.image=bpy.data.images['Natural_Bib_Knit_Normal_2K'];normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=GARMENT_SETTINGS['bib_normal_strength'];links.new(node.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs[0],p.inputs['Normal'])
    node=nodes.new('ShaderNodeTexImage');node.image=bpy.data.images['Natural_Bib_MetallicSmoothness_2K'];invert=nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1;links.new(node.outputs['Alpha'],invert.inputs[1]);links.new(invert.outputs[0],p.inputs['Roughness'])
