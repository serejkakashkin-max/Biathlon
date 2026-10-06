# M13 manual base-section session roster — 2026-10-06

Added `ManualBaseRosterSessionCommit` to bind a complete active athlete-session
roster to one accepted manual world ledger. It checks stable athlete order,
route instance, fixed clock, accepted route credit and physical motion.
It resolves every pending manual preview against the same full-clear world
proposal before publishing the session steps and world tick. A missing or
stale athlete preview rejects the batch with no athlete or world tick paid.

The two-athlete Woodland fixture uses different start times so the bodies
are physically separated, then drives both through the common force/yaw,
joint body/course sweep, physical odometer and race-session commit. It also
checks a wrong tick frequency and a stale second preview. Release .NET
focused regression 9/9 PASS; broader Sprint/field/manual regression 32/32
PASS; hidden Unity 6 EditMode 3/3 PASS, process exit 0. Evidence:
`docs/quality/m13/manual-roster-session-final.trx`,
`docs/quality/m13/manual-roster-session-regression.trx`, and
`docs/quality/m13/manual-roster-session-final-unity.xml`. Unity MCP was
unavailable; batch compilation completed without a C# error in the log.

This bridge does not yet update `PackSprintFieldSession` side state,
field timing/results, start/range/penalty geometry, AI steering, contact
replanning or v2 race replay. It assumes ordinary nonthrowing race event
sinks during publication, as the existing field commit does. No playable
host switch and no intermediate Windows review build were made.
