# TASK-081 — Pursuit gap start

Status: DONE — 2026-09-10.

Implement a scheduler consuming WP-080 qualification data, preserving exact registered gaps and
simultaneous ties. Reject incompatible clocks and overflow before emission. Reuse race start
events, test output-buffer atomicity, lifecycle integration, deterministic reload and allocations.
Run focused .NET/Unity tests. See docs/reports/TASK-081-REPORT.md for evidence and limits.
