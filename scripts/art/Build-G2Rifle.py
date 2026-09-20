"""Original unbranded presentation prop; all coordinates use the retained M11 sockets.
No functional firearm data or third-party geometry is imported.
Run with Blender --background --factory-startup --disable-autoexec.
"""
import bpy, json, math, hashlib
from pathlib import Path
from mathutils import Vector, Matrix
bpy.ops.wm.read_factory_settings(use_empty=True)
materials=[bpy.data.materials.new(n) for n in ('RifleComposite','RifleMetal','RifleRubber','RifleAccent')]
parts=[]
def coord(p):return Vector((p[0],-p[2],p[1]))
def finish(o,name,material,bevel=0):
    o.name=name
    bpy.context.view_layer.objects.active=o
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        m=o.modifiers.new('EdgeSoftening','BEVEL');m.width=bevel;m.segments=2
        bpy.ops.object.modifier_apply(modifier=m.name)
        m=o.modifiers.new('SurfaceNormals','WEIGHTED_NORMAL');m.keep_sharp=True
        bpy.ops.object.modifier_apply(modifier=m.name)
    for mat in materials:o.data.materials.append(mat)
    for f in o.data.polygons:f.material_index=material
    parts.append(o)
def box(name,at,size,material,bevel=.003):
    bpy.ops.mesh.primitive_cube_add(size=1,location=coord(at));o=bpy.context.object
    o.scale=(size[0],size[2],size[1]);finish(o,name,material,bevel)
def rod(name,a,b,r,material):
    a,b=coord(a),coord(b);direction=b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=direction.length,location=(a+b)*.5)
    o=bpy.context.object;o.rotation_mode='QUATERNION';o.rotation_quaternion=Vector((0,0,1)).rotation_difference(direction)
    finish(o,name,material,.0007)
def profile(name,yz,width,material):
    verts=[coord((x,y,z)) for x in (-width*.5,width*.5) for y,z in yz];n=len(yz)
    faces=[tuple(reversed(range(n))),tuple(range(n,n*2))]
    for i in range(n):j=(i+1)%n;faces.append((i,j,j+n,i+n))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);finish(o,name,material,.005)
def ring(name,at,outer,inner,depth,material):
    verts=[];faces=[]
    for z in (-depth*.5,depth*.5):
        for r in (outer,inner):
            for i in range(16):
                a=i*math.tau/16;verts.append(coord((at[0]+r*math.cos(a),at[1]+r*math.sin(a),at[2]+z)))
    for i in range(16):
        j=(i+1)%16
        faces.extend([(i,j,j+32,i+32),(i+16,i+48,j+48,j+16),(i,i+16,j+16,j),(i+32,j+32,j+48,i+48)])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);finish(o,name,material)
# A continuous original competition-stock silhouette, not scaled ellipsoids.
profile('Stock',[(.033,.035),(.018,.235),(-.009,.315),(-.042,.302),(-.059,.18),(-.065,.04)],.060,0)
box('Butt plate',(0,-.013,.012),(.077,.151,.020),2,.007)
box('Cheek piece',(0,.040,.142),(.066,.026,.176),2,.008)
for z in (.087,.199):rod('Cheek support',(0,.010,z),(0,.032,z),.006,1)
profile('Pistol grip',[(.002,.299),(-.014,.368),(-.120,.367),(-.132,.325),(-.084,.308)],.041,2)
profile('Fore stock',[(-.004,.373),(-.005,.610),(-.023,.666),(-.056,.653),(-.069,.435),(-.045,.375)],.058,0)
box('Receiver',(0,.028,.369),(.046,.042,.177),1,.003)
rod('Barrel',(0,.038,.426),(0,.038,1.04),.010,1)
ring('Muzzle rim',(0,.038,1.042),.012,.007,.012,1)
box('Trigger guard lower',(0,-.103,.401),(.011,.008,.056),1,.003)
box('Trigger guard front',(0,-.076,.425),(.011,.062,.008),1,.003)
box('Trigger',(0,-.052,.390),(.007,.035,.006),1,.002)
rod('Bolt handle',(.020,.034,.356),(.068,.006,.361),.006,1)
bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=.012,location=coord((.068,.006,.361)))
finish(bpy.context.object,'Bolt knob',2)
box('Rear sight rail',(0,.054,.296),(.021,.018,.040),1,.002)
ring('Rear diopter',(0,.087,.281),.019,.006,.020,1)
box('Front sight support',(0,.057,1.008),(.014,.038,.020),1,.002)
ring('Front sight hood',(0,.088,1.008),.022,.018,.030,1)
rod('Front sight post',(0,.069,1.008),(0,.086,1.008),.0018,1)
for side in (-1,1):
    box('Original accent strip',(side*.030,-.025,.515),(.002,.014,.150),3,.001)
    for z in (.452,.493,.534,.575):box('Grip texture',(side*.030,-.044,z),(.002,.006,.015),2,.001)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=parts[0]
for p in o.data.polygons:p.material_index=materials.index(o.data.materials[p.material_index])
o.data.calc_loop_triangles();v=[];norm=[];sub=[[] for _ in materials]
for tri in o.data.loop_triangles:
    for loop in tri.loops:
        p=o.matrix_world@o.data.vertices[o.data.loops[loop].vertex_index].co
        n=o.matrix_world.to_3x3()@o.data.corner_normals[loop].vector
        sub[tri.material_index].append(len(v)//3);v.extend((p.x,p.z,-p.y));norm.extend((n.x,n.z,-n.y))
out=Path('artifacts/g2-hero/rifle.json')
payload=dict(schema_version=1,id='g2-original-rifle-v1',vertices=v,normals=norm,submeshes=[dict(indices=t) for t in sub])
out.write_text(json.dumps(payload,separators=(',',':')))
manifest=dict(schema_version=1,id='g2-original-rifle-derivation-v1',origin='Original project-authored unbranded prop; no external mesh or textures',bone_contract='M11 butt origin Z0; muzzle Z1.04m; visual only',triangles=len(v)//9,sha256=hashlib.sha256(out.read_bytes()).hexdigest(),blender=bpy.app.version_string)
out.with_name('rifle-manifest.json').write_text(json.dumps(manifest,indent=2));print(json.dumps(manifest))
