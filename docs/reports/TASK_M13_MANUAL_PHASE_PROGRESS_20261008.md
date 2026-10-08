# M13 — scoped physical-arc progress and ordinary world handoffs

2026-10-08. Continuation of the physical path catalog. M13/WP-175/177/178 OPEN;
WP-176 PASS. No intermediate Windows build or playable manual-control claim.

Added pure `ManualRacePathProgression` with immutable actor/cursor/proposal frames.
It separates observed physical arc, its high water and counted competition
distance. Reverse movement and a return to an already reached point cannot
credit a second pass. Start and penalty travel can increase arc high water while
base credit stays fixed. Observations bind the exact catalog, athlete, occurrence
and world position. One catalog's whole roster validates before any output slot
changes, so a rejected last row cannot publish earlier progress.

Ordinary adjacent handoff observes the same accepted world pose at both exact
endpoints, preserving direction, velocity, odometer, clock and base credit. A
cursor whose high water reached the endpoint but whose body retreated cannot
handoff. Firing, penalty decision, penalty circuit and finish remain lifecycle
gates; this API cannot authorize them. Every actual Woodland ordinary seam is
covered. Tests reuse the catalog fixture without changing its assertions.

- .NET new class10/10 PASS; combined catalog/progression26/26 PASS.
- Native Unity10/10 executed PASS, discovery1317, duration11.6023415 s.
  Compilation settled; Error Console empty.
- Tests cover all five path kinds, reverse/return credit, every ordinary seam and
  closed gate, rejected last-row atomicity, foreign actor/catalog/occurrence,
  mismatched world pose, invalid restored snapshots and zero warm allocations.
- Gameplay/balance impact: none before live world/session hookup. Player shooting,
  athlete capabilities and authoritative tuning are unchanged. The previously
  recorded intermittent .NET allocation risk remains OPEN; no new performance
  qualification is claimed.

[Evidence](../quality/m13/manual-phase-progress-20261008/).
[Design](../adr/ADR-0038-manual-race-physical-occurrences.md).

This is an event-free observation/proposal layer, not accepted race movement or
body admission. Still needed: heterogeneous moving/stationary roster ticks,
safe contact/course/surface continuation, authorized phase gates and session
commit, replay/event context and production host/rig/input integration. Final
M13 validation and one combined Windows build follow those items.
