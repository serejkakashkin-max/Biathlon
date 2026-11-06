"""Original PULSE sporting equipment and tailored weighted garments.

Blender4.5 LTS; unwarped CC0 MakeHuman anatomy and mapped24-joint skeleton.
All outputs are actual meshes/materials, not image generation. Reproducible.
"""
import bpy, bmesh, math, json, argparse, sys, shutil, hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.kdtree import KDTree
from mathutils.bvhtree import BVHTree
sys.path.insert(0,str(Path(__file__).resolve().parent))
from equipment_refine import CONTACT_SETTINGS, HANDLE_PROFILE, grip_frame, tuck_grip_frame, sporting_rifle, grip_finger_matrices
from refine_garments import GARMENT_SETTINGS, race_bib, transfer_weights, analytic_panel_material, anatomical_grip, continuous_collar, projected_print, fitted_harness, fit_harness_adjuster
from bib_print import apply_bib_print
from equipment_detail import SETTINGS as EQUIPMENT_SETTINGS, SETTINGS_PATH as EQUIPMENT_SETTINGS_PATH, mask_parts, ski_and_pole_materials, pole_details, reference_rifle
from mask_webbing import finish_mask
from rifle_visual_fit import fit_rifle
from rifle_suspension import add_suspension
from garment_materials import finish_suit,finish_bib

parser=argparse.ArgumentParser();parser.add_argument('--source',required=True);parser.add_argument('--output',required=True);parser.add_argument('--review',required=True);parser.add_argument('--anatomy');parser.add_argument('--fast',action='store_true')
args=parser.parse_args(sys.argv[sys.argv.index('--')+1:]);src=Path(args.source).resolve();out=Path(args.output).resolve();review=Path(args.review).resolve()
anatomy_path=Path(args.anatomy).resolve() if args.anatomy else out/'Models/Source~/MakeHuman/Natural/athletic_fitted.blend'
anatomy_metadata=json.loads(anatomy_path.with_suffix('.json').read_text(encoding='utf-8'))
if anatomy_metadata.get('proportion_mode')!='natural_unwarped_anatomy':
    raise ValueError('The rejected legacy bone-fit anatomy cannot be used for the natural athlete.')
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
heads={name:Vector(value) for name,value in anatomy_metadata['heads'].items()}
bpy.ops.object.mode_set(mode='EDIT')
preferred={'Hips':'Spine02','Spine02':'Spine01','Spine01':'Spine','Spine':'neck','neck':'Head','Head':'head_end',
 'LeftShoulder':'LeftArm','RightShoulder':'RightArm','LeftArm':'LeftForeArm','RightArm':'RightForeArm','LeftForeArm':'LeftHand','RightForeArm':'RightHand',
 'LeftUpLeg':'LeftLeg','RightUpLeg':'RightLeg','LeftLeg':'LeftFoot','RightLeg':'RightFoot','LeftFoot':'LeftToeBase','RightFoot':'RightToeBase'}
for b in arm.data.edit_bones:
    b.head=arm.matrix_world.inverted()@heads[b.name]
    if b.name in preferred:b.tail=arm.matrix_world.inverted()@heads[preferred[b.name]]
    else:b.tail=Vector(anatomy_metadata['tails'][b.name])
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
wood=material('Sport_Laminate',(.11,.038,.021),.34)
skin=material('Anatomical_Skin_CC0_2K',(.7,.5,.4),.62)
shutil.copyfile(anatomy_path.parent/'young_lightskinned_male_diffuse.png',out/'Textures/MakeHuman_Skin_BaseColor_2K.png')
skin_image=bpy.data.images.load(str(out/'Textures/MakeHuman_Skin_BaseColor_2K.png'))
skin_tex=skin.node_tree.nodes.new('ShaderNodeTexImage');skin_tex.image=skin_image
skin.node_tree.links.new(skin_tex.outputs['Color'],skin.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
skin.node_tree.nodes['Principled BSDF'].inputs['Subsurface Weight'].default_value=.06
materials=[red,black,scarlet,white,ink,rubber,carbon,alloy,titanium,amber,thread,stitch,wood,skin]
glove_shell=material('Race_Glove_Softshell',(.009,.012,.017),.72)
glove_palm=material('Race_Glove_Palm_Grip',(.014,.016,.019),.82)
materials.extend([glove_shell,glove_palm])
#4K real tangent-space textile maps. Fine paired warp/weft includes slight deterministic yarn variation.
size=4096
u=np.arange(size,dtype=np.float32)[None,:];v=np.arange(size,dtype=np.float32)[:,None]
h=.5+.13*np.sin(u*math.tau/16)*np.sin(v*math.tau/12)+.035*np.sin((u+v)*math.tau/64)
dx=np.gradient(h,axis=1)*1.8;dy=np.gradient(h,axis=0)*1.8
normal=np.stack((-dx,-dy,np.ones_like(h)),axis=2);normal/=np.linalg.norm(normal,axis=2,keepdims=True)
def image_from(name,rgb,colorspace):
    existing=out/'Textures'/(name+'.png')
    if existing.exists():
        im=bpy.data.images.load(str(existing));im.colorspace_settings.name=colorspace;return im
    im=bpy.data.images.new(name,width=size,height=size,alpha=False)
    im.colorspace_settings.name=colorspace
    rgba=np.ones((size,size,4),dtype=np.float32);rgba[:,:,:3]=rgb
    im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(out/'Textures'/(name+'.png'));im.file_format='PNG';im.save();return im
weave_n=image_from('Textile_Normal_4K',normal*.5+.5,'Non-Color')
weave_r=image_from('Textile_Roughness_4K',np.repeat(np.clip(.63+(h-.5)*.4,0,1)[:,:,None],3,axis=2),'Non-Color')
weave_c=image_from('Textile_BaseColor_4K',np.repeat(np.clip(.92+(h-.5)*.16,0,1)[:,:,None],3,axis=2),'sRGB')
for glove_material,strength in [(glove_shell,.08),(glove_palm,.035)]:
    nodes=glove_material.node_tree.nodes;links=glove_material.node_tree.links
    tex=nodes.new('ShaderNodeTexImage');tex.image=weave_n
    normal_node=nodes.new('ShaderNodeNormalMap');normal_node.inputs['Strength'].default_value=strength
    links.new(tex.outputs['Color'],normal_node.inputs['Color']);links.new(normal_node.outputs['Normal'],nodes['Principled BSDF'].inputs['Normal'])
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
    bpy.context.view_layer.update()
    matrix=obj.matrix_world.copy();obj.parent=arm;obj.parent_type='BONE';obj.parent_bone=bone;obj.matrix_world=matrix
    if obj not in objects:objects.append(obj)
    return obj
def weight_z(z):
    levels=[(heads[name].z,name) for name in ['Hips','Spine02','Spine01','Spine','neck']]
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
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.resolution_u=8;curve.bevel_depth=radius;curve.bevel_resolution=2;curve.use_fill_caps=True
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
    font=Path(__file__).resolve().parents[3]/'Assets/Resources/Biathlon/UI/Fonts/Roboto.ttf'
    if font.exists():o.data.font=bpy.data.fonts.load(str(font),check_existing=True)
    bpy.context.view_layer.update();s=width/max(.001,o.dimensions.x);o.scale=(s,s,s);bpy.ops.object.convert(target='MESH');o=bpy.context.object;o.data.materials.append(mat);return rigid(o,bone)

# One unwarped anatomical face/neck surface. No clamped source jaw or backing tube.
bpy.data.objects.remove(body,do_unlink=True)
with bpy.data.libraries.load(str(anatomy_path),link=False) as (_,target_data):target_data.objects=['MakeHuman_Athletic_Fitted']
body=target_data.objects[0];bpy.context.collection.objects.link(body);body.parent=arm;body.matrix_world=Matrix.Identity(4);body.modifiers.clear()
bm=bmesh.new();bm.from_mesh(body.data)
bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-6,plane_co=(0,0,1.525),plane_no=(0,0,1),clear_inner=True)
bm.to_mesh(body.data);bm.free();body.name='Anatomical_Head_Neck';body.data.materials.clear();body.data.materials.append(skin)
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body
sub=body.modifiers.new('Continuous facial quad loops','SUBSURF');sub.levels=1;bpy.ops.object.modifier_apply(modifier=sub.name)
mod=body.modifiers.new('Anatomical head and neck skin','ARMATURE');mod.object=arm;objects.append(body);smooth(body)
eye_white=material('Eye_Sclera',(.72,.75,.70),.25);eye_iris=material('Eye_Iris',(.025,.045,.033),.3);eye_pupil=material('Eye_Pupil',(.001,.001,.001),.16)
materials.extend([eye_white,eye_iris,eye_pupil])
for sign,suffix in [(1,'L'),(-1,'R')]:
    center=Vector(anatomy_metadata['source_joints']['eye.'+suffix+'____head'])+Vector((0,.09,0))
    for name,scale,offset,mat in [('Sclera',(.012,.012,.012),Vector(),eye_white),('Iris',(.0053,.001,.0053),Vector((0,-.0116,0)),eye_iris),('Pupil',(.0024,.0007,.0024),Vector((0,-.0125,0)),eye_pupil)]:
        bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,location=center+offset);o=bpy.context.object;o.name=suffix+'_Eye_'+name;o.scale=scale
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat);smooth(o);rigid(o,'Head')

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
text('Bib27_Front','27',(0,-.082,1.32),.132,ink,'Spine01')
text('Bib27_Back','27',(0,.164,1.32),.132,ink,'Spine01',True)
text('Bib_Footer','WORLD CUP',(0,-.073,1.243),.135,ink,'Spine01')
# Chest zip and collar edge.
tube('Chest_Waterproof_Zip',[(0,-.060,1.538),(0,-.065,1.51),(0,-.080,1.46),(0,-.095,1.42)],.0014,black)
block('Zip_Puller',(0,-.060,1.527),(.010,.003,.019),alloy,.002,'neck')

