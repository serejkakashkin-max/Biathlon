"""Plot retained M4 measured outcomes without duplicating simulation tuning."""
import json
from pathlib import Path
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

quality = Path(__file__).resolve().parents[1] / "docs/quality"
data = json.loads((quality / "m4-shooting-balance.json").read_text(encoding="utf-8-sig"))
rows = data["scenarios"]
labels = [r["id"].replace("_", " ") for r in rows]
plt.rcParams.update({"font.size": 10, "axes.spines.top": False, "axes.spines.right": False})
fig, axes = plt.subplots(1, 3, figsize=(15, 6), layout="constrained")
for ax, values, title, limit in zip(axes,
    [[r["hit_rate"] * 100 for r in rows], [r["clean_bouts"] / r["samples"] * 100 for r in rows], [r["mean_seconds"] for r in rows]],
    ["Targets closed / shots (%)", "Clean five-target bouts (%)", "Mean complete bout (s)"], [115, 115, 36]):
    bars = ax.barh(labels, values, color=["#52718e"] * 6 + ["#b56b39", "#79928c", "#348878"])
    ax.bar_label(bars, labels=[f"{v:.1f}" for v in values], padding=3)
    ax.invert_yaxis()
    ax.set_xlim(0, limit)
    ax.set_title(title)
fig.suptitle("BIATHLON · M4 shooting Balance Lab\n200 fixed-seed bouts per condition · provisional tuning · no AI wind compensation", fontsize=14)
fig.savefig(quality / "m4-shooting-comparisons.png", dpi=160)
plt.close(fig)
