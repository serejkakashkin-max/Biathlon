# TASK-080 — Pursuit dependency/results contract

Status: DONE — 2026-09-10. Master Plan WP-080 after M6 PASS.

PursuitQualificationSnapshot imports a complete validated SprintResultSnapshot into a versioned,
stable-ID dependency with embedded source/profile/timing context. Only finishers qualify. Registered
integer gaps, tied source ranks and canonical source order survive serialization; unique start
ordinals do not break ties. Empty/oversized fields, incompatible category/rules/format and altered
rows are rejected. No implicit qualification cutoff or fabricated DNS/DNF time is introduced.
Reload requires caller-owned expected source ID and exact target profile. Validation checks
consistency, not authentication of externally supplied results.

Added a synthetic five-lap PPSS profile and four tests, including an actual completed two-athlete
AI Sprint import. Shared Sprint result fixture helper is now internal; its behavior is unchanged.
No authoritative race physics, AI decisions, shooting geometry or Sprint ranking changed.

Validation: focused .NET 8/8; Unity MCP PursuitQualificationTests 4/4 and SprintResultsTests 4/4.
Unity Console: zero errors, compilation_failed=false; four existing MCP lifecycle warnings.
Raw evidence: artifacts/physiology/wp080-dependency.trx, wp080-unity-pursuit.json,
wp080-unity-sprint.json and wp080-console.json. Full 450/450 standalone regression remains the
immediately preceding M6 baseline; no new full-suite run is claimed. Current discovery: 454 tests.

Limits/next: WP-081 schedules source gaps; WP-082 supplies PPSS orchestration. This contract alone
is not a complete Pursuit race. Authentication and any future qualification cutoff require their
own explicit policy. No owner blocker.