# Compact racing cap (no pompom), amber panoramic wraparound lens and elastic strap.
cap_rings=[]
for j in range(37):
    angle=j/36*math.pi/2
    cap_rings.append((0,.045,1.700+.113*math.sin(angle),max(.0001,.085*math.cos(angle)),max(.0001,.111*math.cos(angle))))
cap=loft('Race_Cap',cap_rings,[red,black],lambda u,v:1 if v<.07 else 0,weight=lambda p,j,i:{'Head':1},n=96)
for vert in cap.data.vertices:
    vert.co.z-=.078*max(0,min(1,(vert.co.y-.025)/.075))*max(0,min(1,(1.75-vert.co.z)/.072))
# Original vector wordmark: flat printed strokes avoid font n-gon artefacts.
letters=[[[[0,0],[0,1],[.8,1],[1,.8],[.8,.55],[0,.55]]],[[[0,1],[0,.2],[.2,0],[.8,0],[1,.2],[1,1]]],[[[0,1],[0,0],[1,0]]],[[[1,.95],[.8,1],[.2,1],[0,.8],[.2,.6],[.8,.4],[1,.2],[.8,0],[.2,0],[0,.05]]],[[[0,0],[0,1],[1,1]],[[0,.53],[.85,.53]],[[0,0],[1,0]]]]
for letter,paths in enumerate(letters):
    for part,path in enumerate(paths):
        vs=[];fs=[]
        points=[Vector((-.041+letter*.017+x*.011,-.10,1.747+z*.018)) for x,z in path]
        for a,b in zip(points,points[1:]):
            direction=(b-a).normalized();normal=Vector((-direction.z,0,direction.x))*.00065
            k=len(vs);vs.extend([a-normal,a+normal,b+normal,b-normal]);fs.append((k,k+1,k+2,k+3))
        rigid(mesh('Cap_PULSE_Stroke_'+str(letter)+'_'+str(part),vs,fs,[white]),'Head')
lensverts=[];lensuv=[];lensfaces=[]
def lenspoint(t,v):
    x=(t-.5)*.177;y=-.050-.065*(1-(x/.102)**2);z=1.686+(v-.5)*.056*(1-.20*(abs(x)/.0885)**4)+.003*(abs(x)/.0885)**2
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
    t=i/96*math.tau;strap.append((.083*math.cos(t),.032+.09*math.sin(t),1.707))
tube('Goggle_Elastic_Strap',strap,.006,black,'Head')
glasses_parts=[o for o in objects if o.name.startswith(('Panoramic_Amber_Lens','Goggle_'))]
mask_meshes=mask_parts(anatomy_path.parents[2]/'SkiingLady',material,mesh,rigid,materials)
for obj in mask_meshes:obj.hide_render=True
ski_print,pole_weave=ski_and_pole_materials(material,materials,out)

# Clean CC0 anatomical topology: the suit has real torso/hip/joint loops rather
# than voxelized T-shirt/short hems. Keep the source head as a separate skin asset.
# anatomy_path resolved before skeleton authoring
if not anatomy_path.exists():raise FileNotFoundError('Prepare the CC0 MakeHuman anatomy first: '+str(anatomy_path))
anatomy_metadata=json.loads(anatomy_path.with_suffix('.json').read_text(encoding='utf-8'))
with bpy.data.libraries.load(str(anatomy_path),link=False) as (source_data,target_data):
    target_data.objects=['MakeHuman_Athletic_Fitted']
