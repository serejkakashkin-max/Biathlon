"""Validate archived weather/night-light coverage, resource bounds and race guards."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
QA = ROOT / "docs/quality/m16-4/weather-lights"


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def main():
    native = read(QA / "native-final.json")["result"]["Summary"]
    assert native["TotalTests"] == native["PassedTests"] == 27
    assert native["FailedTests"] == native["SkippedTests"] == 0
    final = read(QA / "native-readable-final.json")["result"]["Summary"]
    assert final["TotalTests"] == final["PassedTests"] == 3 and final["FailedTests"] == 0
    core = read(QA / "core-summary.json")
    assert core["total"] == core["passed"] == 15 and core["failed"] == 0
    budget = read(QA / "illumination-budget.json")
    assert 20 <= budget["masts"] <= 96 and 2 <= budget["target_lights"] <= 8
    assert budget["shadowed_local_lights"] == budget["warm_render_allocations"] == 0
    assert budget["fixture_instanced_batches"] == 2 and budget["lightmap_bytes"] == 524288
    assert budget["minimum_course_illumination"] > .12
    catalog = read(ROOT / "Assets/Resources/Biathlon/career_weather_v1.json")["presets"]
    assert len(catalog) == 10
    assert {p["visual_preset"] for p in catalog} == {"clear", "overcast", "snowfall", "light_snow", "fog"}
    assert {p["lighting_preset"] for p in catalog} == {"clear_cold", "overcast", "dawn", "late_day", "evening"}
    for phase in ["compiled-day", "compiled-evening"]:
        result = read(QA / phase / "review.json")
        assert result["success"] and not result["errors"] and len(result["cases"]) == 6
        assert result["lamps_on"] == (phase == "compiled-evening")
        assert result["floodlight_masts"] == budget["masts"] and result["target_fill_lights"] == budget["target_lights"]
        for case in result["cases"]:
            assert len(case["samples"]) == 240 and all(s["triangles"] > 0 for s in case["samples"])
    player = read(QA / "player-qualified/review.json")
    assert player["success"] and player["owner_profiles_unchanged"] and not player["errors"]
    assert player["fullscreen"] and player["width"] == player["display_width"] and player["height"] == player["display_height"]
    assert player["countdown_numbers"] == [3, 2, 1, 0] and player["race_loaded"]
    loading = read(QA / "player-qualified/loading-review.json")
    assert loading["hud_hidden"] and loading["clock_held"] and loading["countdown_unarmed"] and loading["painted_stages"] == 15
    build = read(QA / "build.json")
    assert build["result"] == "Succeeded" and build["errors"] == 0
    console = read(QA / "console-final.json")
    assert console["errors"] == console["warnings"] == 0 and not console["playing"] and not console["compiling"]
    authority = read(QA / "authority.json")
    assert authority["core_changes"] == ["Assets/Scripts/Domain/RaceWeatherCatalog.cs"]
    assert authority["prior_weather_and_lighting_preserved"]
    history = read(QA / "state-history.json")
    prefix = (ROOT / "docs/PROJECT_STATE.md").read_bytes()[:history["baseline_bytes"]]
    assert hashlib.sha256(prefix).hexdigest() == history["baseline_sha256"]
    for path, digest in read(QA / "fingerprints.json").items():
        assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == digest, path
    for name in [p["id"] + ".png" for p in catalog] + ["evening-lamp.png", "evening-target-14.png"]:
        assert (QA / name).read_bytes()[:8] == b"\x89PNG\r\n\x1a\n", name
    print("PASS:42 unique checks;10 forecasts;whole-course lighting;compiled day/evening matrix2880frames;fullscreen startup;history and source guards")


if __name__ == "__main__":
    main()
