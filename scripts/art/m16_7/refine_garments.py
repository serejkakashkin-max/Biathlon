"""Sport-specific garment construction, evaluated on the actual anatomical surface.

Blender-only authoring. No images or paid geometry are used as a substitute mesh.
"""
import math,json
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

GARMENT_SETTINGS=json.loads((Path(__file__).resolve().parents[3]/'Assets/Biathlon/Characters/Biathlete_16_7/Documentation/garment-detail-v1.json').read_text())


def anatomical_grip(metadata, vertices, indices, side,tuck=False):
    """Flex the actual three phalanges, retaining the source anatomical skin weights.

    This is an authored static pole grip, not a claim of a runtime finger rig.
    The editable source and joint metadata retain all finger articulation data.
    """
    from equipment_refine import grip_finger_matrices
    matrices,_=grip_finger_matrices(metadata,side,tuck)
    prefix='Left' if side>0 else 'Right';suffix='.L' if side>0 else '.R'
    wrist=Vector(metadata['heads'][prefix+'Hand'])
    longitudinal=(Vector(metadata['finger_bones']['finger3-1'+suffix]['head'])-wrist).normalized()
    guard=metadata['hand_carpal_guard']
    def pose(name):return matrices.get(name,Matrix.Identity(4))
    posed = []
    for index in indices:
        weights = metadata['finger_weights'][index]
        total = sum(weights.values())
        source = Vector(metadata.get('glove_shell_vertex_overrides',{}).get(str(index),vertices[index]))
        result = sum((pose(name) @ source * value for name, value in weights.items()), Vector())
        deformed=result+source*max(0,1-total)
        factor=max(0,min(1,((source-wrist).dot(longitudinal)-guard['start_m'])/(guard['end_m']-guard['start_m'])))
        factor=factor*factor*(3-2*factor)
        posed.append(source.lerp(deformed,factor))
    return posed


def transfer_weights(obj, surface, arm):
    """Interpolate the actual adjacent triangle, including its skin weights.

    Nearest vertices can straddle an armhole or a neckline and pick unrelated
    bones. Barycentric surface interpolation keeps sewn layers on the same skin.
    """
    surfaces=surface if isinstance(surface,list) else [surface]
    points=[];triangles=[];records=[]
    for item in surfaces:
        start=len(points)
        points.extend(item.matrix_world@v.co for v in item.data.vertices)
        records.extend((item,v.index) for v in item.data.vertices)
        item.data.calc_loop_triangles()
        triangles.extend(tuple(start+i for i in triangle.vertices) for triangle in item.data.loop_triangles)
    tree=BVHTree.FromPolygons(points,triangles,all_triangles=True)
    world=obj.matrix_world.copy();obj.parent=None;obj.parent_type='OBJECT';obj.matrix_world=world
    obj.vertex_groups.clear();obj.modifiers.clear()
    groups={bone.name:obj.vertex_groups.new(name=bone.name) for bone in arm.data.bones}
    for vertex in obj.data.vertices:
        hit=tree.find_nearest(world@vertex.co)
        if hit[0] is None:raise RuntimeError('No sewn surface for '+obj.name)
        indices=triangles[hit[2]];a,b,c=(points[i] for i in indices)
        v0,v1,v2=b-a,c-a,hit[0]-a
        d00,d01,d11,d20,d21=v0.dot(v0),v0.dot(v1),v1.dot(v1),v2.dot(v0),v2.dot(v1)
        denominator=d00*d11-d01*d01
        if abs(denominator)>1e-22:
            wb=(d11*d20-d01*d21)/denominator;wc=(d00*d21-d01*d20)/denominator
            factors=[max(0,1-wb-wc),max(0,wb),max(0,wc)]
        else:
            factors=[1/max(.000001,(hit[0]-points[i]).length) for i in indices]
        total=sum(factors);weights={}
        for index,factor in zip(indices,factors):
            source,source_index=records[index]
            for group in source.data.vertices[source_index].groups:
                name=source.vertex_groups[group.group].name
                if name in groups:weights[name]=weights.get(name,0)+group.weight*factor/total
        best=sorted(weights.items(),key=lambda pair:pair[1],reverse=True)[:4]
        total=sum(value for _,value in best)
        if total<=0:raise RuntimeError('Unweighted sewn detail '+obj.name)
        for name,value in best:groups[name].add([vertex.index],value/total,'REPLACE')
    skin=obj.modifiers.new('Sewn detail follows adjacent anatomical triangle','ARMATURE');skin.object=arm
    obj.parent=arm;obj.matrix_world=world


