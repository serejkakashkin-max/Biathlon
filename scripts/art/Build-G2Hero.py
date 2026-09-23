"""Offline CC0 character adaptation. Run with Blender --background --disable-autoexec.

Only the selected body is loaded; embedded scripts, source modifiers and materials are
not executed. Output is intermediate JSON, outside Assets. Unity creates native meshes.
The M11 target bone positions are exported from the actual development rig, not guessed.
"""
import argparse
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True)
parser.add_argument('--bones', required=True)
parser.add_argument('--output', required=True)
parser.add_argument('--goggles', required=True)
args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
source = Path(args.source).resolve()
out = Path(args.output).resolve()
out.mkdir(parents=True, exist_ok=True)
targets = json.loads(Path(args.bones).read_text())
names = [b['name'] for b in targets]
assert len(names) == 17 and names[0] == 'Hips' and names[16] == 'RightFoot'

def blender(p):
    return Vector((p[0], -p[2], p[1]))

target = [blender(b['position']) for b in targets]
# Anatomical joint centres of the CC0 A-pose, in its own metre coordinates.
heads = [(0, 0, .89), (0, 0, 1.06), (0, 0, 1.23), (0, -.008, 1.405), (0, -.032, 1.49)]
for s in (-1, 1):
    heads.extend([(s*.208, 0, 1.345), (s*.315, -.035, 1.085), (s*.388, -.071, .876)])
for s in (-1, 1):
    heads.extend([(s*.107, .008, .865), (s*.145, .02, .46), (s*.172, .06, .085)])
heads = [Vector(p) for p in heads]
parents = [-1, 0, 1, 2, 3, 2, 5, 6, 2, 8, 9, 0, 11, 12, 0, 14, 15]
children = [1, 2, 3, 4, -1, 6, 7, -1, 9, 10, -1, 12, 13, -1, 15, 16, -1]
tails = []
target_tails = []
for i, c in enumerate(children):
    if c >= 0:
        tails.append(heads[c]); target_tails.append(target[c])
    else:
        tails.append(heads[i] + Vector((0, -.105, 0) if i in (13, 16) else (0, 0, .18) if i == 4 else (.035 if i == 10 else -.035, -.025, -.14)))
        target_tails.append(target[i] + Vector((0, -.12, 0) if i in (13, 16) else (0, 0, .185) if i == 4 else (0, 0, -.13)))

bpy.ops.wm.read_factory_settings(use_empty=True)
with bpy.data.libraries.load(str(source), link=False) as (available, loaded):
    assert 'GEO-body_male_realistic' in available.objects
    loaded.objects = ['GEO-body_male_realistic']
body = loaded.objects[0]
bpy.context.collection.objects.link(body)
body.name = 'G2HeroBody'
body.matrix_world = Matrix.Identity(4)
body.modifiers.clear()
body.data.materials.clear()
# Insert actual garment boundary edges. Selecting whole source polygons leaves jagged
# collars/hems; bisecting first also interpolates UVs before automatic skin weighting.
bm = bmesh.new(); bm.from_mesh(body.data)
for axis, value in [(2,z) for z in (.125,.82,.885,1.035,1.27,1.35,1.425,1.59,1.595)] + [(0,x) for x in (-.31,-.19,-.18,-.155,-.145,.145,.155,.18,.19,.31)]:
    point=Vector((0,0,0));normal=Vector((0,0,0));point[axis]=value;normal[axis]=1
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,plane_co=point,plane_no=normal,clear_inner=False,clear_outer=False)
bm.to_mesh(body.data);bm.free()
original = [v.co.copy() for v in body.data.vertices]

# Automatic heat weights on the continuous source topology, before rest-pose adaptation.
arm_data = bpy.data.armatures.new('M11SourceBinding')
arm = bpy.data.objects.new('M11SourceBinding', arm_data)
bpy.context.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
arm.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for i, name in enumerate(names):
    b = arm_data.edit_bones.new(name)
    b.head, b.tail = heads[i], tails[i]
    if parents[i] >= 0: b.parent = arm_data.edit_bones[names[parents[i]]]
