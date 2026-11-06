"""Natural athlete visual equipment. No functional weapon mechanism/gameplay.

Mask derived from Skiing Lady by lancehuang526, CC-BY-4.0; see source LICENSE.
Rifle furniture and ski/pole graphics are project-authored from visual reference.
"""
import bpy,bmesh,math,json,subprocess
import numpy as np
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[3]
SETTINGS_PATH=ROOT/'Assets/Biathlon/Characters/Biathlete_16_7/Documentation/equipment-detail-v1.json'
SETTINGS=json.loads(SETTINGS_PATH.read_text())

def save_image(name,rgb,out,noncolour=False):
    h,w=rgb.shape[:2];im=bpy.data.images.new(name,width=w,height=h,alpha=True);im.colorspace_settings.name='Non-Color' if noncolour else 'sRGB'
    pixels=np.ones((h,w,4),dtype=np.float32);pixels[:,:,:3]=rgb;im.pixels.foreach_set(pixels.ravel());im.filepath_raw=str(out/'Textures'/(name+'.png'));im.file_format='PNG';im.save();return im

def map_texture(mat,im,socket='Base Color'):
    n=mat.node_tree.nodes.new('ShaderNodeTexImage');n.image=im;mat.node_tree.links.new(n.outputs['Color'],mat.node_tree.nodes['Principled BSDF'].inputs[socket]);return n

def mask_parts(source,material,mesh,rigid,materials):
    with bpy.data.libraries.load(str(source/'SkiingLady_Goggles_Selected.blend'),link=False) as (src,dst):dst.objects=list(src.objects)
    parts=list(dst.objects)
    for o in parts:bpy.context.collection.objects.link(o)
    lens=next(o for o in parts if 'glass_pink' in o.name)
    points=np.array([list(v.co)for v in lens.data.vertices]);centre=points.mean(axis=0);_,vectors=np.linalg.eigh(np.cov((points-centre).T))
    x=vectors[:,2];x*=1 if x[0]>0 else -1
    z=vectors[:,1];z*=1 if z[2]>0 else -1
    y=np.cross(z,x);frame=np.stack([x,y,z],axis=1);local=(points-centre)@frame
    scale=Vector((SETTINGS['mask_width_m']/np.ptp(local[:,0]),1,SETTINGS['mask_height_m']/np.ptp(local[:,2])))
    scale.y=scale.x;target=Vector(SETTINGS['mask_lens_centre_m'])
    rim=material('PULSE_Mask_Graphite_Frame',(.018,.026,.034),.39)
    glass=material('PULSE_Mask_Amber_Lens',(.65,.18,.022),.12,.72)
    foam=material('PULSE_Mask_Contact_Foam',(.009,.011,.014),.93)
    elastic=material('PULSE_Mask_Woven_Strap',(.025,.032,.042),.82)
    edge=material('PULSE_Mask_Strap_Accent',(.40,.008,.021),.61)
    materials.extend([rim,glass,foam,elastic,edge])
    head=bpy.data.objects['Anatomical_Head_Neck'];evaluated=head.evaluated_get(bpy.context.evaluated_depsgraph_get());head_mesh=evaluated.to_mesh()
    head_tree=BVHTree.FromPolygons([head.matrix_world@v.co for v in head_mesh.vertices],[list(f.vertices)for f in head_mesh.polygons]);evaluated.to_mesh_clear()
    for obj in list(parts):
        original=obj.name
        if 'fabric_tex' in original or 'fabric_white' in original:
            parts.remove(obj);bpy.data.objects.remove(obj,do_unlink=True);continue
        for v in obj.data.vertices:
            p=Vector(((np.array(v.co)-centre)@frame));p=Vector((p.x*scale.x,p.y*scale.y,p.z*scale.z))
            v.co=target+p
            if 'woolen' in original:
                contact=head_tree.find_nearest(v.co);blend=max(0,min(1,(v.co.y+.130)/.037))
                if contact[0] is not None:
                    # Preserve the donor's nose/cheek opening. Moving padding
                    # vertically to a nearest skin point makes it hang below
                    # the lens; only its depth needs the facial contact fit.
                    v.co.y=v.co.y*(1-blend)+(contact[0].y+contact[1].y*.002)*blend
        # glTF duplicates vertices at shading/UV seams. Subdivision before
        # welding makes each island shrink independently and tears the padding.
        bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(obj.data);bm.free()
        obj.data.materials.clear()
        mat=glass if 'glass_pink' in original else foam if 'woolen' in original else elastic if 'fabric_tex' in original else edge if 'fabric_white' in original else rim
        obj.data.materials.append(mat)
        for face in obj.data.polygons:face.material_index=0;face.use_smooth=True
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
        sub=obj.modifiers.new('Refined donor contour','SUBSURF');sub.levels=SETTINGS['mask_subdivision_levels'];bpy.ops.object.modifier_apply(modifier=sub.name)
        if mat==glass:
            solid=obj.modifiers.new('Lens edge thickness','SOLIDIFY');solid.thickness=.0008;bpy.ops.object.modifier_apply(modifier=solid.name)
        obj.name='Mask_Derived_'+original;rigid(obj,'Head')
    # The solid strap is built later, after the final cap has been fitted.
    return parts