def conform_bib(bib,surface,arm):
    """Use the runtime cloth as the actual outer-layer support and orientation."""
    tree=BVHTree.FromObject(surface,bpy.context.evaluated_depsgraph_get())
    bm=bmesh.new();bm.from_mesh(bib.data)
    for vertex in bm.verts:
        hit=tree.find_nearest(vertex.co);vertex.co=hit[0]+hit[1]*GARMENT_SETTINGS['bib_ease_m']
    bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.normal_update()
    reverse=[face for face in bm.faces if face.normal.dot(tree.find_nearest(face.calc_center_median())[1])<0]
    if reverse:bmesh.ops.reverse_faces(bm,faces=reverse)
    bm.to_mesh(bib.data);bm.free();bib.data.update();transfer_weights(bib,surface,arm)


def race_bib(surface, arm, mesh, tube, weight_z, white, deform_surface=None):
    """Cut one continuous vest from the smooth anatomical shell.

    The neckline and armholes intersect triangles analytically; no whole-face
    deletion, separate floating shoulder strips, or nearest-hit ray discontinuity.
    Exact shared intersections are welded before the bound edges are generated.
    """
    def neck_limit(point):
        t = min(1, abs(point.x) / GARMENT_SETTINGS['bib_neck_half_width_m'])
        smooth = t * t * (3 - 2 * t)
        # The neck is an opening, not a height cap across both shoulders.
        # Outside the opening the vest continues over the shoulder naturally.
        return max(abs(point.x)-GARMENT_SETTINGS['bib_neck_half_width_m'],GARMENT_SETTINGS['bib_neck_base_m']+.050*smooth-point.z)

    def armhole_limit(point):
        t = max(0, min(1, (1.435 - point.z) / .105))
        width = GARMENT_SETTINGS['bib_armhole_top_half_width_m'] + (GARMENT_SETTINGS['bib_armhole_bottom_half_width_m']-GARMENT_SETTINGS['bib_armhole_top_half_width_m'])*t*t*(3-2*t)
        lower=max(0,min(1,(1.30-point.z)/.095));lower=lower*lower*(3-2*lower)
        width=width*(1-lower)+GARMENT_SETTINGS['bib_lower_side_half_width_m']*lower
        return width - abs(point.x)

    from garment_partition import partition_cloth
    vertices,faces,weights,normals=partition_cloth(deform_surface or surface,(lambda p:p.z-1.155,neck_limit,armhole_limit))
    # Preserve exact corresponding skin weights while giving the vest its own fit.
    for index,(point,normal) in enumerate(zip(vertices,normals)):
        # A pullover stays outside the suit even at its openings. The old
        # zero-ease boundary made this read as a sewn-in colour patch.
        hem=max(0,1-(point.z-1.155)/.055)
        fold=(.5+.5*math.sin(point.x*76+point.y*21))*GARMENT_SETTINGS['bib_fold_depth_m']*hem
        vertices[index]=point+normal*(GARMENT_SETTINGS['bib_ease_m']+GARMENT_SETTINGS['bib_hem_flare_m']*hem+fold)
        vertices[index].z+=GARMENT_SETTINGS['bib_hem_lift_m']*max(0,min(1,(1.28-point.z)/.125))
    bib=mesh('Woven_Bib_27',vertices,faces,[white],weights=weights)
    bm=bmesh.new();bm.from_mesh(bib.data);bm.normal_update()
    support=BVHTree.FromObject(deform_surface or surface,bpy.context.evaluated_depsgraph_get())
    reverse=[face for face in bm.faces if face.normal.dot(support.find_nearest(face.calc_center_median())[1])<0]
    if reverse:bmesh.ops.reverse_faces(bm,faces=reverse)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
    bm.to_mesh(bib.data);bm.free();bib.data.update()
    bpy.context.view_layer.objects.active=bib
    subdivision=bib.modifiers.new('Soft continuous vest folds','SUBSURF');subdivision.levels=1;subdivision.render_levels=1
    bpy.ops.object.modifier_apply(modifier=subdivision.name)
    # Smooth the folded edge while retaining actual source-surface clearance.
    for vertex in bib.data.vertices:
        hit=support.find_nearest(vertex.co);signed=(vertex.co-hit[0]).dot(hit[1])
        if signed<GARMENT_SETTINGS['bib_ease_m']:vertex.co+=hit[1]*(GARMENT_SETTINGS['bib_ease_m']-signed)
    bib.data.update()
    # Uniformly follow each true boundary loop, including shoulders and armholes.
    bm = bmesh.new(); bm.from_mesh(bib.data)
    edges = {edge for edge in bm.edges if edge.is_boundary}
    while edges:
        first = next(iter(edges)); edges.remove(first)
        start, current = first.verts
        points = [start.co.copy(), current.co.copy()]
        for _ in range(len(bm.edges)):
            choices = [edge for edge in current.link_edges if edge in edges]
            if not choices:
                break
            edge = choices[0]; edges.remove(edge)
            current = edge.other_vert(current); points.append(current.co.copy())
            if current == start:
                break
        if len(points) > 3:
            binding = tube('Bib_Stitched_Binding', points, GARMENT_SETTINGS['bib_binding_radius_m'], white)
            transfer_weights(binding, bib, arm)
    bm.free()
    bpy.context.view_layer.objects.active=bib
    thickness=bib.modifiers.new('Separate race bib fabric thickness','SOLIDIFY');thickness.thickness=GARMENT_SETTINGS['bib_thickness_m'];thickness.offset=-1;thickness.use_even_offset=False;thickness.use_quality_normals=True
    bpy.ops.object.modifier_apply(modifier=thickness.name)
    return bib, []


