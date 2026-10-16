"""Compare retained native player samples; unavailable counters stay unavailable."""
import json
import math
import statistics
import sys
from pathlib import Path


def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def describe(values):
    values = sorted(x for x in values if math.isfinite(x) and x >= 0)
    if not values:
        return None
    return {"valid": len(values), "mean": statistics.mean(values),
            "median": statistics.median(values), "p95": values[math.ceil(len(values) * .95) - 1],
            "maximum": max(values)}


def compare(before, after):
    for phase, receipt in (("before", before), ("after", after)):
        if not receipt["success"] or receipt["errors"] or receipt["phase"] != phase or len(receipt["cases"]) != 6:
            raise ValueError("Native qualification incomplete: " + phase)
    for key in ("unity", "cpu", "gpu", "api", "trees"):
        if before[key] != after[key]:
            raise ValueError("Unmatched workload: " + key)
    cases = []
    for first, last in zip(before["cases"], after["cases"]):
        for key in ("id", "tier", "tick", "camera", "target", "weather"):
            if first[key] != last[key]:
                raise ValueError("Unmatched case: " + key)
        case = {key: first[key] for key in ("id", "tier", "tick")}
        for phase, source in (("before", first), ("after", last)):
            samples = source["samples"]
            if len(samples) != 240 or not any(x["draw_calls"] > 0 and x["triangles"] > 0 for x in samples):
                raise ValueError("Missing actual rendered workload")
            case[phase] = {key: describe([x[key] for x in samples]) for key in (
                "wall_ms", "cpu_frame_ms", "cpu_main_ms", "cpu_render_ms", "cpu_wait_ms",
                "gpu_ms", "gpu_camera_ms", "draw_calls", "triangles", "gc_bytes", "used_memory_bytes")}
            case[phase]["throughput_fps"] = 1000 / case[phase]["wall_ms"]["mean"]
        case["change_percent"] = {}
        for key in ("wall_ms", "cpu_main_ms", "gpu_ms", "draw_calls", "triangles", "used_memory_bytes"):
            a, b = case["before"][key], case["after"][key]
            case["change_percent"][key] = (b["mean"] / a["mean"] - 1) * 100 if a and b and a["mean"] > 0 else None
        cases.append(case)
    return {"schema_version": 1, "id": "m16_1_native_player_performance", "status": "MEASURED",
            "method": "Sequential same-binary 1920x1080 URP offscreen production camera; 30 real athletes; frozen identical authority ticks. Legacy shoulders without national flags versus natural shoulders with flags. Common apron clipping optimization in both runs. 90 warmup + 240 measured frames per camera/tier. No HUD/swapchain, not ordinary race FPS.",
            "quality_labels": {"high": "Ultra", "balanced": "High", "low": "Performance"},
            "unavailable": "null means unsupported/stale counter, never zero GPU cost",
            "device": {key: before[key] for key in ("unity", "cpu", "gpu", "api", "trees")},
            "cases": cases}


if __name__ == "__main__":
    if len(sys.argv) != 4:
        raise SystemExit("Usage: m161_player_metrics.py before/review.json after/review.json output.json")
    report = compare(read(sys.argv[1]), read(sys.argv[2]))
    Path(sys.argv[3]).write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    for case in report["cases"]:
        print(case["id"], case["tier"], "FPS", round(case["before"]["throughput_fps"], 1), "->", round(case["after"]["throughput_fps"], 1), "change", case["change_percent"])
