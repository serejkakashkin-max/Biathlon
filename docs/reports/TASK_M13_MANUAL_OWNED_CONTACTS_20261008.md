# M13 — current contact pairs derived from the owned world

2026-10-08. M13/WP-175/177/178 OPEN; WP-176 PASS. No Windows build.

World v11 derives continuing contact pairs directly from its complete current
working roster and the existing occupied-body SAT geometry/tolerance. Stable
actor order is precomputed at construction. `TryAppendToOwnedFirstContact`
refreshes these actual pairs before scheduling the next common interval; callers
do not hand-register touching pairs. A separated previous pair returns to normal
first-contact scheduling. Deep overlap rejects the entire geometry set, including
any earlier valid pair. Telemetry exposes count and limiting IDs/geometry; it is
not a future movement proof.

`TryResolveOwnedCurrentContacts` also permits response at actual initial accepted
proximity. The existing immutable mass-bound group solver prevalidates every row
before publishing provisional velocities. Response pays no time, odometer or
route credit and changes no position. Failure leaves every working row intact
and blocks continuation; abort preserves all accepted state. No response policy
can be silently substituted. Existing explicit-contact APIs remain compatible.

Validation:

- Initial **8/8**, expanded **11/11 .NET PASS**. Final relevant scoped-world,
  session/field, contact drive/scheduler and Sprint/Pack/player replay regression
  **140/140 PASS**.
- Native Unity **11/11** new owned-contact cases and **16/16** existing scoped
  roster cases PASS. Discovery1460 is not the execution count. Compile settled,
  not playing, Error Console empty.
- Real Woodland arrival, owned response and force-driven suffix pay a full0.05 s
  clock without a caller pair list. Initial three-body sets and response are
  identical under reversed roster enumeration; a failing final pair exposes no
  partial set. Unresolved initial group changes no row and cannot finalize.
- Initial impulse/abort preserve position/time/odometer/credit/accepted state.
  Separated pairs are dropped on the next clock. Missing policy and stale tick
  are rejected. Warm response/schedule/full-clock proposal loops allocate0 B.
- A rear skier with greater force than the still-touching leader correctly
  remains an unproved suffix. This test retains the remaining persistent-force
  requirement; the registry does not convert it into a paid stopped tick.

[Evidence](../quality/m13/manual-owned-contacts-20261008/).

Earlier broad allocation failures440 B and5496 B remain FAILED/OPEN. A focused
PASS is not their repair. Continuous contact force coupling, controlled yaw while
slipping, surfaces/course seams, reusable mats, finished actors, event/replay
journal/policy binding and production manual host remain required. This completes
the owned pair-list prerequisite, not playable queue removal or M13 qualification.
