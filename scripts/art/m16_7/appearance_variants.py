"""CC0 face targets and fitted hair, using the existing career appearance IDs."""
import json
import math
import shutil
from pathlib import Path

import bpy
import numpy as np
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree
from mathutils.kdtree import KDTree

ROOT = Path(__file__).resolve().parents[3]
ASSET = ROOT / "Assets/Biathlon/Characters/Biathlete_16_7"
SOURCE = ASSET / "Models/Source~/MakeHuman"
DEFINITION = ASSET / "Documentation/appearance-variants-v1.json"
SETTINGS = json.loads(DEFINITION.read_text(encoding="utf-8"))


def smooth(value):
    value = max(0, min(1, value))
    return value * value * (3 - 2 * value)


class FaceTargets:
    def __init__(self):
        self.metadata = json.loads((SOURCE / "Natural/athletic_fitted.json").read_text())
        with bpy.data.libraries.load(str(SOURCE / "Natural/athletic_fitted.blend"), link=False) as (_, data):
            data.objects = ["MakeHuman_Athletic_Fitted"]
        original = data.objects[0]
        self.points = [v.co.copy() for v in original.data.vertices]
        bpy.data.objects.remove(original, do_unlink=True)
        self.tree = KDTree(len(self.points))
        for i, p in enumerate(self.points):
            self.tree.insert(p, i)
        self.tree.balance()
        ids = self.metadata["source_vertex_ids"]
        scale = self.metadata["source_scale"]
        self.fields = {}
        for recipe in SETTINGS["faces"]:
            offsets = {}
            for target in recipe["targets"]:
                for line in (SOURCE / "FaceTargets" / target["file"]).read_text().splitlines():
                    if not line.strip() or line.startswith("#"):
                        continue
                    index, x, y, z = line.split()
                    delta = Vector((float(x), -float(z), float(y))) * (scale * target["weight"])
                    index = int(index)
                    offsets[index] = offsets.get(index, Vector()) + delta
            self.fields[recipe["id"]] = [offsets.get(index, Vector()) for index in ids]
        self.eyes = [Vector(self.metadata["source_joints"]["eye." + side + "____head"]) + Vector((0, .09, 0))
                     for side in ("L", "R")]

    def offsets(self, point):
        # Preserve neck/collar, eye seating and the cap's scalp contact.
        strength = smooth((point.z - 1.575) / .035) * (1 - smooth((point.z - 1.655) / .035))
        for eye in self.eyes:
            distance = math.hypot(point.x - eye.x, point.z - eye.z)
            strength *= smooth((distance - .020) / .018)
        # Nearest-source interpolation must not split the closely layered nostril,
        # lip and ear surfaces. Identity changes belong to the jaw and cheeks.
        for feature in SETTINGS["protected_face_features"]:
            axes = [abs(point[i] - feature["centre"][i]) / feature["radii"][i] for i in range(3)]
            distance = max(axes) if feature.get("shape") == "box" else math.sqrt(sum(a*a for a in axes))
            strength *= smooth((distance - 1) / feature["falloff"])
        neighbours = self.tree.find_n(point, 4)
        weights = [1 / max(.0003, row[2]) ** 2 for row in neighbours]
        total = sum(weights)
        result = {}
        for name, field in self.fields.items():
            delta = sum((field[row[1]] * weight for row, weight in zip(neighbours, weights)), Vector())
            delta *= strength / total
            limit = SETTINGS["maximum_face_displacement_m"]
            if delta.length > limit:
                delta *= limit / delta.length
            result[name] = delta
        return result


def add_face_shapes(obj, targets):
    if obj.data.shape_keys:
        obj.shape_key_clear()
    obj.shape_key_add(name="Basis")
    shapes = {face["id"]: obj.shape_key_add(name="FaceIdentity_" + face["id"])
              for face in SETTINGS["faces"]}
    is_body = obj.name.startswith("Biathlete_Body")
    face_indices = set()
    cap_indices = set()
    for polygon in obj.data.polygons:
        material = obj.data.materials[polygon.material_index]
        if material.name == "Anatomical_Skin_CC0_2K":
            face_indices.update(polygon.vertices)
        if material.name in ("PULSE_Race_Red", "Bib_Ivory"):
            cap_indices.update(i for i in polygon.vertices if obj.data.vertices[i].co.z > 1.64)
    for index in face_indices:
        vertex = obj.data.vertices[index]
        point = obj.matrix_world @ vertex.co
        if point.z < 1.575:
            continue
        for name, delta in targets.offsets(point).items():
            shapes[name].data[index].co += obj.matrix_world.inverted().to_3x3() @ delta
    if is_body:
        hidden = obj.shape_key_add(name="HeadwearHidden")
        centre = Vector((0, .043, 1.73))
        for index in cap_indices:
            point = obj.matrix_world @ obj.data.vertices[index].co
            hidden.data[index].co = obj.matrix_world.inverted() @ (centre + (point - centre) * .72)


