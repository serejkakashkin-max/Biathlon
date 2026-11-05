"""Original PULSE sporting equipment and tailored weighted garments.

Blender4.5 LTS; CC0 Grab3D anatomical head/hands and original24-joint skeleton.
All outputs are actual meshes/materials, not image generation. Reproducible.
"""
import bpy, bmesh, math, json, argparse, sys, shutil
import numpy as np
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.kdtree import KDTree
from mathutils.bvhtree import BVHTree

parser=argparse.ArgumentParser();parser.add_argument('--source',required=True);parser.add_argument('--output',required=True);parser.add_argument('--review',required=True)
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);src=Path(args.source).resolve();out=Path(args.output).resolve();review=Path(args.review).resolve()
for sub in ['Models','Materials','Textures','Prefabs','Animations','Scripts','Editor','Preview','Documentation']:(out/sub).mkdir(parents=True,exist_ok=True)
review.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(src/'01_Character/man-base-character-rigged.glb'))
scene=bpy.context.scene
arm=next(o for o in scene.objects if o.type=='ARMATURE');body=next(o for o in scene.objects if o.type=='MESH')
arm.animation_data_clear();arm.data.pose_position='REST';bpy.context.view_layer.update()
for pb in arm.pose.bones:pb.matrix_basis=Matrix.Identity(4)
# Apply object units before repairing the imported100x display tails.
bpy.ops.object.select_all(action='DESELECT')
body.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
arm.name='BiathleteSkeleton';body.name='AnatomicalSource'
heads={b.name:(arm.matrix_world@b.head_local).copy() for b in arm.data.bones}
bpy.ops.object.mode_set(mode='EDIT')
preferred={'Hips':'Spine02','Spine02':'Spine01','Spine01':'Spine','Spine':'neck','neck':'Head','Head':'head_end',
 'LeftShoulder':'LeftArm','RightShoulder':'RightArm','LeftArm':'LeftForeArm','RightArm':'RightForeArm','LeftForeArm':'LeftHand','RightForeArm':'RightHand',
 'LeftUpLeg':'LeftLeg','RightUpLeg':'RightLeg','LeftLeg':'LeftFoot','RightLeg':'RightFoot','LeftFoot':'LeftToeBase','RightFoot':'RightToeBase'}
for b in arm.data.edit_bones:
    b.head=arm.matrix_world.inverted()@heads[b.name]
    if b.name in preferred:b.tail=arm.matrix_world.inverted()@heads[preferred[b.name]]
    else:b.tail=b.head+Vector((0,-.08,0) if 'Toe' in b.name else (.025 if 'Left' in b.name else -.025,-.055,-.09) if 'Hand' in b.name else (0,0,.04))
    b.roll=0
bpy.ops.object.mode_set(mode='OBJECT')
for pb in arm.pose.bones:pb.matrix_basis=Matrix.Identity(4)
arm.data.pose_position='POSE';bpy.context.view_layer.update()
source_material=body.data.materials[0]
images={i.name:i for i in bpy.data.images}
for key,filename in [('rig_basecolor','Skin_BaseColor_2K'),('rig_normal','Skin_Normal_2K'),('rig_metallic-rig_roughness','Skin_ORM_2K')]:
    img=images[key];img.filepath_raw=str(out/'Textures'/f'{filename}.png');img.file_format='PNG';img.save()
    img.filepath=str(out/'Textures'/f'{filename}.png')

def material(name,color,rough=.5,metal=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    return m
red=material('PULSE_Race_Red',(.27,.003,.010),.43);black=material('PULSE_Stretch_Graphite',(.006,.009,.014),.54)
scarlet=material('PULSE_Scarlet_Accent',(.48,.010,.018),.4);white=material('Bib_Ivory',(.79,.82,.80),.69)
ink=material('Printed_Graphite',(.012,.017,.024),.62);rubber=material('Rubber_Grip',(.012,.014,.017),.64)
carbon=material('Carbon_Satin',(.025,.035,.043),.32,.28);alloy=material('Brushed_Aluminium',(.48,.53,.57),.25,.86)
titanium=material('Gunmetal',(.09,.12,.14),.29,.8);amber=material('Amber_Mirror',(.78,.20,.018),.105,.78)
thread=material('Flatlock_Thread',(.13,.017,.025),.62);stitch=material('Silver_Stitch',(.30,.33,.34),.7)
wood=material('Sport_Laminate',(.11,.038,.021),.34);skin=source_material;skin.name='Anatomical_Skin_2K'
materials=[red,black,scarlet,white,ink,rubber,carbon,alloy,titanium,amber,thread,stitch,wood,skin]
#4K real tangent-space textile maps. Fine paired warp/weft includes slight deterministic yarn variation.
size=4096
u=np.arange(size,dtype=np.float32)[None,:];v=np.arange(size,dtype=np.float32)[:,None]
h=.5+.13*np.sin(u*math.tau/16)*np.sin(v*math.tau/12)+.035*np.sin((u+v)*math.tau/64)
dx=np.gradient(h,axis=1)*1.8;dy=np.gradient(h,axis=0)*1.8
normal=np.stack((-dx,-dy,np.ones_like(h)),axis=2);normal/=np.linalg.norm(normal,axis=2,keepdims=True)
def image_from(name,rgb,colorspace):
    im=bpy.data.images.new(name,width=size,height=size,alpha=False)
    im.colorspace_settings.name=colorspace
    rgba=np.ones((size,size,4),dtype=np.float32);rgba[:,:,:3]=rgb
    im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(out/'Textures'/(name+'.png'));im.file_format='PNG';im.save();return im
weave_n=image_from('Textile_Normal_4K',normal*.5+.5,'Non-Color')
weave_r=image_from('Textile_Roughness_4K',np.repeat(np.clip(.63+(h-.5)*.4,0,1)[:,:,None],3,axis=2),'Non-Color')
weave_c=image_from('Textile_BaseColor_4K',np.repeat(np.clip(.92+(h-.5)*.16,0,1)[:,:,None],3,axis=2),'sRGB')
for m in [red,black,scarlet,white,thread]:
    nodes=m.node_tree.nodes;links=m.node_tree.links;p=nodes.get('Principled BSDF')
    tex=nodes.new('ShaderNodeTexImage');tex.image=weave_n;n=nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.24;links.new(tex.outputs['Color'],n.inputs['Color']);links.new(n.outputs['Normal'],p.inputs['Normal'])
    r=nodes.new('ShaderNodeTexImage');r.image=weave_r;links.new(r.outputs['Color'],p.inputs['Roughness'])
    p.inputs['Sheen Weight'].default_value=.22

objects=[]
def smooth(obj):
    for f in obj.data.polygons:f.use_smooth=True
def mesh(name,verts,faces,mats,indices=None,uv=None,weights=None):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.materials.clear()
    for m in mats:data.materials.append(m)
    data.update();obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj);smooth(obj)
    if indices:
        for f,i in zip(data.polygons,indices):f.material_index=i
    if uv:
        layer=data.uv_layers.new(name='TailoredUV')
        for f in data.polygons:
            for li in f.loop_indices:layer.data[li].uv=uv[data.loops[li].vertex_index]
    if weights:
        groups={n:obj.vertex_groups.new(name=n) for n in arm.data.bones.keys()}
        for i,w in enumerate(weights):
            for n,value in w.items():
                if value>1e-6:groups[n].add([i],value,'REPLACE')
        mod=obj.modifiers.new('Weighted sport garment','ARMATURE');mod.object=arm
        obj.parent=arm;obj.matrix_world=Matrix.Identity(4)
    objects.append(obj);return obj
