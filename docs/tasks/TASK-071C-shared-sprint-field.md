# TASK-071C — Complete shared-physics Sprint field

Dependency: TASK-071A and TASK-071B1–B5C8. Completes race-path orchestration for WP-071.

Status: DONE, 2026-09-09. Evidence: docs/reports/TASK-071C-shared-sprint-field-report.md.

Compose real Sprint physiology, terrain forces, bouts, penalties, checkpoints and results with
physical periodic body occupancy. Freeze one field snapshot for drafting; prepare every athlete
before resolving bounded lane movement and forward prefixes. Validate continuous pair intervals,
directed branch seams, retained incoming bodies and whole-interval junction occupancy before
committing the batch. Start staging and finish exit must be explicit lifecycle policies.

Acceptance: solo equivalence; full-race exact replay; dense changing lanes on flat, rolling and
technical closed tracks; a complete 30-athlete field; actual two-bout penalty entry/exit with
no skipped travel; malformed last commands cannot change authority; bounded lateral work has
physiological cost; warmed movement allocation check; Unity EditMode and real PlayMode run.
Expose shared physical snapshots to existing AI pace/draft/overtake policies and exercise the
same field through those commands. Keep M5 independent race contexts and hashes unchanged.

Record conservative throughput policies explicitly. This task does not claim the subsequent
WP-078 repeated 30-athlete soak, WP-079 believable AI gate or M7 real firing-lane allocation.