def bind_to_head(obj, arm):
    bpy.context.view_layer.update()
    matrix = obj.matrix_world.copy()
    obj.parent = arm
    obj.parent_type = "BONE"
    obj.parent_bone = "Head"
    obj.matrix_world = matrix


def fitted_hair(recipe, raw, scale, floor, arm, textures):
    """Read a MakeHuman graphical proxy; its barycentric offsets retain the hairstyle."""
    source = SOURCE / "HeadOptions"
    name = recipe["source"]
    old = bpy.data.objects.get(recipe["object"])
    if old:
        bpy.data.objects.remove(old, do_unlink=True)
    lines = (source / (name + ".mhclo")).read_text().splitlines()
    factors = np.ones(3)
    start = None
    for i, line in enumerate(lines):
        values = line.split()
        if not values or line.startswith("#"):
            continue
        if values[0] in ("x_scale", "y_scale", "z_scale"):
            axis = ("x_scale", "y_scale", "z_scale").index(values[0])
            factors[axis] = abs(raw[int(values[1]), axis] - raw[int(values[2]), axis]) / float(values[3])
        if values[0] == "verts":
            start = i + 1
            break
    if start is None:
        raise ValueError("Hair proxy has no vertex mapping")
    points = []
    for line in lines[start:]:
        values = line.split()
        if not values:
            continue
        if not values[0].lstrip("-").isdigit():
            break
        if len(values) == 1:
            p = raw[int(values[0])]
        else:
            ids = [int(v) for v in values[:3]]
            weights = np.array([float(v) for v in values[3:6]])
            offset = np.array([float(v) for v in values[6:9]]) * factors
            p = (raw[ids] * weights[:, None]).sum(axis=0) + offset
        points.append((p[0] * scale, -p[2] * scale + .09, (p[1] - floor) * scale))
    bpy.ops.wm.obj_import(filepath=str(source / (name + ".obj")))
    obj = bpy.context.object
    obj.name = recipe["object"]
    if len(points) != len(obj.data.vertices):
        raise ValueError("Hair proxy vertex mapping is incomplete")
    obj.matrix_world = Matrix.Identity(4)
    for vertex, point in zip(obj.data.vertices, points):
        vertex.co = point
    scalp = BVHTree.FromObject(bpy.data.objects["Anatomical_Head_Neck"],
                               bpy.context.evaluated_depsgraph_get())
    for vertex in obj.data.vertices:
        p = vertex.co
        hit = scalp.find_nearest(p)
        if recipe["id"] == "crew":
            # Keep the donor UVs and strand layers, fitting a close sports cut.
            vertex.co = hit[0] + hit[1] * (.002 + min(hit[3], .02) * .05)
        p = vertex.co.copy()
        theta = math.atan2(p.y - .044, p.x)
        lower = 1.697 - .027 * max(0, math.sin(theta)) ** 2 + .006 * max(0, -math.sin(theta)) ** 2
        strength = smooth((p.z - lower + .014) / .014) * (1 - smooth((p.z - lower - .031) / .014))
        centre = Vector((0, .044, p.z))
        radial = Vector((math.cos(theta), math.sin(theta), 0))
        point, _, _, _ = scalp.ray_cast(centre + radial * .2, -radial, .2)
        if point is not None:
            # Preserve the coiffure above the band, with a cropped temple below.
            vertex.co = p.lerp(point + radial * .0012, strength)
    mat = bpy.data.materials.get(recipe["material"]) or bpy.data.materials.new(recipe["material"])
    mat.use_nodes = True
    mat.diffuse_color = (.12, .07, .04, 1)
    mat.node_tree.nodes.clear()
    shader = mat.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
    output = mat.node_tree.nodes.new("ShaderNodeOutputMaterial")
    mat.node_tree.links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    shader.inputs["Roughness"].default_value = SETTINGS["hair_roughness"]
    image = bpy.data.images.load(str(source / (name + "_diffuse.png")), check_existing=True)
    pixels = np.empty(image.size[0] * image.size[1] * 4, dtype=np.float32)
    image.pixels.foreach_get(pixels)
    pixels = pixels.reshape((-1, 4))
    grey = pixels[:, :3].mean(axis=1)
    reference = np.percentile(grey[pixels[:, 3] > .5], 85)
    pixels[:, :3] = np.clip(.2 + .8 * grey / max(.01, reference), 0, 1)[:, None]
    atlas_name = "Hair_PULSE_" + recipe["id"] + "_BaseColor_2K"
    previous = bpy.data.images.get(atlas_name)
    if previous:
        bpy.data.images.remove(previous, do_unlink=True)
    atlas = bpy.data.images.new(atlas_name, width=image.size[0], height=image.size[1], alpha=True)
    atlas.pixels.foreach_set(pixels.ravel())
    atlas.filepath_raw = str(textures / (atlas_name + ".png"))
    atlas.file_format = "PNG"
    atlas.save()
    texture = mat.node_tree.nodes.new("ShaderNodeTexImage")
    texture.image = atlas
    mat.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    mat.node_tree.links.new(texture.outputs["Alpha"], shader.inputs["Alpha"])
    source_normal = source / (name + "_normal.png")
    if source_normal.exists():
        normal_name = "Hair_PULSE_" + recipe["id"] + "_Normal_2K.png"
        shutil.copyfile(source_normal, textures / normal_name)
        image = bpy.data.images.load(str(textures / normal_name), check_existing=True)
        image.colorspace_settings.name = "Non-Color"
        texture = mat.node_tree.nodes.new("ShaderNodeTexImage")
        texture.image = image
        normal = mat.node_tree.nodes.new("ShaderNodeNormalMap")
        normal.inputs["Strength"].default_value = SETTINGS["hair_normal_strength"]
        mat.node_tree.links.new(texture.outputs["Color"], normal.inputs["Color"])
        mat.node_tree.links.new(normal.outputs["Normal"], shader.inputs["Normal"])
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    for polygon in obj.data.polygons:
        polygon.material_index = 0
        polygon.use_smooth = True
    bind_to_head(obj, arm)
    obj.hide_render = True
    return mat


