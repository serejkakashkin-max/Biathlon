"""Slim game-prop silhouette fitted to the owner's athlete reference.

External appearance only. Existing receiver detail is licensed CC0; the outer
barrel is a capped display mesh, with no internal or functional geometry.
"""
import bpy,math,json
from mathutils import Vector,Matrix

def fit_rifle(parts,registry,material,mesh,rigid,materials,settings):
    old=(Matrix.Translation((.045,.188,.81))@Matrix.Rotation(math.radians(-4),4,'Y')).inverted()
    sx=settings['rifle_visual_profile_scale'];sy=settings['rifle_visual_thickness_scale'];sz=settings['rifle_visual_length_scale']
    scale=Matrix.Diagonal((sx,sy,sz,1));carry=Matrix.Translation(settings['rifle_visual_carry_origin_m'])@Matrix.Rotation(math.radians(settings['rifle_visual_carry_angle_degrees']),4,'Y')
    kept=[];removed=[]
    circles={'PULSE_Rear_Diopter','PULSE_Front_Tunnel_Sight','PULSE_Front_Aperture'}
    for obj in parts:
        points=[old@obj.matrix_world@v.co for v in obj.data.vertices]
        low=min(v.z for v in points);high=max(v.z for v in points)
        if obj.name.startswith('CC0_Visible_Metal_') and (low>.605 or high<.1):
            removed.append(obj.name)
            if obj in registry:registry.remove(obj)
            bpy.data.objects.remove(obj,do_unlink=True);continue
        centre=sum(points,Vector())/len(points);round_part=obj.name in circles
        obj.parent=None;obj.matrix_world=Matrix.Identity(4);obj.modifiers.clear();obj.vertex_groups.clear()
        if obj.data.users>1:obj.data=obj.data.copy()
        for vertex,point in zip(obj.data.vertices,points):
            if round_part:
                delta=point-centre;point=scale@centre+Vector((delta.x*.75,delta.y*.75,delta.z*sz))
            else:point=scale@point
            vertex.co=carry@point
        obj.data.update();rigid(obj,settings['rifle_carry_bone']);kept.append(obj)
        if obj.name=='PULSE_Biathlon_Walnut_Stock':obj['aperture_probe_frame']=json.dumps([list(row)for row in carry@scale])
    satin=material('PULSE_Rifle_Barrel_Satin',(.036,.044,.051),.32,.80);materials.append(satin)
    vertices=[];faces=[];uv=[];segments=16;radial=64
    for j in range(segments+1):
        t=j/segments;z=(.50+(1.055-.50)*t)*sz;r=.0098*(1-t)+.0085*t
        for i in range(radial+1):
            angle=i/radial*math.tau;vertices.append(carry@Vector((-.012*sx+r*math.cos(angle),r*math.sin(angle),z)));uv.append((i/radial,t))
            if j and i<radial:
                a=(j-1)*(radial+1)+i;faces.append((a,a+1,a+radial+2,a+radial+1))
    faces.extend([tuple(range(radial-1,-1,-1)),tuple(segments*(radial+1)+i for i in range(radial))])
    barrel=mesh('PULSE_Slim_Barrel_Exterior',vertices,faces,[satin],uv=uv);rigid(barrel,settings['rifle_carry_bone']);kept.append(barrel)
    for face in barrel.data.polygons[-2:]:face.use_smooth=False
    return kept,{'carry_bone':settings['rifle_carry_bone'],'profile_scale':sx,'thickness_scale':sy,'length_scale':sz,'carry_origin_m':settings['rifle_visual_carry_origin_m'],'carry_angle_degrees':settings['rifle_visual_carry_angle_degrees'],'removed_legacy_exterior_parts':removed,'barrel':'original capped round exterior mesh; no functional internals','round_sight_radial_scale':.75,'reference':'owner CGTrader athlete plus real competition photos; artistic fit, not manufacturing specifications'}
