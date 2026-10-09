# M13 — explicit practical numerical tolerances

2026-10-09. Owner asked to stop repeatedly blocking progress on overly strict
technical tolerances. This change selects one versioned policy instead of
continuing to repair individual near-zero residues. The numerical values below
are implementation choices under that direction, not a new owner gate approval.

## Behavior

`manual_numerical_tolerance_v1.json` selects terminal planar rest speed **0.001 m/s**,
terminal yaw **0.001 rad/s**, and course safety-margin allowance **0.02 m**.
These values are defined once in the versioned resource and authenticated in
world/replay context. The legacy optional-null composition remains strict.

At the actual owned firing plane, after continuous body/course/gate admission,
a velocity and yaw within both limits can be represented as rest. Encoded X/Z,
heading, full paid interval, raw physical travel and odometer are retained.
This is limited to a firing endpoint, not a general speed reset or a way to
admit an unpaid route/phase transition. Diagnostic count and maximum removed
speed/yaw are exposed for the current proposal/tick. A rejected proposal does
not publish this scratch state.

The authored Woodland edge guard is 0.10 m. Local ribbon proof may use 0.08 m,
while retaining the complete oriented occupied body. Validation requires at
least half the authored guard for any body definition. All corners and full
edges still need continuous clearance. Explicit supplied wall planes and
pair/contact tolerances are unchanged. This does not permit actual bodies
outside the course, change route geometry or shrink collision envelopes.
Reusable cold mat-path ribbons use the same selected policy.

The observed Sprint firing failure had residual speed about **1.54e-8 m/s**:
its own frozen braking law ended at exact rest, but unrelated contact clock
partitioning cut the terminal double-root about 41 ns early. It no longer
requires a special-case root/clock repair. The earlier 0.044 m/s moving arrival
is above this policy and must not be declared stationary.

## Verification and remaining work

New fixture first produced **2 FAIL / 9 PASS** with missing behavior. Its
eleven cases now pass in .NET and native Unity: policy schema/roundtrip and
limits; selected versus strict terminal rest; above-limit speed and spin;
exact position/heading/time/travel retention; context identity; reset diagnostic
counts; a 2 mm safety-margin loss admitted and a 5 cm loss rejected.
Native final run: **11 PASS, 0 FAIL, 0 skipped**, 8.536 s. Reported discovery
1709 is not the executed count. Unity compilation settled; Error and Exception
Console queries were empty.

The simultaneous spin-aware traffic integration trial was rejected and removed:
it regressed an existing complete Mass3 signed replay (63 PASS / 1 FAIL in the
focused trial). Isolated warning tests passed, which was insufficient. In that
trial the new rest policy let Sprint40 progress beyond the previous tiny-rest
failure at tick13977, reaching a different contact activation failure at14402.
Those experimental AI changes are not part of this implementation.

Final selection and production-field results are recorded in
`manual-numerical-policy-final-20261009.trx`: **64 PASS / 2 FAIL / 0 skipped / 66**,
2m22s. Complete independent signed Mass3/Sprint1/Pursuit3 record/replay remain
green. The enabled full fields still reject course crossing: Mass30 tick164
(previous baseline163), Sprint40 tick8729 (previous8724). The new margin helps
only briefly because both trajectories continue toward the edge. This evidence
rules out merely increasing course tolerance as the traffic solution.
Neither the policy nor any test count certifies full production traffic.

M13 / WP175 / WP177 / WP178 remain OPEN; WP176 PASS. The playable production
host still uses lateral-v1. Full manual steering remains required. No owner
Windows build was made. Existing full-size field failures stay enabled; genuine
course/contact errors, crashes, artificial queues and broken input are blocking.
Further tolerance expansion is not a substitute for those behaviors. Shooting
geometry and tuning are unchanged; the owner's shooting-feel issue stays open.

Next: fix actual dense-field maneuvers/start staging, complete full signed
fields and real Sprint-to-Pursuit, connect player mat capture/manual host, then
finish weather, balance and performance acceptance before the owner build.
