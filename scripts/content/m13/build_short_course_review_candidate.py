"""Rebuild the detached Woodland short-course review package from tracked recipes.

The strict pre-bind failure is an input to measured guard binding. Every later
validation must pass. Nothing is written under Unity Assets or ProjectSettings.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys


HERE = Path(__file__).resolve().parent
PROJECT = HERE.parents[2]
VENUE = "Biathlon/Venues/woodland_basin_v1"
POLICY = "Biathlon/Venues/production_venue_validation_v1"
RECIPE = HERE / "recipes/woodland_basin_short_course_recipe_v1.json"
FORMATS = HERE / "recipes/woodland_basin_short_formats_recipe_v1.json"
ENVIRONMENT = HERE / "recipes/woodland_basin_short_environment_recipe_v1.json"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def run(*args, expected=(0,)):
    result = subprocess.run([str(item) for item in args], cwd=PROJECT,
                            capture_output=True, text=True, encoding="utf-8", errors="replace")
    if result.returncode not in expected:
        raise RuntimeError(f"Stage failed ({result.returncode}): {args}\n{result.stdout}\n{result.stderr}")
    return result


def copy_validation_dependencies(destination):
    source = PROJECT / "Assets/Resources"
    names = ("Biathlon/occupancy_provisional_v1.json",
             POLICY + ".json",
             "Biathlon/Stadium/G2Grandstand/Grandstand.prefab",
             "Biathlon/Stadium/G2Event/Event.prefab")
    for name in names:
        target = destination / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source / name, target)


def validate(resources, report, expected):
    result = run("dotnet", "run", "--project", PROJECT / "dotnet/Biathlon.BalanceLab/Biathlon.BalanceLab.csproj",
                 "-c", "Release", "--", "venue-validate", resources,
                 VENUE + "/manifest", POLICY, report, expected=expected)
    data = json.loads(report.read_text(encoding="utf-8"))
    if data.get("id") != "woodland_basin_v1" or data.get("policy_id") != "production_venue_validation_v1":
        raise ValueError("Native validation used the wrong venue or policy")
    return data, result.stdout.strip()


def build(output):
    output = output.resolve()
    if not output.is_relative_to(PROJECT / "artifacts") or output.exists():
        raise ValueError("A fresh output directory under project artifacts is required")
    output.mkdir(parents=True)
    base, formats, final = (output / name for name in ("base", "formats", "final"))
    run(sys.executable, HERE / "generate_venue01.py", "--recipe", RECIPE, "--output-root", base)
    run(sys.executable, HERE / "generate_format_branches.py", "--base-root", base,
        "--recipe", FORMATS, "--output-root", formats)
    run(sys.executable, HERE / "verify_format_branches.py", "--base-root", base,
        "--output-root", formats)
    resources = formats / "resources"
    copy_validation_dependencies(resources)
    before, _ = validate(resources, output / "pre-bind-validation.json", (1,))
    issues = before.get("issues", [])
    allowed = {"junction.guard_extent", "junction.unprotected_crossing", "range.shared_entry_gate"}
    if not issues or any(issue.get("code") not in allowed for issue in issues):
        raise ValueError("Pre-bind diagnostics contain unexpected failures")
    run(sys.executable, HERE / "bind_compact_range.py", "--resource-root", resources,
        "--venue-id", "woodland_basin_v1", "--recipe", RECIPE,
        "--report", output / "pre-bind-validation.json")
    bound, _ = validate(resources, output / "post-bind-validation.json", (0,))
    if not bound.get("static_checks_pass") or bound.get("issues"):
        raise ValueError("Bound range did not pass the strict validator")
    apron = json.loads((resources / VENUE / "shared_apron.json").read_text(encoding="utf-8"))
    if apron.get("parallel_post_shoot_departures") is not True or apron.get("penalty_world_merge_junctions") != ["range_rejoin"]:
        raise ValueError("Bound range lost parallel departure policy")
    run(sys.executable, HERE / "generate_environment.py", "--output-root", formats,
        "--recipe", ENVIRONMENT)
    run(sys.executable, HERE / "verify_environment.py", "--output-root", formats)
    dressing_recipe = output / "resolved-dressing.json"
    run(sys.executable, HERE / "author_short_course_dressing.py",
        "--source-resources", resources, "--output-recipe", dressing_recipe)
    run(sys.executable, HERE / "dress_woodland_course.py", "--source-resources", resources,
        "--recipe", dressing_recipe, "--output-root", final)
    copy_validation_dependencies(final / "resources")
    dressed, _ = validate(final / "resources", final / "venue-validation.json", (0,))
    if not dressed.get("static_checks_pass") or dressed.get("issues"):
        raise ValueError("Dressed venue did not pass the strict validator")
    run(sys.executable, HERE / "verify_environment.py", "--output-root", final)
    environment = json.loads((final / "resources" / VENUE / "environment.json").read_text(encoding="utf-8"))
    if len(environment["assets"]) < 1400 or len(environment["mesh_assets"]) < 40:
        raise ValueError("Roadside dressing was lost from the review package")
    receipt = {"schema_version": 1, "id": "woodland_short_course_review_candidate_v1",
               "status": "STATIC_PASS_RUNTIME_VISUAL_REVIEW_REQUIRED",
               "pre_bind_diagnostics": len(issues), "bound_diagnostics": len(bound["issues"]),
               "dressed_diagnostics": len(dressed["issues"]),
               "parallel_post_shoot_departures": True,
               "roadside_assets": len(environment["assets"]),
               "mesh_assets": len(environment["mesh_assets"]),
               "recipe_sha256": {name: sha(path) for name, path in
                                 (("course", RECIPE), ("formats", FORMATS), ("environment", ENVIRONMENT),
                                  ("resolved_dressing", dressing_recipe))},
               "final_venue_json_sha256": {p.relative_to(final / "resources").as_posix(): sha(p)
                                          for p in sorted((final / "resources" / VENUE).rglob("*.json"))}}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    return receipt


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-root", type=Path, required=True)
    args = parser.parse_args()
    result = build(args.output_root)
    print(json.dumps({key: result[key] for key in ("status", "pre_bind_diagnostics",
                    "bound_diagnostics", "dressed_diagnostics", "roadside_assets", "mesh_assets")}))
