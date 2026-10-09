# M13 — three shooting bouts, stationary corridors and separated stop clocks

2026-10-09. **M13 OPEN. Full four-bout qualification still FAIL. No owner build.**

Three Woodland AI athletes now complete their first three shooting bouts and
return to the manual base course. Two runs compare actual poses, independent
velocities, motion, physiology, piece counts and events exactly. One athlete
completes all four bouts at tick16360 with zero penalty loops. These witnesses
do not qualify a full field, incurred penalties, finish, replay or production host.

## Changes and evidence

The initial three-athlete four-bout test failed at tick7329: an approaching skier
contacted an already stationary second-bout shooter. AI had checked only the
current centre lines. It now checks the full rotated occupied rectangles and
the corridor implied by achieved velocity up to the obstacle's transverse plane.
An exactly stationary, already owned mat is recognized immediately. Candidate
passing lines are checked separately from the old velocity forecast so a free
side remains selectable. The query changes signed AI intent only. Player input,
body dimensions, physical forces and continuous admission remain unchanged.
The captured two-body query failed before anticipation and now passes; moving
away, rotated-envelope and exact zero-allocation controls also pass.

Later trials exposed an independent force-clock defect. A reached contact between
two actors clipped a third, separated actor's neutral stop at its internal
forward-speed breakpoint. This replaced its complete free snow ramp/endpoint
with a constant acceleration reconstruction. A minimal three-body witness fails
on the previous implementation: its requested0.05s becomes0.004380468s. Removing
the foreign pair makes the control pass. The world now tracks actual contact
participants. A separated, certified neutral stop retains its original free
trajectory, including the snow ramp after the forward stop; that internal
breakpoint cannot clip the unrelated contact calendar. Actual participants retain
coupled reactions. No velocity threshold, zero assignment or position correction
is introduced. New contexts identify the changed deterministic physical output.

Broader controller trials (moving mat avoidance, passing-chart changes, changed
pursuit direction, rejected-command alternatives and edge braking) did not qualify.
They are removed from production source; their FAIL receipts are retained.
Temporary reflection/debug instrumentation is removed. Assertions remain enabled.

## Validation

Evidence: `docs/quality/m13/field-stepper-20261008/`.

| Check | Result / receipt |
|---|---|
| Initial all-four-bout witness | 1PASS/1FAIL tick7329; `manual-all-four-bouts-baseline-20261009.trx` |
| Minimal current-line corridor baseline | 2PASS/1FAIL; `manual-mat-corridor-minimal-red-20261009.trx` |
| Foreign-contact neutral-stop baseline | 1PASS/1FAIL; `manual-separated-neutral-stop-baseline-red-20261009.trx` |
| Stop, corridor, three-bout exact repeat and contact forces | 27/27PASS; `manual-stationary-corridor-and-separated-stop-focused-20261009.trx` |
| Whole manual selection | **512PASS/1FAIL/513**; `manual-stationary-corridor-and-separated-stop-all-20261009.trx` |
| Native stop/control | 2/2PASS; `unity-separated-neutral-stop-20261009.json` |
| Native corridor/allocation | 3/3PASS; `unity-stationary-corridor-20261009.json` |
| Native three-bout exact repeat | 1/1PASS; `unity-three-range-exact-repeat-20261009.json` |
| Native contact complementarity/conservation/allocation | 21/21PASS; `unity-separated-stop-contact-forces-20261009.json` |
| Native Mass30 stationary-player bypass,400 actual ticks | 1/1PASS; `unity-stationary-corridor-mass30-20261009.json` |
| Final native compile / Error+Exception Console | clear; `unity-stationary-corridor-final-state-20261009.json` |

Native discovery totals are not executed counts. The sole whole-selection failure
is retained: all-four-bout, three-athlete test at **tick15737**, athlete1, fourth
mat approach occurrence21, arc1.002m. Contact is resolved, but the next full-body
course proof rejects (source clearance0.01788m, sampled bound-0.00212m). This
requires minimization to distinguish an actual boundary crossing from conservative
body-cover rejection. It is not owner acceptance or a paid traffic queue.

## Operational state

WP175/177/178 and M13 remain OPEN; WP176 PASS. Production host still lateral-v1.
Next: minimize and repair the fourth-approach failure, then qualify incurred
penalties, finish/retired bodies, reusable Sprint40 mats, achieved-world draft,
manual command replay and production input/pose/rig/HUD. Full2–3m mat capture and
shooting preparation remain pending. Owner shooting-feel feedback stays open for
explicit later tuning without hidden RNG. Final Windows owner build follows all
M13. PROJECT_STATE history is appended without changing any historical byte.
