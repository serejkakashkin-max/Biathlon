"""Project-authored bib artwork baked to UV pages, not floating skinned glyphs."""
import subprocess
from pathlib import Path
import bpy

def apply_bib_print(bib,output,material_factory,materials,settings):
    config=settings['bib_print'];project=Path(__file__).resolve().parents[3]
    padding=config['page_padding_uv'];page_width=.5-2*padding
    path=(Path(output)/'Textures'/(config['texture']+'.png')).resolve()
    definition=project/'Assets/Biathlon/Characters/Biathlete_16_7/Documentation/garment-detail-v1.json'
    subprocess.run(['powershell.exe','-NoProfile','-ExecutionPolicy','Bypass','-File',str(Path(__file__).with_name('draw_bib_atlas.ps1')),'-Definition',str(definition),'-Output',str(path)],check=True,capture_output=True,text=True)
    image=bpy.data.images.load(str(path),check_existing=False);image.colorspace_settings.name='sRGB'
    mat=material_factory(config['material'],(1,1,1),.78);mat.node_tree.nodes.get('Principled BSDF').inputs['Sheen Weight'].default_value=.15
    node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image;mat.node_tree.links.new(node.outputs['Color'],mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
    bib.data.materials.clear();bib.data.materials.append(mat);materials.append(mat)
    for layer in list(bib.data.uv_layers):bib.data.uv_layers.remove(layer)
    uv=bib.data.uv_layers.new(name='BibFrontBackUV')
    for face in bib.data.polygons:
        back=sum(bib.data.vertices[k].co.y for k in face.vertices)/len(face.vertices)>.065
        for index in face.loop_indices:
            point=bib.data.vertices[bib.data.loops[index].vertex_index].co;x=-point.x if back else point.x
            uv.data[index].uv=((.5 if back else 0)+padding+(x/(2*config['half_width_m'])+.5)*page_width,(point.z-config['bottom_m'])/(config['top_m']-config['bottom_m']))
    bib['print_method']='shared_fabric_uv_atlas';bib['print_texture']=config['texture']
    return {'method':'shared_fabric_uv_atlas','texture':config['texture'],'resolution':config['resolution'],'pages':2,'number':config['number'],'font':config['font'],'standalone_bib_glyph_meshes':0}
