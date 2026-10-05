"""Bind generated compact range guards, typed apron and physical mats in a draft.

Run after the existing geometry/format generators and a native strict static
report. The report supplies measured overlaps; this data step never grants
runtime admission or substitutes for the subsequent native validator/race tests.
No Unity, artifact-specific imports, body/guard reduction or geometry rewriting.
"""
from pathlib import Path
import argparse
import copy
import hashlib
import json
import math
import re


OVERLAP = re.compile(r"covering (\w+) ([\d.]+)\.\.([\d.]+)m and (\w+) ([\d.]+)\.\.([\d.]+)m,")
STABLE_ID = re.compile(r"[A-Za-z][A-Za-z0-9_]*\Z")


def read_json(path):
    def pairs(values):
        result = {}
        for key, value in values:
            if key in result:
                raise ValueError(f"Duplicate JSON member {key!r}: {path}")
            result[key] = value
        return result
    def invalid(value):
        raise ValueError(f"Nonfinite JSON value {value}: {path}")
    return json.loads(path.read_text(encoding="utf-8-sig"), object_pairs_hook=pairs, parse_constant=invalid)


def number(value, name, minimum=0):
    if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value) or value < minimum:
        raise ValueError(f"Finite {name} >= {minimum} required")
    return value


def whole_mm(value, name):
    value = number(value, name)
    result = round(value * 1000)
    if abs(result / 1000 - value) > 1e-7:
        raise ValueError(f"Whole-millimetre {name} required")
    return result


def identity(value, name):
    if not isinstance(value, str) or not STABLE_ID.fullmatch(value):
        raise ValueError(f"Stable {name} required")
    return value


def resource_file(root, resource):
    if not isinstance(resource, str) or not resource or "\\" in resource:
        raise ValueError("A forward-slash resource ID is required")
    relative = Path(resource + ".json")
    if relative.is_absolute() or any(part in (".", "..") for part in resource.split("/")):
        raise ValueError("Resource must stay under the generated resource root")
    result = (root / relative).resolve()
    if not result.is_relative_to(root):
        raise ValueError("Resource escaped the generated root")
    return result


def native_pairs(report, lengths):
    result = {}
    for issue in report["issues"]:
        if issue.get("code") not in ("junction.guard_extent", "junction.unprotected_crossing"):
            continue
        match = OVERLAP.search(issue.get("detail", ""))
        if match is None:
            raise ValueError("Native overlap report detail cannot be parsed: " + str(issue))
        x, low, high, y, low2, high2 = match.groups()
        if x == y or x not in lengths or y not in lengths:
            raise ValueError("Native overlap must name two distinct authored loops")
        if (y, x) in result:
            raise ValueError("Native report reverses the same measured loop pair")
        ranges = result.setdefault((x, y), {x: [10**12, -1], y: [10**12, -1]})
        for loop, first, last in ((x, low, high), (y, low2, high2)):
            first, last = float(first), float(last)
            if not math.isfinite(first) or not math.isfinite(last) or not 0 <= first <= last <= lengths[loop] / 1000 + 1e-6:
                raise ValueError("Native overlap lies outside its authored loop")
            # Native details round to six decimals. One outward millimetre keeps
            # the complete computed overlap and its existing configured clearance.
            ranges[loop][0] = min(ranges[loop][0], max(0, math.floor(first * 1000) - 1))
            ranges[loop][1] = max(ranges[loop][1], min(lengths[loop], math.ceil(last * 1000) + 1))
    return result


def interval_ports(loop, low, high, region, length, closed):
    if not 0 <= low < high <= length:
        raise ValueError("A positive complete native guard interval is required")
    if high - low < length:
        center = (low + high) // 2
        return [{"id": region + "_" + loop, "anchor": {"loop_id": loop, "distance_m": center / 1000},
                 "guard_before_m": (center - low) / 1000, "guard_after_m": (high - center) / 1000}]
    if closed or length < 2:
        raise ValueError("Full-loop coverage needs two finite legal open-loop ports")
    # Retain both native open endpoints without illegal guard length == loop length.
    return [{"id": region + "_" + loop + "_start", "anchor": {"loop_id": loop, "distance_m": 0},
             "guard_before_m": 0, "guard_after_m": (length - 1) / 1000},
            {"id": region + "_" + loop + "_end", "anchor": {"loop_id": loop, "distance_m": length / 1000},
             "guard_before_m": (length - 1) / 1000, "guard_after_m": 0}]


