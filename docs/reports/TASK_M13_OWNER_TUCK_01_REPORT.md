# TASK_M13_OWNER_TUCK_01 — Shared downhill tuck for player and AI

2026-10-05. Status: implementation, focused/core/native behavior and final production
views PASS; shipping full regression/replay/hidden Windows acceptance IN_PROGRESS.

The player holds Space while skiing(gamepad South). The existing shared tuck requires
actual downhill grade≤−0.01, speed≥3m/s and skill/curvature/risk eligibility. It caps
propulsive effort at200, multiplies aerodynamic drag area by0.6 and steering response
by0.75. The advantage comes from reduced air drag, with ordinary costs and trade-offs.
AI now requests the same recorded command under the same physical eligibility.

Activation is scoped to the Woodland manifest through versioned ai_ski_policy.json.
The locked global AI policy remains byte-identical, preserving career/balance witnesses.
Authoring emits the optional policy from the short-course recipe; absent activation
retains legacy behavior. Start/range alignment does not request tuck.

Focused core6 PASS(shared tuck4/AI2), native AI2 PASS, scoped world/tuck/balance/career
checks7 PASS. Disabled activation and deterministic command replay are covered.
Actual native30-body High host shows active player tuck at tick15376 on route130.702699m;
the normal chase/side GameViews include the actual environment, bent knees/torso,
parallel skis and parked poles. Final captures under
`artifacts/owner-traffic-guidance-20261005/` are `native-delivery-tuck-chase.png` and
`native-delivery-tuck-side.png`. The earlier duplicate scratch host view is superseded.
This is native visual/behavior evidence, not hidden Windows FPS/GPU proof.

Files: AiSkiPolicyDefinition; manifest/authoring binding; PackAiRaceRunner input policy;
read-only athlete DownhillSkill; core tests. Existing tuck simulation/presentation tuning
was not changed. New venue AI choices affect race timing and require fresh full replay.
Approved minimal torso motion and shooting difficulty remain untouched.


## Shipping evidence and owner correction — 2026-10-05

The frozen implementation(a79e4cc/58567d9/c9a660c) passed the full core suite:
1067/1067, zero skips. Fresh completed Mass30(23087ticks), Sprint40(43157ticks),
and actual-Sprint-qualified Pursuit40(46092ticks) all passed persisted exact
state/event/result replay without AI. Final native all29 stationary-start bypass,
actual qualified Pursuit40, guidance and build-lease checks passed.
The Windows Development/High build was delivered and subsequently played by the
owner. Hidden startup/400paid-tick checks passed for all three formats; rendering
was suspended, so these checks are not sustained GPU/FPS evidence. Build reported
0errors and496warnings(36Sentis pixel/460ConvGeneric compute shader warnings).
Owner23 disk paths and6 native settings assets/dirty flags were verified preserved.
Portable replay receipt hashes: `docs/quality/m13/traffic-guidance-shipping-20261005.json`.

Owner hands-on review requests additional changes. Long merge delays remain
OPEN(Sprint actor16 waited2003ticks/100.15s at Main150.106m). Early mat navigation,
tuck pose and visible shooting preparation require correction. Full manual direction
is still disconnected from the playable host. No whole-traffic or M13 closure is
claimed. New work follows the owner directive permitting rare physical contacts;
the previous strict no-contact fixtures remain valid legacy evidence.
