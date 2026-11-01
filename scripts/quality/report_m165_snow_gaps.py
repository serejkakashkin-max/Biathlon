"""Recheck the M16.5 snow-junction and readable-gap delivery."""
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
QA = ROOT / "docs/quality/m16-5/snow-gaps"


def read(name):
    return json.loads((QA / name).read_text(encoding="utf-8-sig"))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    native_names = set()
    for name, count in [("native-snow.json", 12), ("native-ui.json", 18)]:
        result = read(name)["result"]
        summary = result["Summary"]
        assert summary["TotalTests"] == summary["PassedTests"] == count
        assert summary["FailedTests"] == summary["SkippedTests"] == 0
        assert len(result["Results"]) == count
        for row in result["Results"]:
            assert row["Status"] == "Passed" and row["Name"] not in native_names
            native_names.add(row["Name"])
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    for name, count in [("followup-core.trx", 29), ("deterministic-replay.trx", 3)]:
        counters = ET.parse(QA / name).find("t:ResultSummary/t:Counters", ns).attrib
        assert int(counters["total"]) == int(counters["passed"]) == count
        assert int(counters["failed"]) == int(counters["notExecuted"]) == 0
    before = read("snow-before.json")
    after = read("snow-after.json")
    assert before["largest_exposed_area"] > .005
    assert after["exposed_faces"] == 0 and after["largest_exposed_area"] == 0
    for name in ["snow-before.png", "snow-after.png", "snow-return.png", "snow-main.png"]:
        data = (QA / name).read_bytes()
        assert data[:8] == b"\x89PNG\r\n\x1a\n" and len(data) > 10000
    baseline = read("authority-baseline.json")
    for name, digest in baseline["files"].items():
        assert sha(ROOT / name) == digest, name
    state = (ROOT / "docs/PROJECT_STATE.md").read_bytes()
    assert hashlib.sha256(state[:baseline["state_bytes"]]).hexdigest() == baseline["state_sha256"]
    for name, digest in read("fingerprints.json").items():
        assert sha(ROOT / name) == digest, name
    build = read("build.json")
    assert build["result"] == "Succeeded" and build["errors"] == 0
    identity = read("build-identity.json")
    folder = ROOT / identity["folder"]
    assert sha(folder / "Biathlon.exe") == identity["exe_sha256"]
    for name, digest in identity["assemblies"].items():
        assert sha(folder / "Biathlon_Data/Managed" / name) == digest, name
    assert len(identity["core_dlls_identical"]) == 5 and all(identity["core_dlls_identical"].values())
    player = read("startup-player/review.json")
    assert player["success"] and player["owner_profiles_unchanged"] and not player["errors"]
    assert player["fullscreen"] and player["width"] == player["display_width"] and player["height"] == player["display_height"]
    assert player["race_loaded"] and player["countdown_numbers"] == [3, 2, 1, 0]
    assert (QA / "startup-player/exit-code.txt").read_text().strip() == "0"
    loading = read("startup-player/loading-review.json")
    assert loading["hud_hidden"] and loading["clock_held"] and loading["countdown_unarmed"] and loading["painted_stages"] == 15
    console = read("console-final.json")
    assert console["errors"] == 0 and not console["playing"] and not console["compiling"]
    print("PASS: 62 checks; Sprint snow return; all-format contact; RU/EN minute gaps; fullscreen build/startup; unchanged core/history")


if __name__ == "__main__":
    main()
