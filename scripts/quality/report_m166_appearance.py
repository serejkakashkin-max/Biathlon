"""Verify the archived M16.6.1 appearance candidate and its native evidence."""
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
QA = ROOT / "docs/quality/m16-6/appearance"


def read(name):
    return json.loads((QA / name).read_text(encoding="utf-8-sig"))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    for name, total in [("appearance-core.trx", 26), ("appearance-replay.trx", 3)]:
        c = ET.parse(QA / name).find("t:ResultSummary/t:Counters", ns).attrib
        assert int(c["total"]) == int(c["passed"]) == total and int(c["failed"]) == 0
    for name, total in [("native-edit.json", 12), ("native-play.json", 28)]:
        s = read(name)["result"]["Summary"]
        assert s["TotalTests"] == s["PassedTests"] == total and s["FailedTests"] == s["SkippedTests"] == 0
    for name in ["creator-headband-worn.png", "creator-headband-raised.png", "creator-headband-absent.png", "creator-no-headwear.png", *[f"rival-{i}.png" for i in range(6)]]:
        data = (QA / name).read_bytes()
        assert data.startswith(b"\x89PNG\r\n\x1a\n") and len(data) > 10000
    b = read("build.json")
    assert b["result"] == "Succeeded" and b["errors"] == 0
    identity = read("build-identity.json")
    folder = ROOT / identity["folder"]
    for name, digest in identity["files"].items():
        assert sha(folder / name) == digest, name
    for name, digest in read("fingerprints.json").items():
        assert sha(ROOT / name) == digest, name
    history = read("state-history.json")
    assert hashlib.sha256((ROOT / "docs/PROJECT_STATE.md").read_bytes()[:history["bytes"]]).hexdigest() == history["sha256"]
    p = read("startup-player/review.json")
    assert p["success"] and p["owner_profiles_unchanged"] and not p["errors"] and p["race_loaded"]
    assert p["fullscreen"] and p["countdown_numbers"] == [3, 2, 1, 0]
    assert (QA / "startup-player/exit-code.txt").read_text().strip() == "0"
    assert read("console-final.json")["errors"] == 0
    print("PASS: M16.6.1 appearance; 26 core + 3 replay + 12 Unity Edit + 28 Play; native captures; fullscreen player; source/history fingerprints")


if __name__ == "__main__":
    main()
