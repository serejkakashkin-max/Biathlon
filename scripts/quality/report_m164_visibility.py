"""Check native continuity evidence and summarize matched standalone scenery measurements."""
from pathlib import Path
import hashlib
import json
import statistics

ROOT = Path(__file__).resolve().parents[2]
QA = ROOT / "docs/quality/m16-4/visibility"


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def percentile(values, fraction):
    values = sorted(values)
    return values[round((len(values) - 1) * fraction)]


def compare(before, after):
    assert before["success"] and after["success"] and not before["errors"] and not after["errors"]
    assert before["phase"] == "before" and after["phase"] == "after"
    for key in ["unity", "gpu", "cpu", "api", "trees", "flags", "join_vertices", "covered_curtain_faces"]:
        assert before[key] == after[key], key
    assert len(before["cases"]) == len(after["cases"]) == 6
    cases = []
    for b, a in zip(before["cases"], after["cases"]):
        for key in ["id", "tier", "weather", "camera", "target", "tick"]:
            assert b[key] == a[key], key
        assert len(b["samples"]) == len(a["samples"]) == 240
        assert a["visible_trees"] >= b["visible_trees"] and a["visible_crowd"] >= b["visible_crowd"]
        assert a["forest_shadow_triangles"] == b["forest_shadow_triangles"]
        assert a["crowd_shadow_triangles"] == b["crowd_shadow_triangles"]
        result = {"id": a["id"], "tier": a["tier"], "samples_per_phase": 240}
        for key in ["visible_trees", "near_trees", "mid_trees", "far_trees", "visible_crowd", "near_crowd", "far_crowd", "silhouette_crowd", "visible_dressing", "forest_triangles", "crowd_triangles", "dressing_triangles", "forest_shadow_triangles", "crowd_shadow_triangles"]:
            result[key] = {"before": b[key], "after": a[key]}
        for metric in ["wall_ms", "cpu_frame_ms", "cpu_main_ms", "cpu_render_ms", "gpu_ms", "gpu_camera_ms", "draw_calls", "triangles", "gc_bytes", "used_memory_bytes"]:
            result[metric] = {}
            for phase, case in [("before", b), ("after", a)]:
                values = [s[metric] for s in case["samples"] if s[metric] >= 0]
                result[metric][phase] = {"median": statistics.median(values), "p95": percentile(values, .95), "max": max(values)} if values else None
        cases.append(result)
    return {"schema_version": 1, "id": "m16_4_visibility_matched_measurement", "scope": after["scope"], "gpu": after["gpu"], "cpu": after["cpu"], "cases": cases}


def main():
    for name, count in [("native-playmode.json", 41), ("native-editmode.json", 1), ("native-legacy-crowd.json", 1)]:
        s = read(QA / name)["result"]["Summary"]
        assert s["TotalTests"] == s["PassedTests"] == count and s["FailedTests"] == s["SkippedTests"] == 0
    red = read(QA / "native-reproduced.json")["result"]["Summary"]
    assert red["FailedTests"] == 3 and red["PassedTests"] == 1
    build = read(QA / "build.json")
    assert build["result"] == "Succeeded" and build["errors"] == 0
    comparison = compare(read(QA / "player-before/review.json"), read(QA / "player-after/review.json"))
    assert comparison == read(QA / "comparison.json")
    for case in comparison["cases"]:
        assert case["near_trees"]["after"] <= case["near_trees"]["before"]
        assert case["near_crowd"]["after"] <= case["near_crowd"]["before"]
    for phase in ["player-before", "player-after"]:
        for case in comparison["cases"]:
            png = (QA / phase / (case["id"] + "-" + case["tier"] + ".png")).read_bytes()
            assert png[:8] == b"\x89PNG\r\n\x1a\n" and len(png) > 100000
    player = read(QA / "player-qualified/review.json")
    assert player["success"] and player["owner_profiles_unchanged"] and not player["errors"]
    assert player["fullscreen"] and player["width"] == player["display_width"] and player["height"] == player["display_height"]
    assert player["race_loaded"] and player["countdown_numbers"] == [3, 2, 1, 0]
    authority = read(QA / "authority.json")
    assert authority["protected_unchanged"] == 671 and authority["environment_other_members_identical"]
    assert len(authority["core_dlls_identical"]) == 5 and all(authority["core_dlls_identical"].values())
    console = read(QA / "console-final.json")
    assert console["errors"] == console["warnings"] == 0 and not console["playing"] and not console["compiling"]
    history = read(QA / "state-history.json")
    prefix = (ROOT / "docs/PROJECT_STATE.md").read_bytes()[:history["baseline_bytes"]]
    assert hashlib.sha256(prefix).hexdigest() == history["baseline_sha256"]
    fingerprints = read(QA / "fingerprints.json")
    for name, expected in fingerprints.items():
        assert hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == expected, name
    print(f"PASS: red reproduced,43 native cases,6 matched player cases/2880 samples,12 world captures,fullscreen startup,{len(fingerprints)} source/resource/binary guards")


if __name__ == "__main__":
    main()
