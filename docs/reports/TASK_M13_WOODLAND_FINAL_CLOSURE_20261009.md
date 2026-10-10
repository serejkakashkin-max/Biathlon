# M13 — Woodland final closure and owner test build

Date: 2026-10-10. Owner-directed single-venue scope, maximum 30 athletes.
WP170–174 are deferred by the owner; the Master Plan is unchanged. This report
records the final implementation and receipts, without rewriting older reports.

## Changes

The ordinary production host uses fully manual signed heading. A/D turn the
skis, and releasing the keys retains direction. The rendered athlete follows
the accepted world position and heading. The explicitly requested final mat
approach and short departure assist remain. Earlier natural skiing/tuck,
pole clearance, shooting preparation, early offset cyan guidance and roadside
dressing are included. No player shooting geometry, hidden RNG or difficulty
tuning was added. The easy-shooting owner issue remains open for M15 tuning.

Mass Start, Sprint and qualified Pursuit are limited to 30. The bootstrap and
review definition agree, and 31 is rejected by the review definition. The
Pursuit receives the actual completed Sprint result; qualification is neither
fabricated nor silently truncated. IBU shooting-lane selection is retained
from ADR-0039: Sprint stance halves, lowest available Pursuit number, and
Mass Start bib/arrival order. Current helper choice remains automatic.

Traffic uses rotated occupied bodies, achieved velocity and common physical
contacts. A stationary player can be passed through steering. Wider shared
aprons replace exclusive crossing queues. Private mat curves receive their
own centre target, rather than inheriting a passing offset from the shared
approach. Planted shooting bodies receive ground support. Contacts may still
slow athletes; there is no global paid waiting fallback or hidden speed boost.

Production uses explicit practical tolerances: 50 mm contact skin, 50 mm
course sweep tolerance, 40 mm course allowance, 5 mm closed portal tolerance,
10 mm/s terminal rest and 0.01 rad/s terminal spin. A 2.0 m soft snow shoulder
keeps small ski-edge excursions on continuous snow. Ordinary handoff windows
are 4 m, capped at half the successor width, and only apply after the source
endpoint was admitted. The unchanged existing position then pays actual
continuous progress; there is no position warp or free distance. Mat foot
placement uses a 0.50 m longitudinal band, speed at most 2.2 m/s and spin at
most 2.4 rad/s. It dissipates motion while retaining actual pose, paid time and
raw odometer. Phase ownership and full occupied-body checks remain separate.
Strict historical policies remain unchanged. ADR-0040 records these choices.

The encoded Pursuit knot uses a two-nanometre representation reserve. Exact
scoped endpoint integers avoid double/int mismatch. Passive course contacts
use the continuous clearance gradient even at polygon boundaries. Continuing
contact force residual and impact convergence have distinct configured bounds.
Course contact and mat-capture events are recorded for replay/telemetry.

## Separate start and finish

All three formats leave Main at 1130 m on the final lap and turn onto a
50 m finish spur followed by 30 m uncounted runout. Start X/Z is
(138.5, -8.69178); finish is (90.96555, -45.89334), 60.3612 m away. The finish
corridor is clear of start, penalty loop and stands. Earlier laps retain the
normal loop and side range approach. Counted distances are Sprint 4180 m,
Pursuit 4980 m and Mass Start 5980 m. Eight terrain tiles receive bounded road
snow caps; existing forest is retained. Venue validation has zero diagnostics.
The actual Unity Game View receipt shows the separated corridors.

## Verification and acceptance scope

Final receipts live in `docs/quality/m13/separate-finish-20261009/`.
Native numerical checks: 12/12 PASS. Native foot-placement cases: 4/4 PASS.
Native review controls/owner maximum: 4/4 PASS. Native signed production host:
1/1 PASS. These counts are executed tests, not Unity's larger discovery total.
Older native practical fixture: 20/20 PASS before the final handoff/band changes;
its scope/date is retained, and it does not replace the final selected checks.

Historical WP175 evidence covers four canonical snow profiles across three
complete formats and five native weather views. Final signed-steering snow
probes supplement that earlier matrix. WP176 has a saved/restored 52-week,
16-event Woodland season. WP177 has paired climbing-stat, drafting and AI
cadence evidence; final signed fields and exact command replay supplement it.
Final gameplay feel/difficulty and shooting balance remain later tuning work.
No second venue or further cosmetic acceptance loop is introduced.