def rigid(obj,bone):
    matrix=obj.matrix_world.copy();obj.parent=arm;obj.parent_type='BONE';obj.parent_bone=bone;obj.matrix_world=matrix
    if obj not in objects:objects.append(obj)
    return obj
def weight_z(z):
    levels=[(.90,'Hips'),(1.105,'Spine02'),(1.235,'Spine01'),(1.36,'Spine'),(1.455,'neck')]
    if z<=levels[0][0]:return {levels[0][1]:1}
    for (a,n),(b,m) in zip(levels,levels[1:]):
        if z<=b:
            t=(z-a)/(b-a);return {n:1-t,m:t}
    return {'neck':1}
def loft(name,rings,mats,classify=None,n=64,weight=None,wrinkles=0):
    # rings=(centerX,centerY,z,radiusX,radiusY). Seam-aware UV has a duplicate ring endpoint.
    vs=[];uv=[];ws=[];fs=[];mi=[]
    for j,(x,y,z,rx,ry) in enumerate(rings):
        for i in range(n+1):
            t=i/n*math.tau;w=1+wrinkles*math.sin(j*1.9+math.sin(t)*2)*math.sin(t*5)
            q=(x+rx*math.cos(t)*w,y+ry*math.sin(t)*w,z);vs.append(q);uv.append((i/n,j/(len(rings)-1)))
            ws.append(weight(Vector(q),j,i) if weight else weight_z(z))
    for j in range(len(rings)-1):
        for i in range(n):fs.append((j*(n+1)+i,j*(n+1)+i+1,(j+1)*(n+1)+i+1,(j+1)*(n+1)+i));mi.append(classify(i/n,j/(len(rings)-1)) if classify else 0)
    if rings[-1][2]<rings[0][2]:fs=[tuple(reversed(f)) for f in fs]
    return mesh(name,vs,fs,mats,mi,uv,ws)
def interpolated(rings,steps=3):
    result=[]
    for a,b in zip(rings,rings[1:]):
        for s in range(steps):result.append(tuple(x+(y-x)*s/steps for x,y in zip(a,b)))
    return result+[rings[-1]]
def tube(name,points,radius,mat,bone=None,res=8):
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.resolution_u=8;curve.bevel_depth=radius;curve.bevel_resolution=2
    spline=curve.splines.new('POLY');spline.points.add(len(points)-1)
    for p,c in zip(spline.points,points):p.co=(*c,1)
    obj=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(obj)
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.convert(target='MESH');obj=bpy.context.object;obj.data.materials.append(mat);smooth(obj)
    if bone:rigid(obj,bone)
    else:
        groups={n:obj.vertex_groups.new(name=n) for n in arm.data.bones.keys()}
        for vert in obj.data.vertices:
            for n,w in weight_z(vert.co.z).items():groups[n].add([vert.index],w,'REPLACE')
        mod=obj.modifiers.new('Seam follows cloth','ARMATURE');mod.object=arm;obj.parent=arm;obj.matrix_world=Matrix.Identity(4);objects.append(obj)
    return obj
