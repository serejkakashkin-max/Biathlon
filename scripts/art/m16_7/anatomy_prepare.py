"""Prepare the CC0 MakeHuman hm08 anatomy for the existing PULSE 24-bone rig.

Run with portable Blender. No add-on, MakeHuman program code, Unity writes, or
downloads are involved. All inputs are explicit local CC0 graphical data.
The output retains source topology/UVs and exposes finger joints/weights so a
glove can be posed anatomically before being parented to the existing Hand bone.
"""
import argparse
import collections
import hashlib
import json
import math
from pathlib import Path
import sys

import bpy
import numpy as np
from mathutils import Matrix, Vector


def parse_obj(path):
    vertices, uvs, faces, face_uvs, face_groups = [], [], [], [], []
    groups = collections.defaultdict(set)
    group = ""
    for line in path.read_text(encoding="utf-8").splitlines():
        fields = line.split()
        if not fields:
            continue
        if fields[0] == "v":
            vertices.append([float(x) for x in fields[1:4]])
        elif fields[0] == "vt":
            uvs.append([float(x) for x in fields[1:3]])
        elif fields[0] == "g":
            group = fields[1]
        elif fields[0] == "f":
            parts = [x.split("/") for x in fields[1:]]
            indices = [int(x[0]) - 1 for x in parts]
            groups[group].update(indices)
            faces.append(indices)
            face_uvs.append([int(x[1]) - 1 for x in parts])
            face_groups.append(group)
    return np.array(vertices), uvs, faces, face_uvs, face_groups, groups


