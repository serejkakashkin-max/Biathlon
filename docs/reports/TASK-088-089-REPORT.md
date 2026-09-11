# TASK-088–089 — Mass Start stress and deterministic regression

PASS — verified 2026-09-10; recorded 2026-09-11. WP088 and WP089 DONE.

Added rolling and narrow synthetic Mass Start fixtures and a reproducible generator option.
The narrow fixture retains a valid 6 m launch corridor and has a 400 m, 3 m wide no-overtake
sector. Eight generated track/connection/junction/lane files reproduce byte for byte.

The matrix runs three complete 30-AI PPSS fields twice with fixed seeds 123/456/789 and
expert/developing profiles. It checks body clearance and width each tick, monotone physical
travel, bounded stalls and lateral reversals, logical/physical shooting lane agreement,
30 common starts, 120 lane assignments, 600 accepted shots, five laps and actual penalties.
Frames, lateral positions, events, contexts and final results repeat exactly.

All three scenarios passed in .NET and Unity with identical trace and result hashes (see
quality/wp088-089/verification.json). Finish ticks: 17575 / 28285 / 17001. Maximum skiing
stall: 373 / 511 / 181 ticks; first-minute maximum: 2 / 15 / 2 ticks at 20 Hz. Peak concurrent
shooters: 9 / 17 / 9. No precision fallbacks, persistent deadlock or sustained oscillation.
These are synthetic stress observations, not final M8 balance targets or visual acceptance.

Result unit tests verify sub-display-unit finish ordering, shared exact-tie ranks, schema,
clock, grid, duplicate/missing entrant and nonterminal-result rejection.

Validation: full .NET 484/484; focused matrix 1/1; Unity matrix 1/1 (816.26 s), result tests
2/2. The matrix MCP response timed out while Unity continued; its completed NUnit XML is
the pass evidence. No failing test was disabled. Gameplay tuning/physics are unchanged.
No new PlayMode or rendered venue validation is claimed. Next: WP090 three-format harness.
