"""Verify M16.4 loading feedback without changing the retained earlier review."""
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "docs/quality/m16-4/loading"


def read(name):
    return json.loads((OUT / name).read_text(encoding="utf-8-sig"))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    baseline = read("baseline.json")
    changed = [name for name, expected in baseline["files"].items() if digest(ROOT / name) != expected]
    assert not changed, changed
    old = subprocess.check_output(["git", "show", baseline["commit"] + ":docs/PROJECT_STATE.md"], cwd=ROOT)
    assert (ROOT / "docs/PROJECT_STATE.md").read_bytes().replace(b"\r\n", b"\n").startswith(old)
    for name, expected in read("fingerprints.json").items():
        assert digest(ROOT / name) == expected, "Source or build changed: " + name
    for name, count in [("native-final.json", 4), ("editmode-final.json", 21)]:
        summary = read(name)["structuredContent"]["result"]["Summary"]
        assert summary["Status"] == "Passed" and summary["FailedTests"] == 0
        assert summary["TotalTests"] == count
    build = read("build-summary.json")
    assert build["result"] == "Succeeded" and build["errors"] == 0
    audit = read("build-warning-audit.json")
    assert audit["warnings"] == build["warnings"] == 520
    assert audit["api_deprecations"] == 24
    assert audit["conv_generic_variants"] == 460 and audit["sentis_pixel_variants"] == 36
    assert audit["errors"] == 0
    player, loading = read("player/review.json"), read("player/loading-review.json")
    assert player["success"] and not player["errors"] and player["owner_profiles_unchanged"]
    assert player["fullscreen_mode"] == "FullScreenWindow"
    assert (player["width"], player["height"]) == (player["display_width"], player["display_height"])
    assert loading["enabled"] and loading["immediate"] and loading["painted_stages"] == 15
    assert loading["clock_held"] and loading["countdown_unarmed"]
    assert len(loading["captures"]) == 4
    assert not read("player/log-audit.json")["runtime_errors"]
    console = read("console-final.json")
    assert console["errors"] == console["warnings"] == 0
    assert not console["compiling"] and not console["playing"]
    result = dict(schema_version=1, id="m16_4_loading_qualification", status="pass",
                  playmode_passed=4, editmode_passed=21, actual_loading_stages_painted=4,
                  field_clock_held=True, countdown_unarmed_during_loading=True,
                  protected_files=len(baseline["files"]), changed_authority_files=changed,
                  published_history_preserved=True, build_errors=0, build_warnings=build["warnings"],
                  player_success=True, owner_profiles_unchanged=True,
                  fullscreen=[player["width"], player["height"]],
                  async_world_construction=False)
    (OUT / "qualification.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print("PASS: 21 EditMode / 4 PlayMode; four compiled loading stages, held clock, fullscreen, source/history guards.")


if __name__ == "__main__":
    main()
