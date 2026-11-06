"""Sport-specific garment construction, evaluated on the actual anatomical surface.

Blender-only authoring. No images or paid geometry are used as a substitute mesh.
"""
import math
import bpy
import bmesh
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree


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
        width = .156 + .050 * t * t * (3 - 2 * t)
        return width - abs(point.x)

    def clip(points, field):
        result = []
        for first, last in zip(points, points[1:] + points[:1]):
            fa, fb = field(first), field(last)
            if fa >= 0:
                result.append(first)
            if (fa >= 0) != (fb >= 0):
                lo, hi = 0.0, 1.0
                for _ in range(16):
                    middle = (lo + hi) * .5
                    if (field(first.lerp(last, middle)) >= 0) == (fa >= 0):
                        lo = middle
                    else:
                        hi = middle
                result.append(first.lerp(last, (lo + hi) * .5))
        return result

    vertices, faces = [], []
    surface.data.calc_loop_triangles()
    for triangle in surface.data.loop_triangles:
        points = [surface.data.vertices[i].co.copy() for i in triangle.vertices]
        if max(p.z for p in points) < 1.155 or min(p.z for p in points) > 1.565:
            continue
        for field in (lambda p: p.z - 1.155, neck_limit, armhole_limit):
            points = clip(points, field)
            if not points:
                break
        if len(points) >= 3:
            faces.append(tuple(range(len(vertices), len(vertices) + len(points))))
            vertices.extend(points)
    bib = mesh('Woven_Bib_27', vertices, faces, [white])
    bm = bmesh.new(); bm.from_mesh(bib.data)
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=.000012)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.normal_update()
    for vertex in bm.verts:
        vertex.co += vertex.normal * .0048
    bm.to_mesh(bib.data); bm.free(); bib.data.update()
    transfer_weights(bib, deform_surface or surface, arm)
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
            binding = tube('Bib_Stitched_Binding', points, .00075, white)
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
        edgepaths = [[], []]
        for index, point in enumerate(points):
            for edge in range(2):
                query = point + Vector(((-1 if edge == 0 else 1) * .012, 0, 0))
                hit = target.find_nearest(query)
                if hit[0] is None or hit[3] > .07:
                    hit = cloth.find_nearest(query)
                # The ribbon lives on top of the bib, with no duplicated fit of
                # stitches back onto the inner racing suit.
                p = hit[0] + hit[1] * .0035
                vertices.append(p); edgepaths[edge].append(p + hit[1] * .00035)
                uv.append((edge, index / (len(points) - 1))); weights.append(weight_z(p.z))
            if index < len(points) - 1:
                a = index * 2; faces.append((a, a + 1, a + 3, a + 2))
        strap = mesh(('Left' if sign > 0 else 'Right') + '_Rifle_Harness_Webbing', vertices, faces, [black], uv=uv, weights=weights)
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
    torso = op('GREATER_THAN', ax, op('ADD', waist, .126))
    legcenter = op('ADD', .105, op('MULTIPLY', op('SUBTRACT', .89, z), .075))
    leg = op('GREATER_THAN', op('ABSOLUTE', op('SUBTRACT', ax, legcenter)), .051)
    lower = op('LESS_THAN', z, .89)
    center = op('ADD', op('MULTIPLY', lower, leg), op('MULTIPLY', op('SUBTRACT', 1, lower), torso))
    arm = op('GREATER_THAN', ax, .23)
    sleeve = op('GREATER_THAN', y, .048)
    mask = op('ADD', op('MULTIPLY', arm, sleeve), op('MULTIPLY', op('SUBTRACT', 1, arm), center))
    # Keep the collar and central upper torso red.
    mask = op('MULTIPLY', mask, op('LESS_THAN', z, 1.445))
    mix = nodes.new('ShaderNodeMixRGB'); mix.blend_type = 'MIX'
    mix.inputs[1].default_value = red.diffuse_color
    mix.inputs[2].default_value = black.diffuse_color
    links.new(mask, mix.inputs[0])
    return mix.outputs[0]
