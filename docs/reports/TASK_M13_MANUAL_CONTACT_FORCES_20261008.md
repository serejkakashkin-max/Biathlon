# M13 — continuing contacts under unequal traction

2026-10-08. M13/WP-175/177/178 OPEN; WP-176 PASS. No Windows build.

`ManualSkiContactForces` now couples shared longitudinal drive/snow accelerations
through the actual current occupied rectangles. A reached face supplies both
support endpoints; centre impulses alone cannot keep both corners from closing
under unequal traction. Stable-ID projected Gauss-Seidel solves accumulated
nonnegative normal and bounded static/sliding tangent reactions with the existing
mass, rectangle inertia, friction and work definition. Restitution remains an
impact property and does not multiply persistent force. Opening points carry no
adhesive reaction. Initial closing velocity still requires physical impact response.

The new `SkiForceCoupledTrajectory` retains the exact initial pose/raw velocity
and integrates centre/angular acceleration. The common piece ends at the first
free force stop/cap/restart or frozen contact-point kinetic-slip zero. A new solve
is required after that boundary; friction cannot continue across zero and add
energy in the other direction. Existing world slices, physical odometer quadrature
and continuous body/course proofs apply. All provisional trajectories validate
before caller output changes. A force result grants no time, route credit or
movement admission by itself.

World context v12 binds the force policy and immutable masses.
`TryPreviewContactDrivenMotion` reads the owned working bodies, validates every
drive source, and produces one common candidate piece. Pending response blocks
the query. Accepted world state remains unchanged until the usual complete-clock
proof/commit. Reactions and convergence/work telemetry are available by stable IDs.
The old independent-drive negative test is retained: skipping this coupling still
cannot mint a stopped clock.

Validation and evidence:

- Initial existing pair/group/drive .NET regression **38/38 PASS**.
- New class progressed from **13/15** (wrong coordinate-axis assertions), through
  **14/15** (invalid zero snow multiplier fixture), then **15/17** (fixture used
  25 m/s instead of the actual 60 m/s cap; repeated-contact failure). Corrected
  fixtures retain shared tuning. All receipts are preserved.
- The repeated-contact test initially failed at tick8: a residual opposite yaw
  of ±8e-9 rad/s eventually changed a face into a single active point. The solve
  now converges to the smaller of configured speed tolerance and a floating-point
  64-ulp speed-scale reserve. No velocity/yaw clamp, body shrink, widened contact
  tolerance, test retirement or extra pass budget was used for that repair.
- Expanded class **19/19 .NET PASS**, including 100 complete actual Woodland
  world ticks with rear350 W/front0 W and movement/odometer on every tick. Tests
  also cover rest, unequal masses, linear/angular impulse conservation, every
  three-body roster permutation, friction cone/zero crossing, no force restitution,
  force boundaries/slices, work-limit/overlap/foreign-source atomicity and warm
  solver plus owned-world **0 B** loops.
- A cold 40-body witness in five dense columns gives70 support points and2507
  passes under an explicit4096-pass fixture bound. All columns move and reactions
  conserve momentum. This is **not** a production performance qualification or
  proof that a smaller configured work budget suffices.
- Native Unity **19/19** final new tests, **8/8** unchanged impact-group tests and
  **11/11** owned-contact tests PASS. Discovery1479 is not executed-test count.
  Unity compile settled, not playing, Error Console empty. A subsequent context
  string-only clarification refreshed successfully with the same clear Console.
- Broader .NET run **210/211 FAILED**: existing arrival allocation assertion6256 B.
  Final expanded selection **211/213 FAILED**: existing working-phase allocation
  assertion8088 B and existing scoped-roster allocation assertion2576 B. Prior
  failures440/5496 B remain preserved. Isolated/new/native passes do not repair
  this unresolved allocation qualification; assertions are unchanged.

[Evidence](../quality/m13/manual-contact-forces-20261008/).

The model freezes force axis/contact geometry for a piece and still requires
continuous admission. Rotating/corner contacts, opening-point recontact, changing
surfaces, pushed scalar overspeed, all course seams and production work/performance
budgets remain unqualified. Full signed steering while slipping, race runner,
reusable mat/finish lifecycle, events/replay and playable production host are still
required. Owner queue removal and M13 are not declared complete. Next: compose
commanded motion/contact/force boundaries and physical phase transitions on the
common race clock, then qualify the host and deliver the single final owner build.
