# M13 final owner corrections — 2026-10-11

M13 remains OPEN for owner review; M14 has not started.

Build: `Builds/WoodlandM13FinalLaps-20261011/START_WOODLAND_REVIEW.cmd`.
Maximum 30 athletes, High default, restored athlete shadows and original textures.

Implemented:

- Contact impulse geometry is evaluated once per canonical pair per solve, with
  exact differential agreement with the old solver and warm zero allocation.
- Bounded contact-direction sampling avoids repeatedly evaluating hundreds of
  fine-cover cells for a direction. Complete course/body admission is retained.
  Identical contact queries reuse exact values under full geometry identity.
- Mass Start signed movement now publishes the existing IBU arrival-order policy:
  first bout by bib, subsequent bouts from the right in actual entry order.
- Shorter private departure and local turn target; native owner-flow check returns
  controls toward the exit aisle after about six seconds of automatic departure.
- Player A/D requests no automatic brake. Physical grip/slip remain; AI chooses
  an explicit capped brake when needed on ordinary/penalty turns. Automatic
  range transit remains outside that policy.

Rules verified against September 2026 IBU ECR article 8.2.1:
[official PDF](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf).

Evidence directory: `docs/quality/m13/final-laps-20261011/`.
The unchanged-workload contact peak falls 61.128 to 19.269 ms. The later
penalty-entry window falls 126.893 to 49.105 ms after bounded normal work. Its
mean falls 19.135 to 9.578 ms. These are .NET simulation CPU measurements;
they do not establish standalone sustained FPS. An isolated later tick still
reached 75 ms: no claim of universally hitch-free gameplay is made.

The 3 m departure experiment violated the private crossing-clearance invariant
and was rejected; the delivered 1.5 m revision retains that clearance. Cached
leaf/tube hints did not materially help the measured peak and were removed.
A control-launch failure caught one missing corridor-knot width before delivery;
authoring and a native resource validation regression were corrected.

Validation results and build/smoke identity are recorded in `delivery-summary.json`.
Final checks include contact/drive/course/venue units, actual range ordering,
full signed Mass30 completion, persisted signed Mass Start/Sprint replay and
native EditMode/PlayMode owner-flow checks. Broader replay selection reproduces
the known Pursuit directed-passage/start-continuation composition failure before
movement. That test remains enabled; Pursuit is outside this Mass30/Sprint30 build.

Two captured-coordinate regressions were pinned to their existing historical
venue fixture rather than discarded when modern course/start geometry moved.
All temporary stage-timing hooks were removed from shipping code.

Authoritative race output changes intentionally through corrected ordered lanes,
production steering/contact presets and departure layout. The new versioned
definitions and path contexts identify this revision; historical tuning remains.

The append-only PROJECT_STATE history is verified against its captured byte
prefix. Existing owner changes in rendering, packages and settings are preserved.
Next: owner tests first turn, second lap, later mat order and exit control; then
decides whether to close M13 and proceed to M14.
