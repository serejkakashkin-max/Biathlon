"""Render M1 evidence from actual CLI traces, without importing the simulation into plotting code."""
import argparse
import csv
from pathlib import Path
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

parser = argparse.ArgumentParser()
parser.add_argument("input", type=Path)
parser.add_argument("output", type=Path)
args = parser.parse_args()
args.output.mkdir(parents=True, exist_ok=True)

def read(name):
    with (args.input / f"{name}.csv").open(encoding="utf-8") as source:
        rows = list(csv.DictReader(source))
    return {key: [float(row[key]) for row in rows] for key in rows[0]}

plt.rcParams.update({"font.size": 10, "axes.spines.top": False, "axes.spines.right": False,
                     "axes.grid": True, "grid.alpha": .2, "figure.facecolor": "white"})
colors = ["#2563eb", "#e87924", "#15803d"]

attack = read("attack_intervals")
fig, axes = plt.subplots(4, 1, figsize=(10, 10), sharex=True, layout="constrained")
for ax, fields, ylabel in zip(axes,
        [("requested_watts", "delivered_watts"), ("hr_bpm", "hr_target_bpm"), ("anaerobic",), ("fatigue",)],
        ["Power (W)", "Heart rate (bpm)", "Anaerobic reserve (0–1)", "Muscular fatigue (0–1)"]):
    for index, field in enumerate(fields):
        ax.plot(attack["time_s"], attack[field], label=field.replace("_", " "), color=colors[index], linewidth=1.5)
    for start in (60, 180, 300): ax.axvspan(start, start + 60, color="#e0f2fe", alpha=.65, zorder=0)
    ax.set_ylabel(ylabel)
    ax.legend(loc="upper right", fontsize=8)
axes[2].set_ylim(0, 1.05)
axes[3].set_ylim(bottom=0)
axes[-1].set_xlabel("Simulation time (s); shaded = recovery workload")
fig.suptitle("BIATHLON M1 • attack / recovery intervals\nProvisional tuning v1 • fixed 20 Hz • recorded simulation output", fontsize=14)
fig.savefig(args.output / "m1-intervals.png", dpi=160)
plt.close(fig)

fig, axes = plt.subplots(2, 2, figsize=(12, 8), layout="constrained")
for name, label, color in [("recovery_40", "Recovery 40", colors[0]), ("recovery_80", "Recovery 80", colors[1])]:
    rows = read(name)
    axes[0, 0].plot(rows["time_s"], rows["hr_bpm"], label=label, color=color)
    axes[0, 1].plot(rows["time_s"], rows["anaerobic"], label=label, color=color)
for name, label, color in [("capacity_40", "Capacity 40", colors[0]), ("capacity_80", "Capacity 80", colors[1])]:
    rows = read(name)
    axes[1, 0].plot(rows["time_s"], rows["anaerobic"], label=label, color=color)
    axes[1, 1].plot(rows["time_s"], rows["delivered_watts"], label=label, color=color)
for ax, label in zip(axes.flat, ["Heart rate (bpm)", "Anaerobic reserve (0–1)", "Anaerobic reserve (0–1)", "Delivered power (W)"]):
    ax.set_xlabel("Simulation time (s)"); ax.set_ylabel(label); ax.legend(fontsize=9)
fig.suptitle("BIATHLON M1 • stat comparisons\nHigher recovery accelerates refill; larger capacity preserves attack but takes longer to refill", fontsize=13)
fig.savefig(args.output / "m1-stat-comparisons.png", dpi=160)
plt.close(fig)

fig, axes = plt.subplots(1, 2, figsize=(12, 4), layout="constrained")
for name, label, color in [("race_3min_recovery_60s", "Fresh start", colors[0]), ("fatigued_race", "Fatigue 0.6 start", colors[1])]:
    rows = read(name)
    axes[0].plot(rows["time_s"], rows["delivered_watts"], label=label, color=color)
    axes[1].plot(rows["time_s"], rows["hr_bpm"], label=label, color=color)
for ax, label in zip(axes, ["Delivered power (W)", "Heart rate (bpm)"]):
    ax.set_xlabel("Simulation time (s)"); ax.set_ylabel(label); ax.legend(fontsize=9)
fig.suptitle("BIATHLON M1 • same control with fresh / fatigued starts", fontsize=14)
fig.savefig(args.output / "m1-fatigue-comparison.png", dpi=160)
plt.close(fig)