def cropped_knots(knots, low, high):
    if not knots[0]["distance_m"] <= low < high <= knots[-1]["distance_m"]:
        raise ValueError("Complete buffered Main guard window must fit authored geometry")
    def sample(distance):
        for a, b in zip(knots, knots[1:]):
            if a["distance_m"] <= distance <= b["distance_m"]:
                t = (distance - a["distance_m"]) / (b["distance_m"] - a["distance_m"])
                return {"distance_m": distance, **{axis: a[axis] + (b[axis] - a[axis]) * t for axis in ("x", "y", "z")}}
        raise ValueError("Uncovered native Main coordinate")
    return [sample(low)] + [k for k in knots if low < k["distance_m"] < high] + [sample(high)]



def bind_exit_clearance(apron, selected, paths, lengths, half_body):
    """Bind one downstream crossing and one terminal merge per actual mat path.

    Select by exact typed path/port geometry, not the junction's display name.
    This policy does not alter any path, body, guard, mat, or visual coordinate.
    """
    half_mm = math.ceil(half_body * 1000)
    count = 0
    for lane in paths["lanes"]:
        private = lane["mat_anchor"]["loop_id"]
        stop = whole_mm(lane["mat_anchor"]["distance_m"], "mat stop")
        terminal = lane["slices"][-1]["loop_id"]
        if private == paths["main_loop_id"] or private == terminal:
            raise ValueError("A distinct private mat and terminal exit path required")
        candidates = {}
        for index, joint in enumerate(selected):
            if len(joint["ports"]) != 2:
                continue
            ports = {port["anchor"]["loop_id"]: port for port in joint["ports"]}
            if len(ports) != 2 or private not in ports:
                continue
            through = next(loop for loop in ports if loop != private)
            port = ports[private]
            at = whole_mm(port["anchor"]["distance_m"], "private crossing")
            after = whole_mm(port["guard_after_m"], "private crossing guard")
            if at <= stop:
                continue
            if through == paths["main_loop_id"]:
                if at + after + half_mm >= lengths[private]:
                    continue
            elif through == terminal:
                if at != lengths[private] or after != 0:
                    continue
            else:
                continue
            if through in candidates:
                raise ValueError("Ambiguous downstream clearance region for " + private + "/" + through)
            candidates[through] = index
        if set(candidates) != {paths["main_loop_id"], terminal}:
            raise ValueError("Each mat requires one exact downstream Main crossing and terminal exit merge: " + private)
        for through, index in candidates.items():
            apron["regions"][index]["post_shoot_exit_clearance"] = {
                "private_loop_id": private, "through_loop_id": through}
            count += 1
    if count != 2 * len(paths["lanes"]):
        raise ValueError("Complete local crossing policy coverage required")
    apron["schema_version"] = 2
    return count