bpy.ops.object.mode_set(mode='OBJECT')
body.select_set(True)
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
weights = []
for v in body.data.vertices:
    w = [(names.index(body.vertex_groups[g.group].name), g.weight) for g in v.groups if body.vertex_groups[g.group].name in names and g.weight > 1e-6]
    w.sort(key=lambda pair: (-pair[1], pair[0]))
    w = w[:4]
    if not w: raise RuntimeError('Heat weighting left an unbound vertex: '+str(v.index))
    total = sum(x[1] for x in w)
    weights.append([(i, value/total) for i, value in w])
body.modifiers.clear()
body.parent = None

# Scale along each bone only, then rotate to the existing M11 rest axes. Four-weight
# blended positions preserve the source's continuous joints. No runtime retarget cost.
transforms = []
for i in range(17):
    a, b = tails[i]-heads[i], target_tails[i]-target[i]
    q = a.rotation_difference(b)
    transforms.append((a.normalized(), b.length/a.length, q))
for v, p, w in zip(body.data.vertices, original, weights):
    v.co = Vector((0, 0, 0))
    for bone, weight in w:
        direction, scale, rotation = transforms[bone]
        d = p-heads[bone]
        d = d*1.06 + direction * d.dot(direction)*(scale-1.06)
        v.co += (target[bone] + rotation @ d)*weight
for g in list(body.vertex_groups): body.vertex_groups.remove(g)
for n in names: body.vertex_groups.new(name=n)
for v, w in zip(body.data.vertices, weights):
    for bone, weight in w: body.vertex_groups[bone].add([v.index], weight, 'REPLACE')

palette = ['SuitNavy', 'SuitTeal', 'BibIvory', 'Skin', 'GloveRubber', 'BootComposite', 'CapNavy', 'GoggleLens']
for name in palette: body.data.materials.append(bpy.data.materials.new(name))
for p in body.data.polygons:
    c = sum((original[i] for i in p.vertices), Vector()) / len(p.vertices)
    dominant = max(((i, sum(dict(weights[v]).get(i, 0) for v in p.vertices)) for i in range(17)), key=lambda x:x[1])[0]
    material = 0
    if c.z > 1.425: material = 3
    elif abs(c.x) > .31 and c.z < .885: material = 4
    elif c.z < .12: material = 5
    elif abs(c.x) > .19 and .885 < c.z < 1.35: material = 1
    elif c.z < .82 and abs(c.x) > .145: material = 1
    p.material_index = material
    p.use_smooth = True

# A close-fitting garment has less anatomical relief than an undressed base. Smooth
# torso/thigh suit vertices without touching the facial surface or joint weights.
group = body.vertex_groups.new(name='GarmentSmooth')
for i, p in enumerate(original):
    if .68 < p.z < 1.40 and abs(p.x) < .23: group.add([i], 1.0, 'REPLACE')
bpy.context.view_layer.objects.active = body
smooth = body.modifiers.new('GarmentRelax', 'SMOOTH')
smooth.factor = .65; smooth.iterations = 12; smooth.vertex_group = group.name
bpy.ops.object.modifier_apply(modifier=smooth.name)
body.vertex_groups.remove(body.vertex_groups['GarmentSmooth'])

# Separate bib/cap shells follow the same weighted surface; avoid rigid floating panels.
def shell(name, selected, material, offset):
    obj = body.copy(); obj.data = body.data.copy(); obj.name = name
    bpy.context.collection.objects.link(obj)
    bm = bmesh.new(); bm.from_mesh(obj.data); bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.index not in selected], context='FACES')
    bm.normal_update()
    for v in bm.verts: v.co += v.normal*offset
    for f in bm.faces: f.material_index = material; f.smooth = True
    bm.to_mesh(obj.data); bm.free()
    return obj

bib_faces = set(); cap_faces = set()
for p in body.data.polygons:
    c = sum((original[i] for i in p.vertices), Vector()) / len(p.vertices)
    if 1.035 < c.z < 1.35 and abs(c.x) < (.18 if c.z < 1.27 else .155): bib_faces.add(p.index)
    if c.z > 1.59: cap_faces.add(p.index)
