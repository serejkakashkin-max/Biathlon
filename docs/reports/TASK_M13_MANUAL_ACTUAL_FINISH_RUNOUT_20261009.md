# M13 — actual manual finish and physical runout — 2026-10-09

M13 and WP-175/177/178 remain OPEN; WP-176 remains PASS. This closes the
scoped finish transaction, not the production host or the milestone. No build.

The signed-heading candidate now completes a real Woodland Mass Start race
with one or three athletes, all four shooting bouts and all five laps. A finish
receipt uses the end time of the actual paid incoming world interval. The
remainder of that same tick continues onto an explicitly selected physical
runway. While others race, finished actors keep the field clock, apply shared
physical braking and recover at zero delivered effort. Counted race distance
and finish time remain fixed; finish and result events occur exactly once.
There is no finish velocity reset, pose snap, extra race credit or legacy
movement fallback. The existing legacy terminal-command guard remains intact.

`manual_finish_runout_v1.json` supplies the runway distance and braking request.
The catalog adds a non-counted occurrence with an explicit lifecycle handoff
and the exact incoming finish portal. Catalog v4 and stepper v5 contexts include
the selected braking definition. Session, field and Pack publication validate
the same complete world proof before continuing finished rows.

Validation:

- All manual .NET tests: **545/545 PASS**, zero skipped, 4m07s.
- Native Unity EditMode finish tests: **10/10 PASS**, zero skipped, 1m34s.
- Legacy checkpoint/lifecycle/interval-field/time-trial regression: **35/35 PASS**.
- Full actual one-athlete race finishes at tick20001; three-athlete field at20636.
- Independent repeat compares actual poses, velocities, every cursor value,
  odometers, physiology, finish times and the complete event journal exactly.
- Off-centre portal geometry checked in Sprint, Pursuit and Mass Start; these
  geometry checks do not claim complete manual Sprint/Pursuit qualification.
- Unity compilation settled; Error and Exception Console empty.

The initial missing-finish witness failed at the unsupported phase gate.
Two later failing assertions incorrectly equated the production venue's last
intermediate split with the finish. Woodland only authors climb_top and
plateau_exit. The corrected test adds an independent colocated finish checkpoint
and requires exact clock equality, while the unmodified production fixture
requires its intermediate split to precede the finish. Numeric-definition
rejection tests now assert the existing TrackValidation exception contract.
All failed receipts are retained; no failing test was disabled.

Limits and next work: reusable Sprint/Pursuit mat selection/release, actual full
format/large-field qualification, manual command replay, player capture and
production input/pose/HUD selection remain open. Full weather/readability,
balance and worst-case performance acceptance remain required. The owner
shooting-feel feedback remains pending its proper tuning stage. The production
host still uses lateral-v1 auto-follow; the final owner build is only after ALL
M13. Evidence: `docs/quality/m13/field-stepper-20261008/manual-*finish*.trx`
and `manual-actual-finish-native-20261009.json`.