An actual High broadcast snapshot contains 30 visible skinned meshes,
1,442,712 triangles, 2,464 draw calls and 78 SetPass calls, with about 1.88 GB
Editor allocated memory. Its 15.92 ms frame/13.13 ms render time is one Editor
view, not sustained player FPS. Native dense LOD and hidden player smoke supply
functional content checks. The owner allows hidden checks only and relaxed
M13 acceptance; sustained GPU/FPS qualification is unavailable and is carried
forward explicitly. No unavailable counter is represented as a passed budget.

Earlier failed development receipts remain local. The final 30-athlete run
before the mat band correction completed Mass and Sprint, then reproduced a
moving Pursuit mat arrival; it is not counted as full PASS. The current
production test remains enabled. No failing test was deleted or disabled.

Final complete-field, replay, dense LOD and Windows build receipts are appended
below when those running checks complete.

## Final acceptance method

The 30-athlete cap also updates current roster-boundary and condition fixtures.
The detached historical 60-athlete capacity test remains enabled. The old
40-athlete traffic source is preserved; a new actual completed Sprint30 source
is saved as `tests/fixtures/races/woodland_sprint30_traffic_source_v1.json`,
extracted from the completed Sprint qualification used by the final field test.
Current traffic fixtures use this source instead of inventing start gaps.

The following practical run passed 85/86: `owner-max30-practical-final-20261010.trx`.
Its sole failure was two Pursuit athletes trapped at a course edge before the
50,000-tick limit. The support response had stopped inward recovery rotation.
The final correction retains inward spin/acceleration and binds the changed
rule to world context v24. The subsequent native edge/player check passes 1/1;
old failed results remain visible as development evidence, not as final PASS.

## 2026-10-10 — Final course correction

Ordinary AI now recovers when its owned target is behind the skis. Pure pursuit
sin(pi) otherwise requests virtually zero yaw; the existing bounded bearing
controller supplies intent through the same snow forces. The private mat
hairpins retain their earlier forward controller. No player auto-turns, speed
multipliers or pose changes are introduced. AI context v6 and movement context
v23 bind the behavior for deterministic reconstruction.

The 1138.548–1150 m Main closure contained a folded radius-0.85 m range-return
maneuver. Two Pursuit actors could stall there on their final lap. The finish
fork now leaves Main at 1130 m, before this range-return maneuver, and follows
the existing radius-10 m finish curve. This is a concrete course correction,
not a longer test timeout or a disappearing unfinished athlete. Earlier laps,
range ownership and IBU lane order remain unchanged. Static venue validation
passes with zero diagnostics after the edit. Start/finish separation is now
60.3612 m; the finish remains 50 m with 30 m uncounted runout.

The extra mat regression checks both guarded local projection and the public
physical boundary scheduler: an early point on the folded curve cannot grant
firing arrival; the actual mat endpoint can. An initial assertion incorrectly
expected endpoint projection and was corrected to the existing guarded chart
contract. No shipping projection bypass was added.
## Final technical result

M13 PASS in the owner's single-venue/max30/practical scope. WP175–178 PASS;
WP170–174 stay owner-deferred. Master Plan unchanged. Next milestone M14.

All production athletes finish: Mass30 tick36007, Sprint30 tick35465 and the
qualified Pursuit30 tick35775. Measured headless wall times are 144.884 s,
28.978 s and 97.939 s, respectively (12.43x, 61.19x and 18.26x realtime).
The actual Sprint result is saved as the current30 traffic source fixture.
All12 command-tape tests pass, including complete Mass3/Sprint1/Pursuit3 replay
without AI and exact events/world/resources/results. A separate final81-case
regression and four final signed snow probes pass with zero failed/skipped.
Selected native actual mat-boundary and final-fork probes pass1/1 each.

The mixed36-case integration receipt also contains two failed assertions from
the earlier in-flight fixture build. One referenced a final Main arc removed
by the course edit; the other incorrectly expected an unguarded projection.
Both were corrected to the current physical contracts and pass in the final
81-case receipt and native Unity. The mixed receipt is retained transparently;
it is not represented as a36/36 pass. No tests were deleted or disabled.
The optional full native fixture exceeded240s and was aborted during recovery;
its structured receipt says ABORTED, not PASS. Final selected checks run after
the hidden Editor restart. Compilation settled; no gameplay compile error.

The earlier `native-separated-start-finish.png` shows the1170m fork trial with
47.29m separation. It is retained as dated development evidence. Final data
and `geometry.json` use1130m/60.36m, which the owner build will package.
The final Windows build/hidden smoke receipt follows below after packaging.