"""Audited CC0 eyes from the same G2 human base, fitted with the retained Head map.
Run Blender --background --factory-startup --disable-autoexec --python-exit-code 1.
No materials, textures, embedded scripts or unrelated objects are imported.
"""
import json
from pathlib import Path
import bpy
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
import math

root=Path(__file__).resolve().parents[2]
source=root/'external_downloads/g2_visual_remediation/01_characters/hero_base/blender_human_base_v1.4.1/extracted/human-base-meshes-bundle-v1.4.1/human_base_meshes_bundle.blend'
output=root/'artifacts/g2-creator';output.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
with bpy.data.libraries.load(str(source),link=False) as (available,loaded):
    loaded.objects=['GEO-body_male_realistic','GEO-body_male_realistic.eye.R','GEO-body_male_realistic.eye.L']
body,left,right=loaded.objects
results=[]
for lod,ratio in enumerate((1,.45,.05)):
    base=json.loads((root/f'artifacts/g2-hero/hero_lod{lod}.json').read_text())
    base_vertices=[Vector(base['vertices'][i:i+3]) for i in range(0,len(base['vertices']),3)]
    skin_triangles=[tuple(base['submeshes'][3]['indices'][i:i+3]) for i in range(0,len(base['submeshes'][3]['indices']),3)]
    surface=BVHTree.FromPolygons(base_vertices,skin_triangles,all_triangles=True)
    data=dict(schema_version=1,id='g2-creator-eyes-lod'+str(lod),vertices=[],normals=[],uv=[],front=[],tags=[],bone_indices=[],bone_weights=[],indices=[],headwear_indices=[])
    def vertex(p,n,uv,tag,front=1,bones=None,weights=None):
        at=len(data['vertices'])//3;data['vertices'].extend(p);data['normals'].extend(n);data['uv'].extend(uv);data['front'].append(front);data['tags'].append(tag)
        data['bone_indices'].extend(bones or [4,0,0,0]);data['bone_weights'].extend(weights or [1,0,0,0]);return at
    def face_depth(x,y,default):
        hit,n,_,_=surface.ray_cast(Vector((x,y,.35)),Vector((0,0,-1)))
        return default if hit is None else hit.z
    def face_normal(p):
        hit,n,index,_=surface.ray_cast(Vector((p.x,p.y,.35)),Vector((0,0,-1)))
        if hit is None:return Vector((0,0,1))
        tri=skin_triangles[index];a,b,c=(base_vertices[i] for i in tri)
        area=(b-a).cross(c-a).length
        if area<1e-10:return n
        bary=[(b-hit).cross(c-hit).length/area,(c-hit).cross(a-hit).length/area,(a-hit).cross(b-hit).length/area]
        return sum((Vector(base['normals'][i*3:i*3+3])*w for i,w in zip(tri,bary)),Vector()).normalized()
    def patch(points,triangles,tag,uvs):
        ns=[Vector((0,0,0)) for _ in points]
        for a,b,c in triangles:
            n=(points[b]-points[a]).cross(points[c]-points[a]);
            if n.z<0:n=-n
            for i in (a,b,c):ns[i]+=n
        offset=len(data['vertices'])//3
        for p,n,uv in zip(points,ns,uvs):vertex(p,face_normal(p) if tag==0 else n.normalized() if n.length else Vector((0,0,1)),uv,tag)
        for tri in triangles:
            a,b,c=(points[i] for i in tri)
            data['indices'].extend(offset+i for i in (tri if (b-a).cross(c-a).z>=0 else tuple(reversed(tri))))
    for original in (left,right):
        obj=original.copy();obj.data=original.data.copy();obj.modifiers.clear();bpy.context.collection.objects.link(obj)
        rotation=original.rotation_euler.to_matrix()
        def fit(p):
            p=rotation@p+original.location-body.location
            return Vector((p.x*1.06,1.65+(p.z-1.49)*(.185/.18),-(p.y+.032)*1.06))
        centre=fit(Vector((0,0,0)))-Vector((0,0,.002))
        for v in obj.data.vertices:v.co=fit(v.co)-Vector((0,0,.002))
        obj.matrix_world=Matrix.Identity(4);bpy.context.view_layer.objects.active=obj
        if ratio<1:
            mod=obj.modifiers.new('Bounded eye LOD','DECIMATE');mod.ratio=ratio
            bpy.ops.object.modifier_apply(modifier=mod.name)
        mesh=obj.data;mesh.calc_loop_triangles();remap={}
        for tri in mesh.loop_triangles:
            for loop in tri.loops:
                index=mesh.loops[loop].vertex_index
                if index not in remap:
                    remap[index]=len(data['vertices'])//3;p=mesh.vertices[index].co;n=mesh.corner_normals[loop].vector
                    vertex(p,n,(.5+(p.x-centre.x)/(.028*1.06),.5+(p.y-centre.y)/(.028*.185/.18)),1,1 if p.z>=centre.z else -1)
                data['indices'].append(remap[index])
        bpy.data.objects.remove(obj,do_unlink=True)
        # Real upper/lower eyelid coverage, not a white sphere painted over the face.
        # The outer annulus follows the retained CC0 face; its inner edge follows the
        # sclera's surface and creates the anatomical almond aperture.
        segments=(40,24,8)[lod];rows=(4,3,1)[lod];points=[];uvs=[];triangles=[]
        rx=.014*1.06;ry=.014*.185/.18;rz=rx
        for row in range(rows+1):
            t=row/rows
            for j in range(segments):
                angle=j*math.tau/segments;cx=math.cos(angle);sy=math.sin(angle)
                inner=Vector((centre.x+rx*.94*cx,centre.y+ry*(.27 if sy>0 else .31)*sy,0))
                inner.z=centre.z+rz*math.sqrt(max(0,1-((inner.x-centre.x)/rx)**2-((inner.y-centre.y)/ry)**2))+.0004
                outer=Vector((centre.x+rx*1.23*cx,centre.y+ry*.76*sy,0));outer.z=face_depth(outer.x,outer.y,centre.z+.006)+.00005
                p=inner.lerp(outer,t);points.append(p);uvs.append((.5+(p.x-centre.x)/.06,.5+(p.y-centre.y)/.05))
                if row:
                    a=(row-1)*segments+j;b=(row-1)*segments+(j+1)%segments;c=row*segments+j;d=row*segments+(j+1)%segments;triangles.extend(((a,b,c),(b,d,c)))
        patch(points,triangles,0,uvs)
        # Small authored brow strips follow the actual forehead surface.
        points=[];uvs=[];triangles=[];steps=(16,10,4)[lod]
        for j in range(steps+1):
            x=(j/steps*2-1)*.0175;arch=(1-(x/.0175)**2)*.002
            for edge in (-1,1):
                y=centre.y+.018+arch+edge*.00125;p=Vector((centre.x+x,y,face_depth(centre.x+x,y,centre.z+.005)+.0006));points.append(p);uvs.append((j/steps,(edge+1)/2))
            if j:
                a=(j-1)*2;triangles.extend(((a,a+1,a+2),(a+1,a+3,a+2)))
        patch(points,triangles,5,uvs)
    # A retained scalp surface closes the forehead when the hairline is lifted.
    # Both scalp and headband reuse the same audited cap topology/skin weights.
    cap_triangles=[tuple(base['submeshes'][6]['indices'][i:i+3]) for i in range(0,len(base['submeshes'][6]['indices']),3) if all(base_vertices[j].y>1.748 for j in base['submeshes'][6]['indices'][i:i+3])]
    for band in (False,True):
        remap={}
        for tri in cap_triangles:
            if band and min(base_vertices[i].y for i in tri)>1.786:continue
            for index in tri:
                if index not in remap:
                    p=base_vertices[index];n=Vector(base['normals'][index*3:index*3+3]);p=p+n*(.0009 if band else -.0020)
                    remap[index]=vertex(p,n,base['uv'][index*2:index*2+2],6 if band else 0,bones=base['bone_indices'][index*4:index*4+4],weights=base['bone_weights'][index*4:index*4+4])
                data['headwear_indices' if band else 'indices'].append(remap[index])
    target=output/f'eyes_lod{lod}.json';target.write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')
    results.append(dict(file=target.name,vertices=len(data['vertices'])//3,triangles=len(data['indices'])//3))
(output/'manifest.json').write_text(json.dumps(dict(schema_version=1,id='g2-creator-eyes-v1',source_asset='G2-001',license='CC0-1.0',objects=[left.name,right.name],outputs=results),indent=2),encoding='utf-8')
print(json.dumps(results))