hand_source=target_data.objects[0];bpy.context.collection.objects.link(hand_source)
hand_source.parent=None;hand_source.matrix_world=Matrix.Identity(4);hand_source.modifiers.clear()
verts=[v.co.copy() for v in hand_source.data.vertices]
clothes=hand_source.copy();clothes.data=hand_source.data.copy();bpy.context.collection.objects.link(clothes)
clothes.name='RaceSuit_Continuous_Tailored_Shell';clothes.parent=None;clothes.modifiers.clear()
bm=bmesh.new();bm.from_mesh(clothes.data)
for z in [.18,1.54]:bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,plane_co=(0,0,z),plane_no=(0,0,1))
bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z<.18-1e-5 or v.co.z>1.54+1e-5],context='VERTS')
for pre,side in [('Left',1),('Right',-1)]:
    wrist=heads[pre+'Hand'];direction=(wrist-heads[pre+'ForeArm']).normalized()
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,plane_co=wrist-direction*.012,plane_no=direction)
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if side*v.co.x>.38 and (v.co-wrist).dot(direction)>-.012],context='VERTS')
bm.to_mesh(clothes.data);bm.free();clothes.data.update()
# Millimetres of garment ease. Anatomy remains underneath the compression suit;
# bounded shallow wrinkles occur at flexion zones, not the old shorts crotch.
for vertex in clothes.data.vertices:
    vertex.co+=vertex.normal*.0035
    p=vertex.co
    p.y+=.0009*math.sin(p.z*235+p.x*12)*math.exp(-((p.z-.50)/.055)**2)
bpy.ops.object.select_all(action='DESELECT');clothes.select_set(True);bpy.context.view_layer.objects.active=clothes
sub=clothes.modifiers.new('Anatomical quad-loop subdivision','SUBSURF');sub.levels=2;sub.render_levels=2;bpy.ops.object.modifier_apply(modifier=sub.name)
bib_reference=clothes.copy();bib_reference.data=clothes.data.copy();bpy.context.collection.objects.link(bib_reference);bib_reference.name='Bib_High_Resolution_Surface';bib_reference.hide_render=True
dec=clothes.modifiers.new('Compression garment runtime budget','DECIMATE');dec.ratio=.08;bpy.ops.object.modifier_apply(modifier=dec.name)
clothes.data.update();smooth(clothes)
collar=continuous_collar(clothes,mesh,tube,red,weight_z,body)
# The trouser hem follows the anatomical lower leg and tucks into the hinged
# boot cuff. It must not be forced onto the foot several centimetres above the ankle.
for v in clothes.data.vertices:
    if v.co.z<.29:
        pre='Left' if v.co.x>0 else 'Right';center=heads[pre+'Foot']
        blend=max(0,min(1,(.29-v.co.z)/.085));blend=blend*blend*(3-2*blend)
        angle=math.atan2(v.co.y-.056,v.co.x-center.x)
        v.co.x=v.co.x*(1-blend)+(center.x+.033*math.cos(angle))*blend
        v.co.y=v.co.y*(1-blend)+(.056+.047*math.sin(angle))*blend
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
tailored=material('PULSE_Natural_Tailored_Textile',(.27,.003,.010),GARMENT_SETTINGS['textile_roughness'])
clothes.data.materials.clear();clothes.data.materials.append(tailored)
nodes=tailored.node_tree.nodes;links=tailored.node_tree.links;output=nodes.get('Material Output');principal=nodes.get('Principled BSDF')
emit=nodes.new('ShaderNodeEmission');links.new(analytic_panel_material(tailored,red,black),emit.inputs[0]);links.new(emit.outputs[0],output.inputs['Surface'])
atlas=bpy.data.images.new('Natural_Suit_Panels_BaseColor_4K',width=4096,height=4096,alpha=False)
target=nodes.new('ShaderNodeTexImage');target.image=atlas;nodes.active=target
scene.render.engine='CYCLES';scene.cycles.samples=1;scene.render.bake.margin=16
bpy.ops.object.select_all(action='DESELECT');clothes.select_set(True);bpy.context.view_layer.objects.active=clothes
bpy.ops.object.bake(type='EMIT');atlas.filepath_raw=str(out/'Textures/Natural_Suit_Panels_BaseColor_4K.png');atlas.file_format='PNG';atlas.save()
links.new(principal.outputs[0],output.inputs['Surface']);links.new(target.outputs['Color'],principal.inputs['Base Color'])
normaltex=nodes.new('ShaderNodeTexImage');normaltex.image=weave_n;norm=nodes.new('ShaderNodeNormalMap');norm.inputs['Strength'].default_value=GARMENT_SETTINGS['textile_normal_strength'];links.new(normaltex.outputs['Color'],norm.inputs['Color']);links.new(norm.outputs[0],principal.inputs['Normal'])
roughtex=nodes.new('ShaderNodeTexImage');roughtex.image=weave_r;rough_scale=nodes.new('ShaderNodeMath');rough_scale.operation='MULTIPLY';rough_scale.inputs[1].default_value=.2;rough_base=nodes.new('ShaderNodeMath');rough_base.operation='ADD';rough_base.inputs[1].default_value=.6;links.new(roughtex.outputs[0],rough_scale.inputs[0]);links.new(rough_scale.outputs[0],rough_base.inputs[0]);links.new(rough_base.outputs[0],principal.inputs['Roughness']);principal.inputs['Sheen Weight'].default_value=.15
textile_receipt=finish_suit(clothes,tailored,red,black,out)
materials.append(tailored)
mod=clothes.modifiers.new('Original normalized skeletal weights','ARMATURE');mod.object=arm;clothes.parent=arm;clothes.matrix_world=Matrix.Identity(4);objects.append(clothes)
for old in list(objects):
    if old.name in ['RaceSuit_Tailored_Torso','Left_Race_Sleeve','Right_Race_Sleeve','Left_Race_Tight','Right_Race_Tight']:
        objects.remove(old);bpy.data.objects.remove(old,do_unlink=True)
