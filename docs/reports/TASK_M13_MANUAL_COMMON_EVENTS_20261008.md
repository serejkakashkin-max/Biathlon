# M13 — Common contact and physical-boundary clock, 2026-10-08

Implementation and focused/native validation PASS. Broader .NET allocation
qualification remains OPEN at this record. M13 and its final owner build are OPEN.

## Result

The owned world can now select the next physical scope boundary or body contact,
pay one common interval for the complete roster, resolve the contact, hand off an
authorized occurrence and continue the exact remaining fixed clock. This closes
the need for callers to calculate straight endpoint travel times themselves.
It does not add an operational whole-race runner or change the live host.

- `ManualRacePathBoundaryScheduler`: stable quadratic roots of actual centre
  motion, unchanged frozen trajectory slices, independently proved prefix portals
  and actual nanorounded station. No clamped projection, fitted endpoint chord,
  time rounding, pose correction or velocity reset grants a boundary.
- `ManualScopedWorldEvents`: complete source validation before event search;
  current owned contact collection; common contact/boundary selection; continuous
  body/edge/course/cap admission; stable identity order of reached boundary rows.
  A zero-time notification at an owned endpoint changes no pose, clock, credit,
  lifecycle or replay/event log. Departure still needs the existing session owner.
- `ManualSkiWorldTrajectory`: exposes genuine constant planar acceleration for
  coupled/passive pieces and a driven first piece before any stop/cap breakpoint.
  Arbitrary commanded curved motion is not reinterpreted as a polynomial.
- `ManualRacePathGateSweep`: optional strict end clearance proves that a whole
  event-free interval never touches its cap. Existing default admission policy
  remains unchanged. This prevents a curved interval ending at its cap from
  silently returning `Clear` without a boundary event. A contact before a later
  unresolved boundary remains admissible when its own shorter interval proves
  strict clearance.
- Owned world context is v13 and binds boundary/no-touch policy. Prepared world
  publication still requires the entire exact fixed clock and no pending response.

## Validation and preserved failures

[Machine-readable receipts](../quality/m13/manual-common-events-20261008/).

- Initial .NET 13/15: two fixture defects, unreachable post-contact cap and wrong
  track loop ID. Expanded 16/18: tests incorrectly equated contact tolerance with
  a later actual centre boundary and assumed a mathematical time root equalled
  the stored double fixed duration. Corrected physical expectations are preserved
  with the original failed TRX files. No production tolerance was enlarged.
- Focused .NET 18/18, strict 18/18, final qualified 21/21 PASS. Coverage includes
  paid start/base seams, real shared-force penalty entry/repeat/return with session
  publication, contact-before-cap and cap-before-contact, near-coincident events,
  retained tiny remaining clock, zero-time endpoint ownership, invalid last row,
  deep overlap, reverse start portal, force breakpoint, unsupported curved cap,
  stable permutation, incomplete shooting gate and warm boundary search at 0 B.
- Broader first .NET selection 257/257 PASS. Final expanded selection 258/260
  FAILED: existing `ManualScopedWorldRosterTests.RepeatedPreparedFullRosterQueriesAllocateNothing`
  reports 7,440 B and existing
  `ManualSkiContactForcesTests.WarmForceManifoldSolvesAndTrajectorySamplesAllocateNothing`
  reports 3,144 B. All assertions and failed receipts remain intact. This record
  does not treat the earlier pass as resolution of these failures.
- Native Unity EditMode: final new events 21/21, existing working phases 5/5,
  existing continuing forces 19/19 PASS (45 executed tests). MCP `TotalTests=1500`
  is discovery, not execution. Earlier 18 native event passes remain separately
  recorded. Unity 6000.6.0f1 finishes not compiling/not playing; Error and Exception
  Console queries both empty. No visible standalone window or build was launched.

## Allocation investigation added during final regression

SDK 8.0.425 / CoreCLR 8.0.31 Windows x64. A package-free, bounded diagnostic has
no game or NUnit dependency. Its measured empty method contains two current-thread
counter reads and `Thread.SpinWait`; anchors and results remain outside it.
Default concurrent GC gives 7/256 positive empty intervals (six 8,136 B and one
1,448 B), with normal empty controls 0/32 and real 64-byte controls 32/32 correct.
The identical compiled DLL with only concurrent GC disabled gives 0/256, again
with both controls correct. Both runs issue 117 gen2 requests and observe 119 gen2
collections. This proves a local counter discrepancy under background collection,
independent of the game; it does not by itself trace each earlier test failure.

Primary upstream context: [runtime issue 134724](https://github.com/dotnet/runtime/issues/134724)
and [draft accounting fix 134855](https://github.com/dotnet/runtime/pull/134855).
The upstream initial repro uses another platform; our local Windows receipts are
the evidence for this environment. Next apply/verify a test-host-only concurrent
GC setting, retaining exact zero assertions and real-allocation controls.

## Determinism, limits and next work

Legacy movement, shooting, AI tuning and golden race outputs are unchanged. The
manual world policy/context changes intentionally. Boundary notifications remain
provisional; accepted session publication owns important race events.

Supported event roots are frozen constant-acceleration centre pieces. A curved
centre law near a cap is explicitly unproved until another solver is qualified;
there is no standstill fallback. Ordinary and penalty handoffs retain actual
heading, including when it differs from the next course tangent.

This is bounded composition, not a complete Mass Start/Sprint/Pursuit witness.
Signed controlled slip yaw, general seam/body corridor coverage, changing mat
resources, shooting/finished-actor full runner, full replay/AI/host integration and
40-actor production work/performance qualification remain OPEN. ProductionVenueHost
still uses legacy v1 route/lateral movement. WP-176 stays PASS; WP-175/177/178 and
M13 stay OPEN. One combined final owner-test build follows those qualifications.
