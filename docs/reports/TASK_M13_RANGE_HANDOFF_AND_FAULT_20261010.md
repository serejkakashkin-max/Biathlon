# M13 — Range tick failure and control hand-back

2026-10-10. Owner-requested correction and Windows review build. M13 remains
open for the owner's test decision; M14 has not started.

## Result and diagnosis

The supplied player log contains 5,603 argument exceptions. The first rejects
AI26's firing arrival at an admitted folded mat endpoint, with speed about
0.49 m/s. The race tick stays unpaid; subsequent frame input generation then
throws `Commands require consecutive target ticks`. This repeated failure is
a concrete source of the reported post-range slowdown and console flood.

`ManualScopedWorldRoster` now applies the existing bounded foot placement at
the admitted physical endpoint after body/course/gate proof. It preserves
position and heading, records dissipated energy, and still requires exact rest
before shooting. Speed/spin limits and ordinary course/contact proof remain.

`ProductionVenueHost` previews effort before the field step and commits it
after success. `PlayerDeviceInput` retains the pending command until its tick
is accepted. A failed runner reports the original exception once, exposes
`HasSimulationFault`, and stops generating commands against the unpaid tick.
This is fault containment, not recovery or a skipped simulation tick.

`PlayerCourseReturnSystem` captures the local heading once when A/D is released.
The existing gentle bounded intent aligns to that direction; it does not select
a centreline position or follow later bends. Held A/D overrides immediately.
Automatic range movement resets the old release target before hand-back.

Private mat braking now samples the actual turn/straight geometry rather than
applying a tight curve to the entire exit. It uses the shared speed-dependent
yaw capacity, and permits propulsion until a future private turn's stopping
envelope is reached. Ordinary course braking retains its previous policy.
Guidance requests approach effort 650/1000 and departure effort 800/1000, paid
through shared physiology and ski forces. No physical speed bonus was added.

## Verification

- Final bounded .NET regression: 40/40 PASS (`range-bounded-final.trx`).
- Mass30: 30/30 complete the first bout at tick 6124; a separate test continues
  another 300 ticks to 6424, with world/field clocks equal and runner not faulted.
- Final complete signed command replay: Mass3, Sprint1 and Pursuit3, 3/3 PASS.
  All world/resource events and accepted physical state reproduce without AI.
- Final duration scenario: private approach 3.05 s and automatic exit turn
  6.90 s, compared with 5.35 s and 17.30 s in the initial diagnostic scenario.
  The practical 8 s check includes the physical approximately 9 m two-turn exit.
- Native Unity MCP: actual arrow capture, shooting, departure and hand-back
  PASS; deliberate fault containment PASS; all seven device input tests PASS.
  The fault test preserves the unpaid tick and pending command across 600 frames.
  The exact owner AI26 plant witness also passes native EditMode (10 native
  cases across the separate selected runs in total).
- Windows build: Unity 6000.6.0f1, High default, 30 racers, 0 errors/0 warnings.
- Hidden player smoke: Mass30/Sprint30 startup, 400 paid ticks each, quality
  selection and scene transition PASS, EXIT0. Editor Console Error query empty.

Evidence: `docs/quality/m13/range-handoff-20261010/`. The initial broad run
was stopped after 38 passing cases while an extended scenario was still active;
it is an aborted diagnostic, not a passing receipt. Final scoped runs above
are separate. A trial applying the new braking envelope to ordinary course
segments was rejected; the final build confines that change to private mats.

An early native fault-test arrangement incorrectly assumed input prevalidation
faulted the runner. It now explicitly recreates the post-failure runner state.
`LogAssert.NoUnexpectedReceived` also counted an unrelated Unity AI Assistant
NoSubscription information log; the final test checks the expected game
exception using Unity's normal failure handling, without ignoring game errors.

## Limits and next action

The owner log lacks complete preceding force intervals. The exact AI26 pose
is a minimized post-admission plant-decision witness, not a replay of the
owner's complete race. Full 30-athlete races were not repeated in this bounded
correction. The small complete replay tests and first-bout tests have distinct
scope. Hidden rendering was suspended; sustained FPS/GPU performance and
perceived first-range lag still require the owner's player test.

IBU lane selection, one production Woodland venue, max30, High graphics,
G1/G2 approval, and the open M15 easy-shooting feedback remain unchanged.
Race output/context identities can change through effective movement intent;
foreign old tapes are rejected. See ADR-0042 for the control/plant contract.

Owner build:
`Builds/WoodlandM13RangeHandoff-20261010/START_WOODLAND_REVIEW.cmd`.
Russian instructions are in `READ_ME_RU.txt` beside the executable.
Next action: owner checks the first bout/departure, lateral-line release and
manual bends, then decides whether M13 closes and M14 can start.
