"""Validate the committed WP-175 snow matrix against its isolated TRX receipts."""

import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
MATRIX = ROOT / "docs/quality/m13/wp175-conditions-20261006/full-field-snow-matrix.json"
NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
PROFILES = {"packed_neutral", "fresh_soft", "icy_fast", "wet_slow"}


def receipt(name: str) -> str:
    path = MATRIX.parent / name
    if path.parent.resolve() != MATRIX.parent.resolve() or not path.is_file():
        raise ValueError(f"Missing or foreign receipt: {name}")
    root = ET.parse(path).getroot()
    results = root.findall(".//t:UnitTestResult", NS)
    if len(results) != 1 or results[0].get("outcome") != "Passed":
        raise ValueError(f"Receipt did not pass exactly one test: {name}")
    return "\n".join(node.text or "" for node in root.findall(".//t:StdOut", NS))


def require_row(output: str, label: str, snow: str, count: int, ticks: int) -> None:
    pattern = rf"{label} snow={re.escape(snow)} count={count} ticks={ticks}(?!\d)"
    if re.search(pattern, output) is None:
        raise ValueError(f"Missing field result: {pattern}")


def main() -> None:
    data = json.loads(MATRIX.read_text(encoding="utf-8"))
    if data["schema_version"] != 1 or data["id"] != "wp175_woodland_full_field_snow_matrix_20261007":
        raise ValueError("Wrong matrix identity")
    rows = data["rows"]
    if len(rows) != 4 or {row["snow"] for row in rows} != PROFILES:
        raise ValueError("Matrix needs all four canonical snow profiles exactly once")
    for row in rows:
        snow = row["snow"]
        output = receipt(row["sprint_pursuit_trx"])
        require_row(output, "wp175_full_field=sprint", snow, 40, row["sprint40_ticks"])
        require_row(output, "wp175_qualified_pursuit", snow, 40, row["pursuit40_ticks"])
        output = receipt(row["mass_trx"])
        if snow == "packed_neutral":
            if not re.search(rf"wp175_full_field_replay athletes=30 ticks={row['mass30_ticks']}(?!\d)", output):
                raise ValueError("Packed-neutral Mass Start replay receipt mismatch")
        else:
            require_row(output, "wp175_full_field=mass_start", snow, 30, row["mass30_ticks"])
    print("WP-175 full-field snow matrix: 4 profiles x Sprint/Pursuit/Mass Start, 8 TRX receipts verified")


if __name__ == "__main__":
    main()
