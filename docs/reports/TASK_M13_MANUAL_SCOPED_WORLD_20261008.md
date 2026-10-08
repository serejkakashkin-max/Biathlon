# M13 — complete scoped world proposals with different assigned paths

2026-10-08. M13/WP-175/177/178 OPEN; WP-176 PASS. No Windows build.

Added pure `ManualScopedWorldRoster`: complete stable roster, separate selected
catalogs, exact world source identity, all-pair body check, per-actor course/arc
checks, staged physical odometer/progress and one-use full-clear commit. Catalogs
must share the exact race route/profile. Stationary bodies participate in the
same physical check as moving bodies; a different mat does not create a private
copy of a shared physical exit. Initial context captures every scoped cursor,
pose, velocity, physical motion and clock plus body/contact/course identity.

A rejected contact prefix, source mismatch, course start or phase cap invalidates
earlier staged proof and leaves accepted state untouched. Diagnostics identify
the cause. Rejection cannot become a paid stationary race tick. This API does not
resolve contacts: its caller still has to continue contact/edge/surface/seam
intervals inside the tick before any authoritative race/session commit.

Ordinary multi-actor handoff validates all requested rows before changing any
accepted occurrence. World pose, direction, velocity, odometer, route credit and
clock are retained. Firing/penalty/finish gates remain owned by race lifecycle.
Force distance and the existing contact velocity-norm odometer remain distinct
from projected competition distance. No live output/tuning changed before hookup.

- Final .NET scoped catalog/progress/world34/34 PASS; eight new world cases.
- Final native Unity8/8 executed PASS, discovery1325,15.3336588 s.
  Compile settled; Error Console empty.
- Tests: two exact owned ticks with stationary start actor and moving base actor
  on different mat selections; contact prefix rejection; stale/capped final-row
  atomicity; immutable initial context; foreign/stale proof and incomplete roster;
  ordinary heterogeneous handoff atomicity; unproved course start rejection;
  zero warm proposal allocations.
- The first contact test used1.65 m, outside the unchanged1.5 m occupied envelope
  even after0.1 m movement. It correctly returned clear. The fixture now derives
  separation from the actual body definition plus0.05 m, producing a real contact
  inside the tick. The failing receipt is preserved; no product tuning was edited.
- Earlier intermittent .NET allocation failures remain OPEN. Passing warm tests
  are not final player GC/performance qualification.

[Evidence](../quality/m13/manual-scoped-world-20261008/).
[Design](../adr/ADR-0038-manual-race-physical-occurrences.md).

Remaining: continue reached contacts and geometry/surface transitions inside a
full tick, validate private mat/penalty body clearance, authorize lifecycle gates
and dynamic mat choices, commit all sessions/events atomically, bind full replay
and production input/rig/host. Current playable host still follows the route with
legacy lateral input. Final M13 validation and one owner-test build follow hookup.