# Collar is already sewn to the exact continuous suit boundary above.
for side in [-1,1]:
    pre='Left' if side>0 else 'Right';wrist=heads[pre+'Hand'];direction=(wrist-heads[pre+'ForeArm']).normalized()
    keep={i for i,v in enumerate(verts) if (v-wrist).dot(direction)>-.008 and side*v.x>.41}
    faces=[[k for k in f.vertices] for f in hand_source.data.polygons if all(k in keep for k in f.vertices)]
    indices=sorted({v for f in faces for v in f});remap={v:i for i,v in enumerate(indices)}
    palmar=tuck_grip_frame(anatomy_metadata,side)[2]
    glove_panels=[1 if sum((hand_source.data.vertices[k].normal for k in face),Vector()).normalized().dot(palmar)>.05 else 0 for face in faces]
    curled=anatomical_grip(anatomy_metadata,verts,indices,side)
    glove=mesh(pre+'_Anatomical_Glove',curled,[[remap[i] for i in f] for f in faces],[glove_shell,glove_palm],glove_panels);rigid(glove,pre+'Hand')
    bpy.ops.object.select_all(action='DESELECT');glove.select_set(True);bpy.context.view_layer.objects.active=glove
    sub=glove.modifiers.new('Anatomical finger loops','SUBSURF');sub.levels=1;sub.render_levels=1;bpy.ops.object.modifier_apply(modifier=sub.name)
    tucked=anatomical_grip(anatomy_metadata,verts,indices,side,True)
    alternative=mesh(pre+'_Tuck_Glove',tucked,[[remap[i] for i in f] for f in faces],[glove_shell,glove_palm],glove_panels);rigid(alternative,pre+'Hand')
    bpy.context.view_layer.objects.active=alternative
    sub=alternative.modifiers.new('Tuck finger loops','SUBSURF');sub.levels=1;sub.render_levels=1;bpy.ops.object.modifier_apply(modifier=sub.name)
    alternative.hide_render=True
    # Anatomical glove cuff follows the original wrist/forearm surface.
    # The old world-axis box covered the strap loop and pinched the wrist.
    longitudinal=(Vector(anatomy_metadata['finger_bones']['finger3-1'+('.L' if side>0 else '.R')]['head'])-wrist).normalized()
    across=Vector(anatomy_metadata['finger_bones']['finger5-1'+('.L' if side>0 else '.R')]['head'])-Vector(anatomy_metadata['finger_bones']['finger2-1'+('.L' if side>0 else '.R')]['head'])
    across=(across-longitudinal*across.dot(longitudinal)).normalized();palmar=across.cross(longitudinal).normalized()
    original=BVHTree.FromPolygons([v.co for v in hand_source.data.vertices],[list(f.vertices) for f in hand_source.data.polygons])
    cuff_vertices=[];cuff_faces=[];cuff_weights=[];cuff_materials=[];sections=[-.025,-.020,-.010,0,.0035,.005]
    for j,along in enumerate(sections):
        for k in range(48):
            angle=k*math.tau/48;radial=across*math.cos(angle)+palmar*math.sin(angle);origin=wrist+longitudinal*along;hit=original.ray_cast(origin,radial,.10)
            if hit[0] is None:raise RuntimeError('Anatomical cuff misses wrist')
            cuff_vertices.append(hit[0]+radial*.0015);hand_weight=max(0,min(1,(along+.025)/.030));cuff_weights.append({pre+'Hand':hand_weight,pre+'ForeArm':1-hand_weight})
            if j:
                prev=(j-1)*48;cur=j*48;cuff_faces.append((prev+k,prev+(k+1)%48,cur+(k+1)%48,cur+k));cuff_materials.append(1 if j==5 else 0)
    cuff=mesh(pre+'_Anatomical_Glove_Cuff',cuff_vertices,cuff_faces,[black,scarlet],cuff_materials,weights=cuff_weights)
    transfer_weights(cuff,clothes,arm)
    # Match the sleeve weights at the overlap, then blend into the rigid glove.
    for vertex in cuff.data.vertices:
        along=(vertex.co-wrist).dot(longitudinal);blend=max(0,min(1,(along+.012)/.017));blend=blend*blend*(3-2*blend)
        sewn={cuff.vertex_groups[g.group].name:g.weight*(1-blend) for g in vertex.groups}
        sewn[pre+'Hand']=sewn.get(pre+'Hand',0)+blend
        for group in cuff.vertex_groups:group.remove([vertex.index])
        for name,value in sewn.items():
            if value>0:cuff.vertex_groups[name].add([vertex.index],value,'REPLACE')
bpy.data.objects.remove(hand_source,do_unlink=True)

