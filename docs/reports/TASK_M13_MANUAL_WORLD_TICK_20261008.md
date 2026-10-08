# M13 — staged complete-clock world intervals

2026-10-08. M13/WP-175/177/178 OPEN, WP-176 PASS. No build or live input hookup.

`ManualScopedWorldRoster` now supports an owned provisional tick with complete
common intervals. Every interval validates all source poses/velocities/cursors,
body pairs/edges, selected courses and phase portals before changing any scratch
row. Earlier approved scratch intervals survive a later rejection for refinement;
accepted bodies, odometers, route credit, clock and events remain unchanged.

Ordinary heterogeneous handoffs can occur between those intervals. They validate
the entire requested set and retain world pose, heading, velocity, odometer and
elapsed time. Firing/penalty/finish gates still require the actual race lifecycle
owner. No automatic turn or movement is introduced by handoff.

Finalization requires the complete fixed duration, exactly as accumulated in the
same floating clock. A nearly complete interval cannot be rounded up by a time
tolerance. The caller can supply the exact exposed remainder. Only then can a
single-use full-world proposal be validated and committed once. Aborted, foreign,
finalized or replaced transactions/proposals cannot be reused. Physical travel
is accumulated before the tick's nanometre rounding, avoiding per-fragment
rounding drift. Policy/context v3 records interval-grade sampling and this policy.

Ten new tests include a real Woodland ordinary seam at0.02 s with an unchanged
2 m/s velocity and a paid0.03 s suffix: physical travel0.10 m, successor observed
arc0.06 m, one published tick. Other cases cover mixed moving/stationary actors,
late-row atomic rejection/refinement, a contact after a clear prefix, stale/
overlong/incomplete sources, unpaid tiny suffix, lifecycle of transaction handles,
provisional group handoff, sub-nanometre travel accumulation and zero warm staging
allocations. The contact case rejects/aborts; it does not resolve first contact.

Validation:

- .NET new10/10 and existing scoped-world16/16 PASS.
- Native Unity new10/10 PASS (17.0090622 s) and compatibility16/16 PASS
  (26.4498729 s), discovery1343. Error Console empty.
- Broad default .NET256/259 PASS. Three existing allocation assertions failed:
  `ManualRouteJointSweepTests.CommonSafeTimeBindsWholeRosterWorldAndRouteFrames`
 1968 B; `ManualSkiMotionTests.PreviewIsRepeatableAllocationFreeAndBindsItsDefinitions`
 248 B; `ManualWorldRosterLedgerTests.EntireClearRosterCommitsOnceWithoutGuessingRaceOdometer`
 5312 B. None of those measured paths uses this new scoped tick API. Receipts kept;
  tests are not weakened or disabled.
- Explicit diagnostic `DOTNET_TieredCompilation=0`, same built assembly/filter:
 259/259 PASS. This narrows execution conditions, not the root cause, and does not
  erase the ordinary failures. Allocation/performance qualification remains OPEN.

[Evidence](../quality/m13/manual-world-tick-20261008/).
[Design](../adr/ADR-0038-manual-race-physical-occurrences.md).

Remaining: frozen-trajectory prefix/suffix reuse; first-contact/edge response and
surface/geometry continuation; real session lifecycle/mat selection; complete
atomic session/event/replay publication and production controls/rig/host. There
is still no live all-phase manual race or final M13 owner-test build.
