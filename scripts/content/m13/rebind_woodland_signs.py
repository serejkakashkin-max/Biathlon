"""Cold roadside sign correction; native mesh bounds stay outside the ski corridor.

Only positions in environment.json change. Meshes, textures, terrain, race data,
and per-frame rendering remain intact. Existing clear signs are grounded too.
"""
import json
import math
from pathlib import Path

import dress_woodland_course as d
import author_short_course_dressing as author

ROOT = Path(__file__).resolve().parents[3]
VENUE = ROOT / "Assets/Resources/Biathlon/Venues/woodland_basin_v1"


def nearest_station(corridor, position):
    best = None
    for a, b in zip(corridor["knots"], corridor["knots"][1:]):
        dx, dz = b["x"] - a["x"], b["z"] - a["z"]
        square = dx * dx + dz * dz
        if not square:
            continue
        t = max(0., min(1., ((position["x"] - a["x"]) * dx + (position["z"] - a["z"]) * dz) / square))
        x, z = a["x"] + t * dx, a["z"] + t * dz
        item = (math.hypot(position["x"] - x, position["z"] - z),
                a["distance_m"] + t * (b["distance_m"] - a["distance_m"]),
                1 if (position["x"] - x) * dz - (position["z"] - z) * dx >= 0 else -1)
        if best is None or item[0] < best[0]:
            best = item
    return best


def clearance(environment, polygon):
    return min(d.segment_polygon((a["x"], a["z"]), (b["x"], b["z"]), polygon)
               - max(c["widths_m"][i:i + 2]) / 2
               for c in environment["route_corridors"]
               for i, (a, b) in enumerate(zip(c["knots"], c["knots"][1:])))


def run(project=ROOT):
    project = project.resolve()
    venue = project / "Assets/Resources/Biathlon/Venues/woodland_basin_v1"
    environment = d.read_json(venue / "environment.json")
    visual = d.read_json(venue / "visual.json")
    policy = d.read_json(Path(__file__).with_name("woodland_sign_clearance_v1.json"))
    if policy["schema_version"] != 1 or policy["id"] != "woodland_sign_clearance_v1":
        raise ValueError("Explicit versioned sign clearance policy required")
    grid, spacing = d.terrain_grid(project, environment)
    main = next(c for c in environment["route_corridors"] if c["loop_id"] == "main")
    witnesses = []
    groups = []
    for placement in environment["assets"]:
        if placement["resource"].endswith(("/FlagStela", "/Billboard")):
            groups.append(([placement], author.prefab_bounds(project, placement["resource"]), False))
    banners = {}
    for placement in environment["mesh_assets"]:
        if placement["id"].startswith("course_") and "_banner_" in placement["id"]:
            identity = placement["id"].rsplit("_", 1)[0]
            banners.setdefault(identity, []).append(placement)
    for parts in banners.values():
        bounds = d.merged_bounds([d.mesh_bounds(d.resource_path(project, p["mesh_resource"], ".asset")) for p in parts])
        groups.append((parts, bounds, True))
    for parts, bounds, embed in groups:
        placement = parts[0]
        old = placement["position"]
        polygon = d.footprint(bounds, (old["x"], 0., old["z"]), placement["yaw_deg"])
        heights = [y for _, y in d.foundation_samples(grid, spacing, polygon)]
        if (clearance(environment, polygon) >= policy["minimum_course_edge_clearance_m"] - .00001
                and max(heights) - min(heights) <= policy["maximum_foundation_variation_m"]):
            # Clearance does not prove vertical contact. A previous terrain edit
            # can leave an already roadside banner buried or floating; fit its
            # complete native footprint against every underlying triangle.
            height = (min(heights) if embed else max(heights)) - bounds[0][1]
            if abs(height - old["y"]) > .00001:
                fitted = {"x": old["x"], "y": height, "z": old["z"]}
                for part in parts:
                    part["position"] = dict(fitted)
                witnesses.append({"id": placement["id"], "before": old, "after": fitted,
                                  "minimum_edge_clearance_m": clearance(environment, polygon)})
            continue
        _, station, side = nearest_station(main, old)
        accepted = None
        for delta in [0.] + [v for n in range(1, 21) for v in (float(n), -float(n))]:
            candidate_station = station + delta
            if candidate_station < 0 or candidate_station > main["knots"][-1]["distance_m"]:
                continue
            point, normal, _ = d.anchor(visual, "main", candidate_station)
            rotated = d.footprint(bounds, (0., 0., 0.), placement["yaw_deg"])
            inward = min(side * (x * normal[0] + z * normal[1]) for x, z in rotated)
            width = next(max(main["widths_m"][i:i + 2]) for i, (a, b) in enumerate(zip(main["knots"], main["knots"][1:])) if a["distance_m"] <= candidate_station <= b["distance_m"])
            for extra in (0., .5, 1., 2., 3.):
                lateral = side * (width / 2 + policy["minimum_course_edge_clearance_m"] - inward + extra)
                position = (point[0] + normal[0] * lateral, 0., point[2] + normal[1] * lateral)
                polygon = d.footprint(bounds, position, placement["yaw_deg"])
                gap = clearance(environment, polygon)
                if gap < policy["minimum_course_edge_clearance_m"] - .00001:
                    continue
                heights = [y for _, y in d.foundation_samples(grid, spacing, polygon)]
                if max(heights) - min(heights) > policy["maximum_foundation_variation_m"]:
                    continue
                height = (min(heights) if embed else max(heights)) - bounds[0][1]
                accepted = {"x": position[0], "y": height, "z": position[2]}, gap
                break
            if accepted:
                break
        if accepted is None:
            raise ValueError("Cannot fit roadside sign at its native foundation: " + placement["id"])
        for part in parts:
            part["position"] = dict(accepted[0])
        witnesses.append({"id": placement["id"], "before": old, "after": accepted[0], "minimum_edge_clearance_m": accepted[1]})
    (venue / "environment.json").write_text(json.dumps(environment, indent=2) + "\n", encoding="utf-8")
    return {"schema_version": 1, "id": "woodland_sign_clearance_receipt_v1", "moved_sign_groups": len(witnesses), "witnesses": witnesses}


if __name__ == "__main__":
    print(json.dumps(run(), indent=2))