def bind(resource_root, venue_id, recipe_path, report_path):
    root = Path(resource_root).resolve()
    if any(part.lower() == "assets" for part in root.parts):
        raise ValueError("Bind generated draft resources outside Assets, then perform the reviewed import")
    identity(venue_id, "venue id")
    prefix = "Biathlon/Venues/" + venue_id
    folder = root / prefix
    recipe_path, report_path = Path(recipe_path).resolve(), Path(report_path).resolve()
    recipe, report = read_json(recipe_path), read_json(report_path)
    if recipe.get("schema_version") != 1 or recipe.get("compact_range") is not True:
        raise ValueError("A versioned explicit compact_range recipe is required")
    identity(recipe.get("id"), "recipe id")
    if "post_shoot_exit_clearance" in recipe and not isinstance(recipe["post_shoot_exit_clearance"], bool):
        raise ValueError("post_shoot_exit_clearance must be an explicit boolean recipe opt-in")
    if recipe.get("venue_id", venue_id) != venue_id:
        raise ValueError("Recipe venue differs from generated venue")
    buffer = number(recipe.get("shared_apron_guard_buffer_m"), "shared apron guard buffer")
    whole_mm(buffer, "shared apron guard buffer")
    if report.get("schema_version") != 1 or report.get("id") != venue_id or not isinstance(report.get("issues"), list):
        raise ValueError("An exact venue's native strict static report is required")
    def read(name):
        return read_json(folder / (name + ".json"))
    track, visual, paths, junctions, manifest = [read(name) for name in ("track", "visual", "paths", "junctions", "manifest")]
    original = copy.deepcopy(junctions["junctions"])
    if manifest["id"] != venue_id or any(manifest[name] != prefix + "/" + target for name, target in
            (("track_resource", "track"), ("visual_resource", "visual"), ("paths_resource", "paths"), ("junctions_resource", "junctions"))):
        raise ValueError("Exact generated manifest resource bindings required")
    if visual["track_id"] != track["id"] or paths["track_id"] != track["id"]:
        raise ValueError("Exact generated track/visual/path identity required")
    body = read_json(resource_file(root, manifest["occupancy_resource"]))
    policy_resource = prefix.rsplit("/", 1)[0] + "/production_venue_validation_v1"
    policy = read_json(resource_file(root, policy_resource))
    if report.get("policy_id") != policy["id"]:
        raise ValueError("Native report must use the captured validation policy")
    body_length = number(body["body_length_m"], "body length", .001)
    body_width = number(body["body_width_m"], "body width", .001)
    half_body = (body_length + number(body["personal_clearance_m"], "personal clearance")) * .5
    mat = copy.deepcopy(recipe.get("mat_definition"))
    if not isinstance(mat, dict) or mat.get("schema_version") != 1:
        raise ValueError("Versioned physical mat_definition required")
    identity(mat.get("id"), "mat id")
    width, length, thickness, depth = [number(mat.get(key), key) for key in ("width_m", "length_m", "thickness_m", "athlete_stop_depth_m")]
    if not .1 <= width <= 10 or not .1 <= length <= 10 or not .001 <= thickness <= .1 or not 0 <= depth <= length:
        raise ValueError("Physical mat contract bounds required")
    if width < body_width or length < body_length or not body_length * .5 <= depth <= length - body_length * .5:
        raise ValueError("The unchanged athlete body must fit the authored mat stop")
    if recipe.get("athlete_stop_depth_m", depth) != depth:
        raise ValueError("Recipe athlete stop datum differs from mat_definition")
    previous_mat = manifest["range_layout"].get("mat_definition")
    if previous_mat is not None and previous_mat != mat:
        raise ValueError("Generated mat datum differs from the supplied geometry recipe")
    definitions = {identity(loop["id"], "loop id"): loop for loop in track["loops"]}
    if len(definitions) != len(track["loops"]):
        raise ValueError("Duplicate authored loops")
    lengths = {loop_id: sum(whole_mm(s["length_m"], "native edge length") for s in loop["segments"]) for loop_id, loop in definitions.items()}
    measured = native_pairs(report, lengths)
    existing = {j["id"]: j for j in original}
    if len(existing) != len(original):
        raise ValueError("Duplicate generated junction ids")
    bindings = []
    for (x, y), ranges in sorted(measured.items()):
        region = "apron_body_" + x + "_" + y
        ports = []
        for loop, (low, high) in ranges.items():
            ports.extend(interval_ports(loop, low, high, region, lengths[loop], definitions[loop]["closed"]))
        joint = {"id": region, "ports": ports}
        if region in existing:
            if existing[region] != joint:
                raise ValueError("Existing measured region differs; regenerate draft instead of replacing guards: " + region)
        else:
            junctions["junctions"].append(joint)
        bindings.append({"region_id": region, "kind": "full_swept_world_body_pair",
                         "intervals": {loop: {"low_m": low / 1000, "high_m": high / 1000} for loop, (low, high) in ranges.items()}})
    if junctions["junctions"][:len(original)] != original:
        raise ValueError("Every original guard must remain unchanged")
    main = paths["main_loop_id"]
    family = {slice_["loop_id"] for lane in paths["lanes"] for slice_ in lane["slices"]}
    if main not in family or len(paths["lanes"]) != recipe["lane_count"]:
        raise ValueError("Exact authored range path family and lane count required")
    selected = [j for j in junctions["junctions"] if len({p["anchor"]["loop_id"] for p in j["ports"]}) == 2
                and all(p["anchor"]["loop_id"] in family for p in j["ports"])]
    needed = {p["anchor"]["loop_id"] for j in selected for p in j["ports"]}
    if main not in needed:
        raise ValueError("Compact range requires captured Main incident guards")
    maps = {v["loop_id"]: v for v in visual["loops"]}
    corridors = []
    for loop in sorted(needed):
        knots = maps[loop]["knots"]
        if loop == main:
            ports = [p for j in selected for p in j["ports"] if p["anchor"]["loop_id"] == main]
            low = min(p["anchor"]["distance_m"] - p["guard_before_m"] - half_body for p in ports)
            high = max(p["anchor"]["distance_m"] + p["guard_after_m"] + half_body for p in ports)
            low = math.floor((low - buffer) * 1000) / 1000
            high = math.ceil((high + buffer) * 1000) / 1000
            knots = cropped_knots(knots, low, high)
        corridors.append({"loop_id": loop, "knots": knots})
    apron_resource = prefix + "/shared_apron"
    apron = {"schema_version": 1, "id": venue_id.removesuffix("_v1") + "_shared_apron_v1",
             "track_id": track["id"], "range_paths_id": paths["id"], "occupancy_definition_id": body["id"],
             "validation_policy_resource": policy_resource, "corridors": corridors,
             "regions": [{"junction_id": j["id"]} for j in selected]}
    if recipe.get("post_shoot_exit_clearance", False):
        bind_exit_clearance(apron, selected, paths, lengths, half_body)
    bind_departure_policy(apron, junctions, paths, track, recipe)
    manifest["range_layout"]["mat_definition"] = mat
    manifest["shared_apron_resource"] = apron_resource
    # Compute all outputs before any write: malformed report/recipe cannot partially bind.
    outputs = {folder / "junctions.json": junctions, folder / "manifest.json": manifest,
               resource_file(root, apron_resource): apron}
    encoded = {path: json.dumps(value, indent=2, allow_nan=False) + "\n" for path, value in outputs.items()}
    for path, data in encoded.items():
        path.write_text(data, encoding="utf-8")
    result = {"schema_version": 1, "id": "compact_apron_measured_guard_coverage_v1",
              "status": "COMPLETE_OVERLAP_GUARDS_TYPED_ADMISSION_REQUIRED", "original_guards_unchanged": True,
              "added_region_count": len(bindings), "added_regions": bindings,
              "input_static_report_sha256": hashlib.sha256(report_path.read_bytes()).hexdigest(),
              "junctions_sha256": hashlib.sha256((folder / "junctions.json").read_bytes()).hexdigest(),
              "warning": "These are physical guard coverage only. Exclusive semantics would serialize safe competitors; no race/runtime PASS."}
    (root.parent / "measured-guard-coverage.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return {"schema_version": 1, "id": venue_id.removesuffix("_v1") + "_compact_range_binding_v1",
            "status": result["status"], "venue_id": venue_id, "corridors": len(corridors), "regions": len(selected),
            "measured_regions": len(bindings), "original_guards_unchanged": True,
            "recipe_sha256": hashlib.sha256(recipe_path.read_bytes()).hexdigest(),
            "native_static_report_sha256": result["input_static_report_sha256"],
            "outputs_sha256": {path.relative_to(root).as_posix(): hashlib.sha256(path.read_bytes()).hexdigest() for path in encoded}}


