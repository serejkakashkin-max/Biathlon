"""Summarize the real Windows-player probe and its matched stress fixture."""

import argparse
import json
import math
from pathlib import Path
from statistics import mean


def distribution(values):
    values = sorted(value for value in values if value >= 0 and math.isfinite(value))
    if not values:
        return None
    return {
        "n": len(values), "mean": round(mean(values), 3),
        "p95": round(values[math.ceil(.95 * len(values)) - 1], 3),
        "p99": round(values[math.ceil(.99 * len(values)) - 1], 3),
        "max": round(values[-1], 3),
    }


def summarize(samples):
    if not samples:
        return None
    frame_times = [sample["wall_ms"] for sample in samples]
    # Use complete rolling one-second windows, including consecutive stalls.
    left = 0
    interval = 0.0
    minimum_fps = math.inf
    for right, milliseconds in enumerate(frame_times):
        interval += milliseconds
        while left < right and interval - frame_times[left] >= 1000:
            interval -= frame_times[left]
            left += 1
        if interval >= 1000:
            minimum_fps = min(minimum_fps, 1000 * (right - left + 1) / interval)

    return {
        "frames": len(samples), "throughput_fps": round(1000 / mean(frame_times), 2),
        "min_rolling_second_fps": round(minimum_fps, 2) if math.isfinite(minimum_fps) else None,
        "frames_over_16_67_ms": sum(value > 1000 / 60 for value in frame_times),
        "wall_ms": distribution(frame_times),
        "tick_frames_ms": distribution([sample["step_ms"] for sample in samples if sample["step_ms"] > .05]),
        "athlete_ms": distribution([sample["athletes_ms"] for sample in samples]),
        "environment_ms": distribution([sample["environment_ms"] for sample in samples]),
        "gpu_ms": distribution([sample["rendering"]["gpu_ms"] for sample in samples]),
        "triangles": distribution([sample["rendering"]["triangles"] for sample in samples]),
    }


def review(path):
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    samples = data["samples"]
    assert not data["errors"] and data["frames"] == len(samples)
    return {
        "method": data["mode"], "gpu": data["gpu"], "cpu": data["cpu"],
        "resolution": [data["width"], data["height"]], "field": data["athletes"],
        "final_tick": data["final_tick"], "final_distance_m": data["final_player_distance"],
        "all": summarize(samples),
        "skiing": summarize([s for s in samples if not s["shooting"]]),
        "range": summarize([s for s in samples if s["shooting"]]),
    }


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("profiles", type=Path, nargs="+")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = {"schema_version": 1, "id": "m167_pace_race_frame_summary", "runs": {
        path.parent.name: review(path) for path in args.profiles
    }}
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
