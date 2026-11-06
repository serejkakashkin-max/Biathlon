"""Extract only the owner's CC-BY-4.0 goggle meshes. Keeps original topology/UVs.

Run in Blender: --background --python-exit-code 2 --python this.py -- --input GLB --output DIRECTORY
"""
import argparse,sys,json,hashlib
from pathlib import Path
import bpy
from mathutils import Matrix
p=argparse.ArgumentParser();p.add_argument('--input',required=True);p.add_argument('--output',required=True)
a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);source=Path(a.input);out=Path(a.output);out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(source));deps=bpy.context.evaluated_depsgraph_get();objects=[]
for obj in list(bpy.context.scene.objects):
 if obj.type=='MESH' and obj.data.name.startswith('goggles_'):
  evaluated=obj.evaluated_get(deps);data=bpy.data.meshes.new_from_object(evaluated);data.transform(obj.matrix_world);copy=bpy.data.objects.new(obj.data.name,data);objects.append(copy)
for obj in list(bpy.context.scene.objects):bpy.data.objects.remove(obj,do_unlink=True)
for obj in objects:bpy.context.collection.objects.link(obj)
for _ in range(3):
 for collection in [bpy.data.meshes,bpy.data.armatures,bpy.data.actions,bpy.data.materials,bpy.data.images]:
  for block in list(collection):
   if block.users==0:collection.remove(block)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'SkiingLady_Goggles_Selected.blend'))
license='This work is based on "Skiing Lady" (https://sketchfab.com/3d-models/skiing-lady-52aabb1e0773487b93a6e56d0f0b875b) by lancehuang526 (https://sketchfab.com/lancehuang526), licensed under CC-BY-4.0 (https://creativecommons.org/licenses/by/4.0/).\nSelected goggle meshes only. Original topology and UVs retained; initial evaluated pose baked to world space. No donor body, alpine bindings, gloves or animation transferred. Subsequent PULSE adaptation changes fit, subdivision, colors and materials.\n'
(out/'LICENSE.txt').write_text(license)
record={'schema_version':1,'id':'skiing_lady_selected_goggles','source_url':'https://sketchfab.com/3d-models/skiing-lady-52aabb1e0773487b93a6e56d0f0b875b','author':'lancehuang526','license':'CC-BY-4.0','license_url':'https://creativecommons.org/licenses/by/4.0/','owner_input_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'selected_objects':[o.name for o in objects],'extraction':'evaluated initial goggle meshes, original topology/UVs, world transform baked','selected_blend_sha256':hashlib.sha256((out/'SkiingLady_Goggles_Selected.blend').read_bytes()).hexdigest()}
(out/'provenance.json').write_text(json.dumps(record,indent=2)+'\n');print('EXTRACTED',len(objects))
