# TASK-010 / WP-010 — PASS

Added required versioned scenario suite, fixed-tick runner, CSV input/telemetry export, separate
PhysiologyFingerprint and .NET BalanceLab physiology CLI. Eight workloads cover recovery, race effort,
interval attacks, fatigued start and 40/80 recovery/capacity. Full definitions and events accompany CSV.

.NET Release and Unity MCP EditMode: 67/67 each. CLI exported all eight scenarios successfully.
Real Unity reflection returned all eight final fingerprints: exact equality with .NET for every scenario.
Foundation hash unchanged. Export is an explicitly allocating cold observation path; Step remains zero allocation.
Unity Console 0 errors / 4 upstream reload warnings; compilation successful; git diff --check passed.
Evidence artifacts/physiology/wp010-* and artifacts/physiology/lab/{summary.json,*.csv,*-events.json}.

Reproduce: scripts/Run-PhysiologyLab.ps1. New standalone project references pure core, no new packages.
No scenes changed, no commit/remote action. Limitations: this is M1 physiology command, not full race
BalanceLab; final-state/event hashes do not alone prove every intermediate telemetry sample equal.
WP-011 adds full trace snapshots, long runs and measured performance. No owner gate.
