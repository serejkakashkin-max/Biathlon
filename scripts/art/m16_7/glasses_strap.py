"""Fit a connected, solid glasses retainer to the real head before export.

The runtime interpolates these baked fits; no runtime head raycasts or mesh bakes.
"""
import hashlib
import json
import math
from pathlib import Path
import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[3]
ASSET = ROOT / "Assets/Biathlon/Characters/Biathlete_16_7"


def prepare_source(output):
    settings = json.loads((ASSET / "Documentation/glasses-retainer-v1.json").read_text())
    pose_path = ASSET / "Documentation/performance-motion-v1.json"
    pose = json.loads(pose_path.read_text())
    scene = bpy.context.scene
    bpy.context.view_layer.update()
    lens = bpy.data.objects["Panoramic_Amber_Lens"]
    uv = lens.data.uv_layers.active.data
    samples = {}
    for polygon in lens.data.polygons:
        for index in polygon.loop_indices:
            samples[tuple(round(v, 5) for v in uv[index].uv)] = lens.matrix_world @ lens.data.vertices[lens.data.loops[index].vertex_index].co
    def hinge(u):
        edge = [point for uv, point in samples.items() if uv[0] == u]
        return sum(edge, Vector()) / len(edge) + Vector((0, settings["hinge_rear_inset_m"], 0))
    left, right = hinge(0), hinge(1)

    def tree(name):
        obj = bpy.data.objects[name]
        return BVHTree.FromPolygons([obj.matrix_world @ v.co for v in obj.data.vertices],
                                   [list(p.vertices) for p in obj.data.polygons])
    skin, cap, band = tree("Anatomical_Head_Neck"), tree("Race_Cap"), tree("Headband_Prop")
    crew, swept = tree("Hair_Crew_Prop"), tree("Hair_Short_Prop")
    arm = bpy.data.objects["BiathleteSkeleton"]
    pivot = arm.matrix_world @ arm.data.bones["Head"].head_local
    p = pose["eyewear_pivot"]
    pivot += Vector((p["x"], -p["z"], p["y"]))
    offset = pose["glasses_raise_offset"]
    offset = Vector((offset["x"], -offset["z"], offset["y"]))
    frames = []
    variants = (("cap", (skin, cap)), ("headband_crew", (skin, band, crew)),
                ("headband_short_swept", (skin, band, swept)), ("none_crew", (skin, crew)),
                ("none_short_swept", (skin, swept)))
    for headwear, surfaces in variants:
        for step in range(settings["lift_samples"]):
            amount = step / (settings["lift_samples"]-1)
            turn = Matrix.Rotation(math.radians(pose["glasses_raise_euler"]["x"])*amount, 4, "X")
            endpoints = [pivot + turn.to_3x3() @ (p-pivot) + offset*amount for p in (left, right)]
            angles = [math.atan2(p.x, -(p.y-settings["head_centre_y_m"])) for p in endpoints]
            if angles[1] > angles[0]: angles[1] -= math.tau
            centres, normals = [], []
            for row in range(settings["segments"]+1):
                t = row/settings["segments"]
                angle = angles[0]*(1-t) + angles[1]*t
                rear_offset = settings["worn_rear_lift_m"]*(1-amount) + settings["raised_rear_lift_m"]*amount
                height = endpoints[0].z*(1-t) + endpoints[1].z*t + math.sin(math.pi*t)*rear_offset
                centre = Vector((0, settings["head_centre_y_m"], height))
                radial = Vector((math.sin(angle), -math.cos(angle), 0))
                hits = [surface.ray_cast(centre+radial*.3, -radial, .3) for surface in surfaces]
                hits = [hit for hit in hits if hit[0] is not None]
                if not hits: raise ValueError((headwear, step, row, "head ray missed"))
                point, normal, _, _ = min(hits, key=lambda hit: hit[3])
                if normal.dot(radial) < 0: normal = -normal
                fit = point + normal*(settings["clearance_m"]+settings["thickness_m"]*.5)
                blend = smooth(t/settings["hinge_blend"]) * smooth((1-t)/settings["hinge_blend"])
                free = endpoints[0]*(1-t) + endpoints[1]*t
                centres.append(free.lerp(fit, blend))
                normals.append(normal)
            points = []
            for row, centre in enumerate(centres):
                tangent = (centres[min(row+1, settings["segments"])]-centres[max(0, row-1)]).normalized()
                across = tangent.cross(normals[row]).normalized()
                if across.z < 0: across = -across
                normal = across.cross(tangent).normalized()
                if normal.dot(normals[row]) < 0: normal = -normal
                for height_sign, depth_sign in ((-1,-1),(-1,1),(1,1),(1,-1)):
                    points.append(centre + across*(height_sign*settings["width_m"]*.5) + normal*(depth_sign*settings["thickness_m"]*.5))
            frames.append({"headwear": headwear, "amount": amount, "points": [vector(p) for p in points]})

    old = bpy.data.objects.get("Goggle_Elastic_Strap")
    if old: bpy.data.objects.remove(old, do_unlink=True)
    material = bpy.data.materials.get("PULSE_Glasses_Elastic")
    if material is None: material = bpy.data.materials["PULSE_Stretch_Graphite"].copy(); material.name = "PULSE_Glasses_Elastic"
    material.node_tree.nodes.get("Principled BSDF").inputs["Roughness"].default_value = .82
    vertices = [Vector((p["x"], p["y"], p["z"])) for p in frames[0]["points"]]
    faces = []
    for row in range(settings["segments"]):
        for corner in range(4):
            a=row*4+corner; b=row*4+(corner+1)%4
            faces.append((a,b,b+4,a+4))
    faces.extend(((3,2,1,0), tuple(settings["segments"]*4+i for i in range(4))))
    mesh = bpy.data.meshes.new("Connected fitted glasses retainer")
    mesh.from_pydata(vertices, [], faces); mesh.materials.append(material)
    layer = mesh.uv_layers.new(name="UVMap")
    for polygon in mesh.polygons:
        for index in polygon.loop_indices:
            v=mesh.loops[index].vertex_index
            layer.data[index].uv=(v//4/settings["segments"], (v%4)/3)
    obj=bpy.data.objects.new("Goggle_Elastic_Strap",mesh); scene.collection.objects.link(obj)
    world=obj.matrix_world.copy(); obj.parent=arm; obj.parent_type="BONE"; obj.parent_bone="Head"; obj.matrix_world=world
    output.mkdir(parents=True, exist_ok=True)
    data={"schema_version":1,"id":"m167_fitted_glasses_strap_v1","segments":settings["segments"],"lift_samples":settings["lift_samples"],"frames":frames,
          "pose_sha256":hashlib.sha256(pose_path.read_bytes().replace(b'\r\n',b'\n')).hexdigest()}
    (output/"glasses-strap-fit-v1.json").write_bytes((json.dumps(data,separators=(',',':'))+'\n').encode())
    return material


def smooth(t):
    t=max(0,min(1,t)); return t*t*(3-2*t)


def vector(point): return {"x":point.x,"y":point.y,"z":point.z}
