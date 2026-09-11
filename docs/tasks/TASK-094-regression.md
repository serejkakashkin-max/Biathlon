# TASK-094 — Versioned regression snapshots

Dependency: WP091–093. Pin the canonical suite (seeds1200–1203,3 cases), exact context/metrics
hashes and explicitly provisional target windows. CLI must show actionable per-row and per-metric
diffs, reject missing/duplicate/wrong-suite data and return failure without completed status.
Run the same regression in Unity; never relax tests merely to accept a mismatch.