def projected_print(obj, target, offset=.0012, back=False):
    """Project ink along the viewing normal, preserving each glyph's X/Z shape."""
    bpy.context.view_layer.update()
    tree = BVHTree.FromObject(target, bpy.context.evaluated_depsgraph_get())
    world, inverse = obj.matrix_world.copy(), obj.matrix_world.inverted()
    direction = Vector((0, -1 if back else 1, 0))
    for vertex in obj.data.vertices:
        point = world @ vertex.co
        origin = Vector((point.x, .6 if back else -.6, point.z))
        hit = tree.ray_cast(origin, direction, 1.2)
        if hit[0] is None:
            raise RuntimeError('Printed marking extends outside its fabric: ' + obj.name)
        vertex.co = inverse @ (hit[0] - direction * offset)


def continuous_collar(surface, mesh, tube, red, weight_z, skin_surface=None):
    """Finish the suit's own anatomical neckline with a narrow sewn rim.

    A separate loft between unrelated rings peeled off the chest under neck
    flexion. The compression fabric now retains the actual cervical topology
    and weights all the way to its opening; there is no second collar shell.
    """
    bm=bmesh.new();bm.from_mesh(surface.data)
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,
        plane_co=(0,0,1.538),plane_no=(0,0,1),clear_outer=True)
    boundary=[v for v in bm.verts if v.is_boundary and v.co.z>1.537]
    boundary.sort(key=lambda v:math.atan2(v.co.y-.066,v.co.x))
    if len(boundary)<24:raise RuntimeError('Incomplete sewn neckline')
    points=[v.co.copy() for v in boundary]
    bm.to_mesh(surface.data);bm.free();surface.data.update()
    # The incoming suit has no armature modifier yet. Use the provided mesh
    # helper's rig binding, then copy weights from the exact sewn boundary.
    rim=tube('Collar_Folded_Edge',points+[points[0]],.0008,red)
    transfer_weights(rim,surface,rim.parent)
    return rim


