"""Selected Poly Haven CC0 Boulder01 derivatives for benchmark ground dressing."""
import bpy,json,hashlib
from pathlib import Path
root=Path.cwd();source=root/'external_downloads/g2_visual_remediation/05_snow_terrain/rocks/boulder_01/extracted'
out=root/'artifacts/g2-rock';out.mkdir(parents=True,exist_ok=True)
textures=root/'Assets/Resources/Biathlon/Terrain/G2Rock/Textures';textures.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
fbx=source/'boulder_01/boulder_01_4k.fbx';bpy.ops.import_scene.fbx(filepath=str(fbx),use_image_search=False)
outputs=[]
for level,ratio in enumerate((.20,.06)):
    src=bpy.data.objects['boulder_01_LOD3'];o=src.copy();o.data=src.data.copy();bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o
    mod=o.modifiers.new('BoundedRock','DECIMATE');mod.ratio=ratio;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
    m=o.data;m.calc_loop_triangles();points=[o.matrix_world@v.co for v in m.vertices];ground=min(p.z for p in points)
    center=[(min(p[i] for p in points)+max(p[i] for p in points))*.5 for i in (0,1)]
    vertices=[];normals=[];uv=[];indices=[];remap={}
    for tri in m.loop_triangles:
        for loop in tri.loops:
            vi=m.loops[loop].vertex_index;n=(o.matrix_world.to_3x3()@m.corner_normals[loop].vector).normalized();tex=m.uv_layers.active.data[loop].uv
            key=(vi,tuple(round(f,6) for f in tex),tuple(round(f,5) for f in n))
            if key not in remap:
                remap[key]=len(vertices)//3;p=points[vi];vertices.extend((p.x-center[0],p.z-ground,-(p.y-center[1])));normals.extend((n.x,n.z,-n.y));uv.extend(tex)
            indices.append(remap[key])
    path=out/f'rock_lod{level}.json';path.write_text(json.dumps(dict(schema_version=1,id='g2-rock-lod'+str(level),vertices=vertices,normals=normals,uv=uv,indices=indices),separators=(',',':')))
    outputs.append(dict(file=path.name,source_object=src.name,decimation=ratio,vertices=len(vertices)//3,triangles=len(indices)//3,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
for suffix,ext,name,size in [('diff','jpg','RockAlbedo',2048),('nor_gl','exr','RockNormal',1024),('rough','exr','RockRoughness',1024)]:
    src=source/'textures'/('boulder_01_'+suffix+'_4k.'+ext);im=bpy.data.images.load(str(src),check_existing=False)
    if name!='RockAlbedo':im.colorspace_settings.name='Non-Color'
    im.scale(size,size);path=textures/(name+'.png');im.filepath_raw=str(path);im.file_format='PNG';im.save()
    outputs.append(dict(file=path.relative_to(root).as_posix(),source_file=src.name,source_sha256=hashlib.sha256(src.read_bytes()).hexdigest(),dimensions=list(im.size),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
manifest=dict(schema_version=1,id='g2-rock-derivation-v1',asset='G2-020',author='Poly Haven',license='CC0-1.0',source_url='https://polyhaven.com/a/boulder_01',source_sha256=hashlib.sha256(fbx.read_bytes()).hexdigest(),blender=bpy.app.version_string,modifications=['source LOD3 reduced to20/6percent; centered ground pivot and Unity axes, original metre scale','selected diffuse/OpenGL normal/roughness resized to2K/1K/1K; explicit linear EXR data conversion','project partial snow material; off-route visual-only bounded placement, no colliders'],outputs=outputs)
(out/'manifest.json').write_text(json.dumps(manifest,indent=2));print(json.dumps(outputs[:2]))
