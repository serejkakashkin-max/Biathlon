# TASK-083 — Pursuit tactical AI adjustments

Status: DONE — 2026-09-10. Next WP084.

Added a versioned provisional PursuitTacticsDefinition and allocation-free PursuitTacticalPlanner.
The shared AI runner reads this policy only for Pursuit and adds it to the exact replay context.
At the existing tactical cadence, it uses global checkpoint gaps (source start gaps before the
first split), observed place, route-near trailing skiers and physically eligible draft strength.
Chasing and nearby leader defense adjust bounded attack preferences; actual eligible drafting
raises follow/draft preferences. Last standing raises chasing urgency and lowers a leader's
shooting urgency through the existing strategy controller. Current resources, candidate eligibility,
physical space and shooting geometry remain authoritative. No hidden speed/power/hit modifiers.

GetLastPursuitPlan exposes gap/defense pressure, draft strength, shooting urgency and reason flags.
Published plans update after a successful field step; AiPursuitTacticsChanged events record reason
changes. Sprint does not read the new definition or alter its context, choices or event stream.
The preferences are provisional calibration, not a claim of final gameplay balance.

Tests: .NET32/32 including all three repeated30-athlete Sprint canonical scenarios; final focused
5/5 after adding integration assertions. Unity MCP tactics3/3 and complete shared field2/2.
Zero managed allocation over10000 planner/preference evaluations. Ineligible attack remains
ineligible despite preference changes. Definitions, missing observations, last-range response,
invalid inputs and bounded preferences tested. Console0 errors,4 existing MCP warnings.

Repeated Pursuit now14342ticks and result SHA256
449333e5ccdfc60908353e5471f23d2b5285061e8ec6df011532337189dabff2 in both .NET and Unity.
This intentional result change follows decisions and their physical/shooting costs. Previous
WP082 baseline was15326ticks/a7a0eaea...; this small fixture is not statistical balance evidence.
All three WP078 Sprint result hashes remain unchanged. Previous full463/463 is retained;
current466 discovery is covered by that baseline and focused checks, not a new full466 run.

Evidence: artifacts/physiology/wp083-tactics.trx, wp083-final.trx, wp083-unity-tactics.json,
wp083-unity-field.json, wp083-unity-field-results.xml and wp083-console.json; compact hashes under
docs/quality/wp083. Limit: checkpoint position is explicitly observed/historical; it is not an
omniscient continuously updated classification. Physical draft and overtaking remain spatially
validated. Broader starts, overtakes, penalties and close-finish regression follows WP084.
