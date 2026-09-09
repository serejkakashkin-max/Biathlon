# TASK-072 — Pre-range recovery tactics

Status: DONE, 2026-09-09. See docs/reports/TASK-072-pre-range-recovery-report.md.

Implement a data-driven, cadenced request for reduced effort near the next pending shooting
bout. Reduction depends on range distance and observed HR; it must not affect final-lap skiing,
penalty travel or shooting directly. Apply through existing pace control, preserving its seeded
error and bounded response. Expose reason, urgency and reduction for telemetry. Compare complete
races with and without recovery: lower arrival HR must cost approach time. Replay exact commands
through shared physics; preserve existing canonical results when the policy is not selected.