# Nordic boots: narrow forefoot, flexible sole, carbon ankle cuff and zip covers.
for side in [-1,1]:
    pre='Left' if side>0 else 'Right';x=heads[pre+'Foot'].x
    bootrings=[(x,-.022,.043,.047,.145),(x,-.024,.052,.048,.146),(x,-.025,.073,.046,.144),(x,-.022,.095,.045,.135),(x,.008,.119,.043,.105),(x,.049,.145,.039,.067),(x,.056,.195,.037,.056),(x,.056,.238,.036,.052)]
    def bootweight(p,j=0,i=0):
        t=max(0,min(1,(p.z-.14)/.075));t=t*t*(3-2*t)
        return {pre+'Foot':1-t,pre+'Leg':t}
    loft(pre+'_Nordic_Boot',interpolated(bootrings,3),[black,scarlet],lambda u,v:1 if v<.14 else 0,weight=bootweight,n=64)
    loft(pre+'_Carbon_Ankle_Cuff',[(x,.057,.175,.041,.059),(x,.057,.209,.041,.059),(x,.057,.226,.039,.055)], [carbon],weight=lambda p,j,i:{pre+'Leg':1},n=56)
    for z in [.053,.066]:tube(pre+'_Boot_Sole_Seam',[(x+.048*math.cos(t*math.tau/80),-.024+.146*math.sin(t*math.tau/80),z) for t in range(81)],.0012,stitch,pre+'Foot')
    tube(pre+'_Boot_Zip',[(x,-.158,.084),(x,-.113,.108),(x,-.029,.14),(x,.001,.21)],.002,alloy)
    boot_zip=objects[-1]
    for vertex in boot_zip.data.vertices:
        for group in list(vertex.groups):boot_zip.vertex_groups[group.group].remove([vertex.index])
        for name,value in bootweight(vertex.co).items():boot_zip.vertex_groups[name].add([vertex.index],value,'REPLACE')
    block(pre+'_Ankle_Ratchet',(x,-.004,.205),(.068,.012,.016),scarlet,.003,pre+'Leg')
    tube(pre+'_Toe_Hinge_Pin',[(x-.018,-.171,.055),(x+.018,-.171,.055)],.003,alloy,pre+'Foot')
    # Skis in true metre scale; slender44mm waist and rising Nordic tip.
    vs=[];uv=[];fs=[];mi=[]
    for j in range(97):
        t=j/96;y=-1.03+t*1.94;w=.022*(.62+.38*math.sin(math.pi*t)**.15);z=.021+.068*max(0,1-t/.115)**2+.008*max(0,(t-.94)/.06)**2
        for dx,dz in [(-w,0),(w,0),(-w,-.008),(w,-.008)]:vs.append((x+dx,y,z+dz));uv.append(((dx/w+1)/2,t))
        if j<96:
            k=j*4;fs.extend([(k,k+4,k+5,k+1),(k+2,k+3,k+7,k+6),(k,k+2,k+6,k+4),(k+1,k+5,k+7,k+3)]);mi.extend([0,1,2,2])
    ski=mesh(pre+'_Nordic_Ski_194',vs,fs,[ski_print,black,alloy],mi,uv);rigid(ski,pre+'Foot')
    block(pre+'_Binding_Plate',(x,-.014,.036),(.040,.24,.016),carbon,.003,pre+'Foot')
    block(pre+'_Binding_Toe_Clamp',(x,-.183,.046),(.045,.024,.026),titanium,.004,pre+'Foot')
    block(pre+'_Binding_Release_Tab',(x,-.203,.043),(.035,.024,.014),scarlet,.003,pre+'Foot')
    # Pole shaft/grip/basket/strap are independently parented below a hand socket.
    wrist=heads[pre+'Hand'];grip,axis,palm_normal=grip_frame(anatomy_metadata,side);tip=grip+axis*1.46
    shaft=tube(pre+'_Carbon_Pole',[grip,tip],.0043,pole_weave,pre+'Hand')
    if not shaft.data.uv_layers:shaft.data.uv_layers.new(name='CarbonUV')
    for loop in shaft.data.loops:
        p=shaft.matrix_world@shaft.data.vertices[loop.vertex_index].co-grip
        shaft.data.uv_layers.active.data[loop.index].uv=(math.atan2(p.dot(axis.cross(palm_normal)),p.dot(palm_normal))/math.tau+.5,p.dot(axis)/1.46)
    # Rounded upper cap; the contact section remains 24 mm in diameter.
    radial=palm_normal;across=axis.cross(radial).normalized();handle_vertices=[];handle_faces=[]
    rings=HANDLE_PROFILE
    for j,(along,radius) in enumerate(rings):
        for k in range(32):
            angle=k*math.tau/32;handle_vertices.append(grip+axis*along+(radial*math.cos(angle)+across*math.sin(angle))*radius)
            if j:handle_faces.append(((j-1)*32+k,(j-1)*32+(k+1)%32,j*32+(k+1)%32,j*32+k))
    handle_faces.extend([tuple(range(31,-1,-1)),tuple(range((len(rings)-1)*32,len(rings)*32))])
    handle=mesh(pre+'_Pole_Grip',handle_vertices,handle_faces,[rubber]);rigid(handle,pre+'Hand')
    tube(pre+'_Pole_Inlay',[grip+axis*.10,grip+axis*.47],.0047,scarlet,pre+'Hand')
    tube(pre+'_Pole_Tip',[tip,tip+axis*.04],.0027,alloy,pre+'Hand')
    basket=tip-axis*.055
    across=axis.cross(palm_normal).normalized()
    tube(pre+'_Small_Pole_Basket',[basket+(palm_normal*math.cos(t*math.tau/48)+across*math.sin(t*math.tau/48))*.021 for t in range(49)],.003,rubber,pre+'Hand')
    pole_details(pre,grip,axis,palm_normal,tip,tube,mesh,rigid,alloy,rubber,pole_weave)
    # The strap follows the glove, not a rigid triangle on the pole. Each
    # grip posture has its own broad webbing path around the back of the hand.
    longitudinal=(Vector(anatomy_metadata['finger_bones']['finger3-1'+('.L' if side>0 else '.R')]['head'])-wrist).normalized()
    normal=(palm_normal-longitudinal*palm_normal.dot(longitudinal)).normalized();lateral=longitudinal.cross(normal).normalized()
    for variant,frame in [('Skate',grip_frame(anatomy_metadata,side)),('Tuck',tuck_grip_frame(anatomy_metadata,side))]:
        centre,shaft,_=frame
        glove=bpy.data.objects[pre+('_Tuck_Glove' if variant=='Tuck' else '_Anatomical_Glove')]
        strap_offset=CONTACT_SETTINGS['wrist_strap_centre_offset_m'] if variant=='Tuck' else CONTACT_SETTINGS['plant_wrist_strap_centre_offset_m']
        glove['wrist_strap_centre_offset_m']=strap_offset
        bpy.context.view_layer.update()
        surface=BVHTree.FromPolygons([glove.matrix_world@v.co for v in glove.data.vertices],[list(f.vertices) for f in glove.data.polygons])
        vs=[];fs=[]
        # Closed padded wrist loop: the hand is physically inside this band.
        # Every radial edge is fitted to the actual glove cross-section.
        for j in range(64):
            angle=j*math.tau/64;radial=normal*math.cos(angle)+lateral*math.sin(angle)
            for along,band_offset in [(-.006,.0015),(.006,.0015),(.006,.003),(-.006,.003)]:
                origin=wrist+longitudinal*(strap_offset+along)
                hit=surface.ray_cast(origin,radial,.10)
                if hit[0] is None:raise RuntimeError('Wrist strap cross-section misses glove: '+pre+' '+variant)
                vs.append(hit[0]+radial*band_offset)
        for j in range(64):
            a=j*4;b=((j+1)%64)*4
            for k in range(4):fs.append((a+k,a+(k+1)%4,b+(k+1)%4,b+k))
        # Two short palmar connectors join the loop to the handle. They do not
        # form an external triangular cage around the fingers.
        for side_of_hand in [-1,1]:
            radial=(normal*.8+lateral*side_of_hand*.6).normalized()
            origin=wrist+longitudinal*.018;hit=surface.ray_cast(origin,radial,.10)
            start=hit[0]+radial*.0022
            toward_wrist=wrist-centre;toward_wrist=(toward_wrist-shaft*toward_wrist.dot(shaft)).normalized()
            across_handle=shaft.cross(toward_wrist).normalized()
            end=centre-shaft*.040+toward_wrist*.008+across_handle*(side_of_hand*.009)
            points=[]
            for j in range(17):
                t=j/16;point=start.lerp(end,t)
                if 0<j<15:
                    nearest=surface.find_nearest(point)
                    point=nearest[0]+nearest[1]*.0025
                points.append(point)
            offset=len(vs)
            for j,point in enumerate(points):
                tangent=(points[min(16,j+1)]-points[max(0,j-1)]).normalized();width=(lateral-tangent*lateral.dot(tangent)).normalized();depth=tangent.cross(width).normalized()
                for u,v in [(-1,-1),(1,-1),(1,1),(-1,1)]:vs.append(point+width*(u*.0045)+depth*(v*.0007))
                if j:
                    a=offset+(j-1)*4;b=offset+j*4
                    for k in range(4):fs.append((a+k,a+(k+1)%4,b+(k+1)%4,b+k))
            fs.extend([tuple(offset+k for k in [3,2,1,0]),tuple(range(len(vs)-4,len(vs)))])
        strap=mesh(pre+'_'+variant+'_Glove_Strap',vs,fs,[black]);rigid(strap,pre+'Hand');strap.hide_render=variant=='Tuck'



