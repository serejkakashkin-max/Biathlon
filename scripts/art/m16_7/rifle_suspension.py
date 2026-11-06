"""Solid visible suspension between the fitted shoulder straps and carried prop."""
import bpy,math
from mathutils import Matrix,Vector
from mathutils.bvhtree import BVHTree

def surface_tree(objects):
    vertices=[];faces=[]
    for obj in objects:
        start=len(vertices);vertices.extend(obj.matrix_world@v.co for v in obj.data.vertices);faces.extend(tuple(i+start for i in f.vertices)for f in obj.data.polygons)
    return BVHTree.FromPolygons(vertices,faces)

def add_suspension(mesh,material,materials,arm,settings):
    straps=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.endswith('_Rifle_Harness_Webbing')]
    carry=Matrix.Translation(settings['rifle_visual_carry_origin_m'])@Matrix.Rotation(math.radians(settings['rifle_visual_carry_angle_degrees']),4,'Y')
    scale=Matrix.Diagonal((settings['rifle_visual_profile_scale'],settings['rifle_visual_thickness_scale'],settings['rifle_visual_length_scale'],1))
    mat=material('PULSE_Rifle_Harness_Suspension',(.009,.012,.016),.86);materials.append(mat)
    nominal_width=settings['rifle_suspension_width_m'];thickness=settings['rifle_suspension_thickness_m'];rows=[]
    garment=surface_tree([bpy.data.objects['Woven_Bib_27'],bpy.data.objects['RaceSuit_Continuous_Tailored_Shell']])
    for upper in [True,False]:
        for sign in [-1,1]:
            query=Vector((sign*.13,.16,settings['rifle_suspension_upper_shoulder_height_m']if upper else settings['rifle_suspension_lower_shoulder_height_m']))
            # Start at a true transverse row centre, rather than a nearest edge
            # vertex whose midpoint touched while half the ribbon hung in air.
            candidates=[]
            for o in straps:
                columns=int(o['route_columns']);count=int(o['route_rows'])
                for row in [0 if upper else count-1]:
                    ids=list(range(row*columns,(row+1)*columns));centre=sum((o.matrix_world@o.data.vertices[i].co for i in ids),Vector())/columns
                    if centre.y>.07:candidates.append((o,ids,centre))
            strap,ids,start=min(candidates,key=lambda item:(item[2]-query).length_squared)
            available=(strap.matrix_world@strap.data.vertices[ids[-1]].co-strap.matrix_world@strap.data.vertices[ids[0]].co).length
            width=min(nominal_width,available*.70)
            vertex=strap.data.vertices[ids[len(ids)//2]]
            source_weights={strap.vertex_groups[g.group].name:g.weight for g in vertex.groups if g.weight>1e-6}
            anchor=Vector(settings['rifle_suspension_upper_stock_anchor_m']if upper else settings['rifle_suspension_lower_stock_anchor_m'])
            anchor.x+=sign*settings['rifle_suspension_anchor_half_spacing_m'];end=carry@scale@anchor
            strap_tree=surface_tree([strap]);wood_tree=surface_tree([bpy.data.objects['PULSE_Biathlon_Walnut_Stock']])
            # Locate the mounting face on the real stock, with room for the
            # entire width. The exterior prop can change independently.
            end=wood_tree.find_nearest(end)[0]
            verts=[];faces=[];uv=[];weights=[];segments=24
            adjacent_row=1 if upper else int(strap['route_rows'])-2;columns=int(strap['route_columns'])
            adjacent=sum((strap.matrix_world@strap.data.vertices[adjacent_row*columns+j].co for j in range(columns)),Vector())/columns
            departure=(start-adjacent).normalized();control1=start+departure*settings['rifle_suspension_junction_blend_length_m'];control2=end-(end-start).normalized()*settings['rifle_suspension_junction_blend_length_m']
            # Sample the broad fabric face just inside the endpoint. At the
            # capped tip nearest-normal returned the thin end wall instead.
            start_normal=strap_tree.find_nearest(adjacent)[1];end_normal=wood_tree.find_nearest(end)[1]
            if start_normal.y<0:start_normal=-start_normal
            if end_normal.y<0:end_normal=-end_normal
            def section(t,arch):
                u=1-t;centre=start*u*u*u+control1*3*u*u*t+control2*3*u*t*t+end*t*t*t+Vector((0,arch*math.sin(math.pi*t),0))
                tangent=((control1-start)*3*u*u+(control2-control1)*6*u*t+(end-control2)*3*t*t+Vector((0,arch*math.pi*math.cos(math.pi*t),0))).normalized()
                normal=start_normal.lerp(end_normal,t).normalized();across=tangent.cross(normal).normalized();normal=across.cross(tangent).normalized()
                result=[]
                for x,y in [(-1,-1),(1,-1),(1,1),(-1,1)]:
                    point=centre+across*(x*width*.5)+normal*(y*thickness*.5)
                    # The complete end cross-section is seated on each real
                    # surface. Blend the seating correction over nearby rows.
                    for endpoint,tree,influence in [(start,strap_tree,(1-t)**8),(end,wood_tree,t**8)]:
                        probe=endpoint+across*(x*width*.5);hit=tree.find_nearest(probe)
                        point+=(hit[0]-probe)*influence
                    result.append(point)
                return result
            # One smooth outward bow spans the complete garment profile. A
            # straight bridge hid its middle under the raised bib while both
            # endpoint-centre checks misleadingly passed.
            arch=.003
            for _ in range(6):
                needed=0
                for i in range(1,segments):
                    t=i/segments
                    for point in section(t,arch):
                        hit=garment.ray_cast(Vector((point.x,.65,point.z)),Vector((0,-1,0)),1.0)
                        if hit[0]is not None:needed=max(needed,(hit[0].y+settings['rifle_suspension_garment_clearance_m']-point.y)/math.sin(math.pi*t))
                if needed<=.00001:break
                arch+=needed+.0003
            for i in range(segments+1):
                t=i/segments;w={bone:value*(1-t)for bone,value in source_weights.items()};bone=settings['rifle_carry_bone'];w[bone]=w.get(bone,0)+t
                for k,point in enumerate(section(t,arch)):
                    verts.append(point);uv.append((1 if k in [1,2]else 0,t));weights.append(w.copy())
                if i:
                    a=(i-1)*4
                    for k in range(4):faces.append((a+k,a+(k+1)%4,a+4+(k+1)%4,a+4+k))
            faces.extend([(3,2,1,0),tuple(segments*4+k for k in range(4))])
            name=('Left'if sign>0 else'Right')+'_Rifle_Suspension_'+('Upper'if upper else'Lower')
            obj=mesh(name,verts,faces,[mat],uv=uv,weights=weights)
            obj.data.uv_layers.active.name=bpy.data.objects['RaceSuit_Continuous_Tailored_Shell'].data.uv_layers.active.name
            # Follow the actual local torso deformation along the route. A
            # linear interpolation of the two endpoint bones folded the middle
            # through the bib in Tuck even though the rest mesh was clear.
            from refine_garments import transfer_weights
            transfer_weights(obj,[bpy.data.objects['Woven_Bib_27'],bpy.data.objects['RaceSuit_Continuous_Tailored_Shell'],strap],arm)
            for vertex in obj.data.vertices:
                t=(vertex.index//4)/segments;blend=max(0,min(1,(t-.65)/.35));blend=blend*blend*(3-2*blend)
                local={obj.vertex_groups[g.group].name:g.weight*(1-blend)for g in vertex.groups};local[bone]=local.get(bone,0)+blend
                for group in obj.vertex_groups:group.remove([vertex.index])
                best=sorted(local.items(),key=lambda item:item[1],reverse=True)[:4];total=sum(value for _,value in best)
                for key,value in best:
                    if value>0:obj.vertex_groups[key].add([vertex.index],value/total,'REPLACE')
            for face in obj.data.polygons:face.use_smooth=False
            rows.append({'object':name,'shoulder_anchor':list(start),'stock_anchor':list(end),'shoulder_weights':source_weights,'stock_bone':bone,'width_m':width,'thickness_m':thickness,'garment_span_arch_m':arch})
    return rows
