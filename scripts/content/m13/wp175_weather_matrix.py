"""Resolve WP175's single-venue inputs without changing Unity or claiming acceptance.

Tuning comes from canonical resource files. The recipe contains only IDs and
evidence requirements. Generated environment copies and race condition bindings
stay outside Assets; creating them does not import or execute a Unity case.
"""
from __future__ import annotations

import argparse
from copy import deepcopy
import hashlib
import json
from pathlib import Path
import re


ID = re.compile(r"[A-Za-z0-9_-]+\Z")
TIERS = {"low", "balanced", "high"}
WEATHER = {"clear", "overcast", "snowfall"}
SNOW = {"packed_neutral", "fresh_soft", "icy_fast", "wet_slow"}
DEFAULT_RECIPE = Path(__file__).parent / "recipes" / "woodland_weather_matrix_v1.json"


def read_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def resource_file(root: Path, resource: str) -> Path:
    if not isinstance(resource, str) or not resource.startswith("Biathlon/"):
        raise ValueError("A canonical Biathlon resource ID is required")
    if any(not ID.fullmatch(part) for part in resource.split("/")):
        raise ValueError("Unsafe resource ID")
    return root / "Assets" / "Resources" / (resource + ".json")


def catalog(entries: list[dict], key: str = "id") -> dict[str, dict]:
    if not isinstance(entries, list) or not entries:
        raise ValueError("A non-empty canonical catalog is required")
    result = {}
    for entry in entries:
        if not isinstance(entry, dict) or not ID.fullmatch(entry.get(key, "")):
            raise ValueError("Invalid canonical catalog identity")
        identity = entry[key]
        if identity in result:
            raise ValueError("Duplicate canonical catalog identity: " + identity)
        result[identity] = entry
    return result


def assert_recipe(recipe: dict) -> None:
    if recipe.get("schema_version") != 1 or recipe.get("id") != "woodland_weather_matrix_v1":
        raise ValueError("Unsupported weather matrix recipe")
    if recipe.get("venue_id") != "woodland_basin_v1":
        raise ValueError("The owner-approved production scope is Woodland only")
    policy = recipe.get("evidence_policy", {})
    expected = {
        "capture_tool": "screenshot-game-view",
        "physical_condition_inputs_must_be_explicit": True,
        "presentation_weather_does_not_change_authoritative_wind_or_snow": True,
        "synthetic_frame_timings_qualify_performance": False,
        "hidden_player_qualifies_sustained_fps_or_gpu": False,
        "planned_matrix_qualifies_wp175": False,
    }
    if policy != expected:
        raise ValueError("Evidence policy cannot turn a plan or hidden render into performance acceptance")
    rows = recipe.get("visual_rows", [])
    expected_pairs = {(w, "high") for w in WEATHER} | {("snowfall", "balanced"), ("snowfall", "low")}
    pairs, ids = set(), set()
    for row in rows:
        if set(row) != {"id", "tier", "weather_preset", "lighting_preset"}:
            raise ValueError("Visual rows may contain references, not duplicated numeric tuning")
        if not ID.fullmatch(row["id"]) or row["id"] in ids:
            raise ValueError("Invalid or duplicate visual row identity")
        pair = row["weather_preset"], row["tier"]
        if pair in pairs:
            raise ValueError("Duplicate weather/tier pair")
        pairs.add(pair)
        ids.add(row["id"])
    if pairs != expected_pairs:
        raise ValueError("All three High weathers plus snowfall Balanced/Low are required")
    profiles = recipe.get("physical_snow_profiles", [])
    if len(profiles) != 4 or set(profiles) != SNOW:
        raise ValueError("All four canonical physical snow profiles are required exactly once")
    views = catalog(recipe.get("native_view_requirements", []))
    if set(views) != {"chase_course", "technical_descent", "stadium_mass30", "broadcast_stadium", "range_prone", "range_standing"}:
        raise ValueError("Course, stadium, broadcast and both actual stance views are required")
    formats = catalog(recipe.get("race_formats", []))
    if set(formats) != {"sprint", "pursuit", "mass_start"}:
        raise ValueError("All three production formats are required")
    for identity, count in (("sprint", 40), ("pursuit", 40), ("mass_start", 30)):
        if formats[identity].get("field_count") != count:
            raise ValueError("Production field counts must respect the accepted 40/40/30 profiles")
    if formats["pursuit"].get("qualification") != "actual_sprint_result_same_snow_condition":
        raise ValueError("Pursuit needs its actual source Sprint, not fabricated gaps")


