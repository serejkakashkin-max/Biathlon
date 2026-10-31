"""Verify retained M16.4 receipts and the delivered local test build."""
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "docs/quality/m16-4"


def read(name):
    return json.loads((OUT / name).read_text(encoding="utf-8-sig"))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    baseline = read("baseline.json")
    changed = [name for name, value in baseline["files"].items()
               if digest(ROOT / name) != value]
    assert set(changed) == {
        "Assets/Resources/Biathlon/athlete_ground_contact_v1.json",
        "Assets/Resources/Biathlon/athlete_rifle_v1.json",
    }, changed
    old = subprocess.check_output([
        "git", "show", baseline["commit"] + ":docs/PROJECT_STATE.md"], cwd=ROOT)
    current = (ROOT / "docs/PROJECT_STATE.md").read_bytes().replace(b"\r\n", b"\n")
    assert current.startswith(old), "Published state history is not an unchanged prefix"
    for name, value in read("fingerprints.json").items():
        assert digest(ROOT / name) == value, "Source/build fingerprint changed: " + name

    edit = read("final-editmode.json")["structuredContent"]["result"]
    play = read("final-playmode.json")["structuredContent"]["result"]
    visual = read("final-visual.json")["structuredContent"]["result"]
    for receipt in (edit, play, visual):
        assert receipt["Summary"]["Status"] == "Passed"
        assert receipt["Summary"]["FailedTests"] == 0
    assert edit["Summary"]["TotalTests"] == 40
    assert play["Summary"]["TotalTests"] == 33
    assert visual["Summary"]["TotalTests"] == 2
    assert set(row["Name"] for row in visual["Results"]).issubset(
        row["Name"] for row in play["Results"])

    shader = read("shader-final.json")
    assert shader["supported"] and shader["errors"] == 0
    build = read("build-summary.json")
    assert build["result"] == "Succeeded" and build["errors"] == 0
    warnings = read("build-warning-audit.json")
    assert warnings["warnings"] == build["warnings"] == 520
    assert warnings["api_deprecations"] == 24
    assert warnings["conv_generic_variants"] == 460
    assert warnings["sentis_pixel_variants"] == 36
    assert warnings["new_impact_shader_messages"] == warnings["errors"] == 0
    player = read("player/review.json")
    assert player["success"] and not player["errors"] and player["owner_profiles_unchanged"]
    assert player["fullscreen"] and player["fullscreen_mode"] == "FullScreenWindow"
    assert (player["width"], player["height"]) == (player["display_width"], player["display_height"])
    assert not read("player/log-audit.json")["runtime_errors"]
    shooting = read("player-shooting/review.json")
    assert shooting["success"] and not shooting["errors"]
    assert shooting["audio_hits"] == 5 and shooting["audio_misses"] == 0
    assert shooting["audio_other_shots"] > 20
    assert not read("player-shooting/log-audit.json")["runtime_errors"]
    console = read("console-final.json")
    assert console["errors"] == console["warnings"] == 0
    assert not console["compiling"] and not console["playing"]
    for width, locale, maximum in [(1600, "ru", .10), (1280, "ru", .10), (1024, "en", .17)]:
        coverage = float((OUT / f"views/hud-{width}-{locale}-coverage.txt").read_text())
        assert coverage < maximum
    report = dict(schema_version=1, id="m16_4_qualification", status="pass",
                  editmode_passed=40, playmode_passed=33, latest_visual_passed=2,
                  visual_cases_included_in_playmode=True,
                  protected_baseline_files=len(baseline["files"]),
                  authorized_visual_recipe_changes=changed, core_cs_unchanged=True,
                  published_state_history_preserved=True, build_errors=0,
                  build_warnings=build["warnings"], player_success=True,
                  desktop_fullscreen=[player["width"], player["height"]],
                  owner_profiles_unchanged=True,
                  actual_player_shots=shooting["audio_hits"]+shooting["audio_misses"],
                  actual_other_shots=shooting["audio_other_shots"],
                  mechanical_impulse_model=False)
    (OUT / "qualification.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print("PASS: 40 EditMode / 33 PlayMode; shader, fullscreen build, authority and history guards.")


if __name__ == "__main__":
    main()
