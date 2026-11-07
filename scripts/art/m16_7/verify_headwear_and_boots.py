"""Geometry checks on the saved Blender source, before Unity triangulation."""
import argparse
import json
import sys
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree


parser = argparse.ArgumentParser()
parser.add_argument("--asset", required=True, type=Path)
parser.add_argument("--output", required=True, type=Path)
args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
bpy.ops.wm.open_mainfile(filepath=str(args.asset.resolve()))
objects = bpy.data.objects
cap = objects["Race_Cap"]
head = objects["Anatomical_Head_Neck"]
scalp = BVHTree.FromObject(head, bpy.context.evaluated_depsgraph_get())
to_head = head.matrix_world.inverted() @ cap.matrix_world
hem = [to_head @ v.co for v in cap.data.vertices[:96]]
steps = [abs(p.z - q.z) for p, q in zip(hem, hem[1:] + hem[:1])]
clearances = []
for p in hem:
    centre = Vector((0, .044, p.z))
    radial = (p - centre).normalized()
    hit, _, _, _ = scalp.ray_cast(centre + .2 * radial, -radial, .2)
    if hit is not None:
        clearances.append((p - hit).dot(radial))
cap_result = {
    "hem_samples": len(hem),
    "max_adjacent_height_step_m": max(steps),
    "minimum_radial_scalp_gap_m": min(clearances),
    "max_radial_scalp_gap_m": max(clearances),
    "material_slots": [m.name for m in cap.data.materials],
}
cap_result["pass"] = (len(clearances) == 96 and max(steps) < .0025
                      and min(clearances) > .001 and max(clearances) < .007
                      and cap_result["material_slots"] == ["PULSE_Race_Red"])

boots = {}
for side in ("Left", "Right"):
    sole = objects.get(side + "_Nordic_Outsole")
    if sole is None:
        boots[side] = {"pass": False, "reason": "no separate outsole"}
        continue
    mesh = bmesh.new()
    mesh.from_mesh(sole.data)
    boundary = sum(not edge.is_manifold for edge in mesh.edges)
    volume = abs(mesh.calc_volume(signed=True))
    mesh.free()
    bar = objects[side + "_Toe_Hinge_Contact"].matrix_world.translation
    toe = min((sole.matrix_world @ v.co).y for v in sole.data.vertices)
    underside = min((sole.matrix_world @ v.co).z for v in sole.data.vertices)
    boots[side] = {
        "nonmanifold_edges": boundary, "outsole_volume_m3": volume,
        "toe_bar_to_outsole_front_m": abs(bar.y - toe), "underside_height_m": underside,
        "pass": boundary == 0 and volume > .00025 and abs(bar.y - toe) < .004 and .043 <= underside < .045,
    }
report = {
    "schema_version": 1, "id": "m167_cap_hem_and_nordic_outsole",
    "cap": cap_result, "boots": boots,
    "pass": cap_result["pass"] and all(boot["pass"] for boot in boots.values()),
}
args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
if not report["pass"]:
    raise ValueError("Cap edge or Nordic outsole geometry failed")
