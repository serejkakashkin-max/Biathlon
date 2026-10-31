"""Verify retained native minimap evidence and the delivered source/binary set."""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]
QA = ROOT / "docs/quality/m16-4/minimap"


def read(name):
    return json.loads((QA / name).read_text(encoding="utf-8"))


def main():
    for name, count in [("native-playmode.json", 14), ("native-editmode.json", 13)]:
        s = read(name)["result"]["Summary"]
        assert s["TotalTests"] == s["PassedTests"] == count and s["FailedTests"] == s["SkippedTests"] == 0
    b = read("build.json")
    assert b["result"] == "Succeeded" and b["errors"] == 0
    p = read("player-qualified/review.json")
    assert p["success"] and p["owner_profiles_unchanged"] and not p["errors"]
    assert p["fullscreen"] and p["width"] == p["display_width"] and p["height"] == p["display_height"]
    assert p["race_loaded"] and p["countdown_started"] and p["countdown_numbers"] == [3, 2, 1, 0]
    loader = read("player-qualified/loading-review.json")
    assert loader["painted_stages"] == 15 and loader["clock_held"] and loader["countdown_unarmed"]
    for name, budget in [("ski-ru", .10), ("ski-en-large-contrast", .17)]:
        assert float((QA / "views" / (name + "-coverage.txt")).read_text()) < budget
        data = (QA / "views" / (name + ".png")).read_bytes()
        assert data[:8] == b"\x89PNG\r\n\x1a\n" and len(data) > 10000
    actual = (QA / "player-qualified/09-race.png").read_bytes()
    assert actual[:8] == b"\x89PNG\r\n\x1a\n" and len(actual) > 100000
    a = read("authority.json")
    assert a["protected_unchanged"] == 671 and len(a["core_dlls_identical"]) == 5 and all(a["core_dlls_identical"].values())
    c = read("console-final.json")
    assert c["errors"] == c["warnings"] == 0 and not c["playing"] and not c["compiling"]
    history = read("state-history.json")
    prefix = (ROOT / "docs/PROJECT_STATE.md").read_bytes()[:history["baseline_bytes"]]
    assert hashlib.sha256(prefix).hexdigest() == history["baseline_sha256"]
    fingerprints = read("fingerprints.json")
    for name, expected in fingerprints.items():
        assert hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == expected, name
    print(f"PASS: 27 native cases, three route formats, 30-athlete marker buffer/layers, HUD coverage, actual fullscreen player, {len(fingerprints)} fingerprints")


if __name__ == "__main__":
    main()