bib = shell('RaceBib', bib_faces, 2, .009)
cap = shell('RaceCap', cap_faces, 6, .004)
# Relax the bib across the chest instead of reproducing bare-body muscle relief.
bpy.context.view_layer.objects.active=bib
relax=bib.modifiers.new('BibDrape','SMOOTH');relax.factor=.5;relax.iterations=8
bpy.ops.object.modifier_apply(modifier=relax.name)
for v in bib.data.vertices:
    if abs(v.co.x)<.17 and v.co.y<-.07:
        v.co.y=min(v.co.y,-.134*math.sqrt(max(.1,1-(v.co.x/.225)**2)))
for loop in bib.data.loops:
    p=bib.data.vertices[loop.vertex_index].co
    # Independent print UVs; front/back share one original numbered bib design.
    bib.data.uv_layers.active.data[loop.index].uv=(.5+(-p.x if p.y<0 else p.x)/.38,(p.z-1.175)/.345)

# Remove hidden scalp, bare feet and open hands. Boots/closed gloves are authored
# below in rig space without retaining the source's spread finger geometry.
bm = bmesh.new(); bm.from_mesh(body.data); bm.faces.ensure_lookup_table()
remove = []
for f in bm.faces:
    c = sum((original[v.index] for v in f.verts), Vector())/len(f.verts)
    if c.z < .125 or c.z > 1.595 or (abs(c.x)>.31 and c.z<.885): remove.append(f)
bmesh.ops.delete(bm, geom=remove, context='FACES'); bm.to_mesh(body.data); bm.free()

def boot_part(name, center, size, bone, material, bevel):
    bpy.ops.mesh.primitive_cube_add(size=1, location=blender(center))
    o=bpy.context.object; o.name=name; o.scale=(size[0],size[2],size[1])
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    m=o.modifiers.new('RoundedConstruction','BEVEL');m.width=bevel;m.segments=3
    bpy.ops.object.modifier_apply(modifier=m.name)
    for mat in body.data.materials:o.data.materials.append(mat)
    for p in o.data.polygons:p.material_index=material;p.use_smooth=True
    for n in names:o.vertex_groups.new(name=n)
    o.vertex_groups[bone].add(list(range(len(o.data.vertices))),1,'REPLACE')
    return o

parts=[body,bib,cap]
# Original closed sport gloves. The source's spread fingers are not warped into an
# invalid fist. Separate rounded fingers and thumb wrap the typed grip centre.
def glove_tube(name,points,radius,bone):
    verts=[];faces=[]
    for i,p in enumerate(points):
        tangent=(points[min(i+1,len(points)-1)]-points[max(0,i-1)]).normalized()
        first=tangent.cross(Vector((0,1,0)))
        if first.length<.1:first=tangent.cross(Vector((1,0,0)))
        first.normalize();second=tangent.cross(first).normalized()
        for j in range(10):
            a=j*math.tau/10;verts.append(blender(p+radius*(math.cos(a)*first+math.sin(a)*second)))
        if i:
            for j in range(10):a=(i-1)*10+j;b=(i-1)*10+(j+1)%10;faces.append((a,b,b+10,a+10))
    faces.append(tuple(reversed(range(10))));faces.append(tuple((len(points)-1)*10+j for j in range(10)))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    for mat in body.data.materials:mesh.materials.append(mat)
    for p in mesh.polygons:p.material_index=4;p.use_smooth=True
    for n in names:o.vertex_groups.new(name=n)
    o.vertex_groups[bone].add(list(range(len(mesh.vertices))),1,'REPLACE')
    return o
for side,bone in [(-1,7),(1,10)]:
    wrist=Vector(targets[bone]['position'])
    parts.append(boot_part('GlovePalm',wrist+Vector((0,-.04,-.001)),(.066,.105,.043),bone,4,.018))
    parts.append(boot_part('GloveCuff',wrist+Vector((0,.012,0)),(.063,.030,.058),bone,0,.013))
    for finger in range(4):
        radius=.026-finger*.001
        points=[wrist+Vector((radius*math.sin(a),-.031-finger*.017,.026+radius*math.cos(a))) for a in [(-140+j*280/16)*math.pi/180 for j in range(17)]]
        parts.append(glove_tube('ClosedGloveFinger',points,.0085,bone))
    thumb=[wrist+Vector((-side*x,y,z)) for x,y,z in [(.027,-.014,.0),(.034,-.027,.018),(.033,-.042,.041),(.024,-.055,.053),(.012,-.06,.05)]]
    parts.append(glove_tube('ClosedGloveThumb',thumb,.0105,bone))
