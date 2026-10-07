# TASK — M13 actual-world posture and backward base publication

2026-10-07. Intermediate implementation; M13/WP-175/177/178 remain OPEN.
No new Windows build or owner review gate is claimed.

## Result

Manual preparation can now bind an exact accepted actor observation. It samples
surface metadata at that actor's selected occurrence/local station, computes
slope along its actual heading and turn curvature from actual yaw/planar speed,
and feeds the existing tuck and physiology systems. It does not use high-water
race credit as the body's current physical position. Tuck still costs effort
and steering response; no athlete, shooting or difficulty constants changed.

Pack same-section publication accepts backward body movement while its existing
world ledger retains high-water route credit. The physical odometer and clocks
advance, but retreat/retrace cannot award the same route distance again. Snapshot
grade and turn curvature observe the accepted heading/velocity. Publication
compares the exact prepared actor with the ledger's accepted source: another
observation cannot commit staged physiology through a foreign world proof.
The binding context is explicitly versioned as
`manual_pack_same_section_high_water_publication_v2`.

Changed: SprintAthleteSession, PackSprintFieldSession, RaceRoute's explicit
occurrence surface sampler, RacePresentationSnapshot, ManualBaseRouteCommitTests.
Legacy route-following APIs/output remain available; the live host has not yet
switched to a complete manual runner. The compatibility preparation overload
requires coincident observed/credited stations; the accepted-actor overload is
required for actual-world posture.

## Evidence

[Receipts](../quality/m13/manual-actual-world-20261007/).

- Initial 4-case reproduction: 2 PASS / 2 FAIL. A skier facing down the same
  uphill route had drag multiplier 1 instead of tuck 0.6; backward Pack
  publication rejected an otherwise full-clear body/world proposal.
- Updated base fixture: .NET 12/12 PASS, then 14/14 with exact-source rejection
  and allocation coverage. Focused selection 39/39 PASS includes shared tuck,
  Sprint and route progress/continuous sweep regressions.
- Native Unity exact class: 14/14 PASS. Discovery TotalTests=1290 is not the
  execution count. The fully qualified class attempt executed 0/Unknown and
  is not accepted evidence. Unity compile PASS; final last-minute Error Console
  empty. One transient inaccessible constructor error was repaired before
  successful compilation/tests; the internal frame constructor was preserved.
- Five real backward Pack ticks retain route credit, move the world body back,
  increase the physical odometer and keep field/session/ledger/checkpoint
  clocks equal. Actual surface work at retreated station 61 m differs from
  credited 80 m. Uphill, reversed downhill and active-yaw tuck cases pass.
- Broader all-manual run: 182/183 PASS, allocation assertion in
  AcceptedRestAccelerationAndWarmPreviewHaveNoHiddenVelocityResetOrAllocation
  observed 7792 bytes; isolated unchanged case subsequently 1/1 PASS.
  Second broader run: 182/183 PASS, allocation assertion in
  EntireClearRosterCommitsOnceWithoutGuessingRaceOdometer observed 5072 bytes.
  Both red receipts are retained. The cause is unproven; assertions are
  unchanged, and repeated passing narrow tests do not close the runtime
  allocation risk. No broad green/performance claim is made.

## Limits and next work

Same base section only. Phase/start/private range/penalty scopes, geometric
interval splitting, contacts/slip and complete lifecycle replay/host migration
remain necessary. The accepted-actor source is bound at commit, not independently
authoritative just because preparation accepts it. No paid stop-tick fallback,
teleport, route-following yaw correction or shooting retuning was added.
Next: physical path scopes and complete tick continuation; qualify the existing
intermittent allocation failures before M13 performance closure.