def fitted_harness(surface, bib, arm, mesh, tube, black, stitch, weight_z):
    """Closed load-bearing shoulder loops, including the underarm return."""
    bpy.context.view_layer.update()
    target=BVHTree.FromObject(bib,bpy.context.evaluated_depsgraph_get());cloth=BVHTree.FromObject(surface,bpy.context.evaluated_depsgraph_get())
    def project(query):
        hit=cloth.find_nearest(query);normal=hit[1];support=hit[0]
        outer=target.ray_cast(support+normal*.035,-normal,.045)
        if outer[0]is not None and -.0001<(outer[0]-support).dot(normal)<.025 and outer[1].dot(normal)>.5:support=outer[0]
        point=support+normal*GARMENT_SETTINGS['harness_clearance_m']
        for _ in range(6):
            nearby=min([target.find_nearest(point),cloth.find_nearest(point)],key=lambda h:h[3]);signed=(point-nearby[0]).dot(nearby[1])
            if signed>=GARMENT_SETTINGS['harness_clearance_m']*.9:break
            point+=nearby[1]*(GARMENT_SETTINGS['harness_clearance_m']-signed)
        return point,normal
    def clearance(point):
        hit=min([target.find_nearest(point),cloth.find_nearest(point)],key=lambda h:h[3]);return (point-hit[0]).dot(hit[1])
    for sign in (-1,1):
        controls=[Vector((sign*x,y,z))for x,y,z in GARMENT_SETTINGS['harness_route_controls_m']]
        points=[];count=len(controls)
        for i in range(count):
            before,a,b,after=controls[(i-1)%count],controls[i],controls[(i+1)%count],controls[(i+2)%count]
            for step in range(16):
                t=step/16;points.append(.5*((2*a)+(-before+b)*t+(2*before-5*a+4*b-after)*t*t+(-before+3*a-3*b+after)*t*t*t))
        vertices=[];faces=[];uv=[];weights=[];columns=GARMENT_SETTINGS['harness_cross_samples'];rows=len(points)
        for i,point in enumerate(points):
            _,normal=project(point);tangent=(points[(i+1)%rows]-points[(i-1)%rows]).normalized();across=tangent.cross(normal).normalized()
            for column in range(columns):
                fraction=column/(columns-1);offset=(fraction-.5)*GARMENT_SETTINGS['harness_width_m'];centre,normal=project(point)
                p=centre+across*offset+normal*(.0018+offset*offset/.24);vertices.append(p);uv.append((fraction,i/rows));weights.append(weight_z(p.z))
            for column in range(columns-1):
                a=i*columns+column;b=((i+1)%rows)*columns+column;faces.append((a,a+1,b+1,b))
        strap=mesh(('Left'if sign>0 else'Right')+'_Rifle_Harness_Webbing',vertices,faces,[black],uv=uv,weights=weights)
        bm=bmesh.new();bm.from_mesh(strap.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(strap.data);bm.free();strap.data.update();transfer_weights(strap,[bib,surface],arm)
        bpy.context.view_layer.objects.active=strap;mod=strap.modifiers.new('Solid returning webbing','SOLIDIFY');mod.thickness=GARMENT_SETTINGS['harness_thickness_m'];mod.offset=1;mod.use_even_offset=False;mod.use_quality_normals=True;bpy.ops.object.modifier_apply(modifier=mod.name)
        strap['route']='closed shoulder/front/underarm/back load-bearing loop'


def fit_harness_adjuster(obj,arm):
    """Seat adjustment hardware on the actual webbing, not a guessed torso depth."""
    straps=[o for o in bpy.context.scene.objects if o.type=='MESH'and o.name.endswith('_Rifle_Harness_Webbing')]
    points=[];faces=[]
    for strap in straps:
        start=len(points);points.extend(strap.matrix_world@v.co for v in strap.data.vertices);faces.extend(tuple(i+start for i in f.vertices)for f in strap.data.polygons)
    tree=BVHTree.FromPolygons(points,faces)
    world=obj.matrix_world.copy();centre=sum((world@v.co for v in obj.data.vertices),Vector())/len(obj.data.vertices);sign=1 if centre.x>0 else-1
    hit=tree.find_nearest(Vector((sign*.133,-.115,1.275)));delta=hit[0]+hit[1]*.0025-centre
    for vertex in obj.data.vertices:vertex.co=world.inverted()@(world@vertex.co+delta)
    obj.data.update();transfer_weights(obj,straps,arm)

def analytic_panel_material(material, red, black, kit_masks=False):
    """Bake panel boundaries at texel resolution, not coarse triangle vertex colors."""
    nodes, links = material.node_tree.nodes, material.node_tree.links
    geometry = nodes.new('ShaderNodeNewGeometry')
    xyz = nodes.new('ShaderNodeSeparateXYZ')
    links.new(geometry.outputs['Position'], xyz.inputs[0])
    def op(operation, a, b=None):
        node = nodes.new('ShaderNodeMath'); node.operation = operation
        if isinstance(a, (float, int)): node.inputs[0].default_value = a
        else: links.new(a, node.inputs[0])
        if b is not None:
            if isinstance(b, (float, int)): node.inputs[1].default_value = b
            else: links.new(b, node.inputs[1])
        return node.outputs[0]
    x, y, z = xyz.outputs['X'], xyz.outputs['Y'], xyz.outputs['Z']
    ax = op('ABSOLUTE', x)
    # Curved side panels follow the waist and upper thigh. They are design
    # seams, not a hard vertex classification inherited from casual clothes.
    waist = op('MULTIPLY', op('MULTIPLY', op('SUBTRACT', z, 1.07), op('SUBTRACT', z, 1.07)), .42)
    torso = op('SUBTRACT', ax, op('ADD', waist, .126))
    legcenter = op('ADD', .105, op('MULTIPLY', op('SUBTRACT', .89, z), .075))
    leg = op('SUBTRACT', ax, op('ADD',legcenter,.051))
    def smooth_clamp(value):
        t=op('MINIMUM',op('MAXIMUM',value,0),1)
        return op('MULTIPLY',op('MULTIPLY',t,t),op('SUBTRACT',3,op('MULTIPLY',2,t)))
    waist_blend=smooth_clamp(op('MULTIPLY',op('SUBTRACT',z,.83),5))
    outer=op('ADD',op('MULTIPLY',op('SUBTRACT',1,waist_blend),leg),op('MULTIPLY',waist_blend,torso))
    inner_width=op('MULTIPLY',op('SUBTRACT',legcenter,.051),smooth_clamp(op('MULTIPLY',op('SUBTRACT',.85,z),1/.12)))
    inner=op('SUBTRACT',inner_width,ax)
    center=op('GREATER_THAN',op('MAXIMUM',outer,inner),0)
    arm = op('MULTIPLY',op('GREATER_THAN', ax, .23),op('GREATER_THAN',z,1.0))
    sleeve = op('GREATER_THAN', y, .048)
    mask = op('ADD', op('MULTIPLY', arm, sleeve), op('MULTIPLY', op('SUBTRACT', 1, arm), center))
    # Keep the collar and central upper torso red.
    mask = op('MULTIPLY', mask, op('LESS_THAN', z, 1.445))
    if kit_masks:
        trim=op('MAXIMUM',op('MULTIPLY',op('LESS_THAN',op('ABSOLUTE',outer),.008),op('SUBTRACT',1,arm)),op('MULTIPLY',arm,op('LESS_THAN',op('ABSOLUTE',op('SUBTRACT',y,.048)),.008)))
        trim=op('MULTIPLY',trim,op('LESS_THAN',z,1.445))
        accent=op('MULTIPLY',op('MULTIPLY',arm,op('GREATER_THAN',ax,.48)),op('SUBTRACT',1,trim))
        main=op('MULTIPLY',op('MULTIPLY',op('SUBTRACT',1,mask),op('SUBTRACT',1,trim)),op('SUBTRACT',1,accent))
        channels=nodes.new('ShaderNodeCombineXYZ')
        for source,socket in zip([main,trim,accent],channels.inputs):links.new(source,socket)
        return channels.outputs[0]
    mix = nodes.new('ShaderNodeMixRGB'); mix.blend_type = 'MIX'
    mix.inputs[1].default_value = red.diffuse_color
    mix.inputs[2].default_value = black.diffuse_color
    links.new(mask, mix.inputs[0])
    return mix.outputs[0]
