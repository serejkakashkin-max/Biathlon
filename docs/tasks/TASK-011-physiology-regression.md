# TASK-011 — WP-011 physiology regression and benchmarks

Dependency WP-010 PASS. Pin final state/events and full per-tick input/telemetry hashes for all eight
scenarios; qualify both .NET and Unity, without claiming all-platform lockstep. Add long duration,
empty reserve/full fatigue/extreme stats/form scenarios, invalid numerical configurations, no-NaN/Inf,
and a warmed integrated benchmark reporting elapsed milliseconds, allocation bytes and simulated duration.
Do not hide drift by changing snapshots/tolerance after failures. Record initial measured baseline.
Run shared .NET/Unity suites, Console, CLI repeatability. No gameplay retuning in this task.