# Licensed CC0 visible metal plus original sport-specific external geometry.
rifleparts,reused_rifle_parts=sporting_rifle(anatomy_path.parents[2]/'PolyHavenRifle',material,mesh,tube,block,rigid,materials,out)
materials.extend([bpy.data.materials[n] for n in ['Rifle_PolyHaven_CC0_Metal','Rifle_Race_Laminate','Rifle_Padded_Composite','Rifle_Satin_Hardware']])
rifleparts=reference_rifle(rifleparts,material,mesh,tube,block,rigid,materials,out,objects)
rifleparts,rifle_visual_receipt=fit_rifle(rifleparts,objects,material,mesh,rigid,materials,EQUIPMENT_SETTINGS)
reused_rifle_parts=sum(o.name.startswith('CC0_Visible_Metal_')for o in rifleparts)
# Broad webbing with tension-following shoulder routes, weighted along the torso.
for side in [-1,1]:
    controls=[(side*.09,.175,1.11),(side*.145,.14,1.30),(side*.15,.08,1.405),(side*.13,.00,1.425),(side*.12,-.065,1.35),(side*.14,-.076,1.19),(side*.10,.00,1.08)]
    points=interpolated(controls,10)
    # Flat webbing as a real quad ribbon, not a cylinder.
    verts=[];ws=[];uv=[]
    for i,p in enumerate(points):
        for s in [-1,1]:verts.append((p[0]+s*.014,p[1],p[2]));ws.append(weight_z(p[2]));uv.append(((s+1)/2,i/(len(points)-1)))
    mesh(('Left' if side<0 else 'Right')+'_Rifle_Harness_Webbing',verts,[(i*2,i*2+1,i*2+3,i*2+2) for i in range(len(points)-1)],[black],uv=uv,weights=ws)
    for edge in [-1,1]:tube('Harness_Stitch',[(x+edge*.012,y-.0005,z) for x,y,z in points],.00065,stitch)
    block('Harness_Adjuster',(side*.137,-.078,1.205),(.036,.007,.032),titanium,.002,'Spine01')

# Explicit named attachment nodes, serialized by Unity authoring rather than name-based gameplay identity.
contacts=[]
for pre,side in [('Left',1),('Right',-1)]:
    grip,axis,_=grip_frame(anatomy_metadata,side)
    obj=bpy.data.objects.new(pre+'_Pole_Grip_Contact',None);bpy.context.collection.objects.link(obj);obj.location=grip;normal=grip_frame(anatomy_metadata,side)[2];obj.rotation_euler=Matrix((-normal,axis,(-normal).cross(axis))).transposed().to_euler();rigid(obj,pre+'Hand');contacts.append(obj.name)
    # Point markers survive the FBX handedness change without relying on a
    # rotation basis. Native tests compare the imported palm axis to this side.
    longitudinal=(Vector(anatomy_metadata['finger_bones']['finger3-1'+('.L' if side>0 else '.R')]['head'])-heads[pre+'Hand']).normalized()
    physical_normal=(normal-longitudinal*normal.dot(longitudinal)).normalized()
    for label,offset in [('Palm',.01),('Dorsal',-.01)]:
        marker=bpy.data.objects.new(pre+'_'+label+'_Side',None);bpy.context.collection.objects.link(marker);marker.location=heads[pre+'Hand']+longitudinal*.06+physical_normal*offset;rigid(marker,pre+'Hand');contacts.append(marker.name)
    for label,digit in [('Middle',3),('Index',2),('Little',5)]:
        marker=bpy.data.objects.new(pre+'_'+label+'_Knuckle',None);bpy.context.collection.objects.link(marker)
        marker.location=anatomy_metadata['finger_bones'][f'finger{digit}-1'+('.L' if side>0 else '.R')]['head']
        rigid(marker,pre+'Hand');contacts.append(marker.name)
    centre,tuck_axis,tuck_normal=tuck_grip_frame(anatomy_metadata,side)
    obj=bpy.data.objects.new(pre+'_Tuck_Grip_Contact',None);bpy.context.collection.objects.link(obj);obj.location=centre;obj.rotation_euler=Matrix((-tuck_normal,tuck_axis,(-tuck_normal).cross(tuck_axis))).transposed().to_euler();rigid(obj,pre+'Hand');contacts.append(obj.name)

    obj=bpy.data.objects.new(pre+'_Toe_Hinge_Contact',None);bpy.context.collection.objects.link(obj);obj.location=(heads[pre+'Foot'].x,-.171,.055);rigid(obj,pre+'Foot');contacts.append(obj.name)
sockets=[('Rifle_Back_Socket',EQUIPMENT_SETTINGS['rifle_carry_bone'],(.045,.188,1.36)),('Rifle_Hand_Socket','RightHand',heads['RightHand']),('Ski_Left_Socket','LeftFoot',heads['LeftFoot']),('Ski_Right_Socket','RightFoot',heads['RightFoot']),('Pole_Left_Socket','LeftHand',heads['LeftHand']),('Pole_Right_Socket','RightHand',heads['RightHand'])]
for name,bone,position in sockets:
    obj=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(obj);obj.location=position;rigid(obj,bone)

# Fit the bib and printed markings to the final cloth/cap, avoiding clipped text.
def fit_surface(obj,target,offset):
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    mod=obj.modifiers.new('Sewn surface fit','SHRINKWRAP');mod.target=target;mod.wrap_method='NEAREST_SURFACEPOINT';mod.wrap_mode='OUTSIDE_SURFACE';mod.offset=offset
    bpy.ops.object.modifier_apply(modifier=mod.name)
bib=bpy.data.objects['Woven_Bib_27'];objects.remove(bib);bpy.data.objects.remove(bib,do_unlink=True)
bib,shoulders=race_bib(bib_reference,arm,mesh,tube,weight_z,white,clothes)
bpy.data.objects.remove(bib_reference,do_unlink=True)
for old in list(objects):
    if old.name.startswith(('Bib27_','Bib_Header','Bib_Footer')):
        objects.remove(old);bpy.data.objects.remove(old,do_unlink=True)
bib_print_receipt=apply_bib_print(bib,out,material,materials,GARMENT_SETTINGS)
finish_bib(bib.data.materials[0],out)
for obj in list(objects):
    if obj.name.startswith('Bib_Bound_Edge') or 'Rifle_Harness_Webbing' in obj.name or obj.name.startswith('Harness_Stitch') or '_Torso_Flatlock' in obj.name:
        objects.remove(obj);bpy.data.objects.remove(obj,do_unlink=True)
