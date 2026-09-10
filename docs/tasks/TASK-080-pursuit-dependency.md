# TASK-080 — Versioned Sprint dependency for Pursuit

WP080 after M6 PASS. Consume a complete validated Sprint result snapshot, retaining source result
identity, exact source/profile/timing data and registered integer gaps. Import finishers only,
retain tied ranks/gaps and canonical source order, reject empty/oversized fields and incompatible
category/rule revision/target format. Never silently truncate qualifiers or invent times for DNS/DNF.

Serialize a self-validating qualification snapshot. Reload requires the expected Sprint result ID
and exact expected Pursuit profile to reject a wrong/stale dependency. These are consistency checks,
not cryptographic authentication. Starting tick conversion belongs to WP081; full PPSS to WP082.
Use the existing registered Sprint results (ADR0010), not rounded display strings or finer-time tie breaks.

Add a synthetic5000m/five-lap PPSS test profile. Verify tied/source-status cases, malformed/stale
snapshots, capacity/category/profile rejection, and import a complete actual AI Sprint. Run focused
.NET and Unity MCP tests, inspect Console, report/update state/commit/push autonomously.

Status: DONE — 2026-09-10. See docs/reports/TASK-080-REPORT.md.

