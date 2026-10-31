"""Validate M16.4 coaches' retained native evidence and the delivered review build."""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]
QA = ROOT / "docs/quality/m16-4/coaches"


def read(name):
    return json.loads((QA / name).read_text(encoding="utf-8"))


def main():
    for name, count in [("native-playmode.json", 34), ("native-editmode.json", 1)]:
        summary = read(name)["result"]["Summary"]
        assert summary["TotalTests"] == count and summary["PassedTests"] == count
        assert summary["FailedTests"] == 0 and summary["SkippedTests"] == 0
    build = read("build.json")
    assert build["result"] == "Succeeded" and build["errors"] == 0
    player = read("player-qualified/review.json")
    assert player["success"] and player["owner_profiles_unchanged"]
    assert player["race_loaded"] and player["countdown_started"]
    assert player["fullscreen"] and player["width"] == player["display_width"]
    assert player["height"] == player["display_height"]
    assert player["countdown_numbers"] == [3, 2, 1, 0] and not player["errors"]
    loader = read("player-qualified/loading-review.json")
    assert loader["painted_stages"] == 15 and loader["clock_held"] and loader["countdown_unarmed"]
    views = read("views/review.json")
    assert views["coaches"] == views["scopes"] == 12
    assert views["colliders"] == views["skinned_renderers"] == 0
    for name in ["overview", "coach-row", "face-0", "face-1", "face-3", "face-8"]:
        png = (QA / "views" / (name + ".png")).read_bytes()
        assert png[:8] == b"\x89PNG\r\n\x1a\n" and len(png) > 10000
    authority = read("authority.json")
    assert authority["unchanged"] == 669 and authority["original_environment_other_members_identical"]
    assert len(authority["core_dlls_identical"]) == 5 and all(authority["core_dlls_identical"].values())
    console = read("console-final.json")
    assert console["errors"] == console["warnings"] == 0
    assert not console["playing"] and not console["compiling"]
    history = read("state-history.json")
    prefix = (ROOT / "docs/PROJECT_STATE.md").read_bytes()[:history["baseline_bytes"]]
    assert hashlib.sha256(prefix).hexdigest() == history["baseline_sha256"]
    fingerprints = read("fingerprints.json")
    for name, expected in fingerprints.items():
        assert hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == expected, name
    print(f"PASS: 35 native tests, 12 static coaches/scopes, six native views, fullscreen player, {len(fingerprints)} source/art/binary fingerprints")


if __name__ == "__main__":
    main()
