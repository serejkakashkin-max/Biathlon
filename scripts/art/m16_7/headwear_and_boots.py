"""Fitted cap hem and PULSE Nordic racing boots on the natural athlete."""
import math

import bpy
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree


def construction_receipt():
    return {
        "schema_version": 1,
        "id": "m167_fitted_cap_and_nordic_racing_boots",
        "geometry": "Project-authored fitted cap hem, closed outsoles, carbon chassis and cuff, textile gaiter and power strap",
        "carbon_texture": "Boot_PULSE_Carbon_BaseColor_1K.png",
        "carbon_texture_source": "Project-authored procedural weave",
        "references": [
            "https://www.salomon.com/en-us/product/s-lab-equipe-skate-boa-li1215",
            "https://www.fischersports.com/us_en/speedmax-9-skate-rl-boa/9002973202871",
        ],
    }


def finish_cap_hem(cap, head):
    # Keep the crown and its mask contact. Replace the bottom shrinkwrapped
    # rings, which can jump from the scalp to an ear or eyebrow.
    count = 96
    stride = count + 1
    crown_ring = 9
    graph = bpy.context.evaluated_depsgraph_get()
    scalp = BVHTree.FromObject(head, graph)
    cap_to_head = head.matrix_world.inverted() @ cap.matrix_world
    head_to_cap = cap_to_head.inverted()
    rows = (.0, .045, .10, .19, .31, .46, .61, .75, .88)
    targets = [cap_to_head @ v.co.copy() for v in cap.data.vertices]
    for ring, progress in enumerate(rows):
        for i in range(stride):
            angle = (i % count) * math.tau / count
            rear = math.sin(angle)
            crown = targets[crown_ring * stride + i]
            height = 1.697 - .027 * max(0, rear) ** 2 + .006 * max(0, -rear) ** 2
            z = height + (crown.z - height) * progress
            centre = Vector((0, .044, z))
            # Sample a regular angular grid. Nearest-point shrinkwrap can fold
            # neighbouring columns onto opposite sides of the ear.
            radial = Vector((math.cos(angle), math.sin(angle), 0))
            hit, _, _, _ = scalp.ray_cast(centre + .2 * radial, -radial, .2)
            if hit is None:
                raise ValueError(f"Cap hem misses the scalp at ring {ring}, sample {i}")
            lip = .00065 if ring == 1 else 0
            scalp_radius = (hit - centre).dot(radial)
            crown_radius = (crown - centre).dot(radial)
            radius = max(scalp_radius + .0028, scalp_radius + (crown_radius - scalp_radius) * progress) + lip
            cap.data.vertices[ring * stride + i].co = head_to_cap @ (centre + radius * radial)
    for face in cap.data.polygons:
        face.material_index = 0
    while len(cap.data.materials) > 1:
        cap.data.materials.pop(index=1)
    cap.data.update()


def refit_mask_webbing(cap, strap, gap, thickness):
    """Reseat the saved rear strap, preserving the donor frame and UVs."""
    bpy.context.view_layer.update()
    evaluated = cap.evaluated_get(bpy.context.evaluated_depsgraph_get())
    data = evaluated.to_mesh()
    surface = BVHTree.FromPolygons([cap.matrix_world @ v.co for v in data.vertices],
                                   [tuple(face.vertices) for face in data.polygons])
    evaluated.to_mesh_clear()
    to_local = strap.matrix_world.inverted()
    for vertex in strap.data.vertices:
        point = strap.matrix_world @ vertex.co
        centre = Vector((0, .032, point.z))
        radial = Vector((point.x, point.y - centre.y, 0)).normalized()
        if point.y >= centre.y:
            hit, _, _, _ = surface.ray_cast(centre, radial, .2)
            if hit is None:
                raise ValueError("Rear mask webbing misses the fitted cap hem")
            depth = thickness if vertex.index % 4 in (2, 3) else 0
            vertex.co = to_local @ (hit + radial * (gap + depth))
        elif point.y > -.02:
            # Blend the short connector back to the preserved temple clips.
            sign = 1 if point.x > 0 else -1
            centre.z = max(1.700, point.z)
            hit, _, _, _ = surface.ray_cast(centre, Vector((sign, 0, 0)), .2)
            depth = thickness if vertex.index % 4 in (2, 3) else 0
            if hit is not None:
                t = (point.y + .02) / .052
                point.x += (hit.x + sign * (gap + depth) - point.x) * t * t * (3 - 2 * t)
                vertex.co = to_local @ point
    strap.data.update()


