# M16 — Captured penalty yaw reliability correction

2026-10-15. Source frozen for the root's new complete production soak.

The second-season Pursuit failure was traced to AI steering, not penalty phase
handoff. A valid first entry was followed by yaw overshoot: at tick5760 achieved
yaw was+0.763rad/s with steering+7; twenty ticks later steering-404 still yielded
yaw+0.726. The skier reached the inner snow edge, retreated for thousands of paid
ticks, and finally rejected tick15577. The clock correctly stayed at15576.

The final correction adds optional `penalty_yaw_tracking_error_radps=0.35` in
the existing data policy. Only a moving penalty skier whose achieved yaw differs
from the pursuit request by that amount uses the existing bounded snow inverse.
The original low-speed recovery and all player/range/ordinary-course decisions
remain. Missing/zero field preserves the previous behavior and JSON encoding;
enabled AI contextv29 records the change. No simulation/course geometry, force
solver, athlete ability, body, admission tolerance, response/time budget, phase
gate, visual quality or animation is modified. See ADR-0073.

An exploratory broader ordinary-course version was rejected after it exposed a
first-season moving firing arrival. The final penalty-only scope passes that
exact captured Mass Start in19440ticks. Its fixture now protects the scope.

## Verification

- New `M16PenaltyRecoveryTests`:4/4PASS on CoreCLR; build0warnings/errors.
- Exact prepared second-season Pursuit completes twice in21411ticks, authority
  SHA256 `14f635e15385712d9b13bd086e714afcd96ec6bc13e6de6b8824938b9632a010`,
  identical authority JSON, event hash12010429436503977100 and shot count.
- Historical optional-field-disabled policy reproduces its exact rejected
  tick15577, accepted clock15576 and unresolved source. No fallback pays time.
- Exact first-season Mass Start completes in19440ticks, authority SHA256
  `56774d8ffa1ff5ddd0e2ce1d974e95c498d99d325c7b7616551278e836cfeef4`.
- Broader related AI selection:32/40PASS; eight fail in untouched standalone
  controller/snow-inverse fixtures. An isolated source copy with pre-correction
  `bbd01e62` code reproduces the same eight names and numeric failure messages,
 29/37PASS. Six were already documented in ADR-0052; two concern the pre-existing
  bounded inverse's attainable-yaw search. No test is removed, skipped or relaxed.
  This is not a claim that the complete suite is green.

The first focused attempt had one test-observation error: inspecting the world's
live `LastAttempt` after transaction abort reads its reset `Clear` state. The
correct immutable step receipt retains the rejected course state. The final
test checks `movement.LastStep.WorldAttempt` and the unchanged accepted clocks.

Evidence and LF-normalized source hashes are in `docs/quality/m16/penalty-yaw/`.
Two generated prepared-career captures and their seed/version receipt are pinned
under `dotnet/Biathlon.Tests/Fixtures/m16_penalty_yaw/`. Native qualification and
the fresh48-event/156-week production soak are root-owned remaining acceptance
checks; no full-soak pass or M16 closure is claimed in this report. Headless test
wall time under concurrent validation is not a visible GPU FPS measurement.

## Files

Production: `ManualAiSteeringDefinition.cs`, `ManualScopedAiMovement.cs`,
`manual_ai_steering_v2.json`. Tests: `M16PenaltyRecoveryTests.cs`, two prepared
career fixtures and receipt. Documentation: ADR-0073 and this report. The
diagnosis-only `SoakDiagnosis.cs` records the early5600–7000 tick interval to a
local JSONL file when invoked with `--diagnose`; it does not run in the game.

Race results may change where the AI now controls this previously unchecked yaw
overshoot. The recorded effective command and shared physical cost remain the
authority. The next task is final native qualification and completion of WP-215
on this frozen implementation, followed by the Windows owner-review build.

## Native qualification correction

Unity Mono's first captured Mass Start completed in19442ticks; the CoreCLR
receipt was19440. Mono also did not enter the exact old CoreCLR Pursuit rejection
by tick20000. The first native attempts therefore exposed test contracts tied to
a different simulation runtime, not a new production compile or solver failure.

The final Mass Start test independently runs the full prepared race twice and
requires exact tick, complete authority JSON/hash, events and shots equality,
plus source snapshot immutability. The old tick15577 full-race control remains
explicitly named/compiled on CoreCLR. Every runtime instead also runs the exact
captured invalid-source world primitive and must reject complete-body admission,
issue no proof, preserve every accepted body/cursor/motion value and pay zero
clock or odometer. Commit of that rejected proof must throw. The source clearance
remains materially negative; no tolerance or production policy was modified.

The updated fixture has5 CoreCLR cases and4 native cases. The final native full
Pursuit repeat and policy case already passed before this test-only correction;
the root requalifies the repeated Mass Start and new primitive. Earlier raw test
results are retained, including the native failures; they are not reported as
passing runs. Updated runtime-contract receipts follow the final rerun.

The updated CoreCLR fixture passes5/5 (86.6seconds with concurrent validation).
Both complete race pairs repeat exactly, the historical CoreCLR rejection is
unchanged, and the independent invalid-source guard passes. Production hashes
still match the root's complete three-season soak input manifest exactly. Final
receipts: `penalty-runtime-qualified.trx` and
`verification-runtime-contract.json` in the quality directory. Native repeat
and guard receipts are root-owned final qualification work.