def block(name,loc,scale,mat,bevel=.004,bone=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Manufactured edge','BEVEL');mod.width=bevel;mod.segments=3
        bpy.ops.object.modifier_apply(modifier=mod.name)
    o.data.materials.append(mat);smooth(o)
    return rigid(o,bone) if bone else o
def text(name,bodytext,loc,width,mat,bone,back=False,horizontal=False):
    bpy.ops.object.text_add(location=loc,rotation=(0,0,math.pi/2) if horizontal else (math.pi/2,0,math.pi if back else 0));o=bpy.context.object;o.name=name;o.data.body=bodytext;o.data.align_x='CENTER';o.data.align_y='CENTER';o.data.extrude=0;o.data.bevel_depth=0;o.data.resolution_u=3;o.data.size=.1;o.data.space_character=1.1
    bpy.context.view_layer.update();s=width/max(.001,o.dimensions.x);o.scale=(s,s,s);bpy.ops.object.convert(target='MESH');o=bpy.context.object;o.data.materials.append(mat);return rigid(o,bone)

# Source anatomy retained only on exposed head/neck. Rest is replaced, not recolored.
bm=bmesh.new();bm.from_mesh(body.data)
bmesh.ops.delete(bm,geom=[v for v in bm.verts if (body.matrix_world@v.co).z<1.505 or (body.matrix_world@v.co).z>1.69 or ((body.matrix_world@v.co).z<1.54 and abs((body.matrix_world@v.co).x)>.080) or ((body.matrix_world@v.co).z>1.60 and (body.matrix_world@v.co).y>.085)],context='VERTS')
bm.to_mesh(body.data);bm.free();body.name='Anatomical_Head_Neck';objects.append(body)
for v in body.data.vertices:
    world=body.matrix_world@v.co
    if world.z<1.56:
        limit=.054+max(0,world.z-1.505)*.25
        if abs(world.x)>limit:world.x=math.copysign(limit,world.x)
        if world.z<1.535:world.y=max(.041-.050,min(.041+.050,world.y))
        else:world.y=min(.10,world.y)
        v.co=body.matrix_world.inverted()@world
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body
dec=body.modifiers.new('Retain facial silhouette','DECIMATE');dec.ratio=.72;bpy.ops.object.modifier_apply(modifier=dec.name)
smooth(body)

# Tailored torso: separate shell, garment ease, collar and bounded compression folds.
torso_rings=[(0,.035,.815,.133,.083),(0,.035,.88,.157,.098),(0,.035,.96,.162,.105),(0,.035,1.055,.151,.097),(0,.035,1.15,.162,.103),(0,.038,1.25,.187,.110),(0,.045,1.34,.203,.111),(0,.048,1.39,.195,.098),(0,.045,1.425,.108,.070),(0,.041,1.452,.057,.056),(0,.041,1.518,.061,.059)]
torso=loft('RaceSuit_Tailored_Torso',interpolated(torso_rings,4),[red,black,scarlet],lambda u,v:1 if abs(math.cos(u*math.tau))>.91 and v<.82 else 2 if .52<u<.58 else 0,wrinkles=.007)
for side in (-1,1):
    prefix='Left' if side>0 else 'Right'
    for theta in [0 if side>0 else math.pi,4.05 if side<0 else 5.38]:
        points=[(x+rx*math.cos(theta)*1.008,y+ry*math.sin(theta)*1.008,z) for x,y,z,rx,ry in torso_rings[:-2]]
        tube(prefix+'_Torso_Flatlock',points,.0009,thread)
    # Two-joint tailored sleeves in the actual source A-pose; ease/folds at elbow.
    shoulder=heads[prefix+'Arm'];elbow=heads[prefix+'ForeArm'];wrist=heads[prefix+'Hand']
    centers=[];radii=[];weights=[]
    for k in range(25):
        t=k/24*2
        if t<=1:c=shoulder.lerp(elbow,t);radius=.068*(1-t)+.047*t
        else:c=elbow.lerp(wrist,t-1);radius=.049*(2-t)+.031*(t-1)
        centers.append(c);radii.append(radius*(1+.035*math.sin(k*2.8)*math.exp(-((t-1)/.35)**2)))
        blend=max(0,min(1,(t-.72)/.56));weights.append({prefix+'Arm':1-blend,prefix+'ForeArm':blend} if t<1.7 else {prefix+'ForeArm':max(0,(2-t)/.3),prefix+'Hand':max(0,1-(2-t)/.3)})
    tangent=(wrist-shoulder).normalized();basis=Vector((0,-1,0));basis=(basis-tangent*basis.dot(tangent)).normalized();other=tangent.cross(basis)
    vs=[];uv=[];ws=[];faces=[];indices=[];n=48
    for k,c in enumerate(centers):
        for i in range(n+1):
            angle=i/n*math.tau;q=c+(basis*math.cos(angle)+other*.92*math.sin(angle))*radii[k]
            vs.append(q);uv.append((i/n,k/24));ws.append(weights[k])
            if k<24 and i<n:faces.append((k*(n+1)+i,k*(n+1)+i+1,(k+1)*(n+1)+i+1,(k+1)*(n+1)+i));indices.append(1 if .35<i/n<.58 else 0)
    sleeve=mesh(prefix+'_Race_Sleeve',vs,faces,[red,black],indices,uv,ws)
    tube(prefix+'_Sleeve_Seam',[c+basis*(r+.001) for c,r in zip(centers,radii)],.0008,thread,prefix+'ForeArm')
    hip=heads[prefix+'UpLeg'];knee=heads[prefix+'Leg'];ankle=heads[prefix+'Foot']
    rings=[]
    for k in range(37):
        t=k/36*2;c=hip.lerp(knee,min(1,t)) if t<=1 else knee.lerp(ankle,t-1)
        radius=(.094*(1-t)+.054*t) if t<=1 else .054+(math.sin((t-1)*math.pi)*.009)-.019*(t-1)
        rings.append((c.x,c.y,c.z,radius,radius*.89))
    def legweight(p,j,i):
        t=j/36*2;v=max(0,min(1,(t-.78)/.44))
        return {prefix+'UpLeg':1-v,prefix+'Leg':v} if t<1.82 else {prefix+'Leg':(2-t)/.18,prefix+'Foot':1-(2-t)/.18}
    loft(prefix+'_Race_Tight',rings,[red,black,scarlet],lambda u,v:1 if abs(math.cos(u*math.tau+v*1.7))>.85 else 2 if .69<u<.73 else 0,weight=legweight,wrinkles=.008)
    for th in [0,math.pi]:tube(prefix+'_Leg_Flatlock_'+str(th),[(x+(rx+.0007)*math.cos(th),y+(ry+.0007)*math.sin(th),z) for x,y,z,rx,ry in rings],.0007,thread,prefix+'Leg')

# Bib is a real garment with narrow binding and large readable original bib27.
bib_rings=[]
for z in [1.145,1.16,1.19,1.23,1.27,1.31,1.35,1.375]:
    a,b=next((a,b) for a,b in zip(torso_rings,torso_rings[1:]) if a[2]<=z<=b[2]);t=(z-a[2])/(b[2]-a[2]);ring=tuple(x+(y-x)*t for x,y in zip(a,b));bib_rings.append((ring[0],ring[1],z,ring[3]+.0045,ring[4]+.005))
loft('Woven_Bib_27',bib_rings,[white],n=72)
for at in [0,-1]:
    x,y,z,rx,ry=bib_rings[at];tube('Bib_Bound_Edge',[(x+rx*math.cos(t*math.tau/100),y+ry*math.sin(t*math.tau/100),z) for t in range(101)],.0015,black)
text('Bib27_Front','27',(0,-.082,1.25),.132,ink,'Spine01')
text('Bib27_Back','27',(0,.164,1.25),.132,ink,'Spine01',True)
text('Bib_Footer','WORLD CUP',(0,-.073,1.173),.135,ink,'Spine01')
# Chest zip and collar edge.
tube('Chest_Waterproof_Zip',[(0,-.018,1.49),(0,-.025,1.45),(0,-.056,1.415),(0,-.072,1.38)],.002,black)
block('Zip_Puller',(0,-.024,1.46),(.010,.003,.019),alloy,.002,'neck')

# Compact racing cap (no pompom), amber panoramic wraparound lens and elastic strap.
cap_rings=[]
for j in range(37):
    angle=j/36*math.pi/2
    cap_rings.append((0,.045,1.678+.141*math.sin(angle),max(.0001,.085*math.cos(angle)),max(.0001,.111*math.cos(angle))))
cap=loft('Race_Cap',cap_rings,[red,black],lambda u,v:1 if v<.07 else 0,weight=lambda p,j,i:{'Head':1},n=96)
for vert in cap.data.vertices:
    vert.co.z-=.078*max(0,min(1,(vert.co.y-.025)/.075))*max(0,min(1,(1.75-vert.co.z)/.072))
text('Cap_PULSE','PULSE',(0,-.052,1.754),.09,white,'Head')
lensverts=[];lensuv=[];lensfaces=[]
def lenspoint(t,v):
    x=(t-.5)*.177;y=-.050-.065*(1-(x/.102)**2);z=1.664+(v-.5)*.056*(1-.20*(abs(x)/.0885)**4)+.003*(abs(x)/.0885)**2
    if v<.32:z+=.012*math.exp(-(x/.019)**2)*(1-v/.32)
    return (x,y,z)
for j in range(13):
    for i in range(49):lensverts.append(lenspoint(i/48,j/12));lensuv.append((i/48,j/12))
for j in range(12):
    for i in range(48):lensfaces.append((j*49+i,j*49+i+1,(j+1)*49+i+1,(j+1)*49+i))
lens=mesh('Panoramic_Amber_Lens',lensverts,lensfaces,[amber],uv=lensuv);rigid(lens,'Head')
for v in [0,1]:tube('Goggle_Frame', [lenspoint(i/96,v) for i in range(97)],.0018,black,'Head')
for u0 in [0,1]:tube('Goggle_Temple',[lenspoint(u0,v/12) for v in range(13)],.0022,black,'Head')
strap=[]
for i in range(97):
    t=i/96*math.tau;strap.append((.083*math.cos(t),.032+.09*math.sin(t),1.685))
tube('Goggle_Elastic_Strap',strap,.006,black,'Head')

# Gloves retain actual anatomical finger shapes, new material sections/cuffs.
bpy.ops.import_scene.gltf(filepath=str(src/'01_Character/man-base-character-rigged.glb'))
newarm=next(o for o in scene.objects if o.type=='ARMATURE' and o!=arm)
hand_source=next(o for o in scene.objects if o.type=='MESH' and o not in objects)
newarm.animation_data_clear();newarm.data.pose_position='REST';bpy.context.view_layer.update()
hand_source.matrix_world=hand_source.matrix_world.copy();verts=[hand_source.matrix_world@v.co for v in hand_source.data.vertices]
# A new separate cloth shell is cut from the measured anatomical surface, then
# fitted/smoothed past the casual garment hems. This replaces the disconnected
# parametric blockout, rather than accepting it as the final garment.
clothes=hand_source.copy();clothes.data=hand_source.data.copy();bpy.context.collection.objects.link(clothes)
clothes.name='RaceSuit_Continuous_Tailored_Shell';clothes.parent=None;clothes.matrix_world=Matrix.Identity(4);clothes.data.transform(hand_source.matrix_world);clothes.modifiers.clear()
bm=bmesh.new();bm.from_mesh(clothes.data)
bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
for z in [.17,1.505]:bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,plane_co=(0,0,z),plane_no=(0,0,1))
bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z<.17 or v.co.z>1.505],context='VERTS')
for pre,side in [('Left',1),('Right',-1)]:
    wrist=heads[pre+'Hand'];direction=(wrist-heads[pre+'ForeArm']).normalized()
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if side*v.co.x>.4 and (v.co-wrist).dot(direction)>-.018],context='VERTS')
bm.to_mesh(clothes.data);bm.free()
bpy.ops.object.select_all(action='DESELECT');clothes.select_set(True);bpy.context.view_layer.objects.active=clothes
sm=clothes.modifiers.new('Remove loose casual hems','SMOOTH');sm.factor=.65;sm.iterations=22;bpy.ops.object.modifier_apply(modifier=sm.name)
dec=clothes.modifiers.new('Anatomical garment topology','DECIMATE');dec.ratio=.48;bpy.ops.object.modifier_apply(modifier=dec.name)
for vert in clothes.data.vertices:
    p=vert.co
    # Compression-fit torso and shorts; retained radial anatomy rather than straight cylinders.
    if abs(p.x)<.22 and .83<p.z<1.32:
        taper=.91+.08*min(1,abs(p.z-1.06)/.2);p.x*=taper;p.y=.035+(p.y-.035)*taper
    if .62<p.z<.91:
        center=.11 if p.x>0 else -.11;fit=.84+.14*min(1,abs(p.z-.77)/.15)
        p.x=center+(p.x-center)*fit;p.y=.035+(p.y-.035)*fit
    if .17<=p.z<.89 and (abs(p.x)>.055 or p.z<.81):
        side='Left' if p.x>0 else 'Right';hip=heads[side+'UpLeg'];knee=heads[side+'Leg'];ankle=heads[side+'Foot']
        center=hip.lerp(knee,max(0,min(1,(hip.z-p.z)/(hip.z-knee.z)))) if p.z>knee.z else knee.lerp(ankle,max(0,min(1,(knee.z-p.z)/(knee.z-ankle.z))))
        profile=[(.17,.036),(.28,.049),(.39,.058),(.51,.055),(.62,.069),(.75,.082),(.89,.091)]
        (za,ra),(zb,rb)=next((a,b) for a,b in zip(profile,profile[1:]) if a[0]<=p.z<=b[0]);rx=ra+(rb-ra)*(p.z-za)/(zb-za);ry=rx*.91
        delta=Vector((p.x-center.x,p.y-center.y,0));distance=delta.length
        if distance>1e-6:
            radial=delta/distance;target=1/math.sqrt(radial.x**2/rx**2+radial.y**2/ry**2);distance=target*.95+distance*.05
            p.x=center.x+radial.x*distance;p.y=center.y+radial.y*distance
    if abs(p.x)<.065 and .76<p.z<.91:p.y=.035+(p.y-.035)*.80
    if .89<=p.z<1.34 and abs(p.x)<.20:
        profile=[(.89,.157,.100),(1.06,.150,.096),(1.18,.171,.106),(1.34,.195,.113)]
        (za,xa,ya),(zb,xb,yb)=next((a,b) for a,b in zip(profile,profile[1:]) if a[0]<=p.z<=b[0]);t=(p.z-za)/(zb-za);rx=xa+(xb-xa)*t;ry=ya+(yb-ya)*t
        delta=Vector((p.x,p.y-.035,0));distance=delta.length
        if distance>1e-6:
            radial=delta/distance;target=1/math.sqrt(radial.x**2/rx**2+radial.y**2/ry**2)
            edge=max(0,min(1,(1.34-p.z)/.08));edge=edge*edge*(3-2*edge)
            influence=.9*edge;distance=target*influence+distance*(1-influence)
            p.x=radial.x*distance;p.y=.035+radial.y*distance
    # Physical compression folds at knees and elbows, millimetres not broad body bulges.
    fold=math.exp(-((p.z-.50)/.065)**2)*.0016*math.sin(p.z*215+p.x*15)
    p.y+=fold
