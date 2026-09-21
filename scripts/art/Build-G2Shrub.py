"""Selected CC0 Shrub02 branch skeleton: winter leaf removal, bounded LODs and maps."""
import bpy,bmesh,json,hashlib
from pathlib import Path
from mathutils import Vector
root=Path.cwd();source=root/'external_downloads/g2_visual_remediation/04_vegetation/polyhaven/shrub_02/extracted'
out=root/'artifacts/g2-shrub';out.mkdir(parents=True,exist_ok=True)
textures=root/'Assets/Resources/Biathlon/Vegetation/G2Shrub/Textures';textures.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
fbx=source/'shrub_02/shrub_02_4k.fbx';bpy.ops.import_scene.fbx(filepath=str(fbx),use_image_search=False)
base=bpy.data.objects['shrub_02_d_LOD0'];original_triangles=len(base.data.polygons)
# Inspected atlas: branch polygon UV centroids .07-.15; all leaf islands .20-.93.
# Keep only the woody source geometry. No summer leaves are hidden by a texture trick.
bm=bmesh.new();bm.from_mesh(base.data);uv_layer=bm.loops.layers.uv.active
leaves=[f for f in bm.faces if sum(l[uv_layer].uv.x for l in f.loops)/len(f.loops)>=.18]
removed=len(leaves);bmesh.ops.delete(bm,geom=leaves,context='FACES');bm.to_mesh(base.data);bm.free()
points=[base.matrix_world@v.co for v in base.data.vertices];ground=min(p.z for p in points);low=[p for p in points if p.z<ground+.04]
pivot=Vector((sum(p.x for p in low)/len(low),sum(p.y for p in low)/len(low),ground));outputs=[]
for level,ratio in enumerate((.8,.45,.2)):
 o=base.copy();o.data=base.data.copy();bpy.context.collection.objects.link(o);bpy.context.view_layer.objects.active=o
 modifier=o.modifiers.new('WinterBranchLod','DECIMATE');modifier.ratio=ratio;modifier.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=modifier.name)
 mesh=o.data;mesh.calc_loop_triangles();vertices=[];normals=[];uv=[];kinds=[];indices=[];remap={}
 for tri in mesh.loop_triangles:
  for loop in tri.loops:
   index=mesh.loops[loop].vertex_index;tex=mesh.uv_layers.active.data[loop].uv;n=(o.matrix_world.to_3x3()@mesh.corner_normals[loop].vector).normalized()
   assert tex.x<.2,'Unexpected leaf UV in winter skeleton'
   key=(index,tuple(round(f,6) for f in tex),tuple(round(f,5) for f in n))
   if key not in remap:
    remap[key]=len(vertices)//3;p=o.matrix_world@mesh.vertices[index].co-pivot
    vertices.extend((p.x,p.z,-p.y));normals.extend((n.x,n.z,-n.y));uv.extend(tex);kinds.append(.5)
   indices.append(remap[key])
 path=out/f'shrub_lod{level}.json';path.write_text(json.dumps(dict(schema_version=1,id='g2-winter-shrub-lod'+str(level),vertices=vertices,normals=normals,uv=uv,kinds=kinds,indices=indices),separators=(',',':')))
 outputs.append(dict(file=path.name,ratio=ratio,vertices=len(vertices)//3,triangles=len(indices)//3,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
for name,dest in [('shrub_02_diff_4k.jpg','LeafAlbedo'),('shrub_02_nor_gl_4k.exr','LeafNormal')]:
 path=source/'textures'/name;image=bpy.data.images.load(str(path),check_existing=False)
 if dest=='LeafNormal':image.colorspace_settings.name='Non-Color'
 image.scale(1024,1024);target=textures/(dest+'.png');image.filepath_raw=str(target);image.file_format='PNG';image.save()
 outputs.append(dict(file=target.relative_to(root).as_posix(),source=name,source_sha256=hashlib.sha256(path.read_bytes()).hexdigest(),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),dimensions=list(image.size)))
manifest=dict(schema_version=1,id='g2-shrub-derivation-v1',asset='G2-016',author='Poly Haven',license='CC0',source_url='https://polyhaven.com/a/shrub_02',source_sha256=hashlib.sha256(fbx.read_bytes()).hexdigest(),blender=bpy.app.version_string,
 modifications=['selected shrub02 variant d only; source gallery offset removed using low trunk centroid; original metre scale and ground pivot','removed leaf polygons using inspected separate source UV islands; retained woody branch skeleton for winter','three decimated branch LODs; source normals and UV retained; Unity axes','diffuse and OpenGL normal at1K; EXR normal exported as Non-Color; no leaf/alpha/roughness maps imported','reused project winter shader with snow on upper-facing branches; no source colliders or materials'],source_polygons=original_triangles,removed_leaf_polygons=removed,outputs=outputs)
(out/'manifest.json').write_text(json.dumps(manifest,indent=2));print(json.dumps(outputs[:3]))