def resolve(root: Path, recipe: dict) -> tuple[dict, dict[str, dict]]:
    assert_recipe(recipe)
    root = root.resolve()
    resources = recipe["resources"]
    documents, sources = {}, []
    resource_ids = list(resources.values())
    for row in recipe["race_formats"]:
        resource_ids += [row["profile_resource"], row["route_resource"]]
    for resource in dict.fromkeys(resource_ids):
        file = resource_file(root, resource)
        documents[resource] = read_json(file)
        sources.append({"resource": resource, "path": file.relative_to(root).as_posix(), "sha256": hashlib.sha256(file.read_bytes()).hexdigest()})
    manifest = documents[resources["manifest"]]
    if manifest.get("id") != recipe["venue_id"]:
        raise ValueError("Manifest does not belong to the sole production venue")
    environment = documents[resources["environment"]]
    if environment.get("venue_id") != recipe["venue_id"]:
        raise ValueError("Environment does not belong to the sole production venue")
    weather_data = documents[resources["weather"]]
    weather = catalog(weather_data["presets"])
    lighting = catalog(documents[resources["lighting"]]["presets"])
    snow = catalog(documents[resources["snow_catalog"]]["profiles"])
    surfaces = sorted({segment["snow_profile_id"] for loop in documents[resources["track"]]["loops"] for segment in loop["segments"]})
    if not surfaces:
        raise ValueError("The production track needs explicit surface bindings")
    generated, visual = {}, []
    for row in recipe["visual_rows"]:
        preset = weather[row["weather_preset"]]
        light = lighting[row["lighting_preset"]]
        if row["weather_preset"] != "snowfall" and light["recommended_weather"] != row["weather_preset"]:
            raise ValueError("Lighting/weather recommendation mismatch")
        clone = deepcopy(environment)
        clone["id"] = recipe["venue_id"] + "_wp175_" + row["id"]
        clone["status"] = "DRAFT_WP175_NATIVE_VALIDATION_PENDING"
        clone["lighting"]["weather_preset"] = row["weather_preset"]
        clone["lighting"]["preset_id"] = row["lighting_preset"]
        relative = "environments/" + row["id"] + ".json"
        generated[relative] = clone
        scale = clone["lighting"].get("fog_distance_scale", 1)
        density = 1 if row["tier"] == "high" else weather_data[row["tier"] + "_fraction"]
        visual.append({**row, "environment_file": relative, "status": "NOT_EXECUTED",
                       "resolved_fog_start_m": preset["fog_start_m"] * scale,
                       "resolved_fog_end_m": preset["fog_end_m"] * scale,
                       "maximum_flake_candidates_before_ground_and_camera_cull": round(weather_data["max_particles"] * density * preset["snowfall"]),
                       "required_view_ids": [view["id"] for view in recipe["native_view_requirements"]]})
    physical = []
    for profile in recipe["physical_snow_profiles"]:
        canonical = snow[profile]
        relative = "conditions/" + profile + ".json"
        generated[relative] = {"schema_version": 1, "id": "woodland_wp175_" + profile + "_v1",
                               "surfaces": [{"surface_id": surface, "profile_id": profile} for surface in surfaces]}
        physical.append({"profile_id": profile, "conditions_file": relative, "status": "NOT_EXECUTED",
                         "resolved_resistance_multiplier": canonical["snow_drag_multiplier"] / canonical["glide_multiplier"],
                         "resolved_grip_multiplier": canonical["grip_multiplier"],
                         "race_formats": deepcopy(recipe["race_formats"])})
    proof = {"schema_version": 1, "id": "woodland_wp175_resolved_plan_v1", "venue_id": recipe["venue_id"],
             "status": "INPUT_PLAN_RESOLVED_NATIVE_AND_PERFORMANCE_ACCEPTANCE_OPEN",
             "source_sha256": sources, "visual_rows": visual, "physical_conditions": physical,
             "native_view_requirements": deepcopy(recipe["native_view_requirements"]),
             "evidence_policy": deepcopy(recipe["evidence_policy"]),
             "native_cases_executed": 0, "physical_races_executed": 0, "performance_qualified": False,
             "wp175_complete": False}
    return proof, generated


def write_plan(root: Path, recipe: dict, output: Path) -> dict:
    root, output = root.resolve(), output.resolve()
    assets = root / "Assets"
    if output == assets or assets in output.parents:
        raise ValueError("WP175 planning output must remain outside Assets")
    proof, generated = resolve(root, recipe)
    for name, document in {**generated, "plan.json": proof}.items():
        target = output / name
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(json.dumps(document, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    return proof


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[3])
    parser.add_argument("--recipe", type=Path, default=DEFAULT_RECIPE)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    proof = write_plan(args.project, read_json(args.recipe), args.output)
    print(json.dumps({"id": proof["id"], "status": proof["status"], "visual_rows": len(proof["visual_rows"]),
                      "physical_conditions": len(proof["physical_conditions"]), "wp175_complete": False}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
