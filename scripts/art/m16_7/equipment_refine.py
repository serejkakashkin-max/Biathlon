"""Editable sporting-prop kitbash and anatomical contact frames (Blender only)."""
import bpy,bmesh,math,json
from pathlib import Path
import numpy as np
from mathutils import Vector,Matrix

CONTACT_SETTINGS=json.loads((Path(__file__).resolve().parents[3]/'Assets/Biathlon/Characters/Biathlete_16_7/Documentation/contact-adaptation-v1.json').read_text())
HANDLE_PROFILE=[(-.075,.008),(-.074,.0105),(-.071,.012),(-.066,.012),(.035,.012),(.043,.0105),(.045,.009)]


def anatomical_grip_frame(metadata,side,obliquity,longitudinal_offset,palmar_offset,upper_offset):
    pre='Left' if side>0 else 'Right';suffix='.L' if side>0 else '.R'
    wrist=Vector(metadata['heads'][pre+'Hand'])
    longitudinal=(Vector(metadata['finger_bones']['finger3-1'+suffix]['head'])-wrist).normalized()
    knuckles=Vector(metadata['finger_bones']['finger5-1'+suffix]['head'])-Vector(metadata['finger_bones']['finger2-1'+suffix]['head'])
    across=(knuckles-longitudinal*knuckles.dot(longitudinal)).normalized()
    palmar=across.cross(longitudinal).normalized()
    if palmar.x*side>0:palmar=-palmar
    angle=math.radians(obliquity)
    axis=(across*math.cos(angle)-longitudinal*math.sin(angle)).normalized()
    centre=wrist+longitudinal*longitudinal_offset+palmar*palmar_offset
    index=Vector(metadata['finger_bones']['finger2-1'+suffix]['head'])
    centre+=axis*((index-centre).dot(axis)+upper_offset)
    return centre,axis,palmar


def grip_frame(metadata,side):
    # Working grip is fitted to the actual palmar surface, never circles
    # through virtual bone tails. The old circle fit left fingertips 7-20mm away.
    s=CONTACT_SETTINGS
    return anatomical_grip_frame(metadata,side,s['plant_power_grip_obliquity_degrees'],s['plant_grip_longitudinal_offset_m'],s['plant_grip_palmar_offset_m'],s['plant_upper_grip_offset_m'])


def tuck_grip_frame(metadata,side):
    s=CONTACT_SETTINGS
    return anatomical_grip_frame(metadata,side,s['tuck_power_grip_obliquity_degrees'],.075,s['tuck_grip_palmar_offset_m'],s['tuck_upper_grip_offset_m'])


def grip_finger_matrices(metadata,side,tuck=False):
    # Both authored grips use the same independently verified surface solver.
    # Hand posture differs through the frame, not a second unchecked algorithm.
    from power_grip import solve_finger
    from thumb_grip import solve_thumb
    frame=(tuck_grip_frame if tuck else grip_frame)(metadata,side);matrices={}
    for digit in range(2,6):matrices.update(solve_finger(metadata,side,digit,*frame)[0])
    band=(-.065,-.048) if tuck else CONTACT_SETTINGS['plant_thumb_axis_band_m']
    thumb,receipt=solve_thumb(metadata,side,*frame,axis_band=band);matrices.update(thumb)
    return matrices,receipt