clothes.data.update()
# Remove the inherited shirt/short topology completely. Voxel union removes
# doubled hems and disconnected UV-split surfaces; nearest-surface weights are
# transferred from the original normalized24-bone skin, not regenerated RNG.
weight_records=[];kd=KDTree(len(clothes.data.vertices))
for vert in clothes.data.vertices:
    kd.insert(vert.co,vert.index);weight_records.append({clothes.vertex_groups[g.group].name:g.weight for g in vert.groups})
kd.balance()
bm=bmesh.new();bm.from_mesh(clothes.data)
bmesh.ops.holes_fill(bm,edges=[e for e in bm.edges if e.is_boundary],sides=0)
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(clothes.data);bm.free()
remesh=clothes.modifiers.new('Continuous compression garment retopology','REMESH');remesh.mode='VOXEL';remesh.voxel_size=.0035;remesh.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=remesh.name)
smoothmod=clothes.modifiers.new('Cloth surface relaxation','SMOOTH');smoothmod.factor=.45;smoothmod.iterations=5;bpy.ops.object.modifier_apply(modifier=smoothmod.name)
dec=clothes.modifiers.new('Hero garment budget','DECIMATE');dec.ratio=.27;bpy.ops.object.modifier_apply(modifier=dec.name)
clothes.vertex_groups.clear();groups={n:clothes.vertex_groups.new(name=n) for n in arm.data.bones.keys()}
for vert in clothes.data.vertices:
    entries=kd.find_n(vert.co,3);weights={};normalizer=0
    for _,i,distance in entries:
        proximity=1/max(.0001,distance)**2;normalizer+=proximity
        for n,w in weight_records[i].items():weights[n]=weights.get(n,0)+w*proximity
    total=sum(weights.values())
    for n,w in weights.items():
        if w/total>.0001:groups[n].add([vert.index],w/total,'REPLACE')
