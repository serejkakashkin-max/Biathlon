"""Check archived M16.4 personal-start, loader and ski-camera qualification."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
QA = ROOT / "docs/quality/m16-4/start-loader-orbit"


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def main():
    cases = set()
    for name, count in [("camera_start_final", 13), ("start_loader_regression", 12),
                        ("broadcast_regression", 4), ("start_edit", 9)]:
        receipt = read(QA / (name + ".json"))["result"]
        s = receipt["Summary"]
        assert s["TotalTests"] == s["PassedTests"] == count
        assert s["FailedTests"] == s["SkippedTests"] == 0
        cases.update(row["Name"] for row in receipt["Results"])
    assert len(cases) == 38
    for name, failures in [("red_start_loader", 2), ("red_render_order", 1)]:
        s = read(QA / (name + ".json"))["result"]["Summary"]
        assert s["FailedTests"] == s["TotalTests"] == failures
    build = read(QA / "build.json")
    assert build["result"] == "Succeeded" and build["errors"] == 0
    player = read(QA / "player-qualified/review.json")
    assert player["success"] and player["owner_profiles_unchanged"] and not player["errors"]
    assert player["fullscreen_mode"] == "FullScreenWindow" and player["fullscreen"]
    assert player["width"] == player["display_width"] and player["height"] == player["display_height"]
    assert player["race_loaded"] and player["start_wait_skipped"] and player["countdown_numbers"] == [3, 2, 1, 0]
    loading = read(QA / "player-qualified/loading-review.json")
    assert loading["enabled"] and loading["immediate"] and loading["clock_held"]
    assert loading["hud_hidden"] and loading["countdown_unarmed"] and loading["painted_stages"] == 15
    for name in ["orbit-0.png", "orbit-90.png", "orbit-180.png"] + ["player-qualified/" + n for n in loading["captures"]]:
        png = (QA / name).read_bytes()
        assert png[:8] == b"\x89PNG\r\n\x1a\n" and len(png) > 10000, name
    authority = read(QA / "authority.json")
    assert authority["protected_source_resource_files"] == 1050 and authority["protected_unchanged"]
    assert len(authority["core_dlls_identical"]) == 5 and all(authority["core_dlls_identical"].values())
    console = read(QA / "console-final.json")
    assert console["errors"] == console["warnings"] == 0 and not console["playing"] and not console["compiling"]
    history = read(QA / "state-history.json")
    prefix = (ROOT / "docs/PROJECT_STATE.md").read_bytes()[:history["baseline_bytes"]]
    assert hashlib.sha256(prefix).hexdigest() == history["baseline_sha256"]
    for path, digest in read(QA / "fingerprints.json").items():
        assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == digest, path
    print("PASS:38 native cases;clean compiled loader;fullscreen startup;camera views;unchanged race authority and state history")


if __name__ == "__main__":
    main()
