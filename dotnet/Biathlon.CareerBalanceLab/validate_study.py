"""Check the completed lab's scope and real measurements; do not rerun or synthesize races."""
import json
import pathlib
import sys

root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
study = root / "artifacts/m15-6/authoritative-race-study"
data = json.loads((study / "summary-all.json").read_text(encoding="utf-8"))
errors = []

def require(condition, message):
    if not condition:
        errors.append(message)

require(data["selection"] == "all", "Final evidence must use the complete declared case selection.")
require(data["status"] == "PASS" and data["failed"] == 0, "An authority case failed.")
require(data["completed"] == 22 and len(data["results"]) == 22, "Expected22 actual cases.")
require(data["full_races"] == 19 and data["opening_only"] == 3, "Expected19 full races plus3 measured openings.")
require(data["geometric_shots"] == 5700, "Full races must resolve5700 geometric shots.")
require(data["stat_projection_checks"] == 8580, "Every13-stat projection in every30-person case must be checked.")
require(data["deterministic_repeat"] is True, "Current-context repeated Sprint diverged.")
require(len(data["authority_assembly_sha256"]) == 4, "Missing actual loaded authority DLL hashes.")
rows = {r["Id"]: r for r in data["results"]}
opening_diagnostic_path = study / "summary-openings.json"
opening_diagnostic = json.loads(opening_diagnostic_path.read_text(encoding="utf-8")) if opening_diagnostic_path.exists() else None
opening_observations = {r["Id"]: r for r in opening_diagnostic["results"]} if opening_diagnostic else {}
diagnostic_repeats = 0
maximum_stop = max(r["MaximumStationarySeconds"] for r in rows.values())
require(maximum_stop < 30, "At least one actual skier remained below0.3 m/s for30 seconds.")
for row in rows.values():
    require(len(row["PlayerStats"]) == 13 and all(0 <= n <= 100000 for n in row["PlayerStats"]), row["Id"] + ": invalid actual source stats.")
    if not row["Id"].startswith("catalog_"):
        require(row["CreationBudgetSpent"] == 24, row["Id"] + ": unequal starting allocation.")
    if row["OpeningOnly"]:
        if "UphillTuckTicks" not in row["FirstClimb"][0]:
            observed = opening_observations.get(row["Id"])
            require(observed is not None, row["Id"] + ": missing slope-aware observation.")
            if observed is None:
                continue
            require(opening_diagnostic["authority_assembly_sha256"] == data["authority_assembly_sha256"], "Opening diagnostic used different authority assemblies.")
            require(all(data["definition_sha256"].get(k) == v for k, v in opening_diagnostic["definition_sha256"].items()), "Opening diagnostic used different resource definitions.")
            require(observed["Ticks"] == row["Ticks"] and observed["EventFnv64"] == row["EventFnv64"], row["Id"] + ": diagnostic observation changed actual authority output.")
            row = observed
            diagnostic_repeats += 1
        require(row["FirstSplitP90GapSeconds"] is not None and row["FirstSplitP90GapSeconds"] < 20, row["Id"] + ": fresh first-split p90 gap exceeds20 seconds.")
        require(len(row["FirstClimb"]) == 30 and all(a["CrestElapsedSeconds"] is not None and a["ObservedTicks"] > 0 for a in row["FirstClimb"]), row["Id"] + ": some fresh entrants did not complete the measured climb.")
        require(sum(a["UphillTuckTicks"] for a in row["FirstClimb"]) == 0, row["Id"] + ": AI chose tucked coasting on positive prior-step grade during the measured opening.")
    else:
        require(row["Shots"] == 300 and len(row["Results"]) == 30, row["Id"] + ": incomplete authoritative field.")
        require(len({a["AthleteId"] for a in row["Results"]}) == 30, row["Id"] + ": duplicated athlete.")
        require(all(0 <= a["Penalties"] <= 10 for a in row["Results"]), row["Id"] + ": invalid shot/penalty count.")
require(rows["junior_controlled_full_effort"]["PlayerLeadSeconds"] < 60, "A novice with upper-skill control won by an uncontested minute.")
winners = []
for seed in (123, 124, 125):
    cases = [rows["world_cup_debut_" + preset + ("" if seed == 123 else "_seed_" + str(seed))] for preset in ("balanced", "skier", "shooter")]
    elapsed = [(r["Id"], next(a["ElapsedMilliseconds"] for a in r["Results"] if a["AthleteId"] == r["PlayerId"])) for r in cases]
    winners.append({"race_seed": seed, "fastest_case": min(elapsed, key=lambda a: a[1])[0], "times_ms": dict(elapsed)})
for name in ("metadata-invariance", "nonneutral-player-ai-readiness"):
    require(json.loads((study / (name + ".json")).read_text(encoding="utf-8"))["status"] == "PASS", name + " failed.")
result = {
    "schema_version": 1,
    "id": "m15_6_authoritative_race_study_validation_v1",
    "status": "PASS" if not errors else "FAIL",
    "errors": errors,
    "actual_cases": data["completed"],
    "full_races": data["full_races"],
    "geometric_shots": data["geometric_shots"],
    "actual_ticks": data["actual_ticks"],
    "additional_diagnostic_opening_repeats": diagnostic_repeats,
    "additional_diagnostic_ticks": opening_diagnostic["actual_ticks"] if diagnostic_repeats else 0,
    "maximum_stationary_seconds": maximum_stop,
    "matched_wc_winners": winners,
    "limitations": "Three fixed race seeds, not a universal preset win-rate or format suitability claim. MassStart samples stop after the first actual timing station; full MassStart determinism belongs to the separate course-clock fixture.",
}
(study / "validation.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(result, ensure_ascii=False, indent=2))
sys.exit(1 if errors else 0)
