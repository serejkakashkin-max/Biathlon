# ADR-0051 — M13 final laps, IBU range order and bounded contact-direction work

2026-10-11. Implemented for owner review. M13 remains OPEN; M14 has not started.

The owner finished a Mass Start and requests the remaining first-turn/second-lap
hitches, later-bout mat assignment, prolonged automatic departure and A/D braking
be corrected without reducing graphics. This explicitly revises the production
automatic steering-brake choice retained by ADR-0049/0050. Historical definitions
retain their original law; production uses versioned gameplay definitions.

IBU Event and Competition Rules, September 2026, article 8.2.1: first Mass Start
shooting uses start numbers; later bouts fill from the right in range-arrival
order. Source: [official IBU rules](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf).
The existing policy worked in the earlier movement adapter but was omitted from
signed world publication. The signed path family now includes ordered Mass Start
lanes. Only an accepted physical crossing of the admission line can stage the
selected path; the common-prefix proof, clocks and event publication remain
transactional. The first bout retains bib assignment. Later selection records
`MassStartRangeLaneAssigned`; a six-athlete integration fixture checks actual
arrival fractions against assigned mats and requires changed bib ordering.

At tick 2198 of a measured 30-athlete workload, 44.934 of 60.448 ms went to the
simultaneous-contact impulse response. Poses do not change during its velocity
iterations, but every support impulse recomputed the same pair geometry. Cache
each canonical pair manifold once per solve, retaining exact impulse order,
current velocities/spin, convergence and work limits. A frozen old solver checks
24 dense 30-body cases, exact outputs and warm zero allocation. In the unchanged
workload, the affected 200-tick window peak falls 61.128 to 19.269 ms. Timing is
local CPU evidence, not a standalone FPS guarantee.

A separate penalty-entry capture made 37,908 snow disk queries in one tick.
Finite-difference contact normals repeatedly evaluated the complete fine body
cover. Production `manual_course_contact_gameplay_v2` uses 5 by 3 sampled support
points solely to estimate the reaction direction. Every point is sampled fresh;
normal probes never admit movement. Complete rectangle/fine-cover course checks,
body dimensions, snow shoulder, clock proofs and trajectory rejection remain.
The capture compares the bounded normal against the exhaustive field direction;
full race and replay checks cover the resulting intentional simulation revision.
Legacy contact definitions retain exhaustive directional probes. Identical
contact-clearance queries also cache exact values, keyed by pose, both complete
owned windows and apron identity; velocity/spin changes cannot change clearance.

Production `cornering_gameplay_v2` removes an implicit longitudinal brake from
signed steering for both player and AI. Grip and slip remain physical; an explicit
brake uses the original force cap. AI can choose that same explicit brake on an
ordinary course, start approach or penalty turn when its steering demand is too
high. This is recorded control intent, not altered power/speed or athlete stats.
The player is passed through, and automatic range transit is excluded. This
avoids repeated wide penalty entries after removing implicit braking.

Private departures are shortened by 1.5 m while preserving firing/entry anchors
and the course rejoin. Equivalent shared rejoin travel maintains the established
equal replaced-main-distance invariant. A short local AI target follows the
private exit bend instead of aiming a chord through its far bend. Manual control
returns under the existing exit-heading alignment rule. No pose snap, rest pivot,
course-centre following or artificial progress credit is introduced. The bounded
authoring script updates track, paths, junctions, visual and dressing together.

Textures, render scale, meshes and restored High shadows are unchanged. No
performance timing threshold is an acceptance gate. M13 closes only after the
owner tests the delivered build; pre-existing Pursuit composition and broad
grandstand-foundation issues remain separate and their tests remain enabled.