def sporting_rifle(source,material,mesh,tube,block,rigid,materials,out):
    """Visible decorative geometry only; no functional internal mechanism."""
    parts=[]
    def add(obj):parts.append(obj);return obj
    metal=material('Rifle_PolyHaven_CC0_Metal',(.24,.25,.27),.38,.85)
    nodes=metal.node_tree.nodes;links=metal.node_tree.links;p=nodes.get('Principled BSDF')
    def image(file,colour):
        image=bpy.data.images.load(str(source/file),check_existing=True);image.colorspace_settings.name=colour;return image
    diff=image('bolt_action_rifle_7_62_diff_2k.jpg','sRGB')
    pixels=np.empty(diff.size[0]*diff.size[1]*4,dtype=np.float32);diff.pixels.foreach_get(pixels);pixels=pixels.reshape(-1,4)
    luminance=pixels[:,:3]@np.array([.2126,.7152,.0722],dtype=np.float32)
    pixels[:,:3]=(.035+.40*luminance[:,None])*np.array([.80,.88,1.0],dtype=np.float32)
    derived=bpy.data.images.new('Sport_Rifle_Cleaned_CC0_Metal',width=diff.size[0],height=diff.size[1],alpha=True);derived.colorspace_settings.name='sRGB';derived.pixels.foreach_set(pixels.ravel());diff=derived
    normal=image('bolt_action_rifle_7_62_nor_gl_2k.exr','Non-Color')
    rough=image('bolt_action_rifle_7_62_rough_2k.exr','Non-Color');metallic=image('bolt_action_rifle_7_62_metal_2k.exr','Non-Color')
    for im,socket in [(diff,'Base Color'),(rough,'Roughness'),(metallic,'Metallic')]:
        n=nodes.new('ShaderNodeTexImage');n.image=im;links.new(n.outputs['Color'],p.inputs[socket])
    n=nodes.new('ShaderNodeTexImage');n.image=normal;mapping=nodes.new('ShaderNodeNormalMap');mapping.inputs['Strength'].default_value=.65;links.new(n.outputs['Color'],mapping.inputs['Color']);links.new(mapping.outputs[0],p.inputs['Normal'])
    # Preserve the original UV layout on selected visible metal components.
    with bpy.data.libraries.load(str(source/'bolt_action_rifle_7_62_2k.blend'),link=False) as (src,dst):
        dst.objects=[n for n in src.objects if n.startswith('bolt_action_rifle_7_62') and not any(t in n for t in ('scope','wrap','bullet','trigger'))]
    imported=[]
    for obj in dst.objects:
        bpy.context.collection.objects.link(obj);imported.append(obj)
    body=next(o for o in imported if o.name.startswith('bolt_action_rifle_7_62') and len(o.data.vertices)>3000)
    bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.separate(type='LOOSE');bpy.ops.object.mode_set(mode='OBJECT')
    imported=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('bolt_action_rifle_7_62')]
    reused=0;source_stock=None
    for obj in imported:
        points=[obj.matrix_world@v.co for v in obj.data.vertices]
        lo=Vector(tuple(min(p[i] for p in points) for i in range(3)));hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
        # Exclude hunting stock/butt, ammunition, optical mounts and magazine.
        is_stock=(hi.x-lo.x)>.5 and lo.x<-.5
        keep=(is_stock or 'bolt_a' in obj.name or 'bolt_b' in obj.name or (lo.x>-.08 and lo.z>.012 and hi.z<.080))
        if not keep:bpy.data.objects.remove(obj,do_unlink=True);continue
        obj.parent=None;obj.matrix_world=Matrix.Identity(4)
        for vertex,point in zip(obj.data.vertices,points):
            vertex.co=Vector((-(point.z-.05),point.y,.41+(point.x+.18)*.85))
        if is_stock:source_stock=obj
        obj.data.materials.clear();obj.data.materials.append(metal)
        for face in obj.data.polygons:face.material_index=0;face.use_smooth=True
        obj.name='CC0_Visible_Metal_'+str(reused);reused+=1;add(obj)
    if reused<4:raise RuntimeError('Insufficient licensed metal geometry imported')
    # Appended source materials may reference their original textures/ subfolder.
    # Repair those retained datablocks before packing the editable production file.
    for im in bpy.data.images:
        candidate=source/Path(im.filepath).name
        if candidate.is_file():im.filepath=str(candidate)
        if not im.has_data and candidate.is_file():im.reload()
    # Convert the actual source maps for a portable URP Lit material.
    def save_rgb(im,name,noncolour=False):
        pixels=np.empty(im.size[0]*im.size[1]*4,dtype=np.float32);im.pixels.foreach_get(pixels)
        copy=bpy.data.images.new(name,width=im.size[0],height=im.size[1],alpha=True);copy.colorspace_settings.name='Non-Color' if noncolour else 'sRGB';copy.pixels.foreach_set(pixels);copy.filepath_raw=str(out/'Textures'/(name+'.png'));copy.file_format='PNG';copy.save();bpy.data.images.remove(copy)
    save_rgb(image('bolt_action_rifle_7_62_diff_2k.jpg','sRGB'),'Rifle_CC0_Laminate_BaseColor_2K');
    save_rgb(diff,'Rifle_CC0_BaseColor_2K');save_rgb(normal,'Rifle_CC0_Normal_2K',True)
    w,h=metallic.size;mr=np.empty(w*h*4,dtype=np.float32);rr=np.empty(w*h*4,dtype=np.float32);metallic.pixels.foreach_get(mr);rough.pixels.foreach_get(rr);mr=mr.reshape(-1,4);rr=rr.reshape(-1,4)
    mr[:,1]=mr[:,0];mr[:,2]=mr[:,0];mr[:,3]=1-rr[:,0]
    packed=bpy.data.images.new('Rifle_CC0_MetallicSmoothness_2K',width=w,height=h,alpha=True);packed.colorspace_settings.name='Non-Color';packed.pixels.foreach_set(mr.ravel());packed.filepath_raw=str(out/'Textures/Rifle_CC0_MetallicSmoothness_2K.png');packed.file_format='PNG';packed.save()
    wood=material('Rifle_Race_Laminate',(.16,.055,.024),.36)
    nodes=wood.node_tree.nodes;links=wood.node_tree.links;p=nodes.get('Principled BSDF')
    tex=nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=4;tex.inputs['Detail'].default_value=3
    coord=nodes.new('ShaderNodeTexCoord');mapping=nodes.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY';mapping.inputs[1].default_value=(16,12,.6);links.new(coord.outputs['Generated'],mapping.inputs[0]);links.new(mapping.outputs[0],tex.inputs[0])
    ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(.07,.021,.008,1);ramp.color_ramp.elements[1].color=(.27,.095,.035,1);links.new(tex.outputs['Fac'],ramp.inputs[0]);links.new(ramp.outputs[0],p.inputs['Base Color'])
    black=material('Rifle_Padded_Composite',(.018,.022,.025),.63);silver=material('Rifle_Satin_Hardware',(.30,.33,.36),.29,.88)
    def profile(name,outline,depth,mat):
        n=len(outline);verts=[(x,y,z) for y in [-depth/2,depth/2] for x,z in outline];faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
        obj=mesh(name,verts,faces,[mat]);bpy.context.view_layer.objects.active=obj
        bevel=obj.modifiers.new('Rounded sporting stock','BEVEL');bevel.width=.006;bevel.segments=4;bpy.ops.object.modifier_apply(modifier=bevel.name)
        return add(obj)
    if source_stock is None:raise RuntimeError('Missing CC0 stock geometry')
    stock=source_stock;stock.name='CC0_Biathlon_Adapted_Laminate_Stock';stock.data.materials.clear();stock.data.materials.append(wood)
    base=nodes.new('ShaderNodeTexImage');base.image=image('bolt_action_rifle_7_62_diff_2k.jpg','sRGB');links.new(base.outputs['Color'],p.inputs['Base Color'])
    n=nodes.new('ShaderNodeTexImage');n.image=normal;bump=nodes.new('ShaderNodeNormalMap');bump.inputs['Strength'].default_value=.5;links.new(n.outputs['Color'],bump.inputs['Color']);links.new(bump.outputs[0],p.inputs['Normal'])
    # Preserve the complete licensed stock and its UVs. Cosmetic sport fittings
    # use the same transform and surface, avoiding mismatched floating parts.
    add(block('Butt_Rubber_Plate',(.085,0,.025),(.14,.052,.025),black,.008))
    add(tube('Butt_External_Hook',[(.13,0,.02),(.16,0,-.005),(.175,0,.005)],.009,black))
    add(block('Raised_Cheek_Pad',(.025,0,.17),(.032,.057,.15),black,.010))
    for z in [.46,.51,.56]:
        add(block('Spare_Magazine_Carrier',(.025,-.028,z),(.031,.017,.036),black,.004))
        add(block('Magazine_Coloured_Base',(.025,-.030,z-.015),(.034,.027,.008),silver,.002))
    add(tube('External_Trigger_Guard',[(.045,0,.29),(.075,0,.29),(.076,0,.35),(.050,0,.37)],.0045,black))
    def ring(name,centre,radius,depth,mat):
        x,y,z=centre
        for zz in [-depth/2,depth/2]:add(tube(name,[(x+radius*math.cos(i*math.tau/64),y+radius*math.sin(i*math.tau/64),z+zz) for i in range(65)],.0035,mat))
    # Real hollow-looking diopter/front hood: rings instead of closed cylinders.
    ring('Rear_Diopter_Ring',(-.040,0,.405),.018,.018,black)
    add(block('Rear_Sight_Rail',(-.025,0,.385),(.040,.030,.015),silver,.003))
    ring('Front_Sight_Hood',(-.032,0,1.055),.022,.022,black)
    add(block('Front_Sight_Riser',(-.015,0,1.035),(.029,.022,.020),silver,.003))
    add(block('Snow_Cover_Hinged',(-.033,.020,1.055),(.052,.006,.052),black,.004))
    for z in [.11,.205,.365,.56,.645]:
        add(tube('External_Stock_Fastener',[(-.030,-.027,z),(-.030,-.031,z)],.004,silver))
    for side in [-1,1]:add(block('Back_Harness_Anchor',(.040,side*.028,.245),(.025,.010,.025),silver,.003))
    # Pack original procedural wood into a 2K UV-free colour swatch is avoided:
    # Unity receives a coherent dyed laminate base with fine normal detail.
    for obj in parts:
        matrix=Matrix.Translation((.045,.188,.81))@Matrix.Rotation(math.radians(-4),4,'Y');obj.matrix_world=matrix@obj.matrix_world;rigid(obj,'Spine')
    return parts,reused
