"""Local combined derivatives: CC BY winter garments + restricted Renderpeople heads.
Run after Build-G2Crowd.py. Combined output remains ignored, never publish it.
Source materials/labels are discarded; original neutral vertex materials replace them.
"""
import bpy, bmesh, json, math, hashlib, zipfile
from pathlib import Path
from mathutils import Vector, Matrix

root=Path.cwd();source=root/'external_downloads/g2_visual_remediation/winter';out=root/'artifacts/g2-crowd'
temp=root/'artifacts/g2-owner-feedback/winter';temp.mkdir(parents=True,exist_ok=True)
with zipfile.ZipFile(source/'warm-jacket/source/model.zip') as archive:
    for item in archive.infolist():
        assert (temp/item.filename).resolve().is_relative_to(temp.resolve())
    archive.extractall(temp)
sources=[('leather',source/'black-leather-coat-3d-model-free/source/Leather coat.glb',None,'d12f555e66a14cc287baec58f5ed5c70'),
         ('puffer-matte',source/'puffer-jacket/source/Coat.fbx','Matte','8703efe71fd140539787dabe494cc415'),
         ('puffer-shiny',source/'puffer-jacket/source/Coat.fbx','Shiny','8703efe71fd140539787dabe494cc415'),
         ('warm',temp/'model/model.dae',None,'703dbac11fe44edebd4c895860d2b602')]
manifest=[]
for garment,(name,path,selection,uid) in enumerate(sources):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if path.suffix=='.glb':bpy.ops.import_scene.gltf(filepath=str(path))
    elif path.suffix=='.fbx':bpy.ops.import_scene.fbx(filepath=str(path))
    else:bpy.ops.wm.collada_import(filepath=str(path))
    objects=[o for o in bpy.data.objects if o.type=='MESH' and (selection is None or o.name==selection)]
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();obj=bpy.context.object
    for v in obj.data.vertices:v.co=obj.matrix_world@v.co
    obj.matrix_world=Matrix.Identity(4)
    verts=[v.co for v in obj.data.vertices];lo=Vector([min(v[i] for v in verts) for i in range(3)]);hi=Vector([max(v[i] for v in verts) for i in range(3)])
    center=(lo.x+hi.x)/2;scale=.72/(hi.z-lo.z)
    # Fit the connected source garment with continuous two-bone pose weights.
    # Keep every sleeve face: cutting by X produces visible open shoulder seams.
    def smooth(a,b,x):
        t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
    if name.startswith('puffer'):
        joints=[Vector(p) for p in ((.20,.96,.02),(.31,.75,-.005),(.455,.596,-.112))]
    else:
        joints=[Vector(p) for p in ((.20,.96,-.06),(.34,.72,-.077),(.52,.603,-.25))]
    target=[Vector(p) for p in ((.20,.96,0),(.245,.69,-.055),(.21,.69,-.255))]
    rotations=[(joints[i+1]-joints[i]).rotation_difference(target[i+1]-target[i]).to_matrix() for i in range(2)]
    for vertex in obj.data.vertices:
        v=vertex.co.copy();p=Vector(((v.x-center)*scale,(v.z-lo.z)*scale+.36,v.y*scale))
        if name!='leather':
            side=1 if p.x>=0 else -1;p.x=abs(p.x)
            along=(p-joints[0]).dot((joints[2]-joints[0]).normalized())
            elbow=(joints[1]-joints[0]).length
            w=smooth(elbow-.075,elbow+.075,along)
            posed=(target[0]+rotations[0]@(p-joints[0])).lerp(target[1]+rotations[1]@(p-joints[1]),w)
            # Blend across the shoulder only. Below the armpit, separate sleeves
            # from the torso using the actual source A-pose gap.
            attachment=smooth(.17,.27,p.x)
            p=p.lerp(posed,attachment);p.x*=side
        vertex.co=p
    # The axis conversion reflects handedness. Reverse topology and recalculate geometric normals.
    if obj.data.has_custom_normals:
        bpy.ops.mesh.customdata_custom_splitnormals_clear()
    bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
    for face in obj.data.polygons:face.use_smooth=True
    obj.data.update()
    # Retain full source geometry in near LOD; only distant LOD is reduced.
    original=obj.data.copy()
    for lod in range(2):
        obj.data=original.copy()
        if lod:
            mod=obj.modifiers.new('Distant garment','DECIMATE');mod.ratio=.16;bpy.ops.object.modifier_apply(modifier=mod.name)
        mesh=obj.data;mesh.calc_loop_triangles()
        for face_variant in range(2):
            variant=2+garment*2+face_variant;data=json.loads((out/f'Crowd{face_variant}LOD{lod}.json').read_text())
            clean={k:[] for k in ('vertices','normals','colors','pivots','face_uv','indices')}
            for start in range(0,len(data['indices']),3):
                ids=data['indices'][start:start+3]
                # Replace the whole original coat, keeping hat, face, scarf, legs and gloves.
                if all(data['colors'][i*4+3]>.5 and data['vertices'][i*3+1]<1.1 for i in ids):continue
                for i in ids:
                    clean['indices'].append(len(clean['vertices'])//3)
                    for key,width in [('vertices',3),('normals',3),('colors',4),('pivots',4),('face_uv',3)]:clean[key].extend(data[key][i*width:(i+1)*width])
                    if name=='leather' and data['pivots'][i*4+3]>1:
                        # Original gloves follow the retained downwards leather cuffs.
                        sign=1 if clean['vertices'][-3]>=0 else -1
                        clean['vertices'][-3:]=(clean['vertices'][-3]+sign*(.18-.21),clean['vertices'][-2]-.19,clean['vertices'][-1]+.255)
            for tri in mesh.loop_triangles:
                ids=[]
                for loop in tri.loops:
                    p=mesh.vertices[mesh.loops[loop].vertex_index].co;n=mesh.corner_normals[loop].vector
                    ids.append(len(clean['vertices'])//3);clean['vertices'].extend(p);clean['normals'].extend(n)
                    clean['colors'].extend((.8,.8,.8,1) if name!='leather' else (.075,.075,.075,0))
                    arm=abs(p.x)>.20 and p.y<1.02 and p.y>.58
                    pivot=(0,0,0,0)
                    clean['pivots'].extend(pivot);clean['face_uv'].extend((0,0,0))
                # Coordinates already use Unity axes. Blender input was reflected at the initial mapping.
                clean['indices'].extend(ids)
            clean.update(schema_version=1,id=f'g2-winter-{name}-{face_variant}-{lod}')
            dest=out/f'Crowd{variant}LOD{lod}.json';dest.write_text(json.dumps(clean,separators=(',',':')))
            manifest.append(dict(variant=variant,garment=name,face_variant=face_variant,lod=lod,source_uid=uid,source_sha256=hashlib.sha256(path.read_bytes()).hexdigest(),triangles=len(clean['indices'])//3,output_sha256=hashlib.sha256(dest.read_bytes()).hexdigest()))
(out/'winter-manifest.json').write_text(json.dumps(dict(schema_version=1,id='winter-crowd-derivation-v1',variants=10,outputs=manifest,modifications=['source materials, textures and leather label discarded; original neutral materials','connected source sleeves posed for seated spectators; original gloves; full garment geometry near, reduced distant LOD','combined with restricted Renderpeople heads: local-only output']),indent=2))
print(json.dumps(manifest))