def ski_and_pole_materials(material,materials,out):
    top=material('PULSE_Ski_Printed_Topsheet',(1,1,1),SETTINGS['ski_topsheet_roughness'],.16)
    font=ROOT/'Assets/Resources/Biathlon/UI/Fonts/Roboto.ttf';atlas=out/'Textures/Ski_PULSE_Topsheet_BaseColor_4K.png'
    subprocess.run(['powershell','-NoProfile','-File',str(Path(__file__).with_name('draw_ski_graphics.ps1')),'-Output',str(atlas),'-FontPath',str(font)],check=True,capture_output=True)
    map_texture(top,bpy.data.images.load(str(atlas)))
    pole=material('PULSE_Pole_Carbon_Weave',(.045,.055,.065),SETTINGS['pole_carbon_roughness'],.38)
    v,u=np.mgrid[0:2048,0:512].astype(np.float32);warp=np.sin((u+v*.45)*math.tau/32);weft=np.sin((u-v*.45)*math.tau/32)
    tone=.025+.017*(warp*weft+.8)+.005*np.sin(u*math.tau/2)
    colour=np.stack([tone*.75,tone*.90,tone],axis=2)
    map_texture(pole,save_image('Pole_PULSE_Carbon_BaseColor_2K',colour,out))
    materials.extend([top,pole]);return top,pole

def pole_details(pre,grip,axis,normal,tip,tube,mesh,rigid,alloy,rubber,pole):
    # The shaft exits the bottom of the existing verified handle. Its contact
    # geometry and hand/strap frames remain untouched.
    across=axis.cross(normal).normalized();centre=tip-axis*.055
    for k in range(3):
        radial=normal*math.cos(k*math.tau/3)+across*math.sin(k*math.tau/3)
        tube(pre+'_Pole_Basket_Spoke',[centre,centre+radial*.020],.0016,rubber,pre+'Hand')
    tube(pre+'_Pole_Ferrule',[tip-axis*.026,tip+axis*.004],.0045,alloy,pre+'Hand')
    tube(pre+'_Pole_Carbon_Collar',[grip+axis*.055,grip+axis*.071],.0051,alloy,pre+'Hand')

