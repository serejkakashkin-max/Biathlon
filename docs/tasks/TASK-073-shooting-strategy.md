# TASK-073 — AI shooting strategy

Status: DONE, 2026-09-09. See docs/reports/TASK-073-shooting-strategy-report.md.

Add validated data-driven speed/stability thresholds selected from time urgency and stability
need. Feed the existing aim controller without modifying tracking error, reactions, sway,
target apertures, wind or hit resolution. Preserve legacy behavior when no strategy is supplied.
Changed tolerances invalidate previously accumulated stability. Verify earlier fast triggers,
invalid-input atomicity, complete geometric bouts and serialized command replay, then full
deterministic .NET and Unity regressions. Telemetry exposes selected risk and all thresholds.
