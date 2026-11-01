"""Validate the archived M16.5 result/distance/night-scene delivery."""
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
QA = ROOT / "docs/quality/m16-5"


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def main():
    for file, count in [("m165_native_final.json", 10), ("m165_regression.json", 11)]:
        summary = read(QA / file)["result"]["Summary"]
        assert summary["TotalTests"] == summary["PassedTests"] == count
        assert summary["FailedTests"] == summary["SkippedTests"] == 0
    core = read(QA / "core-summary.json")
    assert core["total"] == core["passed"] == 9 and core["failed"] == 0
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    counters = ET.parse(QA / "core-final.trx").find("t:ResultSummary/t:Counters", ns).attrib
    assert int(counters["total"]) == int(counters["passed"]) == 9 and int(counters["failed"]) == 0
    assert read(QA / "m165_red_results.json")["result"]["Summary"]["FailedTests"] == 1
    assert read(QA / "m165_lap_red.json")["result"]["Summary"]["FailedTests"] == 3
    for discipline, total in [("sprint", 10), ("pursuit", 12.5), ("mass_start", 15)]:
        labels = (QA / (discipline + "-checkpoint-labels.txt")).read_text().splitlines()
        distances = [float(t.replace(" KM", "")) for t in labels]
        assert len(distances) >= 8 and all(a < b for a, b in zip(distances, distances[1:]))
        assert 0 < distances[0] < distances[-1] <= total
    mass = (QA / "mass_start-checkpoint-labels.txt").read_text()
    assert all(str(k) + ".00 KM" in mass for k in [3, 6, 9, 12])
    for file in ["results-ru.png", "results-en-large.png", "crowd-evening-lit.png", "crowd-evening-unlit.png"]:
        data = (QA / file).read_bytes()
        assert data[:8] == b"\x89PNG\r\n\x1a\n" and len(data) > 10000
    build = read(QA / "build.json")
    assert build["result"] == "Succeeded" and build["errors"] == 0
    evening = read(QA / "evening-player/review.json")
    assert evening["success"] and not evening["errors"] and evening["lamps_on"]
    assert len(evening["cases"]) == 6 and all(len(case["samples"]) == 240 for case in evening["cases"])
    assert (QA / "evening-player/exit-code.txt").read_text().strip() == "0"
    player = read(QA / "startup-player/review.json")
    assert player["success"] and player["owner_profiles_unchanged"] and not player["errors"]
    assert player["fullscreen"] and player["width"] == player["display_width"] and player["height"] == player["display_height"]
    assert player["race_loaded"] and player["countdown_numbers"] == [3, 2, 1, 0]
    assert (QA / "startup-player/exit-code.txt").read_text().strip() == "0"
    loading = read(QA / "startup-player/loading-review.json")
    assert loading["hud_hidden"] and loading["clock_held"] and loading["painted_stages"] == 15
    authority = read(QA / "authority.json")
    assert authority["core_sources_unchanged"] and len(authority["core_dlls_identical"]) == 5 and all(authority["core_dlls_identical"].values())
    console = read(QA / "console-final.json")
    assert console["errors"] == console["warnings"] == 0 and not console["playing"] and not console["compiling"]
    history = read(QA / "state-history.json")
    assert hashlib.sha256((ROOT / "docs/PROJECT_STATE.md").read_bytes()[:history["baseline_bytes"]]).hexdigest() == history["baseline_sha256"]
    for path, digest in read(QA / "fingerprints.json").items():
        assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == digest, path
    print("PASS:30checks;10row results;official cumulative splits;lit scenery;6compiled evening cases;fullscreen startup;unchanged authority/history")


if __name__ == "__main__":
    main()
