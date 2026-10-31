"""Verify native LOD/HUD evidence, bounded render cost and protected gameplay files."""
from pathlib import Path
import hashlib
import json
import statistics

ROOT = Path(__file__).resolve().parents[2]
QA = ROOT / "docs/quality/m16-4/forest-ui"


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def compare(before, after):
    assert before["success"] and after["success"] and not before["errors"] and not after["errors"]
    assert before["phase"] == "before" and after["phase"] == "after"
    assert before["id"] == after["id"] == "m16_4_forest_lod_review"
    for key in ["unity", "gpu", "cpu", "api", "trees", "excluded_trees", "flags", "join_vertices", "covered_curtain_faces"]:
        assert before[key] == after[key], key
    assert len(before["cases"]) == len(after["cases"]) == 6
    cases = []
    for b, a in zip(before["cases"], after["cases"]):
        for key in ["id", "tier", "weather", "camera", "target", "tick", "visible_trees", "visible_crowd", "visible_dressing", "crowd_triangles", "crowd_shadow_triangles", "dressing_triangles"]:
            assert b[key] == a[key], key
        assert len(b["samples"]) == len(a["samples"]) == 240
        assert b["transition_trees"] == 0
        assert a["tree_submissions"] == a["visible_trees"] + a["transition_trees"] <= 2 * a["visible_trees"]
        result = {"id": a["id"], "tier": a["tier"], "samples_per_phase": 240}
        for key in ["visible_trees", "near_trees", "mid_trees", "far_trees", "transition_trees", "tree_submissions", "visible_crowd", "visible_dressing", "forest_triangles", "forest_shadow_triangles"]:
            result[key] = {"before": b[key], "after": a[key]}
        for key in ["wall_ms", "cpu_frame_ms", "cpu_main_ms", "cpu_render_ms", "gpu_ms", "gpu_camera_ms", "triangles", "draw_calls", "gc_bytes", "used_memory_bytes"]:
            result[key] = {}
            for phase, value in [("before", b), ("after", a)]:
                numbers = sorted(s[key] for s in value["samples"] if s[key] >= 0)
                result[key][phase] = {"median": statistics.median(numbers), "p95": numbers[round((len(numbers)-1)*.95)], "max": max(numbers)} if numbers else None
        cases.append(result)
    return {"schema_version": 1, "id": "m16_4_forest_lod_matched_measurement", "scope": after["scope"], "gpu": after["gpu"], "cpu": after["cpu"], "cases": cases}


def main():
    names = set()
    for file in ["forest_regression.json", "ui_green1.json", "ui_regression.json", "final_native.json", "edit_regression.json"]:
        report = read(QA / file)["result"]
        assert report["Summary"]["TotalTests"] == report["Summary"]["PassedTests"]
        assert report["Summary"]["FailedTests"] == report["Summary"]["SkippedTests"] == 0
        names.update(r["Name"] for r in report["Results"])
    assert len(names) == 79, len(names)
    for file in ["forest_red.json", "ui_red.json"]:
        assert read(QA / file)["result"]["Summary"]["FailedTests"] == 1
    assert read(QA / "build.json")["result"] == "Succeeded" and read(QA / "build.json")["errors"] == 0
    assert compare(read(QA / "player-before/review.json"), read(QA / "player-after/review.json")) == read(QA / "comparison.json")
    for phase in ["player-before", "player-after"]:
        for case in read(QA / phase / "review.json")["cases"]:
            png = QA / phase / (case["id"] + "-" + case["tier"] + ".png")
            assert png.read_bytes()[:8] == b"\x89PNG\r\n\x1a\n" and png.stat().st_size > 100000
    player = read(QA / "player-qualified/review.json")
    assert player["success"] and not player["errors"] and player["owner_profiles_unchanged"]
    assert player["fullscreen"] and player["width"] == player["display_width"] and player["height"] == player["display_height"]
    assert player["race_loaded"] and player["countdown_numbers"] == [3, 2, 1, 0]
    impact = read(QA / "authority.json")
    assert impact["protected_unchanged"] == 1026 and len(impact["core_dlls_identical"]) == 5 and all(impact["core_dlls_identical"].values())
    assert impact["changed_resources"] == ["Assets/Resources/Biathlon/UI/front_end_text_v1.json", "Assets/Resources/Biathlon/venue_visibility_v1.json"]
    history = read(QA / "state-history.json")
    assert hashlib.sha256((ROOT / "docs/PROJECT_STATE.md").read_bytes()[:history["baseline_bytes"]]).hexdigest() == history["baseline_sha256"]
    console = read(QA / "console-final.json")
    assert console["errors"] == console["warnings"] == 0 and not console["playing"] and not console["compiling"]
    for path, digest in read(QA / "fingerprints.json").items():
        assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == digest, path
    print("PASS: both symptoms reproduced;79 unique native cases;6 matched cases/2880 frame samples;unchanged visible populations;fullscreen startup;protected gameplay and state history")


if __name__ == "__main__":
    main()
