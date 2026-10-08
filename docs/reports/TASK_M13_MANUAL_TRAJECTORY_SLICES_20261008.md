# M13 — frozen world trajectory slices

2026-10-08. M13 and WP-175/177/178 OPEN; WP-176 PASS. No Windows build.

Added allocation-free `ManualSkiWorldTrajectory.Slice(from,to)`. Slices retain the
original force or snow interval, speed/yaw evolution and conservative bounds.
Nested times remain relative to the selected slice while preserving one root.
Endpoint pose/velocity match the root; cutting time cannot recalculate a snow
impulse or apply a command twice. Whole-trajectory query behavior is retained.
Invalid, empty, out-of-range and unrepresentable source-clock intervals fail.

Force travel uses its original cumulative-distance difference. The initial
contact implementation did the same with two64-panel quadratures, which was
wrong: each query resizes the integration grid. A synthetic velocity-zero case
found a tiny positive-duration slice with negative travel
`-5.1236874812343736E-10` m. The failing receipt is retained. Contact slices now
integrate the nonnegative velocity norm on the actual selected interval, using
the existing64-panel rule and no clamping. Fragment sums have quadrature error;
they are not claimed to telescope bitwise to the whole interval's approximation.
The original whole contact query is unchanged. Scoped world context v4 records
this numerical policy. Body dimensions, force/grip/snow tuning and live races
are unchanged; slicing alone does not admit movement or resolve contacts.

Validation:

- Initial negative-travel regression0/1 PASS; fixed1/1; expanded44/44 PASS.
- Final focused .NET84/84 PASS: new slices/scoped tick/world, force/snow/joint
  primitives and existing Sprint/Pack/player replay regressions.
- Native Unity17/17 new slice cases;11/11 staged world tick;9/9 existing force
  and8/8 existing snow cases PASS, discovery1361. Error Console empty.
- Independent reference checks cover acceleration/braking-stop/speed-cap slices,
  frozen versus recomputed snow impulse, velocity-zero travel, nested/root bounds,
  exact prefix/suffix pose/velocity continuity, whole-query compatibility, invalid
  clocks and zero warm allocations. Synthetic reversal tests are numerical
  representation tests, not a change to passive snow behavior.
- A real heterogeneous world fixture pays one original snow interval as0.02 s
  plus0.03 s: final body and velocity equal the root, one world tick publishes,
  physical odometer matches the full interval at nanometre encoding in that case.

[Evidence](../quality/m13/manual-trajectory-slices-20261008/).
[Design](../adr/ADR-0038-manual-race-physical-occurrences.md).

Earlier default broad.NET256/259 allocation failures remain OPEN; the prior
explicit tiering-off259/259 diagnostic and current focused passes do not resolve
that qualification. Remaining: actual first-contact/edge response, surface and
phase continuation, real lifecycle/mat selection, complete session/event/replay
transaction, production input and rig. Playable host still uses legacy lateral
route-following input. No live all-phase manual or M13/build closure is claimed.
