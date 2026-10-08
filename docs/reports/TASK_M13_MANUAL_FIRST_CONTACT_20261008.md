# M13 — actual first-contact scheduling and paid world prefix

2026-10-08. M13 and WP-175/177/178 OPEN; WP-176 PASS. No Windows build.

`ManualSkiContactSweep.PreviewFirstContact` finds actual body proximity by
conservative advancement on the current separating axis. Relative projected
velocity, acceleration, occupied-body rotation and existing numerical reserves
bound each step. The half band of the existing contact tolerance gives a reached
endpoint, not an exact zero-gap time or a new gameplay tolerance. A continuously
admitted arrival prefix is required before returning Reached. Uncertainty, work
exhaustion and initial contact remain explicit; a possible-contact bracket is
never promoted into a guessed impulse. A whole clear result requires the existing
continuous exclusion proof.

`ManualSkiContactScheduler` checks the complete proposed roster, selects the
earliest reached proximity, groups actual simultaneous endpoint pairs in stable
ID order, then verifies the common prefix against every pair and explicit edge.
Existing contacts retain their continuous checks. Cold preallocated arrays bound
storage to the roster size. World context v9 binds the scheduler policy.
`TryAppendToFirstContact` stages this prefix through the existing scoped body,
course, phase and credit checks, or pays a wholly clear interval. It never publishes
partial accepted state. An arriving prefix still requires group response and the
remaining fixed clock; a rejected schedule/course cannot mint a stopped tick.

Validation:

- Initial pair8/8 and expanded pair/scheduler16/16 .NET PASS. One test setup had
  an invalid plane constructor; corrected to the existing five-argument plane
  and physical edge clearance before execution. Final regression **103/103 PASS**,
  including contact/sweep/scoped session/phase and Sprint/Pack/player replay.
- Native Unity **9/9** pair cases and **9/9** scheduler cases PASS. Compile settled;
  Error Console empty; discovery1417 is not the executed count.
- Tests include fast pass-through with separated endpoints, shared-force
  acceleration, rotating corners at stationary centres, simultaneous independent
  contacts, later pair exclusion, stable initial identity order, continuing
  contacts, earlier edge rejection, coarse integration uncertainty and work caps.
- Real Woodland world staging reaches contact, responds and pays the suffix in
  the same0.05 s clock with positive speed. Failed final-row phase admission keeps
  the working pose, time, receipts and pending-contact flag untouched. Warm pair
  and four-actor scheduling loops allocate0 B.

[Evidence](../quality/m13/manual-first-contact-20261008/).

The scheduler handles proposed trajectories; it is not the complete live race
runner. Surface/phase cut timing, persistent existing-contact management and
slip with propulsion/gravity/drag remain required. Earlier edges and unresolved
contact continuations correctly reject rather than supplying an automatic stop.
Full-field performance/event journal/replay/Pack/manual host and earlier broad
allocation qualification remain OPEN. No live queue, manual steering or M13
completion claim follows from these passing primitive integration tests.
