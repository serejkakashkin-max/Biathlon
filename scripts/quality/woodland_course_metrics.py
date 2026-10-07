"""Reproduce the authored Woodland course/route geometry summary for WP-177.

This reads data only. It never modifies race tuning or presentation assets.
"""

import bisect
import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
VENUE = ROOT / "Assets/Resources/Biathlon/Venues/woodland_basin_v1"


def read(name):
    raw = (VENUE / (name + ".json")).read_bytes()
    return json.loads(raw), hashlib.sha256(raw).hexdigest()


def rounded(value):
    return round(value, 6)


def main():
    track, track_hash = read("track")
    visual, visual_hash = read("visual")
    loop = next(item for item in track["loops"] if item["id"] == "main")
    knots = next(item["knots"] for item in visual["loops"] if item["loop_id"] == "main")
    distances = [point["distance_m"] for point in knots]

    def elevation(at):
        index = min(max(bisect.bisect_right(distances, at) - 1, 0), len(knots) - 2)
        a, b = knots[index], knots[index + 1]
        fraction = (at - a["distance_m"]) / (b["distance_m"] - a["distance_m"])
        return a["y"] + fraction * (b["y"] - a["y"])

    station = 0.0
    climbing = descending = draft = passing = technical = 0.0
    slope_runs = []
    active_run = None
    zones = {}
    mismatch = 0.0
    for segment in loop["segments"]:
        length = segment["length_m"]
        grade = (segment["start_grade"] + segment["end_grade"]) / 2
        vertical = grade * length
        climbing += max(0, vertical)
        descending += min(0, vertical)
        draft += length if segment["draft_allowed"] else 0
        passing += length if segment["overtake_allowed"] else 0
        technical += length if grade < -0.04 and abs(segment["curvature_per_m"]) > 0.015 else 0
        zone = zones.setdefault(segment["semantic_zone"], [0.0, 0.0])
        zone[0] += length
        zone[1] += vertical
        mismatch = max(mismatch, abs(elevation(station + length) - elevation(station) - vertical))
        if grade < -0.02:
            if active_run is None:
                active_run = [station, 0.0, 0.0, 0.0]
            active_run[1] += length
            active_run[2] += vertical
            active_run[3] = min(active_run[3], grade)
        elif active_run is not None:
            slope_runs.append(active_run)
            active_run = None
        station += length
    if active_run is not None:
        slope_runs.append(active_run)

    loop_lengths = {item["id"]: sum(segment["length_m"] for segment in item["segments"])
                    for item in track["loops"]}
    route_lengths = {}
    for format_name in ("sprint", "pursuit", "mass_start"):
        route, _ = read(format_name + "_route")
        if route["schema_version"] == 1:
            distance = sum(loop_lengths[loop_id]
                           for lap in route["laps"] for loop_id in lap["loop_ids"])
        else:
            distance = sum(part["end_m"] - part["start_m"]
                           for lap in route["laps"] for part in lap["slices"])
        route_lengths[format_name] = rounded(distance)

    result = {
        "schema_version": 1,
        "id": "woodland_wp177_authored_course_metrics_v1",
        "track_sha256": track_hash,
        "visual_sha256": visual_hash,
        "main_length_m": rounded(station),
        "route_lengths_m": route_lengths,
        "main_climb_m": rounded(climbing),
        "main_descent_m": rounded(-descending),
        "visual_elevation_min_m": rounded(min(point["y"] for point in knots)),
        "visual_elevation_max_m": rounded(max(point["y"] for point in knots)),
        "main_draft_allowed_m": rounded(draft),
        "main_overtake_allowed_m": rounded(passing),
        "main_technical_downhill_m": rounded(technical),
        "maximum_segment_height_disagreement_m": rounded(mismatch),
        "descending_runs_over_2_percent": [
            {"start_m": rounded(start), "length_m": rounded(length),
             "drop_m": rounded(-drop), "steepest_grade": rounded(steepest)}
            for start, length, drop, steepest in slope_runs
        ],
        "semantic_zones": {
            name: {"length_m": rounded(values[0]), "net_rise_m": rounded(values[1])}
            for name, values in sorted(zones.items())
        },
    }
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
