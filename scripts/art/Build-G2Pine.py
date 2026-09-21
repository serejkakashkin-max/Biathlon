"""Select one audited LOLIPOP CC BY pine variant and its authored LODs.
Source files stay in ignored staging. No source scripts or external paths are loaded.
"""
import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector
root=Path.cwd();source=root/'external_downloads/g2_visual_remediation/04_vegetation/gameready/pine_trees_lolipop/extracted'
out=root/'artifacts/g2-pine';out.mkdir(parents=True,exist_ok=True)
textures=root/'Assets/Resources/Biathlon/Vegetation/G2Pine/Textures';textures.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
fbx=source/'source/Pine_pack.fbx'
bpy.ops.import_scene.fbx(filepath=str(fbx),use_image_search=False)
base=bpy.data.objects['Pine_medium_1_LOD1']
points=[base.matrix_world@v.co-base.location for v in base.data.vertices]
minimum=min(p.z for p in points);maximum=max(p.z for p in points)
# Source gallery translations are baked into the vertices; use the low trunk ring as pivot.
low=[p for p in points if p.z<minimum+.15]
pivot=Vector((sum(p.x for p in low)/len(low),sum(p.y for p in low)/len(low),0))
scale=8/(maximum-minimum);outputs=[]
for level,source_lod in enumerate((1,2,2)):
    source_object=bpy.data.objects['Pine_medium_1_LOD'+str(source_lod)]
    o=source_object.copy();o.data=source_object.data.copy();bpy.context.collection.objects.link(o)
    if level==2:
        bpy.context.view_layer.objects.active=o
        modifier=o.modifiers.new('ConservativeFarGeometry','DECIMATE');modifier.ratio=.8;modifier.use_collapse_triangulate=True
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    mesh=o.data;mesh.calc_loop_triangles()
    ground=min((o.matrix_world@v.co-o.location).z for v in mesh.vertices)
    height=max((o.matrix_world@v.co-o.location).z for v in mesh.vertices)-ground
    vertices=[];normals=[];uv=[];types=[];indices=[];remap={}
    for tri in mesh.loop_triangles:
        material=mesh.materials[tri.material_index].name
        kind=0 if material=='Bark_Mat' else .5 if material=='Clusters_Mat' else 1
        for loop in tri.loops:
            n=(o.matrix_world.to_3x3()@mesh.corner_normals[loop].vector).normalized();tex=mesh.uv_layers.active.data[loop].uv
            index=mesh.loops[loop].vertex_index;key=(index,tuple(round(f,6) for f in tex),tuple(round(f,5) for f in n),kind)
            if key not in remap:
                remap[key]=len(vertices)//3
                p=o.matrix_world@mesh.vertices[index].co-o.location;p-=pivot;p.x*=scale*.92;p.y*=scale*.92;p.z=(p.z-ground)*8/height
                vertices.extend((p.x,p.z,-p.y));normals.extend((n.x,n.z,-n.y));uv.extend(tex);types.append(kind)
            indices.append(remap[key])
    path=out/f'pine_lod{level}.json';path.write_text(json.dumps(dict(schema_version=1,id='g2-pine-lod'+str(level),vertices=vertices,normals=normals,uv=uv,kinds=types,indices=indices),separators=(',',':')))
    outputs.append(dict(file=path.name,source_object=source_object.name,decimation=.8 if level==2 else 1,vertices=len(vertices)//3,triangles=len(indices)//3,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
maps=[('Bark_basecolor.png','BarkAlbedo',1024),('Cluster_full_basecolor.png','LeafAlbedo',2048),('Bark_normal.png','BarkNormal',1024),('Cluster_full_normal.png','LeafNormal',1024)]
for name,dest,limit in maps:
    image=bpy.data.images.load(str(source/'textures'/name),check_existing=False)
    size=list(image.size);factor=limit/max(size);image.scale(round(size[0]*factor),round(size[1]*factor))
    sources=[dict(name=name,sha256=hashlib.sha256((source/'textures'/name).read_bytes()).hexdigest())]
    if dest=='LeafAlbedo':
        # Pine has a separate audited opacity map; never infer cutout from dark RGB.
        import numpy as np
        alpha=bpy.data.images.load(str(source/'textures/Cluster_full_Opacity.png'),check_existing=False);alpha.scale(*image.size)
        pixels=np.empty(image.size[0]*image.size[1]*4,dtype=np.float32);image.pixels.foreach_get(pixels)
        mask=np.empty_like(pixels);alpha.pixels.foreach_get(mask);pixels[3::4]=mask[0::4]
        combined=bpy.data.images.new('Pine foliage RGBA',width=image.size[0],height=image.size[1],alpha=True);combined.pixels.foreach_set(pixels);image=combined
        sources.append(dict(name='Cluster_full_Opacity.png',sha256=hashlib.sha256((source/'textures/Cluster_full_Opacity.png').read_bytes()).hexdigest()))
    path=textures/(dest+'.png');image.filepath_raw=str(path);image.file_format='PNG';image.save()
    outputs.append(dict(file=path.relative_to(root).as_posix(),sources=sources,dimensions=list(image.size),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
manifest=dict(schema_version=1,id='g2-pine-derivation-v1',asset='G2-011',author='LOLIPOP / lolipop_1707',license='CC-BY-4.0',source_url='https://sketchfab.com/3d-models/e1e9c07b8e2e445c943fec660beefba2',source_sha256=hashlib.sha256(fbx.read_bytes()).hexdigest(),blender=bpy.app.version_string,modifications=['selected medium pine variant1 only; source LOD1/2 as near/mid, conservative80-percent LOD2 far; rejected25-percent far reduction for broken silhouette and LOD0/1/2 selection for excessive submitted geometry; source cross-card excluded','removed baked gallery offsets using low-trunk centroid; metres, ground pivot, Unity axes; each LOD height8m and horizontal scale92percent to fit existing2.5m canopy envelope','consolidated material submeshes using explicit per-vertex bark/leaf channels; source UV repeat preserved','four runtime maps from five selected sources; separate foliage opacity combined into RGBA; custom instanced winter/wind presentation shader','no source FBX, source material or external absolute image path imported'],outputs=outputs)
(out/'manifest.json').write_text(json.dumps(manifest,indent=2));print(json.dumps(outputs[:3]))