if not clothes.data.uv_layers:clothes.data.uv_layers.new(name='TailoredAtlas')
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.008);bpy.ops.object.mode_set(mode='OBJECT')
clothes.data.update();smooth(clothes)
color=clothes.data.color_attributes.new(name='TailoredPanels',type='FLOAT_COLOR',domain='CORNER')
for poly in clothes.data.polygons:
    for li in poly.loop_indices:
        p=clothes.data.vertices[clothes.data.loops[li].vertex_index].co
        blackpanel=False
        if p.z<.89:
            sign=1 if p.x>0 else -1;center=sign*(.103+(.89-p.z)*.075)
            offset=p.x-center+.012*math.sin(p.z*11)
            blackpanel=abs(offset)>.050-(.89-p.z)*.025
        elif abs(p.x)<.22:blackpanel=abs(p.x)>.134 and p.z<1.36
        else:blackpanel=p.y>.049
        if abs(p.x)<.07 and p.z>.79:blackpanel=False
        c=black.diffuse_color if blackpanel else red.diffuse_color
        color.data[li].color=c
tailored=material('PULSE_Tailored_Textile',(.27,.003,.010),.52)
clothes.data.materials.clear();clothes.data.materials.append(tailored)
nodes=tailored.node_tree.nodes;links=tailored.node_tree.links;output=nodes.get('Material Output');principal=nodes.get('Principled BSDF')
attr=nodes.new('ShaderNodeVertexColor');attr.layer_name='TailoredPanels';emit=nodes.new('ShaderNodeEmission');links.new(attr.outputs['Color'],emit.inputs[0]);links.new(emit.outputs[0],output.inputs['Surface'])
atlas=bpy.data.images.new('Suit_Panels_BaseColor_4K',width=4096,height=4096,alpha=False)
target=nodes.new('ShaderNodeTexImage');target.image=atlas;nodes.active=target
scene.render.engine='CYCLES';scene.cycles.samples=1;scene.render.bake.margin=16
bpy.ops.object.bake(type='EMIT');atlas.filepath_raw=str(out/'Textures/Suit_Panels_BaseColor_4K.png');atlas.file_format='PNG';atlas.save()
links.new(principal.outputs[0],output.inputs['Surface']);links.new(target.outputs['Color'],principal.inputs['Base Color'])
normaltex=nodes.new('ShaderNodeTexImage');normaltex.image=weave_n;norm=nodes.new('ShaderNodeNormalMap');norm.inputs['Strength'].default_value=.30;links.new(normaltex.outputs['Color'],norm.inputs['Color']);links.new(norm.outputs[0],principal.inputs['Normal'])
roughtex=nodes.new('ShaderNodeTexImage');roughtex.image=weave_r;links.new(roughtex.outputs[0],principal.inputs['Roughness']);principal.inputs['Sheen Weight'].default_value=.15
materials.append(tailored)
mod=clothes.modifiers.new('Original normalized skeletal weights','ARMATURE');mod.object=arm;clothes.parent=arm;clothes.matrix_world=Matrix.Identity(4);objects.append(clothes)
for old in list(objects):
    if old.name in ['RaceSuit_Tailored_Torso','Left_Race_Sleeve','Right_Race_Sleeve','Left_Race_Tight','Right_Race_Tight']:
        objects.remove(old);bpy.data.objects.remove(old,do_unlink=True)
# Collar is independent sewn geometry, covering the genuine neck/suit boundary.
loft('High_Race_Collar',[(0,.041,1.442,.064,.061),(0,.048,1.475,.066,.069),(0,.055,1.56,.067,.075)],[red],weight=lambda p,j,i:{'neck':1},n=64)
for side in [-1,1]:
    pre='Left' if side>0 else 'Right';wrist=heads[pre+'Hand'];direction=(wrist-heads[pre+'ForeArm']).normalized()
    keep={i for i,v in enumerate(verts) if (v-wrist).dot(direction)>-.008 and side*v.x>.41}
    faces=[[k for k in f.vertices] for f in hand_source.data.polygons if all(k in keep for k in f.vertices)]
    indices=sorted({v for f in faces for v in f});remap={v:i for i,v in enumerate(indices)}
    curled=[];finger_axis=Vector((side*.12,-.03,-1)).normalized();palm=Vector((0,-1,0));palm=(palm-finger_axis*palm.dot(finger_axis)).normalized()
    for i in indices:
        delta=verts[i]-wrist;t=delta.dot(finger_axis);across=delta-finger_axis*t
        if t>.075:
            theta=min(1.35,(t-.075)/.065)
            curled.append(wrist+across+finger_axis*(.075+.065*math.sin(theta))+palm*(.065*(1-math.cos(theta))))
        else:curled.append(verts[i])
    glove=mesh(pre+'_Anatomical_Glove',curled,[[remap[i] for i in f] for f in faces],[rubber,black], [0 if sum(verts[i].y for i in f)/len(f)<0 else 1 for f in faces]);rigid(glove,pre+'Hand')
    bpy.ops.object.select_all(action='DESELECT');glove.select_set(True);bpy.context.view_layer.objects.active=glove
    dec=glove.modifiers.new('Preserve anatomical grip silhouette','DECIMATE');dec.ratio=.46;bpy.ops.object.modifier_apply(modifier=dec.name)
    # Separate wrist strap with shaped padded cuff.
    cuff=block(pre+'_Glove_Cuff',wrist,(.068,.047,.045),black,.010,pre+'Hand')
    tube(pre+'_Glove_Red_Piping',[wrist+Vector((side*.028,0,.023)),wrist+Vector((side*.031,-.012,.005)),wrist+Vector((side*.028,0,-.016))],.002,scarlet,pre+'Hand')
bpy.data.objects.remove(hand_source,do_unlink=True);bpy.data.objects.remove(newarm,do_unlink=True)

