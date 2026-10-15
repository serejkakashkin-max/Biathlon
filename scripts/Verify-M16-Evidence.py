"""Verify retained M16 source, native case counts and actual race evidence."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

root = Path(__file__).resolve().parents[1]
quality = root / "docs/quality/m16"
sys.path.insert(0, str(root / "dotnet/Biathlon.ReliabilityLab"))
from validate_soak import validate

soak = validate(quality / "soak")
assert soak["summary_verified"]
manifest = json.loads((quality / "soak/input-manifest.json").read_text(encoding="utf-8-sig"))
changed = []
for item in manifest["files"]:
    path = root / item["path"]
    if hashlib.sha256(path.read_bytes()).hexdigest() != item["sha256"]:
        changed.append(item["path"])
assert not changed, f"Soak source has changed: {changed}"

baseline = subprocess.check_output(["git", "show", "9a8b34d4:docs/PROJECT_STATE.md"], cwd=root).decode("utf-8-sig").replace("\r\n", "\n")
current = (root / "docs/PROJECT_STATE.md").read_text(encoding="utf-8-sig").replace("\r\n", "\n")
assert current.startswith(baseline), "PROJECT_STATE historical prefix changed"
receipts = []
cases = set()
for path in sorted(quality.glob("native-*.json")):
    if path.name.startswith("native-health-first-"):
        continue
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    if "Summary" not in data:
        continue
    assert data["Summary"]["FailedTests"] == 0, path.name
    assert data["Summary"]["SkippedTests"] == 0, path.name
    rows = data["Results"]
    assert len(rows) == data["Summary"]["PassedTests"], path.name
    for row in rows:
        assert row["Status"] == "Passed", (path.name, row)
        cases.add(row["Name"])
    receipts.append(path.name)

result = {"schema_version": 1, "id": "m16_evidence_integrity_v1", "status": "PASS", "soak": soak,
          "native_unique_cases": len(cases), "native_receipts": receipts,
          "source_manifest_matches": True, "project_state_history_preserved": True,
          "scope": "Latest applicable selected checkpoints, not a full monolithic assembly run. Retained red/oracle diagnosis evidence is excluded from passing-case receipts."}
(quality / "evidence-integrity.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
print(json.dumps(result, indent=2))