def boot_materials(textures):
    # Inputs are scene-linear. Fabric maps are reused; carbon weave is original.
    v, u = np.mgrid[0:1024, 0:1024].astype(np.float32)
    weave = np.sin((u + v) * math.tau / 40) * np.sin((u - v) * math.tau / 40)
    tone = .28 + .065 * weave + .006 * np.sin(u * math.tau / 3)
    pixels = np.ones((1024, 1024, 4), dtype=np.float32)
    pixels[:, :, :3] = np.stack((tone * .86, tone * .94, tone), axis=-1)
    image_name = "Boot_PULSE_Carbon_BaseColor_1K"
    existing = bpy.data.images.get(image_name)
    if existing:
        bpy.data.images.remove(existing, do_unlink=True)
    carbon_map = bpy.data.images.new(image_name, width=1024, height=1024, alpha=True)
    carbon_map.pixels.foreach_set(pixels.ravel())
    carbon_map.filepath_raw = str(textures / "Boot_PULSE_Carbon_BaseColor_1K.png")
    carbon_map.file_format = "PNG"
    carbon_map.save()

    def image(name):
        filename = name + ".png"
        return (bpy.data.images.get(filename) or bpy.data.images.get(name)
                or bpy.data.images.load(str(textures / filename), check_existing=True))

    definitions = (
        ("PULSE_Boot_Upper", (.022, .031, .042), .72, 0, "Textile_BaseColor_4K", .23),
        ("PULSE_Boot_Carbon", (1, 1, 1), .45, .05, "Boot_PULSE_Carbon_BaseColor_1K", 0),
        ("PULSE_Boot_Outsole", (.009, .012, .015), .82, 0, "Textile_BaseColor_4K", .10),
    )
    result = []
    for name, color, roughness, metallic, base_map, bump in definitions:
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = (*color, 1)
        mat.use_nodes = True
        nodes, links = mat.node_tree.nodes, mat.node_tree.links
        shader = nodes.get("Principled BSDF")
        shader.inputs["Metallic"].default_value = metallic
        shader.inputs["Roughness"].default_value = roughness
        texture = nodes.new("ShaderNodeTexImage")
        texture.image = image(base_map)
        tint = nodes.new("ShaderNodeMixRGB")
        tint.blend_type = "MULTIPLY"
        tint.inputs[0].default_value = 1
        tint.inputs[2].default_value = (*color, 1)
        links.new(texture.outputs["Color"], tint.inputs[1])
        links.new(tint.outputs["Color"], shader.inputs["Base Color"])
        if bump:
            texture = nodes.new("ShaderNodeTexImage")
            texture.image = image("Textile_Normal_4K")
            texture.image.colorspace_settings.name = "Non-Color"
            normal = nodes.new("ShaderNodeNormalMap")
            normal.inputs["Strength"].default_value = bump
            links.new(texture.outputs["Color"], normal.inputs["Color"])
            links.new(normal.outputs["Normal"], shader.inputs["Normal"])
        result.append(mat)
    return result


def boot_shell(name, arm, materials, rings, weights, face_slots=None, wrap=True, close_sole=False):
    """Join sampled cross sections with welded edges and a loop UV seam."""
    width = len(rings[0])
    sections = width if wrap else width - 1
    vertices = [v for ring in rings for v in ring]
    faces = []
    for row in range(len(rings) - 1):
        for i in range(sections):
            a, b = row * width, (row + 1) * width
            j = (i + 1) % width
            faces.append((a + i, a + j, b + j, b + i))
    side_faces = len(faces)
    if close_sole:
        faces.extend((tuple(reversed(range(width))), tuple(range(len(vertices) - width, len(vertices)))))
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    for mat in materials:
        data.materials.append(mat)
    uv = data.uv_layers.new(name="TailoredUV")
    for face in data.polygons:
        if face.index >= side_faces:
            for li in face.loop_indices:
                px, py, _ = vertices[data.loops[li].vertex_index]
                uv.data[li].uv = ((px - vertices[0][0]) / .1 + 1, (py + .18) / .31)
            continue
        row, i = divmod(face.index, sections)
        if face_slots:
            face.material_index = face_slots[row]
        face.use_smooth = True
        for li, (column, level) in zip(face.loop_indices, ((i, row), (i + 1, row), (i + 1, row + 1), (i, row + 1))):
            uv.data[li].uv = (column / sections, level / (len(rings) - 1))
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.parent = arm
    obj.matrix_world = Matrix.Identity(4)
    groups = {}
    for i, point in enumerate(vertices):
        for bone, value in weights(point).items():
            if value <= 0:
                continue
            if bone not in groups:
                groups[bone] = obj.vertex_groups.new(name=bone)
            groups[bone].add([i], value, "REPLACE")
    if not wrap:
        # Carbon and strap pieces have a closed, millimetre-scale edge.
        # Apply thickness before skinning so their edge follows the same bones.
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        solid = obj.modifiers.new("Moulded shell edge", "SOLIDIFY")
        solid.thickness = .001
        solid.offset = -.5
        bpy.ops.object.modifier_apply(modifier=solid.name)
    skin = obj.modifiers.new("Weighted boot construction", "ARMATURE")
    skin.object = arm
    return obj