# Nordic boots: narrow forefoot, flexible sole, carbon ankle cuff and zip covers.
for side in [-1,1]:
    pre='Left' if side>0 else 'Right';x=heads[pre+'Foot'].x
    bootrings=[(x,-.022,.043,.047,.145),(x,-.024,.052,.048,.146),(x,-.025,.073,.046,.144),(x,-.022,.095,.045,.135),(x,.008,.119,.043,.105),(x,.049,.145,.039,.067),(x,.056,.195,.037,.056),(x,.056,.238,.036,.052)]
    loft(pre+'_Nordic_Boot',interpolated(bootrings,3),[black,scarlet],lambda u,v:1 if v<.14 else 0,weight=lambda p,j,i:{pre+'Foot':1},n=64)
    loft(pre+'_Carbon_Ankle_Cuff',[(x,.057,.175,.041,.059),(x,.057,.209,.041,.059),(x,.057,.226,.039,.055)], [carbon],weight=lambda p,j,i:{pre+'Foot':1},n=56)
    for z in [.053,.066]:tube(pre+'_Boot_Sole_Seam',[(x+.048*math.cos(t*math.tau/80),-.024+.146*math.sin(t*math.tau/80),z) for t in range(81)],.0012,stitch,pre+'Foot')
    tube(pre+'_Boot_Zip',[(x,-.158,.084),(x,-.113,.108),(x,-.029,.14),(x,.001,.21)],.002,alloy,pre+'Foot')
    block(pre+'_Ankle_Ratchet',(x,-.004,.205),(.068,.012,.016),scarlet,.003,pre+'Foot')
    # Skis in true metre scale; slender44mm waist and rising Nordic tip.
    vs=[];uv=[];fs=[];mi=[]
    for j in range(97):
        t=j/96;y=-1.03+t*1.94;w=.022*(.62+.38*math.sin(math.pi*t)**.15);z=.021+.068*max(0,1-t/.115)**2+.008*max(0,(t-.94)/.06)**2
        for dx,dz in [(-w,0),(w,0),(-w,-.008),(w,-.008)]:vs.append((x+dx,y,z+dz));uv.append(((dx/w+1)/2,t))
        if j<96:
            k=j*4;fs.extend([(k,k+4,k+5,k+1),(k+2,k+3,k+7,k+6),(k,k+2,k+6,k+4),(k+1,k+5,k+7,k+3)]);mi.extend([0 if j%40<28 else 1,1,2,2])
    ski=mesh(pre+'_Nordic_Ski_194',vs,fs,[scarlet,black,alloy],mi,uv);rigid(ski,pre+'Foot')
    block(pre+'_Binding_Plate',(x,-.014,.036),(.040,.24,.016),carbon,.003,pre+'Foot')
    block(pre+'_Binding_Toe_Clamp',(x,-.133,.058),(.052,.027,.035),titanium,.004,pre+'Foot')
    block(pre+'_Binding_Release_Tab',(x,-.165,.055),(.035,.024,.014),scarlet,.003,pre+'Foot')
    text(pre+'_Ski_Brand','PULSE',(x,-.60,.026),.22,white,pre+'Foot',horizontal=True)
    # Pole shaft/grip/basket/strap are independently parented below a hand socket.
    wrist=heads[pre+'Hand'];grip=wrist+Vector((side*.025,-.050,-.080));tip=grip+Vector((side*.22,1.12,-.92));axis=(tip-grip).normalized()
    tube(pre+'_Carbon_Pole',[grip,tip],.0043,carbon,pre+'Hand')
    tube(pre+'_Pole_Grip',[grip-axis*.075,grip+axis*.045],.012,rubber,pre+'Hand')
    tube(pre+'_Pole_Inlay',[grip+axis*.10,grip+axis*.47],.0047,scarlet,pre+'Hand')
    tube(pre+'_Pole_Tip',[tip,tip+axis*.04],.0027,alloy,pre+'Hand')
    basket=tip-axis*.055
    tube(pre+'_Small_Pole_Basket',[basket+Vector((math.cos(t*math.tau/48)*.021,math.sin(t*math.tau/48)*.021,0)) for t in range(49)],.003,rubber,pre+'Hand')
    tube(pre+'_Pole_Wrist_Loop',[grip-axis*.035,wrist+Vector((side*.04,.01,.015)),wrist+Vector((-side*.025,.025,.019)),grip+axis*.015],.0035,black,pre+'Hand')

# Original decorative sporting rifle. Open stock/hardware, no functional internals.
rifleparts=[]
def rp(o):rifleparts.append(o);return o
def profiled(name,outline,depth,mat):
    verts=[(x,y,z) for y in [-depth/2,depth/2] for x,z in outline];n=len(outline)
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    o=mesh(name,verts,faces,[mat]);bevel=o.modifiers.new('Stock softened edges','BEVEL');bevel.width=.005;bevel.segments=3
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=bevel.name);return rp(o)
stock=profiled('Sporter_Open_Stock',[(-.085,.025),(.075,.025),(.065,.105),(.048,.16),(.048,.30),(.08,.35),(.075,.49),(.015,.53),(-.055,.49),(-.042,.36),(-.045,.31),(-.050,.16),(-.075,.10)],.044,wood)
bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,location=(0,0,.235));cut=bpy.context.object;cut.scale=(.026,.065,.064)
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);bpy.context.view_layer.objects.active=stock
modifier=stock.modifiers.new('Decorative ergonomic stock relief','BOOLEAN');modifier.operation='DIFFERENCE';modifier.solver='EXACT';modifier.object=cut;bpy.ops.object.modifier_apply(modifier=modifier.name);bpy.data.objects.remove(cut,do_unlink=True)
profiled('Cheek_Piece',[(-.07,.25),(-.045,.25),(-.025,.41),(-.07,.425),(-.083,.39)],.061,carbon)
rp(block('Butt_Plate',(-.004,0,.027),(.175,.060,.031),rubber,.007))
rp(block('Receiver_Sport',(.016,0,.55),(.055,.050,.18),alloy,.005))
rp(block('Stock_Composite_Inlay',(.015,-.025,.38),(.027,.006,.18),scarlet,.003))
def rifle_cyl(name,a,b,r,mat):
    d=Vector(b)-Vector(a);bpy.ops.mesh.primitive_cylinder_add(vertices=32,radius=r,depth=d.length,location=(Vector(a)+Vector(b))*.5);o=bpy.context.object;o.name=name;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();o.data.materials.append(mat);smooth(o);return rp(o)
rifle_cyl('Match_Barrel',(.016,0,.60),(.016,0,1.12),.012,titanium)
for z in [.67,.76,.89,1.10]:rifle_cyl('Barrel_Collar',(.016,0,z-.009),(.016,0,z+.009),.015,alloy)
rp(block('Rear_Diopter',(-.015,-.002,.618),(.055,.059,.029),carbon,.004))
rp(block('Diopter_Mount',(-.016,0,.59),(.023,.029,.035),alloy,.002))
rifle_cyl('Front_Sight_Tunnel',(.016,0,1.10),(.016,0,1.13),.024,carbon)
rp(block('Front_Sight_Protective_Hood',(.016,-.010,1.13),(.060,.054,.006),alloy,.002))
rp(block('Decorative_Receiver_Lever',(.058,0,.566),(.042,.016,.016),titanium,.004))
for z in [.22,.32,.46,.535]:
    rifle_cyl('Stock_Fastener',(-.045,-.026,z),(-.045,-.031,z),.005,alloy)