def bind_departure_policy(apron, junctions, paths, track, recipe):
    """Bind only explicit optional traffic authority; geometry/guards stay intact."""
    key = "parallel_post_shoot_departures"
    regions_key = "penalty_world_merge_junctions"
    if key not in recipe and regions_key not in recipe:
        return
    enabled = recipe.get(key)
    if not isinstance(enabled, bool) or apron["schema_version"] != 2:
        raise ValueError("Parallel departures require an explicit boolean and schema2 exit clearance")
    regions = recipe.get(regions_key)
    if regions is not None:
        if not enabled or not isinstance(regions, list) or not 1 <= len(regions) <= 16:
            raise ValueError("A bounded explicit parallel penalty region list is required")
        if len(set(identity(item, "penalty junction id") for item in regions)) != len(regions):
            raise ValueError("Duplicate penalty junction")
        penalty = track["penalty"]["loop_id"]
        family = {s["loop_id"] for lane in paths["lanes"] for s in lane["slices"]}
        known = {j["id"]: j for j in junctions["junctions"]}
        for region in regions:
            if region not in known:
                raise ValueError("Unknown penalty junction: " + region)
            loops = {p["anchor"]["loop_id"] for p in known[region]["ports"]}
            if penalty not in loops or not loops - {penalty} or not loops <= family | {penalty}:
                raise ValueError("Penalty region must join actual penalty and paid range paths")
    apron[key] = enabled
    if regions is not None:
        apron[regions_key] = list(regions)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--resource-root", type=Path, required=True, help="Generated draft resources directory")
    parser.add_argument("--venue-id", required=True, help="Stable generated venue ID")
    parser.add_argument("--recipe", type=Path, required=True, help="Versioned compact geometry recipe")
    parser.add_argument("--report", type=Path, required=True, help="Native strict static report for these generated resources")
    args = parser.parse_args()
    try:
        result = bind(args.resource_root, args.venue_id, args.recipe, args.report)
    except (KeyError, TypeError, ValueError, OSError) as error:
        parser.exit(2, "Compact range binding rejected: " + str(error) + "\n")
    print(json.dumps(result, allow_nan=False))


if __name__ == "__main__":
    main()
