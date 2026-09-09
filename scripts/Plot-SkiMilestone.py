"""Plot retained M2 comparative results; no independent simulation or tuning constants."""
import json
from pathlib import Path
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

root = Path(__file__).resolve().parents[1]
quality = root / "docs/quality"
data = json.loads((quality / "m2-comparisons.json").read_text(encoding="utf-8-sig"))
rows = {row["id"]: row for row in data["scenarios"]}
groups = [
    ("Technique skill · flat 1 km", ["technique_30", "technique_90"], ["Skill 30", "Skill 90"]),
    ("Climbing skill · uphill 1 km", ["climbing_30", "climbing_90"], ["Skill 30", "Skill 90"]),
    ("Snow and skis · flat 1 km", ["flat_steady", "flat_soft", "flat_glide_skis"], ["Neutral", "Soft snow", "Glide skis"]),
    ("Grip trade-off · technical descent", ["technical_packed", "technical_ice"], ["Packed", "Ice"]),
]
plt.rcParams.update({"font.size": 10, "axes.spines.top": False, "axes.spines.right": False})
fig, axes = plt.subplots(2, 2, figsize=(11, 7), layout="constrained")
for ax, (title, ids, labels) in zip(axes.flat, groups):
    values = [rows[key]["seconds"] for key in ids]
    bars = ax.bar(labels, values, color=["#52718e", "#d67b39", "#348878"][:len(ids)])
    ax.bar_label(bars, labels=[f"{v:.2f}" for v in values], padding=4)
    ax.set_ylim(0, max(values) * 1.16)
    ax.set_title(title)
    ax.set_ylabel("Finish time (s)")
fig.suptitle("BIATHLON · M2 headless comparisons\nProvisional tuning; measured complete-course results", fontsize=14)
fig.savefig(quality / "m2-course-comparisons.png", dpi=160)
plt.close(fig)

ids = ["flat_steady", "flat_final_sprint", "flat_attack", "flat_intervals", "rolling_upright", "rolling_tuck"]
labels = ["Steady", "Final sprint", "Attack", "Intervals", "Rolling upright", "Rolling tuck"]
fig, axes = plt.subplots(1, 3, figsize=(13, 5), layout="constrained")
for ax, key, title in zip(axes, ["seconds", "reserve", "fatigue"], ["Finish time (s)", "Remaining anaerobic reserve", "Final muscular fatigue"]):
    values = [rows[item][key] for item in ids]
    bars = ax.barh(labels, values, color=["#52718e"] * 4 + ["#d67b39", "#348878"])
    ax.bar_label(bars, labels=[f"{v:.3f}" for v in values], padding=3, fontsize=8)
    ax.set_xlim(0, max(values) * 1.22)
    ax.set_title(title)
    ax.invert_yaxis()
fig.suptitle("Pacing and tuck have measurable resource costs\nFlat and rolling scenarios are separate course comparisons", fontsize=14)
fig.savefig(quality / "m2-resource-tradeoffs.png", dpi=160)
plt.close(fig)