def prepare_source(textures):
    arm = bpy.data.objects["BiathleteSkeleton"]
    bpy.context.view_layer.update()
    targets = FaceTargets()
    for source_name, target_name in (
        ("young_darkskinned_male_diffuse.png", "MakeHuman_Skin_Deep_BaseColor_2K.png"),
        ("young_lightskinned_male_diffuse3.png", "MakeHuman_Skin_Warm_BaseColor_2K.png"),
    ):
        shutil.copyfile(SOURCE / "HeadOptions" / source_name, textures / target_name)
    raw = np.array([list(map(float, line.split()[1:4])) for line in (SOURCE / "base.obj").read_text().splitlines()
                    if line.startswith("v ")])
    for filename, weight in targets.metadata["morph_targets"].items():
        for line in (SOURCE / filename).read_text().splitlines():
            if line and not line.startswith("#"):
                i, x, y, z = line.split()
                raw[int(i)] += np.array((float(x), float(y), float(z))) * weight
    body_ids = targets.metadata["source_vertex_ids"]
    floor = raw[body_ids, 1].min()
    materials = [fitted_hair(recipe, raw, targets.metadata["source_scale"], floor, arm, textures)
                 for recipe in SETTINGS["hair"]]
    old = bpy.data.objects.get("Headband_Prop")
    if old:
        bpy.data.objects.remove(old, do_unlink=True)
    head = bpy.data.objects["Anatomical_Head_Neck"]
    scalp = BVHTree.FromObject(head, bpy.context.evaluated_depsgraph_get())
    vertices, faces = [], []
    for surface in (0, 1):
        for row in range(5):
            for i in range(96):
                angle = i * math.tau / 96
                lower = 1.697 - .027 * max(0, math.sin(angle)) ** 2 + .006 * max(0, -math.sin(angle)) ** 2
                z = lower + .031 * row / 4
                centre = Vector((0, .044, z))
                radial = Vector((math.cos(angle), math.sin(angle), 0))
                hit, _, _, _ = scalp.ray_cast(centre + .2 * radial, -radial, .2)
                if hit is None:
                    raise ValueError("Headband misses the scalp")
                vertices.append(hit + radial * (.0024 + .0012 * surface))
        start = surface * 480
        for row in range(4):
            for i in range(96):
                j = (i + 1) % 96
                quad = (start + row * 96 + i, start + row * 96 + j,
                        start + (row + 1) * 96 + j, start + (row + 1) * 96 + i)
                faces.append(quad if surface else tuple(reversed(quad)))
    for row in (0, 4):
        for i in range(96):
            j = (i + 1) % 96
            quad = (row * 96 + i, row * 96 + j, 480 + row * 96 + j, 480 + row * 96 + i)
            faces.append(quad if row == 4 else tuple(reversed(quad)))
    data = bpy.data.meshes.new("PULSE racing headband")
    data.from_pydata(vertices, [], faces)
    data.materials.append(bpy.data.materials["PULSE_Race_Red"])
    uv = data.uv_layers.new(name="Woven band")
    for polygon in data.polygons:
        polygon.use_smooth = True
        for li in polygon.loop_indices:
            index = data.loops[li].vertex_index % 480
            uv.data[li].uv = (index % 96 / 96, index // 96 / 4)
    band = bpy.data.objects.new("Headband_Prop", data)
    bpy.context.collection.objects.link(band)
    bind_to_head(band, arm)
    band.hide_render = True
    add_face_shapes(head, targets)
    return materials


def add_runtime_shapes(objects):
    if bpy.data.objects.get("Headband_Prop") is None:
        return
    targets = FaceTargets()
    for obj in objects:
        add_face_shapes(obj, targets)