def build_boots(arm, heads, textures):
    upper, carbon, rubber = boot_materials(textures)
    graphite = bpy.data.materials["PULSE_Stretch_Graphite"]
    accent = bpy.data.materials["PULSE_Scarlet_Accent"]
    objects = []
    for side, sign in (("Left", 1), ("Right", -1)):
        x = heads[side + "Foot"].x
        foot_z = heads[side + "Foot"].z
        shin = heads[side + "Leg"]

        def centre_x(z):
            t = max(0, (z - foot_z) / (shin.z - foot_z))
            return x + (shin.x - x) * t

        def foot(_):
            return {side + "Foot": 1}

        def leg(_):
            return {side + "Leg": 1}

        def flex(point):
            t = max(0, min(1, (point[2] - .14) / .075))
            t = t * t * (3 - 2 * t)
            return {side + "Foot": 1 - t, side + "Leg": t}

        def profile(rows):
            rings = []
            for y, z, width, length in rows:
                ring = []
                for i in range(64):
                    angle = i * math.tau / 64
                    longitudinal = math.sin(angle)
                    # Broad metatarsal area, tapered heel and slight medial shape.
                    taper = 1 - .23 * max(0, longitudinal) ** 2
                    medial = -.003 * sign * max(0, -longitudinal)
                    ring.append((centre_x(z) + medial + width * taper * math.cos(angle),
                                 y + length * longitudinal, z))
                rings.append(ring)
            return rings

        def add(label, mats, rings, weights, slots=None, wrap=True, close_sole=False):
            objects.append(boot_shell(side + "_" + label, arm, mats, rings, weights, slots, wrap, close_sole))

        # Preserve the toe bar and the original minimum sole height. The
        # chassis narrows into the heel cup and the outsole turns up at the toe.
        sole = profile((
            (-.024, .044, .043, .141), (-.024, .045, .046, .144),
            (-.024, .053, .048, .147), (-.024, .060, .048, .147),
            (-.024, .066, .047, .145), (-.024, .071, .044, .142),
        ))
        for ring in sole:
            for i, (px, py, pz) in enumerate(ring):
                toe = max(0, (-py - .133) / .038)
                ring[i] = (px, py, pz + .004 * toe * toe)
        add("Nordic_Outsole", [rubber, carbon, accent], sole, foot, (0, 0, 0, 1, 2), close_sole=True)

        add("Nordic_Boot", [upper, graphite], profile((
            (-.024, .065, .046, .143), (-.023, .075, .046, .143),
            (-.022, .089, .044, .138), (-.016, .104, .042, .124),
            (.000, .119, .041, .106), (.021, .134, .040, .084),
            (.041, .153, .038, .066), (.054, .176, .037, .054),
            (.056, .201, .038, .052), (.056, .233, .040, .053),
            (.056, .246, .041, .054), (.056, .248, .039, .052),
        )), flex, (0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1))

        # A carbon cradle follows the back and both sides of the foot.
        # It ends ahead of the arch, leaving the textile forefoot exposed.
        heel = []
        for z, y, rx, ry in ((.068, .019, .045, .105), (.077, .023, .046, .099),
                             (.097, .032, .044, .090), (.118, .051, .040, .071),
                             (.142, .053, .036, .063)):
            ring = []
            for i in range(41):
                t = -.42 + (math.pi + .84) * i / 40
                flare = math.sin(math.pi * i / 40) ** .7
                ring.append((x + rx * math.cos(t), y + ry * math.sin(t), .068 + (z - .068) * (.15 + .85 * flare)))
            heel.append(ring)
        add("Carbon_Heel_Chassis", [carbon], heel, foot, wrap=False)

        # The upper support is articulated with the shin. The flexible gaiter
        # below it blends with the foot and keeps the ankle covered in stride.
        cuff = []
        for z, rx, ry in ((.158, .039, .060), (.174, .045, .063),
                          (.204, .045, .060), (.227, .042, .055),
                          (.231, .040, .053)):
            ring = []
            for i in range(41):
                t = -.5 + (math.pi + 1) * i / 40
                side_cut = .014 * abs(math.cos(t)) ** 6
                ring.append((centre_x(z) + rx * math.cos(t), .053 + ry * math.sin(t), z - side_cut))
            cuff.append(ring)
        add("Carbon_Ankle_Cuff", [carbon, graphite], cuff, leg, (0, 0, 0, 1), wrap=False)

        # The power strap closes the open front of the articulated cuff.
        band = []
        for z, rx, ry in ((.213, .044, .057), (.215, .045, .058),
                          (.230, .045, .058), (.232, .044, .057)):
            band.append([(centre_x(z) + rx * math.cos(t), .053 + ry * math.sin(t), z)
                         for t in (math.pi + .3 + (math.pi - .6) * i / 32 for i in range(33))])
        add("Cuff_Power_Strap", [graphite, accent], band, leg, (0, 0, 1), wrap=False)

        # Low-profile zipper cover follows the front of the textile from
        # instep to gaiter. The offset is small enough to read as a sewn part.
        zip_path = (
            (-.150, .085), (-.142, .104), (-.106, .119), (-.064, .134),
            (-.026, .153), (-.001, .176), (.005, .201), (.006, .224),
        )
        zipper = []
        for y, z in zip_path:
            zipper.append([(x + .004 * math.cos(i * math.tau / 12),
                            y + .0018 * math.sin(i * math.tau / 12), z) for i in range(12)])
        add("Boot_Sealed_Zip", [graphite], zipper, flex)
    return objects, [upper, carbon, rubber]
