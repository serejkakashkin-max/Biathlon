# TASK-071B2 — Constrained prefix report

Status: PASS for trajectory application primitive, 2026-09-08. ConstrainPreparedTravel retains
only a prefix of the prepared timed motion and stops at its endpoint. It cannot add distance,
change branches arbitrarily or increase velocity. Full prepared physiological workload is
committed; range/penalty/checkpoint/lap/finish accounting uses actual retained travel. A changed
preview receives a new revision, invalidating the original handle.

362/362 standalone and Unity EditMode pass; 6/6 Unity PlayMode reverified. Post-PlayMode Console:
zero errors, one upstream warning, compilation_failed=false. Tests cover full blocking without
refunding physiology, bad caps/stale handles, stops one nanometre before mat/finish and partial
penalty motion without false completion. All previous full-race fingerprints remain unchanged.

Prefix time follows the existing piecewise-linear motion receipt convention, stopping thereafter.
It is not an arbitrary slow trajectory and is NOT itself proof of collision safety. The future
field resolver must verify the retained trajectory, including its stationary suffix, before
commit. Simultaneous time/branch mapping and junction occupancy are still pending in WP071.
Next: TASK-071B3 common timeline and timed trajectory sampling.
