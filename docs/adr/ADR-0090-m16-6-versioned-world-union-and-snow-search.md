# ADR-0090 — Versioned complete start-world union and snow-intent search

2026-11-02. Owner authorized repairing old failures in this task. Qualification
in progress; do not represent the historical assembly as current acceptance.

## Start/Main mutual wait

The current Sprint diagnostic stops at tick4071. Athlete6 is at
start_approach122.831065596m,line-1.6; athlete0 is at Main944.854533801m,line-3.4.
Each reports the other as initial blocker, while WorldBodyConflict=false. The
start continuation knows the whole source and only the beginning of Main. The
range-world operator knows Main and every paid route/range loop, but not start.
Neither separate proof covers this pair around the periodic Main approach.

Start recording policy2 adds the exact whole authored start corridor to the
existing complete range-world contact capture. Every sub-interval, oriented body,
seam rotation and incoming tail retains the same proof. Start is still excluded
from paid route/bypass scope. No pose, force, width, body, margin, clock, speed,
power, route credit, athlete ability or RNG changes. Unknown geometry remains
conservative; same-loop self-crossing guards remain exclusive.

The start_continuation_version field now supports0/1/2. Zero keeps original
guards; one keeps the previously published complete two-corridor policy exactly;
two adds the union. New recordings store the actual selected value. Decoding
passes it explicitly. Policy1field-context hashes were captured before this
change on both runtimes and are asserted unchanged:

* CoreCLR b262bcadf0e1c03c35459a69f6262169f4c6841aa7a09ab6c5680f82b3a7a3a8
* Unity a448e76b075145fc2f8f3e8308620a461e8f4cfecf29afc944deb1670c924a8f

The static venue validator recognizes this complete captured cross-loop range/
start-body protection, which executes before local point guards. Same-loop and
unknown/non-range crossings still need full authored guard coverage. Negative
tests remove the world-merge policy or start capture and retain guard failures;
existing enlarged-body/unprotected-crossing controls remain. This diagnostic
change does not alter authoritative collision or route data.

## Reachable snow yaw

The existing shared snow response is not unimodal, even on one signed half.
For the fixed slip/spin witness, command217 attains0.384143575825rad/s, while the
old ternary search chooses563 and reaches0.438659254072rad/s. Scoring/tie rules
and the shared force law are not the cause.

New search policy1 evaluates the existing integer actuator domain in increasing
absolute deflection, negative before positive. The first exact pair is minimal;
otherwise every command is checked and minimum absolute yaw error wins, with
minimum deflection on ties. At most2001previews, no allocation or new tuning.
LastPreviewCount and SearchVersion expose useful diagnostic state. AI intent
still pays the same shared physical/grip/physiology/course costs. The player is
unchanged. Existing recovery/penalty activation conditions from ADR-0073 remain.

ai_snow_search_version is independently recorded. Missing/zero selects the exact
old v3 search/context. New career/production hosts select1; legacy generic
bindings retain0 unless explicitly selected. Old journal playback and current
backward seek are tested separately. This preserves recorded decisions while
repairing new races; no hidden advantage or geometric shot change.

Focused.NET policy/original-v25/venue group44/44PASS before the final extra
negative control. On2026-11-02 native policy/venue/metadata35/35PASS, including
the extra missing-start-capture control and unchanged policy1context. Current
Mass Start and policy1recorded start replay/backward seek2/2PASS. Fresh full
assembly, remaining old replay policies, long current Sprint/Pursuit acceptance
and a separate fullscreen build remain in qualification.
