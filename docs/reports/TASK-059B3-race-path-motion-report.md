# TASK-059B3 — Main/penalty path movement

PASS, 2026-09-08. Added RangePenaltyTraversal.PreviewMotion and RacePathMotion: pure path
lookahead plus shared technique, snow/equipment, cornering and force substeps. A preview
returns travelled/main-route distance, accepted range movement, firing/route stops and timed
motion segments without changing the authoritative range. Physics resamples at segment,
anchor and circuit boundaries, using the remaining fixed-tick time. Firing-line arrival
returns the unused time for the orchestrator's stationary-stop policy.

308/308 standalone and Unity EditMode pass; Console zero errors/four upstream warnings.
Evidence: artifacts/physiology/wp059b3-{standalone.trx,editmode.json,console.json}.
All 0–5 miss path previews match authoritative receipts; complete physical traversal with
0,2,5 misses adds exactly 150 m per miss. Surface/time continuity, stationary firing,
insufficient output buffer, deterministic replay and allocation-free previews pass.
Prior movement/shooting fingerprints unchanged. No tuning or commit.

Physiology, commands, checkpoint receipt publication and terminal classification still belong
to the next composition task. Next: TASK-059B4 same-tick receipts and substep timestamps.
