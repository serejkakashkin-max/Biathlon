# M13 — Physical stop pieces and complete body cover, 2026-10-09

Partial integration checkpoint. M13 / WP175/177/178 OPEN; WP176 PASS. The production
host still uses lateral-v1 movement. No Windows review build was made.

## Changes and evidence

`ManualScopedWorldContacts` retains an immutable full free drive only when the
complete current field has no reached body contacts and at least one genuine
stop/cap breakpoint. Previously the force coupler split every actor at a roughly
6.8 ns longitudinal stop, then restarted an incompletely paid steering/snow ramp.
The docking diagnostic reached 4096 pieces after paying only 0.008603 seconds.
The correction preserves physical stop distance, residual slip/spin and the other
actors' complete force ramps. Future arrivals still require continuous scheduling,
full course/phase admission and the same atomic field publication. Reached pairs
retain force complementarity; ordinary constant-force fields retain their
established polynomial representation. Source/mass/duration validation precedes
every candidate output. World policy is versioned to v17.

`ManualRouteRibbonSweep` adds a bounded rectangular cell disk cover when the old
one-dimensional cover's oversized end caps cannot prove clearance. Both covers
contain the full occupied rectangle plus unchanged personal/edge clearances.
Each disk must independently fit the existing conservative connected tubes or
a proved simple local polygon. Maximum-depth-derived grid has at most 1024 disks;
there is no body shrink, global hull, disconnected bridge or new lane width.
The trial non-simple winding proof was not selected. Cover context is v5.

`ManualSkiControlledDriveSystem.PreviewStopping` inverts the existing frozen
longitudinal force law for an explicitly neutral, unpowered stopping intent.
It reports required brake fraction and infeasibility; automatic corner braking
cannot be undone, and the shared maximum brake force cannot be exceeded. The
result does not grant mat arrival or erase unresolved lateral velocity/spin.
This API is not yet selected by the field/production host. Drive context is v4.

New tests cover tiny physical stop/full common clock, future body arrivals,
last-row source atomicity, zero allocations, unchanged occupied-body dimensions,
refined-cover allocation/determinism, achievable/infeasible inverse braking and
unavoidable automatic corner braking. Existing stationary-player bypass, energy,
impulse, contact and replay tests remain active.

## Validation

Receipts: `docs/quality/m13/field-stepper-20261008/`.

- `free-drive-cover-focused.trx`: 66/66 PASS before the final two inverse-brake
  boundary/allocation tests were added.
- `manual-free-drive-cover-final.trx`: **482 PASS / 1 FAIL / 483 executed**.
  All added cases pass. The single active failure remains the known first-bout
  manual AI arrival at tick3214, with real residual velocity/spin.
- Native Unity: inverse/shared controlled drive30/30, free drive4/4, full-body
  cover13/13, contact forces21/21, actual Mass30 stationary-player bypass1/1 PASS.
  The latter covers400 complete common clocks. JSON receipts contain actual
  execution results; discovery counts are not execution counts.
- Final refresh/compile successful; recent Unity Error/Exception logs empty.
  No visible standalone player or unsupported GPU performance claim.

## Rejected trials and next work

The provisional terminal heading/point controller did not qualify. Its initial
unpowered capture could stall before the mat; later rolling-effort/heading trials
still crossed the firing line with real motion or left the body corridor. Broader
free-drive selection and pursuit changes also regressed the Mass30 bypass. Those
trials were removed from selected code, not used to relax any failing assertion.
The final bypass passes again. Failed receipts, the owned experiment patch and
unused docking-definition snapshot are retained outside Unity under the evidence
directory. No historical PROJECT_STATE entry or test was deleted.

Next: an independently qualified terminal mat controller using the approved last
2–3 m assist, then shared shooting/departure/finish, reusable mats, achieved draft,
manual replay and production-host selection. WP175 weather/readability, WP177
balance including the owner's too-easy-shooting issue, and WP178 sustained hidden
profiling remain open. This checkpoint changes manual candidate physics/context;
it does not retune authoritative shooting or qualify M13 completion.
