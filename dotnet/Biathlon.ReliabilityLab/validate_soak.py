"""Independent artifact validator; no simulation and no repair of failed evidence."""
import collections
import hashlib
import json
from pathlib import Path
import sys


def sha(text):
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def require(ok, message):
    if not ok:
        raise AssertionError(message)


def validate(folder):
    rows = json.loads((folder / "weeks.json").read_text(encoding="utf-8-sig"))
    require(len(rows) == 156, "Missing full weekly career chain")
    previous = None
    formats = collections.Counter()
    ticks = shots = races = 0
    for ordinal, row in enumerate(rows):
        season, week = ordinal // 52 + 1, ordinal % 52 + 1
        require((row["season"], row["week"]) == (season, week), "Skipped/repeated season/week")
        if previous:
            require(previous["after_sha256"] == row["before_sha256"], "Save chain diverged")
        previous = row
        require(90 <= row["world_count"] <= 180, "World population out of authored bounds")
        require(row["history_count"] == (ordinal + 1) // 52, "History count mismatch")
        if not row["event_id"]:
            require(row["authority_ticks"] == row["shots"] == row["event_hash"] == 0, "Non-race week invented authority")
            continue
        record = json.loads((folder / f"race-{season}-{week}.json").read_text(encoding="utf-8-sig"))
        payload = record["authority_result_json"]
        require(sha(payload) == row["authority_sha256"] == record["authority_sha256"], "Altered authority payload")
        authority = json.loads(payload)
        require(len(authority["rows"]) == len(record["finishes"]) == 30, "Incomplete actual authority field")
        require(record["calendar_event_id"] == row["event_id"], "Mismatched race event")
        require(len({r["athlete_id"] for r in record["finishes"]}) == 30, "Duplicate entrant identity")
        require(row["authority_ticks"] > 0, "No actual authority ticks")
        bouts = 2 if row["format"] == "Sprint" else 4
        require(all(len(f["details"]["closed_masks"]) == len(f["details"]["stances"]) == bouts for f in record["finishes"]), "Missing athlete bout details")
        require(row["shots"] == 30 * bouts * 5, "Incomplete geometric shot accounting")
        for finish in record["finishes"]:
            misses = sum(5 - int(mask).bit_count() for mask in finish["details"]["closed_masks"])
            require(misses == finish["penalties"], "Penalty count disagrees with actual target closures")
        formats[row["format"]] += 1
        races += 1
        ticks += row["authority_ticks"]
        shots += row["shots"]
    require(races == 48 and formats == {"Sprint": 18, "Pursuit": 18, "MassStart": 12}, "Incorrect actual production event schedule")
    require(sum(r["rejected_write"] for r in rows) == 4, "Missing failed-write retry cases")
    require(sum(r["abandoned_retry"] for r in rows) == 3, "Missing abandon/retry cases")
    summary_file = folder / "summary.json"
    if summary_file.exists():
        summary = json.loads(summary_file.read_text(encoding="utf-8-sig"))
        require(summary["status"] == "PASS", "Run did not pass its integration assertions")
        require(summary["authority_ticks"] == ticks and summary["geometric_shots"] == shots, "Summary accounting mismatch")
        require(summary["final_save_sha256"] == rows[-1]["after_sha256"], "Final save is not the published 156-week chain")
    return {"schema_version": 1, "id": "m16_soak_independent_verification_v1", "status": "PASS", "weeks": len(rows), "authentic_race_payloads": races, "formats": dict(formats), "authority_ticks": ticks, "geometric_shots": shots, "final_save_sha256": rows[-1]["after_sha256"], "summary_verified": summary_file.exists()}


if __name__ == "__main__":
    directory = Path(sys.argv[1]).resolve()
    result = validate(directory)
    (directory / "independent-verification.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, indent=2))