harness_material=material('PULSE_Rifle_Carry_Webbing',(.009,.012,.016),.85);materials.append(harness_material)
fitted_harness(clothes,bib,arm,mesh,tube,harness_material,stitch,weight_z)
rifle_suspension_receipt=add_suspension(mesh,material,materials,arm,EQUIPMENT_SETTINGS)
# Sample the centre-front surface explicitly. Nearest-point fitting a tube from
# inside the old torso collapsed its cross-section and made it bridge the neck.
for old in list(objects):
    if old.name.startswith(('Chest_Waterproof_Zip','Zip_Puller')):
        objects.remove(old);bpy.data.objects.remove(old,do_unlink=True)
bpy.context.view_layer.update();zip_tree=BVHTree.FromObject(clothes,bpy.context.evaluated_depsgraph_get())
zip_points=[]
for i in range(49):
    z=1.536-(1.536-1.400)*i/48
    hit=zip_tree.ray_cast(Vector((0,-.6,z)),Vector((0,1,0)),1.2)
    if hit[0] is None:raise RuntimeError('Zip has no continuous front fabric support')
    zip_points.append(hit[0]+Vector((0,-.0018,0)))
zipper=tube('Chest_Waterproof_Zip',zip_points,.0014,black);transfer_weights(zipper,clothes,arm)
puller=block('Zip_Puller',zip_points[3]+Vector((0,-.002,0)),(.008,.002,.013),alloy,.001)
puller.rotation_euler=(zip_points[0]-zip_points[6]).to_track_quat('Z','X').to_euler()
transfer_weights(puller,clothes,arm)
fit_surface(cap,body,.0035)
mask_webbing_receipt=finish_mask(mask_meshes,cap,mesh,tube,block,rigid,materials,out,EQUIPMENT_SETTINGS)
for obj in list(objects):
    if obj.name.startswith(('Bib27_','Bib_Header','Bib_Footer')):
        bm=bmesh.new();bm.from_mesh(obj.data)
        bmesh.ops.triangulate(bm,faces=list(bm.faces))
        bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=4,use_grid_fill=True)
        bm.to_mesh(obj.data);bm.free()
        projected_print(obj,bib,.0012,back=obj.name.endswith('_Back'));transfer_weights(obj,bib,arm)
    if obj.name.startswith('Cap_PULSE'):
        projected_print(obj,cap,.0015)
    if any(token in obj.name for token in ['Flatlock','Sleeve_Seam']):
        fit_surface(obj,clothes,.0012);transfer_weights(obj,clothes,arm)
    if obj.name.startswith('Harness_Adjuster'):
        fit_harness_adjuster(obj,arm)

# Keep the complete suit underneath the separate, eased pullover vest.
# Both layers retain normalized source weights and are checked after Unity skinning.
from garment_partition import retain_covered_suit
garment_coverage_receipt=retain_covered_suit(clothes)
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
blendpath=out/'Models/Source~/Biathlete_16_7_Natural.blend';bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(blendpath))
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
glasses=join_meshes('Eyewear_Glasses',glasses_parts,'Head',heads['Head']);gear.append(glasses)
mask=join_meshes('Eyewear_Mask',mask_meshes,'Head',heads['Head']);mask.hide_render=True;gear.append(mask)
rifle=join_meshes('Rifle_Prop',rifleparts,EQUIPMENT_SETTINGS['rifle_carry_bone'],(.045,.188,1.36));gear.append(rifle)
for pre in ['Left','Right']:
    poleparts=[o for o in scene.objects if o.type=='MESH' and o.name.startswith(pre+'_') and ('_Pole' in o.name or '_Carbon_Pole' in o.name)]
    gear.append(join_meshes(pre+'_Pole_Prop',poleparts,pre+'Hand',heads[pre+'Hand']))
    skiparts=[o for o in scene.objects if o.type=='MESH' and o.name.startswith(pre+'_') and any(s in o.name for s in ['Nordic_Ski','Binding_','Ski_Brand'])]
    gear.append(join_meshes(pre+'_Ski_Prop',skiparts,pre+'Foot',heads[pre+'Foot']))
# Keep two authored anatomical hand postures. A plant fist cannot be
# rotated wholesale into the palm-supported tuck without a broken wrist.
for pre in ['Left','Right']:
    for original,label in [(pre+'_Anatomical_Glove',pre+'_Skate_Glove'),(pre+'_Tuck_Glove',pre+'_Tuck_Glove')]:
        obj=join_meshes(label,[bpy.data.objects[original],bpy.data.objects[pre+'_'+('Tuck' if 'Tuck' in label else 'Skate')+'_Glove_Strap']],pre+'Hand',heads[pre+'Hand']);gear.append(obj)