def prepare(source, audit, output, render=False, natural=False):
    output.mkdir(parents=True, exist_ok=True)
    raw, uv, faces, face_uv, face_group, groups = parse_obj(source / "base.obj")
    # Endurance-athlete morphology: 45% maximum-muscle delta, with a lean blend.
    # Each .target is an official CC0 displacement field, not program code.
    targets = {
        "caucasian-male-young.target": 1.0,
        "universal-male-young-averagemuscle-averageweight.target": .55,
        "universal-male-young-maxmuscle-averageweight.target": .20,
        "universal-male-young-maxmuscle-minweight.target": .25,
    }
    for name, weight in targets.items():
        for line in (source / name).read_text(encoding="utf-8").splitlines():
            if not line.strip() or line.startswith("#"):
                continue
            index, x, y, z = line.split()
            raw[int(index)] += np.array([float(x), float(y), float(z)]) * weight

    body_ids = sorted(groups["body"])
    body_min, body_max = raw[body_ids].min(axis=0), raw[body_ids].max(axis=0)
    scale = 1.80 / (body_max[1] - body_min[1])
    # Right-handed Y-up/+Z-forward -> Z-up/-Y-forward, metres, sole at zero.
    coordinates = np.column_stack((raw[:, 0], -raw[:, 2], raw[:, 1] - body_min[1])) * scale
    skel = json.loads((source / "default.mhskel").read_text(encoding="utf-8"))
    mh_weights = json.loads((source / "default_weights.mhw").read_text(encoding="utf-8"))["weights"]
    source_joints = {name: Vector(coordinates[indices].mean(axis=0)) for name, indices in skel["joints"].items()}
    source_bones = {name: (source_joints[bone["head"]], source_joints[bone["tail"]]) for name, bone in skel["bones"].items()}
    record = next(a for a in json.loads(audit.read_text(encoding="utf-8"))["assets"] if a["file"] == "01_Character/man-base-character-rigged.glb")
    rig_bones = record["armatures"][0]["bones"]
    heads = {bone["name"]: Vector(bone["head"]) for bone in rig_bones}
    successors = {"Hips": "Spine02", "Spine02": "Spine01", "Spine01": "Spine", "Spine": "neck", "neck": "Head", "Head": "head_end"}
    for prefix in ("Left", "Right"):
        successors.update({prefix + a: prefix + b for a, b in (("Shoulder", "Arm"), ("Arm", "ForeArm"), ("ForeArm", "Hand"), ("UpLeg", "Leg"), ("Leg", "Foot"), ("Foot", "ToeBase"))})
    tails = {}
    for name, head in heads.items():
        if name in successors:
            tails[name] = heads[successors[name]]
        else:
            tails[name] = head + Vector((0, -.08, 0) if "Toe" in name else (.025 if "Left" in name else -.025, -.055, -.09) if "Hand" in name else (0, 0, .04))
    natural_shift=Vector((0,.09,0))
    if natural:
        # Preserve anatomical proportions. Fitting the jaw/head pivots to the
        # older skeleton elongated the head and flattened the cervical transition.
        mapping={'Hips':'spine05','Spine02':'spine04','Spine01':'spine02','Spine':'spine01','neck':'neck01','Head':'head'}
        for suffix,prefix in [('L','Left'),('R','Right')]:
            for legacy,mh in [('Shoulder','clavicle'),('Arm','upperarm01'),('ForeArm','lowerarm01'),('Hand','wrist'),('UpLeg','upperleg01'),('Leg','lowerleg01'),('Foot','foot'),('ToeBase','toe2-1')]:
                mapping[prefix+legacy]=mh+'.'+suffix
        for name,mh in mapping.items():heads[name]=source_bones[mh][0]+natural_shift
        heads['head_end']=source_bones['head'][1]+natural_shift
        for name,head in heads.items():
            if name in successors:tails[name]=heads[successors[name]]
            elif name.endswith('Hand'):tails[name]=source_bones['finger3-1.'+('L' if name.startswith('Left') else 'R')][0]+natural_shift
            else:tails[name]=head+Vector((0,-.07,0) if 'Toe' in name else (0,0,.04))

    def destination_bone(name):
        stem, _, suffix = name.partition(".")
        prefix = "Left" if suffix == "L" else "Right"
        if stem.startswith(("finger", "metacarpal")) or stem == "wrist":
            return prefix + "Hand"
        if stem.startswith("upperarm"):
            return prefix + "Arm"
        if stem.startswith("lowerarm"):
            return prefix + "ForeArm"
        if stem in ("clavicle", "shoulder01"):
            return prefix + "Shoulder"
        if stem.startswith("upperleg"):
            return prefix + "UpLeg"
        if stem.startswith("lowerleg"):
            return prefix + "Leg"
        if stem == "foot":
            return prefix + "Foot"
        if stem.startswith("toe"):
            return prefix + "ToeBase"
        if stem in ("root", "pelvis", "spine05"):
            return "Hips"
        if stem in ("spine04", "spine03"):
            return "Spine02"
        if stem in ("spine02", "breast"):
            return "Spine01"
        if stem == "spine01":
            return "Spine"
        if stem.startswith("neck"):
            return "neck"
        return "Head"

    original_weights = [collections.defaultdict(float) for _ in raw]
    weights = [collections.defaultdict(float) for _ in raw]
    for name, entries in mh_weights.items():
        for index, weight in entries:
            original_weights[index][name] += weight
            weights[index][destination_bone(name)] += weight
    for entries in weights:
        total = sum(entries.values())
        for name in entries:
            entries[name] /= total

    # Torso/head deformation is continuous by height. It avoids separate affine
    # bone fits producing a kink where one spine bone hands off to the next.
    height_pairs = [
        (source_bones["spine05"][0].z, heads["Hips"].z),
        (source_bones["spine04"][0].z, heads["Spine02"].z),
        (source_bones["spine02"][0].z, heads["Spine01"].z),
        (source_bones["spine01"][0].z, heads["Spine"].z),
        (source_bones["neck01"][0].z, heads["neck"].z),
        # MH places its head pivot near the jaw, whereas the legacy rig's Head
        # starts at the neck. Fitting those two pivots directly stretches the
        # skull. The skin envelope retains a natural skull height independently
        # of the existing bind pivot (the final hero uses the textured old head).
        (source_bones["head"][0].z, 1.610),
        (1.8, 1.8),
    ]
    height_pairs.sort()
    def interpolate(value, knots):
        for (a, x), (b, y) in zip(knots, knots[1:]):
            if value <= b:
                return x + (value - a) / (b - a) * (y - x)
        a, x = knots[-2]
        b, y = knots[-1]
        return x + (value - a) / (b - a) * (y - x)

    def torso_point(point):
        # Existing source chest centres sit ~4 cm behind the model origin.
        # Preserve the lean MakeHuman cross-section rather than fit casual cloth.
        z = interpolate(point.z, height_pairs)
        x_shift = interpolate(z, [(heads["Hips"].z, heads["Hips"].x), (heads["neck"].z, heads["neck"].x)])
        return Vector((point.x + x_shift, point.y + .035, z))

    segments = {}
    for suffix, prefix in (("L", "Left"), ("R", "Right")):
        get = lambda stem: source_bones[stem + "." + suffix]
        segments[prefix + "Arm"] = (get("upperarm01")[0], get("lowerarm01")[0])
        segments[prefix + "ForeArm"] = (get("lowerarm01")[0], get("wrist")[0])
        segments[prefix + "Hand"] = (get("wrist")[0], get("finger3-3")[1])
        segments[prefix + "UpLeg"] = (get("upperleg01")[0], get("lowerleg01")[0])
        segments[prefix + "Leg"] = (get("lowerleg01")[0], get("foot")[0])
        segments[prefix + "Foot"] = (get("foot")[0], get("toe2-1")[0])
        segments[prefix + "ToeBase"] = (get("toe2-1")[0], get("toe2-3")[1])

    transforms = {}
    for name, (start, end) in segments.items():
        delta, dest_delta = end - start, tails[name] - heads[name]
        rotation = delta.rotation_difference(dest_delta)
        # Hand/finger geometry remains anatomically sized; tail is only a short
        # orientation marker on the legacy 24-bone rig.
        length_scale = 1.0 if "Hand" in name else dest_delta.length / delta.length
        transforms[name] = (start, heads[name], delta.normalized(), rotation, length_scale)

    def map_point(point, name):
        if natural:return point+natural_shift
        if name not in transforms:
            return torso_point(point)
        start, dest, axis, rotation, length_scale = transforms[name]
        local = point - start
        return dest + rotation @ (local + axis * local.dot(axis) * (length_scale - 1))

    fitted = []
    for index, coordinate in enumerate(coordinates):
        point = Vector(coordinate)
        if weights[index]:
            fitted.append(sum((map_point(point, name) * weight for name, weight in weights[index].items()), Vector()))
        else:
            fitted.append(torso_point(point))

    bpy.ops.wm.read_factory_settings(use_empty=True)
    remap = {old: new for new, old in enumerate(body_ids)}
    selected = [i for i, name in enumerate(face_group) if name == "body"]
    mesh = bpy.data.meshes.new("MakeHuman_CC0_Athletic_QuadTopology")
    mesh.from_pydata([fitted[i] for i in body_ids], [], [[remap[i] for i in faces[f]] for f in selected])
    mesh.update()
    obj = bpy.data.objects.new("MakeHuman_Athletic_Fitted", mesh)
    bpy.context.collection.objects.link(obj)
    layer = mesh.uv_layers.new(name="MakeHumanUV")
    for polygon, source_face in zip(mesh.polygons, selected):
        polygon.use_smooth = True
        for loop, source_uv in zip(polygon.loop_indices, face_uv[source_face]):
            layer.data[loop].uv = uv[source_uv]
    for name in heads:
        obj.vertex_groups.new(name=name)
    for new_id, old_id in enumerate(body_ids):
        for name, weight in weights[old_id].items():
            obj.vertex_groups[name].add([new_id], weight, "REPLACE")
    rig_data = bpy.data.armatures.new("Existing24Bone_RestFit")
    rig = bpy.data.objects.new("BiathleteSkeleton", rig_data)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    for bone in rig_bones:
        edit = rig_data.edit_bones.new(bone["name"])
        edit.head, edit.tail = heads[bone["name"]], tails[bone["name"]]
    for bone in rig_bones:
        if bone["parent"]:
            rig_data.edit_bones[bone["name"]].parent = rig_data.edit_bones[bone["parent"]]
    bpy.ops.object.mode_set(mode="OBJECT")
    modifier = obj.modifiers.new("24-bone original rest skeleton", "ARMATURE")
    modifier.object = rig
    obj.parent = rig
    gray = bpy.data.materials.new("Neutral anatomy review")
    gray.diffuse_color = (.28, .33, .37, 1)
    gray.use_nodes = True
    gray.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = gray.diffuse_color
    gray.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = .65
    mesh.materials.append(gray)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.ops.wm.obj_export(filepath=str(output / "athletic_fitted.obj"), export_selected_objects=True, apply_modifiers=False, forward_axis="Y", up_axis="Z", export_materials=False)
    bpy.ops.wm.save_as_mainfile(filepath=str(output / "athletic_fitted.blend"))
    fingers = {}
    for name, (start, end) in source_bones.items():
        if name.startswith(("finger", "metacarpal", "wrist")):
            parent = skel["bones"][name]["parent"]
            fingers[name] = {"head": list(map_point(start, destination_bone(name))), "tail": list(map_point(end, destination_bone(name))), "parent": parent}
    metadata = {
        "schema_version": 1,
        "id": "m167_makehuman_natural_anatomy" if natural else "m167_makehuman_athletic_fitted",
        "proportion_mode": "natural_unwarped_anatomy" if natural else "legacy_rig_fitted",
        "coordinate_system": "Blender world, right-handed Z up / -Y front, metres",
        "source_vertex_ids": body_ids,
        "weights": [dict(weights[i]) for i in body_ids],
        "finger_weights": [{name: value for name, value in original_weights[i].items() if name.startswith(("finger", "metacarpal", "wrist"))} for i in body_ids],
        "finger_bones": fingers,
        "heads": {name: list(head) for name, head in heads.items()},
        "tails": {name: list(tail) for name, tail in tails.items()},
        "source_joints": {name: list(point) for name, point in source_joints.items()},
        "morph_targets": targets,
        "source_scale": scale,
        "vertex_count": len(mesh.vertices),
        "quad_count": sum(len(p.vertices) == 4 for p in mesh.polygons),
        "triangle_count": sum(len(p.vertices) - 2 for p in mesh.polygons),
        "unweighted_vertices": sum(not bool(weights[i]) for i in body_ids),
        "bounds_min": [min(v.co[d] for v in mesh.vertices) for d in range(3)],
        "bounds_max": [max(v.co[d] for v in mesh.vertices) for d in range(3)],
        "license": "CC0-1.0; source notices retained beside base.obj",
        "source_sha256": {name: hashlib.sha256((source / name).read_bytes()).hexdigest() for name in sorted(["base.obj", "default.mhskel", "default_weights.mhw", "license.txt", "LICENSE.ASSETS.md", *targets])},
        "notes": ["Do not mistake original MakeHuman helpers for render geometry: only the body group is exported.", "Hand finger weights/joints are metadata only; this model keeps the existing 24-bone rig.", "This anatomy is a tailoring base; final skin/suit/glove materials and equipment belong in the character generator.", "Natural mode supplies the active head/neck without fit deformation; the legacy fit is a rejected regression fixture."],
    }
    metadata["finger_pad_centres"] = {}
    for name in fingers:
        if name.startswith("finger") and "-3." in name:
            points = [mesh.vertices[k].co for k, i in enumerate(body_ids) if original_weights[i].get(name, 0) > .7]
            if points: metadata["finger_pad_centres"][name] = list(sum(points, Vector()) / len(points))
    sys.path.insert(0,str(Path(__file__).resolve().parent))
    from hand_anatomy import add_palmar_pads
    add_palmar_pads(metadata,mesh)
    (output / "athletic_fitted.json").write_text(json.dumps(metadata, indent=2), encoding="utf-8")
    if render:
        scene = bpy.context.scene
        scene.render.engine = "CYCLES"
        scene.cycles.samples = 24
        scene.render.resolution_x, scene.render.resolution_y = 700, 1000
        scene.render.resolution_percentage = 100
        scene.world = bpy.data.worlds.new("ReviewWorld")
        scene.world.use_nodes = True
        scene.world.node_tree.nodes["Background"].inputs[0].default_value = (.12, .14, .17, 1)
        def point(object, target):
            object.rotation_euler = (Vector(target) - object.location).to_track_quat("-Z", "Y").to_euler()
        for position, energy, size in (((3, -4, 4), 700, 4), ((-3, -2, 3), 450, 3), ((1, 3, 3), 900, 3)):
            bpy.ops.object.light_add(type="AREA", location=position)
            light = bpy.context.object
            light.data.energy, light.data.size = energy, size
            point(light, (0, 0, .9))
        bpy.ops.object.camera_add(location=(0, -4, 1))
        camera = bpy.context.object
        scene.camera = camera
        camera.data.type, camera.data.ortho_scale = "ORTHO", 2.05
        for name, position in (("front", (0, -4, 1)), ("side", (4, 0, 1)), ("back", (0, 4, 1))):
            camera.location = position
            point(camera, (0, 0, .9))
            scene.render.filepath = str(output / ("anatomy_" + name + ".png"))
            bpy.ops.render.render(write_still=True)
    print("M167_ANATOMY_PREPARED", json.dumps({key: metadata[key] for key in ("vertex_count", "quad_count", "triangle_count", "unweighted_vertices", "bounds_min", "bounds_max")}))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True)
    parser.add_argument("--audit", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--render", action="store_true")
    parser.add_argument("--natural", action="store_true")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    prepare(Path(args.source).resolve(), Path(args.audit).resolve(), Path(args.output).resolve(), args.render,args.natural)
