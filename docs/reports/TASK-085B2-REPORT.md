# TASK-085B2 — Shared Mass Start launch report

PASS — 2026-09-10. WP085 DONE.

Implemented explicit main-circuit start overlay, common field scheduling, initial grid occupancy,
per-actor projection, actual approach travel, complete global-order Mass Start result snapshots and
an AI physical-preview binding fix. Generated synthetic stadium fixtures reproducibly with
scripts/New-SyntheticMassStart.ps1. See ADR0016 for constraints and physical justification.

.NET15/15 focused (MassStartField3, grid5, approach3, projection2, Pursuit field2). Unity MCP3/3,
478 discovered,23.115s. Console0 errors/4 existing MCP warnings, no compilation failure. Repeated
30-body1200tick launch has minimum credited route307100214491nm and377 matching events; all30
start on tick1,27 rear rows emit approach completion, no overlaps or precision fallbacks. Six AI
finish five laps/four bouts, exact penalties/travel and snapshot roundtrip. Source Sprint/Pursuit
contracts and deterministic Pursuit regression pass. Prior full475 remains baseline; no full478
or new PlayMode run claimed.

Initial failures correctly rejected using a later narrow penalty corridor as the start width and
using a nonphysical30m alias as an AI loop. Actual start-width validation and the explicit alias
binding fix both issues without relaxing collision/radius checks for physical paths.

Limits: synthetic constant terminal start overlay, conservative race-wide steering targets,
complete finisher results, provisional fixed lanes. Full30-AI PPSS/stress and explicit Mass Start
lane policy remain WP086–089. No new tuning or hidden physical bonus. Next WP086 full PPSS audit.
Evidence: artifacts/physiology/wp085b2-final.trx, wp085b2-unity.json, wp085b2-unity-results.xml,
wp085b2-console.json; compact hashes under docs/quality/wp085b2.
