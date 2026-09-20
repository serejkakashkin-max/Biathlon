"""Select one audited LOLIPOP CC BY fir variant and its authored LODs.
Source files stay in ignored staging. No source scripts or external paths are loaded.
"""
import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector
root=Path.cwd();source=root/'external_downloads/g2_visual_remediation/04_vegetation/gameready/fir_trees_lolipop/extracted'
out=root/'artifacts/g2-fir';out.mkdir(parents=True,exist_ok=True)
textures=root/'Assets/Resources/Biathlon/Vegetation/G2Fir/Textures';textures.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
fbx=source/'source/Christmass trees pack.fbx'
bpy.ops.import_scene.fbx(filepath=str(fbx),use_image_search=False)
scale=8/9.3242853798;outputs=[]
for level,source_lod in enumerate((1,2,2)):
    source_object=bpy.data.objects['Christmas tree_LOD'+str(source_lod)]
    o=source_object.copy();o.data=source_object.data.copy();bpy.context.collection.objects.link(o)
    if level==2:
        bpy.context.view_layer.objects.active=o
        modifier=o.modifiers.new('BoundedFarGeometry','DECIMATE');modifier.ratio=.18;modifier.use_collapse_triangulate=True
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    mesh=o.data;mesh.calc_loop_triangles()
    ground=min((o.matrix_world@v.co-o.location).z for v in mesh.vertices)
    vertices=[];normals=[];uv=[];types=[];indices=[];remap={}
    for tri in mesh.loop_triangles:
        material=mesh.materials[tri.material_index].name
        kind=0 if material=='Bark_Mat' else .5 if material=='Brunches_Mat' else 1
        for loop in tri.loops:
            n=(o.matrix_world.to_3x3()@mesh.corner_normals[loop].vector).normalized();tex=mesh.uv_layers.active.data[loop].uv
            index=mesh.loops[loop].vertex_index;key=(index,tuple(round(f,6) for f in tex),tuple(round(f,5) for f in n),kind)
            if key not in remap:
                remap[key]=len(vertices)//3
                p=o.matrix_world@mesh.vertices[index].co-o.location;p.z-=ground;p*=scale
                vertices.extend((p.x,p.z,-p.y));normals.extend((n.x,n.z,-n.y));uv.extend(tex);types.append(kind)
            indices.append(remap[key])
    path=out/f'fir_lod{level}.json';path.write_text(json.dumps(dict(schema_version=1,id='g2-fir-lod'+str(level),vertices=vertices,normals=normals,uv=uv,kinds=types,indices=indices),separators=(',',':')))
    outputs.append(dict(file=path.name,source_object=source_object.name,decimation=.18 if level==2 else 1,vertices=len(vertices)//3,triangles=len(indices)//3,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
maps=[('BarkAlbedo.png','BarkAlbedo',1024),('BrunchesAlbedo.png','LeafAlbedo',2048),('Bark_normal.png','BarkNormal',1024),('Brunches_normal.png','LeafNormal',1024)]
for name,dest,limit in maps:
    image=bpy.data.images.load(str(source/'textures'/name),check_existing=False)
    size=list(image.size);factor=limit/max(size);image.scale(round(size[0]*factor),round(size[1]*factor))
    path=textures/(dest+'.png');image.filepath_raw=str(path);image.file_format='PNG';image.save()
    outputs.append(dict(file=path.relative_to(root).as_posix(),source_texture=name,source_sha256=hashlib.sha256((source/'textures'/name).read_bytes()).hexdigest(),dimensions=list(image.size),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
manifest=dict(schema_version=1,id='g2-fir-derivation-v1',asset='G2-010',author='LOLIPOP / lolipop_1707',license='CC-BY-4.0',source_url='https://sketchfab.com/3d-models/f58e8b6d733e4b0586e5b7db847b89e7',source_sha256=hashlib.sha256(fbx.read_bytes()).hexdigest(),blender=bpy.app.version_string,modifications=['selected largest fir variant only; source LOD1/2 as near/mid; own 18-percent decimation of LOD2 as far, rejected source cross-card LOD3','removed source gallery offsets; metres, ground pivot, Unity axes; maximum height8m','consolidated material submeshes using explicit per-vertex bark/leaf channels; source UV repeat preserved','four selected resized maps only; alpha preserved; custom instanced winter/wind presentation shader','no source FBX, source material or external absolute image path imported'],outputs=outputs)
(out/'manifest.json').write_text(json.dumps(manifest,indent=2));print(json.dumps(outputs[:3]))