body_parts=[o for o in scene.objects if o.type=='MESH' and o not in gear and not o.get('authoring_reference_only',False)]
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
bpy.ops.export_scene.fbx(filepath=str(out/'Models/Biathlete_16_7_Natural.fbx'),use_selection=True,object_types={'ARMATURE','MESH','EMPTY'},add_leaf_bones=False,bake_anim=False,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',path_mode='RELATIVE',use_mesh_modifiers=True)
# Source walk/run preserved verbatim and explicitly labelled, not ski animation.
shutil.copyfile(src/'01_Character/man-base-character-rigged.fbx',out/'Animations/Source_Walk_Run_Not_Ski.fbx')
manifest={'schema_version':1,'id':'biathlete_16_7_natural_v1','export_fbx_sha256':hashlib.sha256((out/'Models/Biathlete_16_7_Natural.fbx').read_bytes()).hexdigest(),'blender':bpy.app.version_string,'height_m':1.8,'bones':len(arm.data.bones),'material_color_space':'scene-linear','source_license':'CC0 MakeHuman hm08 anatomy/skin and CC0 Poly Haven rifle (Mateusz Sadek); mask adapted from Skiing Lady by lancehuang526, CC-BY-4.0; project-authored garments/equipment adaptations','meshes':[],'materials':[],'sockets':[x[0] for x in sockets]}
manifest['equipment_detail_definition_sha256']=hashlib.sha256(EQUIPMENT_SETTINGS_PATH.read_bytes()).hexdigest()
manifest['eyewear_variants']=['glasses','mask']
manifest['mask_provenance']=json.loads((anatomy_path.parents[2]/'SkiingLady/provenance.json').read_text())
manifest['rifle_reference_usage']=EQUIPMENT_SETTINGS['rifle_reference_usage']
manifest['rifle_visual_fit']=rifle_visual_receipt
manifest['rifle_suspension']=rifle_suspension_receipt
manifest['harness_routes']={'schema_version':1,'id':'m167_smooth_shoulder_paths_without_redundant_rear_loops','shoulder_paths':2,'rear_vertical_loops':0,'route':'upper rifle junction over smooth padded shoulder/front, narrow underarm return to lower rifle junction; four full-width seated connections','material':'PULSE_Rifle_Carry_Webbing'}
manifest['national_textiles']=textile_receipt
manifest['mask_webbing']=mask_webbing_receipt
manifest['walnut_source']=json.loads((anatomy_path.parents[2]/'Walnut/provenance.json').read_text())
for obj in character_objects:
    if obj.type=='MESH':obj.data.calc_loop_triangles();manifest['meshes'].append({'name':obj.name,'triangles':len(obj.data.loop_triangles),'vertices':len(obj.data.vertices),'uv_layers':len(obj.data.uv_layers),'skinned':any(m.type=='ARMATURE' for m in obj.modifiers),'bone_parent':obj.parent_bone})
for m in materials:manifest['materials'].append({'name':m.name,'color':list(m.diffuse_color),'roughness':m.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value,'metallic':m.node_tree.nodes.get('Principled BSDF').inputs['Metallic'].default_value})
manifest['rifle_cc0_reused_visible_components']=reused_rifle_parts
manifest['contact_frames']=contacts
manifest['grip_variants']=['Skate','Tuck']
manifest['garment_detail_definition_sha256']=hashlib.sha256((Path(__file__).resolve().parents[3]/'Assets/Biathlon/Characters/Biathlete_16_7/Documentation/garment-detail-v1.json').read_bytes()).hexdigest()
manifest['bib_print']=bib_print_receipt
manifest['garment_skinning']='separate_pullover_vest_shared_skin_weights'
manifest['garment_coverage']=garment_coverage_receipt
manifest['contact_basis_conversion']='Blender -palm X, shaft Y; after Unity FBX handedness conversion, imported +X is the physical palmar side. Point markers independently validate the sign.'
manifest['grip_audit']={pre:grip_finger_matrices(anatomy_metadata,side)[1] for pre,side in [('Left',1),('Right',-1)]}
from power_grip import solve_finger
manifest['tuck_grip_style']='full_palmar_power_grip_at_upper_handle'
manifest['tuck_grip_palmar_offset_m']=CONTACT_SETTINGS['tuck_grip_palmar_offset_m']
manifest['wrist_strap_centre_offset_m']=CONTACT_SETTINGS['wrist_strap_centre_offset_m']
manifest['plant_wrist_strap_centre_offset_m']=CONTACT_SETTINGS['plant_wrist_strap_centre_offset_m']
manifest['hand_carpal_guard']=anatomy_metadata['hand_carpal_guard']
manifest['glove_shell_receipt']=anatomy_metadata['glove_shell_receipt']
manifest['finger_joint_limits_degrees']={'mcp_flexion':[0,100],'pip_flexion':[55,115],'dip_flexion':[25,85],'mcp_adduction':[-20,20],'ring_metacarpal_cupping':[0,15],'little_metacarpal_cupping':[0,20]}
from thumb_grip import solve_thumb
manifest['plant_thumb_surface_pose']={pre:grip_finger_matrices(anatomy_metadata,side)[1] for pre,side in [('Left',1),('Right',-1)]}
manifest['tuck_thumb_surface_pose']={pre:solve_thumb(anatomy_metadata,side,*tuck_grip_frame(anatomy_metadata,side))[1] for pre,side in [('Left',1),('Right',-1)]}
manifest['plant_grip_settings']={key:value for key,value in CONTACT_SETTINGS.items() if key.startswith('plant_')}
manifest['tuck_power_grip_obliquity_degrees']=CONTACT_SETTINGS['tuck_power_grip_obliquity_degrees']
manifest['tuck_upper_grip_offset_m']=CONTACT_SETTINGS['tuck_upper_grip_offset_m']
manifest['power_grip_finger_pose']={pre:{str(digit):solve_finger(anatomy_metadata,side,digit,*tuck_grip_frame(anatomy_metadata,side))[2] for digit in range(2,6)} for pre,side in [('Left',1),('Right',-1)]}

manifest['triangles']=sum(x['triangles'] for x in manifest['meshes'])
manifest['stored_lod0_triangles']=sum(x['triangles'] for x in manifest['meshes'] if not x['name'].endswith(('LOD1','LOD2')))
manifest['active_lod0_triangles']=sum(x['triangles'] for x in manifest['meshes'] if not x['name'].endswith(('LOD1','LOD2')) and not x['name'].endswith('Tuck_Glove') and x['name']!='Eyewear_Mask')
(out/'Documentation/natural-asset-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(review/'model-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
# Studio render, actual model only. World/light floor are not exported with the rig.
scene.render.engine='CYCLES';scene.cycles.samples=16 if args.fast else 48;scene.cycles.use_denoising=True
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
    for d in prefs.devices:d.use=d.type=='OPTIX'
    scene.cycles.device='GPU'
except Exception:pass
scene.render.resolution_x=1100;scene.render.resolution_y=1400;scene.render.resolution_percentage=65 if args.fast else 100
scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=-.55
scene.world=bpy.data.worlds.new('Neutral studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.15,.20,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
def point(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
for pos,energy,size,color in [((2,-3,4),650,3,(1,.91,.82)),((-3,-1,2.5),420,3,(.74,.84,1)),((1,3,3.5),800,2,(.85,.92,1))]:
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=energy;o.data.shape='DISK';o.data.size=size;o.data.color=color;point(o,(0,0,1))
floor=block('Studio cyclorama',(0,0,-.02),(200,200,.04),material('Studio_Grey',(.12,.15,.19),.72),0)
bpy.ops.object.camera_add();cam=bpy.context.object;scene.camera=cam;cam.data.type='ORTHO';cam.data.lens=55
for name,pos,target,scale in [('front',(.12,-4,1.0),(0,0,.98),2.22),('side',(4,-.25,1.05),(0,0,.98),2.22),('back',(.0,4,1.04),(0,0,.98),2.22),('face',(.2,-2,1.68),(0,0,1.664),.42),('rifle',(.65,2,1.35),(.04,.2,1.29),1.30),('boots',(.7,-1.4,.38),(0,-.08,.16),.68)]:
    if args.fast and name not in ['front','side','face']:continue
    cam.location=pos;point(cam,target);cam.data.ortho_scale=scale;scene.render.filepath=str(review/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(review/'Biathlete_16_7_Studio.blend'))
print('M167_BUILD_COMPLETE',manifest['triangles'],len(manifest['meshes']))
