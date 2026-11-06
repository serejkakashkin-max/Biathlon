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
        t = min(1, abs(point.x) / .112)
        smooth = t * t * (3 - 2 * t)
        return 1.437 + .055 * smooth - point.z

    def armhole_limit(point):
        t = max(0, min(1, (1.435 - point.z) / .105))
        width = GARMENT_SETTINGS['bib_armhole_top_half_width_m'] + (GARMENT_SETTINGS['bib_armhole_bottom_half_width_m']-GARMENT_SETTINGS['bib_armhole_top_half_width_m'])*t*t*(3-2*t)
        return width - abs(point.x)

    from garment_partition import partition_cloth
    vertices,faces,weights,normals=partition_cloth(deform_surface or surface,(lambda p:p.z-1.155,neck_limit,armhole_limit))
    # Identical base triangles and normalized weights; only sewn ease differs.
    # Independent nearest-weight fitting produced up to2.2mm cuts when posed.
    # Sew the layer to the exact shared boundary. Full ease at an armhole or
    # hem separates adjacent triangles, which can cross on a deep tuck.
    import heapq
    groups={};keys=[];points=[]
    for point in vertices:
        key=tuple(round(float(v),7)for v in point)
        if key not in groups:groups[key]=len(points);points.append(point)
        keys.append(groups[key])
    edges={};adj=[{}for _ in points]
    for face in faces:
        for a,b in zip(face,face[1:]+face[:1]):
            a,b=keys[a],keys[b]
            if a==b:continue
            edge=tuple(sorted((a,b)));edges[edge]=edges.get(edge,0)+1
            adj[a][b]=adj[b][a]=(points[a]-points[b]).length
    distance=[float('inf')]*len(points);queue=[]
    for edge,count in edges.items():
        if count==1:
            for vertex in edge:distance[vertex]=0;heapq.heappush(queue,(0,vertex))
    while queue:
        value,vertex=heapq.heappop(queue)
        if value>distance[vertex]:continue
        for other,length in adj[vertex].items():
            candidate=value+length
            if candidate<distance[other]:distance[other]=candidate;heapq.heappush(queue,(candidate,other))
    for index,(point,normal) in enumerate(zip(vertices,normals)):
        t=min(1,distance[keys[index]]/GARMENT_SETTINGS['bib_edge_ease_blend_m']);t=t*t*(3-2*t)
        vertices[index]=point+normal*(GARMENT_SETTINGS['bib_ease_m']*t)
    bib=mesh('Woven_Bib_27',vertices,faces,[white],weights=weights)
    bm=bmesh.new();bm.from_mesh(bib.data);bm.normal_update()
    support=BVHTree.FromObject(deform_surface or surface,bpy.context.evaluated_depsgraph_get())
    reverse=[face for face in bm.faces if face.normal.dot(support.find_nearest(face.calc_center_median())[1])<0]
    if reverse:bmesh.ops.reverse_faces(bm,faces=reverse)
    bm.to_mesh(bib.data);bm.free();bib.data.update()
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
    """One continuous, flat shoulder webbing route on the outer vest surface."""
    bpy.context.view_layer.update()
    target = BVHTree.FromObject(bib, bpy.context.evaluated_depsgraph_get())
    cloth = BVHTree.FromObject(surface, bpy.context.evaluated_depsgraph_get())
    def project(query):
        hit=cloth.find_nearest(query);normal=hit[1];support=hit[0]
        outer=target.ray_cast(support+normal*.025,-normal,.03)
        if outer[0] is not None and -.0001<(outer[0]-support).dot(normal)<.012 and outer[1].dot(normal)>.5:support=outer[0]
        point=support+normal*GARMENT_SETTINGS['harness_clearance_m']
        # A bib boundary can be nearer than the normal ray's support. Keep the
        # ribbon above that edge as well instead of burying it in the hem.
        for _ in range(4):
            nearby=min([target.find_nearest(point),cloth.find_nearest(point)],key=lambda h:h[3])
            signed=(point-nearby[0]).dot(nearby[1])
            if signed>=GARMENT_SETTINGS['harness_clearance_m']*.9:break
            point+=nearby[1]*(GARMENT_SETTINGS['harness_clearance_m']-signed)
        return point,normal
    def clearance(point):
        hit=min([target.find_nearest(point),cloth.find_nearest(point)],key=lambda h:h[3])
        return (point-hit[0]).dot(hit[1])
    for sign in (-1, 1):
        controls = [Vector((sign * x, y, z+.07)) for x, y, z in (
            (.130, -.095, 1.125), (.128, -.115, 1.24), (.126, -.115, 1.35),
            (.124, -.065, 1.415), (.126, .015, 1.445), (.130, .075, 1.415),
            (.135, .112, 1.32), (.139, .105, 1.20), (.122, .092, 1.12))]
        points = []
        for index in range(len(controls) - 1):
            a, b = controls[index], controls[index + 1]
            before = controls[max(0, index - 1)]
            after = controls[min(len(controls) - 1, index + 2)]
            for step in range(12):
                t = step / 12
                points.append(.5 * ((2 * a) + (-before + b) * t + (2 * before - 5 * a + 4 * b - after) * t * t + (-before + 3 * a - 3 * b + after) * t * t * t))
        points.append(controls[-1])
        vertices, faces, uv, weights = [], [], [], []
        edgepaths = [[], []];columns=GARMENT_SETTINGS['harness_cross_samples']
        for index, point in enumerate(points):
            for column in range(columns):
                fraction=column/(columns-1)
                query=point+Vector(((fraction-.5)*GARMENT_SETTINGS['harness_width_m'],0,0))
                # Route from the continuous suit, then find the outer bib along
                # its actual normal. Nearest bib edges can be centimetres away
                # and fold a strap through the chest/shoulder.
                p,normal=project(query);vertices.append(p)
                if column in [0,columns-1]:edgepaths[0 if column==0 else 1].append(p+normal*.00035)
                uv.append((fraction,index/(len(points)-1)));weights.append(weight_z(p.z))
            if index < len(points) - 1:
                for column in range(columns-1):
                    a=index*columns+column;faces.append((a,a+1,a+columns+1,a+columns))
        strap = mesh(('Left' if sign > 0 else 'Right') + '_Rifle_Harness_Webbing', vertices, faces, [black], uv=uv, weights=weights)
        # Points can all be outside while a long triangle cuts through a convex
        # shoulder. Refine only those chords, then project their new vertices.
        bm=bmesh.new();bm.from_mesh(strap.data);bmesh.ops.triangulate(bm,faces=list(bm.faces))
        for _ in range(6):
            marked={edge for edge in bm.edges if clearance((edge.verts[0].co+edge.verts[1].co)*.5)<.0004 and edge.calc_length()>.001}
            for face in bm.faces:
                if clearance(face.calc_center_median())<.0004:marked.update(face.edges)
            if not marked:break
            bmesh.ops.subdivide_edges(bm,edges=list(marked),cuts=1,use_grid_fill=True)
            for vertex in bm.verts:vertex.co=project(vertex.co)[0]
            bmesh.ops.triangulate(bm,faces=list(bm.faces))
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(strap.data);bm.free();strap.data.update()
        transfer_weights(strap, [bib,surface], arm)
        for points in edgepaths:
            binding = tube('Harness_Stitch', points, .00045, stitch)
            transfer_weights(binding, [bib,surface], arm)


def analytic_panel_material(material, red, black):
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
    mix = nodes.new('ShaderNodeMixRGB'); mix.blend_type = 'MIX'
    mix.inputs[1].default_value = red.diffuse_color
    mix.inputs[2].default_value = black.diffuse_color
    links.new(mask, mix.inputs[0])
    return mix.outputs[0]