def reference_rifle(oldparts,material,mesh,tube,block,rigid,materials,out,registry):
    # Keep licensed CC0 receiver/barrel UVs, replace every hunting furniture part.
    parts=[]
    for obj in oldparts:
        if obj.name.startswith('CC0_Visible_Metal_'):parts.append(obj)
        else:
            if obj in registry:registry.remove(obj)
            bpy.data.objects.remove(obj,do_unlink=True)
    authored=[]
    def add(o):authored.append(o);return o
    wood=bpy.data.materials['Rifle_Race_Laminate'];wood.use_nodes=True;wood.node_tree.nodes.clear()
    p=wood.node_tree.nodes.new('ShaderNodeBsdfPrincipled');output=wood.node_tree.nodes.new('ShaderNodeOutputMaterial');wood.node_tree.links.new(p.outputs['BSDF'],output.inputs['Surface'])
    p.inputs['Roughness'].default_value=SETTINGS['rifle_walnut_roughness'];wood.diffuse_color=(.18,.065,.025,1)
    source=ROOT/'Assets/Biathlon/Characters/Biathlete_16_7/Models/Source~/Walnut'
    diffuse=bpy.data.images.load(str(source/'natural_walnut_veneer_diff_2k.jpg'));diffuse.colorspace_settings.name='sRGB';w,h=diffuse.size
    pixels=np.empty(w*h*4,np.float32);diffuse.pixels.foreach_get(pixels);rgb=pixels.reshape(h,w,4)[:,:,:3]*np.array(SETTINGS['rifle_walnut_linear_tint'],np.float32)
    map_texture(wood,save_image('Rifle_PULSE_Walnut_BaseColor_2K',rgb,out))
    normal=bpy.data.images.load(str(source/'natural_walnut_veneer_nor_gl_2k.png'));normal.colorspace_settings.name='Non-Color';pixels=np.empty(w*h*4,np.float32);normal.pixels.foreach_get(pixels)
    node=wood.node_tree.nodes.new('ShaderNodeTexImage');node.image=save_image('Rifle_PULSE_Walnut_Normal_2K',pixels.reshape(h,w,4)[:,:,:3],out,True)
    bump=wood.node_tree.nodes.new('ShaderNodeNormalMap');bump.inputs['Strength'].default_value=SETTINGS['rifle_walnut_normal_strength'];wood.node_tree.links.new(node.outputs['Color'],bump.inputs['Color']);wood.node_tree.links.new(bump.outputs[0],p.inputs['Normal'])
    black=bpy.data.materials['Rifle_Padded_Composite'];silver=bpy.data.materials['Rifle_Satin_Hardware'];red=material('PULSE_Rifle_Magazine_Red',(.48,.006,.012),.34)
    materials.append(red)
    def profile(name,outline,depth,mat,bevel=.003):
        n=len(outline);obj=mesh(name,[(x,y,z)for y in [-depth/2,depth/2]for x,z in outline],[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n)for i in range(n)],[mat])
        bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(obj.data);bm.free()
        bpy.context.view_layer.objects.active=obj
        mod=obj.modifiers.new('Soft manufactured edges','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
        return add(obj)
    outline=[(-.008,.035),(-.008,.21),(-.015,.29),(.011,.405),(.012,.64),(.054,.64),(.065,.54),(.071,.485),(.10,.455),(.11,.375),(.15,.345),(.16,.08),(.15,.035)]
    stock=profile('PULSE_Biathlon_Walnut_Stock',outline,.045,wood)
    for x,z,rx,rz in [(.038,.316,.024,.038),(.036,.413,.023,.036)]:
        bpy.ops.mesh.primitive_uv_sphere_add(segments=48,ring_count=24,location=(x,0,z));cutter=bpy.context.object;cutter.scale=(rx,.08,rz)
        bpy.context.view_layer.objects.active=stock;cut=stock.modifiers.new('Visible sporting furniture aperture','BOOLEAN');cut.operation='DIFFERENCE';cut.solver='EXACT';cut.object=cutter;bpy.ops.object.modifier_apply(modifier=cut.name);bpy.data.objects.remove(cutter,do_unlink=True)
    bpy.context.view_layer.objects.active=stock;bevel=stock.modifiers.new('Soft aperture rims','BEVEL');bevel.width=.0015;bevel.segments=3;bpy.ops.object.modifier_apply(modifier=bevel.name)
    for z in [.115,.225]:
        add(tube('PULSE_Cheek_Rest_Post',[(.003,0,z),(-.048,0,z)],.005,silver))
        add(tube('PULSE_Cheek_Post_Collar',[(-.021,0,z),(-.030,0,z)],.008,black))
    profile('PULSE_Raised_Walnut_Cheek_Rest',[(-.060,.082),(-.061,.26),(-.043,.267),(-.041,.081)],.051,wood,.005)
    profile('PULSE_Adjustable_Butt_Plate',[(-.036,.012),(-.038,.025),(.16,.025),(.175,.009),(.17,-.005),(.148,.009)],.055,black,.005)
    for x in [.013,.115]:add(tube('PULSE_Butt_Adjuster',[(x,0,.016),(x,0,.043)],.005,silver))
    add(tube('PULSE_Lower_Butt_Hook',[(.151,0,.012),(.182,0,-.006),(.185,0,.030)],.007,silver))
    for z in [.462,.505,.548,.591]:
        add(block('PULSE_Magazine_Rail',(.019,-.029,z),(.025,.019,.037),black,.003))
        add(block('PULSE_Spare_Magazine',(.004,-.034,z),(.018,.017,.033),red,.0025))
    def ring(name,centre,radius,length,mat):
        x,y,z=centre;verts=[];faces=[]
        for dz,r in [(-length/2,radius),(length/2,radius),(-length/2,radius-.004),(length/2,radius-.004)]:
            verts.extend([(x+math.cos(i*math.tau/64)*r,y+math.sin(i*math.tau/64)*r,z+dz)for i in range(64)])
        for i in range(64):
            j=(i+1)%64;faces.extend([(i,j,64+j,64+i),(128+j,128+i,192+i,192+j),(i,128+i,128+j,j),(64+i,64+j,192+j,192+i)])
        return add(mesh(name,verts,faces,[mat]))
    ring('PULSE_Rear_Diopter',(-.050,0,.416),.021,.024,black)
    add(block('PULSE_Rear_Diopter_Pedestal',(-.027,0,.408),(.040,.026,.034),silver,.003))
    add(tube('PULSE_Rear_Diopter_Adjustment',[(-.052,.011,.408),(-.052,.031,.408)],.009,black))
    ring('PULSE_Front_Tunnel_Sight',(-.032,0,1.055),.022,.029,black)
    ring('PULSE_Front_Aperture',(-.032,0,1.055),.007,.002,silver)
    add(block('PULSE_Front_Sight_Riser',(-.014,0,1.043),(.024,.021,.024),black,.002))
    for side in [-1,1]:
        add(tube('PULSE_External_Support_Rod',[(.128,side*.026,.065),(.085,side*.026,.390)],.0025,silver))
        for x,z in [(.128,.065),(.085,.390)]:add(tube('PULSE_Stock_Fastener',[(x,side*.021,z),(x,side*.029,z)],.005,black))
    carry=Matrix.Translation((.045,.188,.81))@Matrix.Rotation(math.radians(-4),4,'Y')
    for obj in authored:
        # All newly generated surfaces use an explicit longitudinal wood UV.
        if not obj.data.uv_layers:obj.data.uv_layers.new(name='EquipmentUV')
        for face in obj.data.polygons:
            face.use_smooth=face.area<.0003
            for loop in face.loop_indices:
                p=obj.matrix_world@obj.data.vertices[obj.data.loops[loop].vertex_index].co;obj.data.uv_layers.active.data[loop].uv=(p.z/.65,p.x/.19+.2)
        obj.matrix_world=carry@obj.matrix_world;rigid(obj,'Spine')
    return parts+authored
