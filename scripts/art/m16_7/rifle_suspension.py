"""Solid visible suspension between the fitted shoulder straps and carried prop."""
import bpy,math
from mathutils import Matrix,Vector

def add_suspension(mesh,material,materials,arm,settings):
    straps=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.endswith('_Rifle_Harness_Webbing')]
    carry=Matrix.Translation(settings['rifle_visual_carry_origin_m'])@Matrix.Rotation(math.radians(settings['rifle_visual_carry_angle_degrees']),4,'Y')
    scale=Matrix.Diagonal((settings['rifle_visual_profile_scale'],settings['rifle_visual_thickness_scale'],settings['rifle_visual_length_scale'],1))
    mat=material('PULSE_Rifle_Harness_Suspension',(.009,.012,.016),.86);materials.append(mat)
    width=settings['rifle_suspension_width_m'];thickness=settings['rifle_suspension_thickness_m'];rows=[]
    for upper in [True,False]:
        for sign in [-1,1]:
            query=Vector((sign*.13,.16,1.45 if upper else 1.19))
            strap,vertex=min(((o,v) for o in straps for v in o.data.vertices),key=lambda pair:((pair[0].matrix_world@pair[1].co)-query).length_squared)
            start=strap.matrix_world@vertex.co+Vector((0,.0008,0))
            source_weights={strap.vertex_groups[g.group].name:g.weight for g in vertex.groups if g.weight>1e-6}
            end=carry@scale@Vector(((.043 if upper else .110)+sign*.006,-.0225,.55 if upper else .29))
            verts=[];faces=[];uv=[];weights=[];segments=24
            for i in range(segments+1):
                t=i/segments;centre=start.lerp(end,t)+Vector((0,.003*math.sin(math.pi*t),0))
                w={bone:value*(1-t)for bone,value in source_weights.items()}
                bone=settings['rifle_carry_bone'];w[bone]=w.get(bone,0)+t
                for x,y in [(-1,-1),(1,-1),(1,1),(-1,1)]:
                    verts.append(centre+Vector((x*width*.5,y*thickness*.5,0)));uv.append(((x+1)*.5,t));weights.append(w.copy())
                if i:
                    a=(i-1)*4
                    for k in range(4):faces.append((a+k,a+(k+1)%4,a+4+(k+1)%4,a+4+k))
            faces.extend([(3,2,1,0),tuple(segments*4+k for k in range(4))])
            name=('Left'if sign>0 else'Right')+'_Rifle_Suspension_'+('Upper'if upper else'Lower')
            obj=mesh(name,verts,faces,[mat],uv=uv,weights=weights)
            for face in obj.data.polygons:face.use_smooth=False
            rows.append({'object':name,'shoulder_anchor':list(start),'stock_anchor':list(end),'shoulder_weights':source_weights,'stock_bone':bone,'width_m':width,'thickness_m':thickness})
    return rows