rp(block('Harness_Lower_Fitting',(-.016,.028,.18),(.06,.012,.028),carbon,.003))
riflematrix=Matrix.Translation((.045,.188,.81))@Matrix.Rotation(math.radians(-4),4,'Y')
for obj in rifleparts:obj.matrix_world=riflematrix@obj.matrix_world;rigid(obj,'Spine')
# Broad webbing with tension-following shoulder routes, weighted along the torso.
for side in [-1,1]:
    points=[(side*.09,.175,1.11),(side*.145,.14,1.30),(side*.15,.08,1.405),(side*.13,.00,1.425),(side*.12,-.065,1.35),(side*.14,-.076,1.19),(side*.10,.00,1.08)]
    # Flat webbing as a real quad ribbon, not a cylinder.
    verts=[];ws=[];uv=[]
    for i,p in enumerate(points):
        for s in [-1,1]:verts.append((p[0]+s*.014,p[1],p[2]));ws.append(weight_z(p[2]));uv.append(((s+1)/2,i/(len(points)-1)))
    mesh(('Left' if side<0 else 'Right')+'_Rifle_Harness_Webbing',verts,[(i*2,i*2+1,i*2+3,i*2+2) for i in range(len(points)-1)],[black],uv=uv,weights=ws)
    for edge in [-1,1]:tube('Harness_Stitch',[(x+edge*.012,y-.0005,z) for x,y,z in points],.00065,stitch)
    block('Harness_Adjuster',(side*.137,-.078,1.205),(.036,.007,.032),titanium,.002,'Spine01')

# Explicit named attachment nodes, serialized by Unity authoring rather than name-based gameplay identity.
sockets=[('Rifle_Back_Socket','Spine',(.045,.188,1.36)),('Rifle_Hand_Socket','RightHand',heads['RightHand']),('Ski_Left_Socket','LeftFoot',heads['LeftFoot']),('Ski_Right_Socket','RightFoot',heads['RightFoot']),('Pole_Left_Socket','LeftHand',heads['LeftHand']),('Pole_Right_Socket','RightHand',heads['RightHand'])]
for name,bone,position in sockets:
    obj=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(obj);obj.location=position;rigid(obj,bone)

# Fit the bib and printed markings to the final cloth/cap, avoiding clipped text.
def fit_surface(obj,target,offset):
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    mod=obj.modifiers.new('Sewn surface fit','SHRINKWRAP');mod.target=target;mod.wrap_method='NEAREST_SURFACEPOINT';mod.wrap_mode='OUTSIDE_SURFACE';mod.offset=offset
    bpy.ops.object.modifier_apply(modifier=mod.name)
bib=bpy.data.objects['Woven_Bib_27'];objects.remove(bib);bpy.data.objects.remove(bib,do_unlink=True)
bpy.context.view_layer.update();surface=BVHTree.FromObject(clothes,bpy.context.evaluated_depsgraph_get())
radii=[];n=96;rows=25
for j in range(rows):
    z=1.14+j/(rows-1)*.185;row=[]
    for i in range(n):
        direction=Vector((math.cos(i/n*math.tau),math.sin(i/n*math.tau),0))
        # Start outside: an inside-out ray can stop at an internal remnant of
        # the source shirt instead of the visible outer compression shell.
        center=Vector((0,.035,z));hit=surface.ray_cast(center+direction*.45,-direction,.45)
        if hit[0] is None:raise RuntimeError('Bib radial cloth fit has no body surface')
        # Bound only the arm extension: the old117mm sagittal cap cut through
        # the actual chest, clipping both the cloth and printed27 in rest pose.
        envelope=1/math.sqrt(direction.x**2/.205**2+direction.y**2/.170**2)
        row.append(min(envelope,(hit[0]-Vector((0,.035,z))).length))
    radii.append(row)
radii=np.array(radii);outer_radii=radii.copy()
for _ in range(6):radii=(radii*2+np.roll(radii,1,axis=1)+np.roll(radii,-1,axis=1))/4
radii=np.maximum(radii,outer_radii)
verts=[];uv=[];weights=[];faces=[]
for j in range(rows):
    z=1.14+j/(rows-1)*.185
    for i in range(n+1):
        angle=i/n*math.tau;r=radii[j,i%n]+.008
        verts.append((r*math.cos(angle),.035+r*math.sin(angle),z));uv.append((i/n,j/(rows-1)));weights.append(weight_z(z))
        if j<rows-1 and i<n:faces.append((j*(n+1)+i,j*(n+1)+i+1,(j+1)*(n+1)+i+1,(j+1)*(n+1)+i))
bib=mesh('Woven_Bib_27',verts,faces,[white],uv=uv,weights=weights)
bib_segments=n
for obj in list(objects):
    if obj.name.startswith('Bib_Bound_Edge'):objects.remove(obj);bpy.data.objects.remove(obj,do_unlink=True)
for obj in list(objects):
    if obj.name.startswith(('Bib27_','Bib_Header','Bib_Footer')):fit_surface(obj,bib,.0012)
    if obj.name=='Cap_PULSE':fit_surface(obj,cap,.0012)
    if 'Rifle_Harness_Webbing' in obj.name:fit_surface(obj,clothes,.007)
    if any(token in obj.name for token in ['Flatlock','Sleeve_Seam','Harness_Stitch']):fit_surface(obj,clothes,.0012)
    if obj==bib or obj.name.startswith(('Bib27_','Bib_Header','Bib_Footer')):
        world=obj.matrix_world.copy();obj.parent=None;obj.parent_type='OBJECT';obj.matrix_world=world;obj.vertex_groups.clear();obj.modifiers.clear()
        groups={n:obj.vertex_groups.new(name=n) for n in arm.data.bones.keys()}
        for vert in obj.data.vertices:
            for n,w in weight_z((obj.matrix_world@vert.co).z).items():groups[n].add([vert.index],w,'REPLACE')
        mod=obj.modifiers.new('Bib torso deformation','ARMATURE');mod.object=arm;obj.parent=arm;obj.matrix_world=world
for j in [0,rows-1]:tube('Bib_Stitched_Binding',[(Vector(verts[j*(bib_segments+1)+i])+Vector((0,0,.0004 if j==0 else -.0004))) for i in range(bib_segments+1)],.001,white)

# Ensure material paths portable, document exact produced geometry and source basis.
bpy.context.view_layer.update()
for obj in objects:
    if obj.type=='MESH' and not obj.data.uv_layers:
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.1,island_margin=.01);bpy.ops.object.mode_set(mode='OBJECT')
    if obj.type=='MESH':
        while len(obj.data.uv_layers)>1:obj.data.uv_layers.remove(obj.data.uv_layers[-1])
        obj.data.uv_layers[0].name='UVMap';obj.data.uv_layers.active_index=0;obj.data.uv_layers[0].active_render=True
