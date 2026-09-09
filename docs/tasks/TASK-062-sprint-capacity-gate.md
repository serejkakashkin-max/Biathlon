# TASK-062 — 60-athlete capacity / M5 gate

Measure a complete 60-athlete independent-path Sprint field with interval starts, both bouts,
actual penalties and final classification. Retain wall time, simulated duration, allocation,
event/result fingerprints and exact runtime evidence. Separately measure warmed active ticks
without lifecycle initialization; do not label whole-race allocations zero if bout/result
creation allocates. Validate all entrants finish, starts/shots/penalties/results are unique,
and repeated .NET/Unity outputs match. Re-run relevant PlayMode resources/replay harness.

Produce MILESTONE-M5-REPORT.md and machine-readable benchmark/balance evidence. M5 gate is
technical, not an owner stop: after PASS continue first unlocked M6 work package.