# Original broad fabric harness, fitted to the bib. Continuous spine/chest weights.
surface_vertices=[];surface_faces=[];surface_weights=[]
for surface in (body,bib):
    start=len(surface_vertices);surface.data.calc_loop_triangles()
    surface_vertices.extend(v.co.copy() for v in surface.data.vertices)
    surface_weights.extend({names.index(surface.vertex_groups[g.group].name):g.weight for g in v.groups if surface.vertex_groups[g.group].name in names} for v in surface.data.vertices)
    surface_faces.extend(tuple(start+i for i in tri.vertices) for tri in surface.data.loop_triangles)
surface_tree=BVHTree.FromPolygons(surface_vertices,surface_faces,all_triangles=True)
for side in (-1,1):
    path=[(side*.105,1.22,.150),(side*.12,1.35,.150),(side*.145,1.49,.135),(side*.16,1.54,0),
          (side*.14,1.49,-.145),(side*.115,1.35,-.15),(side*.10,1.22,-.145)]
    centres=[]
    for a,b in zip(path,path[1:]):
        for step in range(8):centres.append(Vector(a).lerp(Vector(b),step/8))
    centres.append(Vector(path[-1]))
    verts=[];faces=[];skin_weights=[]
    for x,y,z in centres:
        for edge in (-.014,.014):
            nearest,normal,face,_=surface_tree.find_nearest(blender((x+edge,y,z)))
            verts.append(nearest+normal*.012)
            triangle=surface_faces[face]
            bary=barycentric_transform(nearest,*(surface_vertices[i] for i in triangle),Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)))
            w={}
            for i,factor in zip(triangle,bary):
                for bone,weight in surface_weights[i].items():w[bone]=w.get(bone,0)+max(0,factor)*weight
            skin_weights.append(w)
    for i in range(len(centres)-1):
        a=i*2;faces.extend([(a,a+1,a+3,a+2),(a+2,a+3,a+1,a)])
    mesh=bpy.data.meshes.new('OriginalHarness');mesh.from_pydata(verts,[],faces);mesh.update()
    harness=bpy.data.objects.new('OriginalHarness',mesh);bpy.context.collection.objects.link(harness)
    for mat in body.data.materials:mesh.materials.append(mat)
    for p in mesh.polygons:p.material_index=4;p.use_smooth=True
    for name in names:harness.vertex_groups.new(name=name)
    for v,w in zip(mesh.vertices,skin_weights):
        for bone,weight in w.items():harness.vertex_groups[bone].add([v.index],weight,'REPLACE')
    parts.append(harness)
# Audited CC BY goggles: geometry only. Deliberately discard every source material,
# image/normal map (including lettering atlas); use our plain lens/frame/band materials.
goggles_source=Path(args.goggles).resolve()
with bpy.data.libraries.load(str(goggles_source),link=False) as (available,loaded):
    loaded.objects=['glass','glass.001','glass.002']
for o, material in zip(loaded.objects,[7,4,6]):
    bpy.context.collection.objects.link(o)
    matrix=o.matrix_world.copy()
    for v in o.data.vertices:
        p=matrix@v.co
        front_clearance=.033*max(0,min(1,(p.x+3.5)/2.8))
        v.co=(p.y*.055,-p.x*.051-.103-front_clearance,(p.z-.4)*.035+1.716)
    o.matrix_world=Matrix.Identity(4);o.modifiers.clear();o.data.materials.clear()
    for mat in body.data.materials:o.data.materials.append(mat)
    for p in o.data.polygons:p.material_index=material;p.use_smooth=True
    for n in names:o.vertex_groups.new(name=n)
    o.vertex_groups['Head'].add(list(range(len(o.data.vertices))),1,'REPLACE')
    parts.append(o)
for side, bone in [(-1,13),(1,16)]:
    x=side*.105
    parts.extend([
        boot_part('BootSole',(x,.067,.04),(.106,.026,.272),bone,5,.011),
        boot_part('BootUpper',(x,.107,.047),(.099,.069,.247),bone,4,.028),
        boot_part('BootCuff',(x,.193,-.007),(.084,.146,.10),bone,5,.024),
        boot_part('BootClosure',(x,.203,.043),(.087,.021,.020),bone,1,.008),
    ])

bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=body
bpy.ops.object.join()
body.name='G2Hero'
# Deduplicate material slots introduced by joining accessory meshes.
for p in body.data.polygons:
    p.material_index=palette.index(body.data.materials[p.material_index].name)
while len(body.data.materials)>len(palette):body.data.materials.pop(index=len(body.data.materials)-1)

def export_mesh(obj, path):
    obj.data.calc_loop_triangles()
    vertices=[]; normals=[]; uvs=[]; bone_indices=[]; bone_weights=[]
    sub=[[] for _ in palette]; remap={}
    uv=obj.data.uv_layers.active
    for tri in obj.data.loop_triangles:
        face=[]
        for loop in tri.loops:
            index=obj.data.loops[loop].vertex_index
            v=obj.data.vertices[index]
            normal=obj.data.corner_normals[loop].vector
            tex=uv.data[loop].uv if uv else (0,0)
            key=(index,round(tex[0],6),round(tex[1],6),tuple(round(n,5) for n in normal))
            if key not in remap:
                remap[key]=len(vertices)//3
                p=obj.matrix_world @ v.co
                vertices.extend([p.x,p.z,-p.y]);normals.extend([normal.x,normal.z,-normal.y]);uvs.extend(tex)
                w=[(names.index(obj.vertex_groups[g.group].name),g.weight) for g in v.groups if obj.vertex_groups[g.group].name in names and g.weight>1e-6]
                w=sorted(w,key=lambda x:(-x[1],x[0]))[:4];total=sum(v for _,v in w)
                if total<=0:raise RuntimeError('Unweighted exported vertex')
                w=[(i,v/total) for i,v in w]+[(0,0)]*(4-len(w))
                bone_indices.extend(i for i,_ in w);bone_weights.extend(v for _,v in w)
            face.append(remap[key])
        # (x,y,z) -> (x,z,-y) is a rotation (determinant +1). Preserve winding.
        sub[tri.material_index].extend(face)
    payload={'schema_version':1,'id':path.stem,'materials':palette,'vertices':vertices,'normals':normals,'uv':uvs,'bone_indices':bone_indices,'bone_weights':bone_weights,'submeshes':[{'indices':s} for s in sub]}
    path.write_text(json.dumps(payload,separators=(',',':')))
    return {'file':path.name,'vertices':len(vertices)//3,'triangles':sum(len(s) for s in sub)//3,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}

results=[]
for level, ratio in enumerate([1.0,.45,.12]):
    obj=body.copy();obj.data=body.data.copy();bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active=obj
    if ratio<1:
        dec=obj.modifiers.new('RuntimeLOD','DECIMATE');dec.ratio=ratio;dec.use_collapse_triangulate=True
        bpy.ops.object.modifier_apply(modifier=dec.name)
    results.append(export_mesh(obj,out/f'hero_lod{level}.json'))
    bpy.data.objects.remove(obj,do_unlink=True)
(out/'manifest.json').write_text(json.dumps({'schema_version':1,'id':'g2-hero-body-v1','source_asset':'G2-001','source_file':source.name,'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'license':'CC0-1.0','goggles':{'asset':'G2-032','author':'BlurryRender / arjunmano10','license':'CC-BY-4.0','source':'https://sketchfab.com/3d-models/ski-goggles-29bf30fb9c4d43548106c095505054a2','sha256':hashlib.sha256(goggles_source.read_bytes()).hexdigest(),'modifications':'geometry rescaled/fitted to M11 head; all original textures/materials discarded including lettering/normal atlas; neutral new materials; rigid Head weights; included in three LODs'},'blender':bpy.app.version_string,'bone_contract':targets,'outputs':results,'modifications':['selected male body only; removed source multires', 'automatic heat weights limited and normalized to four M11 bones', 'adapted A-pose to M11 rest axes', 'garment relaxation, own suit panels, bib/cap shells, boots', 'source spread hands removed; original closed sport gloves and cuffs fitted to typed M11 grips', 'surface-weighted harness ribbons; separate front/back bib print UVs and original number atlas', 'three runtime LODs; source materials/textures not imported']},indent=2))
print(json.dumps(results))