character_objects=[arm]+[o for o in objects if o.name in bpy.data.objects]
# Main scene stores a clean rest rig. Renders use presentation-only poses in a separate review file.
(out/'Models/Source~').mkdir(exist_ok=True)
blendpath=out/'Models/Source~/Biathlete_16_7.blend';bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(blendpath))
# Runtime derivative: one weighted body, independent rifle/skis/poles and3 true LODs.
def join_meshes(name,parts,bone=None,origin=None):
    bpy.context.view_layer.update();bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:
        world=obj.matrix_world.copy();parent_bone=obj.parent_bone if obj.parent_type=='BONE' else None
        obj.parent=None;obj.parent_type='OBJECT';obj.matrix_world=world
        if bone is None and parent_bone:
            group=obj.vertex_groups.get(parent_bone) or obj.vertex_groups.new(name=parent_bone);group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
        obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();obj=bpy.context.object;obj.name=name
    obj.modifiers.clear()
    if origin is not None:
        scene.cursor.location=origin;bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    if bone:rigid(obj,bone)
    else:
        matrix=obj.matrix_world.copy();obj.parent=arm;obj.matrix_world=matrix
        mod=obj.modifiers.new('Humanoid skin','ARMATURE');mod.object=arm
    return obj
allmesh=[o for o in scene.objects if o.type=='MESH'];gear=[];rigged=[]
rifle=join_meshes('Rifle_Prop',rifleparts,'Spine',(.045,.188,1.36));gear.append(rifle)
for pre in ['Left','Right']:
    poleparts=[o for o in scene.objects if o.type=='MESH' and o.name.startswith(pre+'_') and ('_Pole' in o.name or '_Carbon_Pole' in o.name)]
    gear.append(join_meshes(pre+'_Pole_Prop',poleparts,pre+'Hand',heads[pre+'Hand']))
    skiparts=[o for o in scene.objects if o.type=='MESH' and o.name.startswith(pre+'_') and any(s in o.name for s in ['Nordic_Ski','Binding_','Ski_Brand'])]
    gear.append(join_meshes(pre+'_Ski_Prop',skiparts,pre+'Foot',heads[pre+'Foot']))
body_parts=[o for o in scene.objects if o.type=='MESH' and o not in gear]
primary=next(o for o in body_parts if o.name=='RaceSuit_Continuous_Tailored_Shell');body_parts.remove(primary);body_parts.insert(0,primary)
gamebody=join_meshes('Biathlete_Body_LOD0',body_parts);rigged.append(gamebody)
for level,ratio in [(1,.40),(2,.14)]:
    lod=gamebody.copy();lod.data=gamebody.data.copy();bpy.context.collection.objects.link(lod);lod.name='Biathlete_Body_LOD'+str(level)
    bpy.ops.object.select_all(action='DESELECT');lod.select_set(True);bpy.context.view_layer.objects.active=lod
    modifier=lod.modifiers.new('Measured geometry LOD','DECIMATE');modifier.ratio=ratio;bpy.ops.object.modifier_apply(modifier=modifier.name);lod.hide_render=True;rigged.append(lod)
character_objects=[arm]+gear+rigged+[o for o in scene.objects if o.type=='EMPTY']
bpy.ops.object.select_all(action='DESELECT')
for obj in character_objects:obj.select_set(True)
bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(out/'Models/Biathlete_16_7.fbx'),use_selection=True,object_types={'ARMATURE','MESH','EMPTY'},add_leaf_bones=False,bake_anim=False,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',path_mode='RELATIVE',use_mesh_modifiers=True)
# Source walk/run preserved verbatim and explicitly labelled, not ski animation.
shutil.copyfile(src/'01_Character/man-base-character-rigged.fbx',out/'Animations/Source_Walk_Run_Not_Ski.fbx')
manifest={'schema_version':1,'id':'biathlete_16_7_v1','blender':bpy.app.version_string,'height_m':1.8,'bones':len(arm.data.bones),'material_color_space':'scene-linear','source_license':'CC0 Grab3D; original tailored garments/equipment','meshes':[],'materials':[],'sockets':[x[0] for x in sockets]}
for obj in character_objects:
    if obj.type=='MESH':obj.data.calc_loop_triangles();manifest['meshes'].append({'name':obj.name,'triangles':len(obj.data.loop_triangles),'vertices':len(obj.data.vertices),'uv_layers':len(obj.data.uv_layers),'skinned':any(m.type=='ARMATURE' for m in obj.modifiers),'bone_parent':obj.parent_bone})
for m in materials:manifest['materials'].append({'name':m.name,'color':list(m.diffuse_color),'roughness':m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value,'metallic':m.node_tree.nodes.get('Principled BSDF').inputs['Metallic'].default_value})
manifest['triangles']=sum(x['triangles'] for x in manifest['meshes'])
manifest['active_lod0_triangles']=sum(x['triangles'] for x in manifest['meshes'] if not x['name'].endswith(('LOD1','LOD2')))
(out/'Documentation/asset-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(review/'model-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
# Studio render, actual model only. World/light floor are not exported with the rig.
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
    for d in prefs.devices:d.use=d.type=='OPTIX'
    scene.cycles.device='GPU'
except Exception:pass
scene.render.resolution_x=1100;scene.render.resolution_y=1400;scene.render.resolution_percentage=100
scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=-.55
scene.world=bpy.data.worlds.new('Neutral studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.15,.20,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
def point(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
for pos,energy,size,color in [((2,-3,4),650,3,(1,.91,.82)),((-3,-1,2.5),420,3,(.74,.84,1)),((1,3,3.5),800,2,(.85,.92,1))]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=energy;o.data.shape='DISK';o.data.size=size;o.data.color=color;point(o,(0,0,1))
floor=block('Studio cyclorama',(0,0,-.02),(200,200,.04),material('Studio_Grey',(.12,.15,.19),.72),0)
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.lens=55
for name,pos,target,scale in [('front',(.12,-4,1.0),(0,0,.98),2.22),('side',(4,-.25,1.05),(0,0,.98),2.22),('back',(.0,4,1.04),(0,0,.98),2.22),('face',(.2,-2,1.68),(0,0,1.664),.42),('rifle',(.65,2,1.35),(.04,.2,1.29),1.30),('boots',(.7,-1.4,.38),(0,-.08,.16),.68)]:
    cam.location=pos;point(cam,target);cam.data.ortho_scale=scale;scene.render.filepath=str(review/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(review/'Biathlete_16_7_Studio.blend'))
print('M167_BUILD_COMPLETE',manifest['triangles'],len(manifest['meshes']))
