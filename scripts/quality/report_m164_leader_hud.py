"""Validate archived leader-bib UI qualification and unchanged race authority."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
QA = ROOT / "docs/quality/m16-4/leader-hud"


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def main():
    for name, count in [("bib_native", 5), ("bib_regression", 7), ("bib_edit", 14)]:
        summary = read(QA / (name + ".json"))["result"]["Summary"]
        assert summary["TotalTests"] == summary["PassedTests"] == count
        assert summary["FailedTests"] == summary["SkippedTests"] == 0
    build = read(QA / "build.json")
    assert build["result"] == "Succeeded" and build["errors"] == 0
    for name in ["tab-ru.png", "tab-en.png", "country-en-large.png", "pre-race-ru.png"]:
        image = (QA / name).read_bytes()
        assert image[:8] == b"\x89PNG\r\n\x1a\n" and len(image) > 10000
    player = read(QA / "player-qualified/review.json")
    assert player["success"] and player["owner_profiles_unchanged"] and not player["errors"]
    assert player["fullscreen"] and player["width"] == player["display_width"] and player["height"] == player["display_height"]
    assert player["race_loaded"] and player["countdown_numbers"] == [3, 2, 1, 0]
    authority = read(QA / "authority.json")
    assert authority["protected_core_files"] == 356 and authority["core_sources_unchanged"]
    assert len(authority["core_dlls_identical"]) == 5 and all(authority["core_dlls_identical"].values())
    console = read(QA / "console-final.json")
    assert console["errors"] == console["warnings"] == 0 and not console["playing"] and not console["compiling"]
    history = read(QA / "state-history.json")
    assert hashlib.sha256((ROOT / "docs/PROJECT_STATE.md").read_bytes()[:history["baseline_bytes"]]).hexdigest() == history["baseline_sha256"]
    for path, digest in read(QA / "fingerprints.json").items():
        assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == digest, path
    assert "kit_swatch" not in (ROOT / "Assets/Scripts/Unity/Presentation/NationKitUi.cs").read_text()
    print("PASS:26 native cases;actual career TAB and country UI views;fullscreen startup;unchanged core sources/DLLs and state history")


if __name__ == "__main__":
    main()
