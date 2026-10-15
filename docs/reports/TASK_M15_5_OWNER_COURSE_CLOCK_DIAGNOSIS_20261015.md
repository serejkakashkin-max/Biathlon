# M15.5 owner Course clock failure — isolated M15.6 follow-up

2026-10-15. Implementation candidate; release and native qualification belong
to the root M15.6 integration. The owner requested this follow-up after M15.6.
No milestone closure or Windows-build acceptance is implied by this report.

## Observed failure and reproduction boundary

The owner's screenshot and the matching local `Player.log` contain
`InvalidOperationException: Manual field step rejected: WorldRejected / Course /
career_4cb8ad4d77af4d4b81ff4210f65be39c_athlete_0; candidate clock not paid.`
The stack is `ManualScopedAiMovement.Step` -> `PackAiRaceRunner` ->
`ProductionVenueHost.StepDeviceTick`. It is a rejected authoritative course
transaction; the screenshot alone is not evidence of a GPU or animation fault.

The local log has no tick, controls or physical pose for the failing interval.
The interrupted race has no retained completed command journal. The owner's full
input sequence therefore cannot be replayed exactly. The saved profile was read
without modification; it is a v2 development-circuit career at season1/week17.
No private profile file or unrelated game log is copied into this report.

A deterministic search through the production world/drive/contact/course path
reproduced the same `Course` rejection class. Every initial complete ski body
first passes the unchanged native C# ribbon certificate. Fixed seed5156 reaches
the first failing state at sample118: main85.706m, outer lateral5.562509765625m,
heading offset0.092rad, speed9.614m/s plus1m/s outward slip, spin0.579rad/s.
Source X/Z/heading: 215.131683788 / -23.674832777 / 2.685201267.

The reduced regression runs this single50ms world transaction with either the
production5x3 normal probe or the existing full normal field. Both fail before
the fix, with32 repeated responses and no paid tick. This directly reaches
`TryPreviewContactDrivenMotion` -> `TryAppendToOwnedFirstEvent` ->
`TryRespondToCourseEdge`, the same transaction path used by the live stepper.
It is not claimed to be a recording of the owner's entire interrupted race.

## Diagnosis and rejected alternatives

1. Coarse normal sampling was a candidate. Switching5x3 to the full field did
   not remove the rejection; this hypothesis was falsified.
2. With `arrest_contact_spin`, `ApplyCourseReaction` returned after suppressing
   outward linear acceleration and angular acceleration. It skipped the
   existing bounded geometric continuation used by the other contact policy.
   The body could already have inward0.1m/s normal velocity and zero spin while
   its straight finite suffix still crossed a rounded snow edge. Repeated
   responses then reproduced the same failed trajectory until the32-response
   limit rejected the complete shared clock.
3. A first experiment simply reused the geometric continuation. It removed
   the rejection but an explicit energy test found about1946J of additional
   translational energy in the reduced case. That experiment was rejected.

## Change

`ManualScopedWorldCourseContact.ApplyCourseReaction` first constructs the exact
historical normal-only response. If the existing complete collinear-body or
ribbon certificate admits that interval, it returns that unchanged response.
Only an uncertified normal-only suffix receives the bounded curved continuation.
The angular lever remains zero for the arrested-spin policy. The new curvature
reaction removes positive initial reaction work and bounds terminal translation
energy by the original paid force piece. Its energy projection factors the
energy difference by duration before division, avoiding cancellation on tiny
contact prefixes. Historical normal-only response is preserved; no claim is
made that this task retroactively changes all older normal-force energetics.

A conservative source-clearance/displacement/error bound avoids constructing an
exact-source-gap spring when a short unrelated contact interval already fits
the existing admission reserve. The complete continuous world sweep remains
mandatory afterward. The global32-response limit and all geometry tolerances
are unchanged. `CurvedCourseContinuations` exposes use of the new branch and
resets with the existing per-tick numerical diagnostics.

The real30-athlete race caught an intermediate implementation defect that the
single-body probes did not: applying curvature correction to every supported
piece generated excessive acceleration on a0.22-microsecond contact prefix.
That intermediate implementation and a later overly broad reserve-target
experiment were rejected. The final candidate qualifies actual field completion
and saved-reload determinism as well as the isolated edge witness.

`ManualScopedAiMovement` now appends structured failure-only diagnostics to the
existing exception: rejected tick, athlete/occurrence/loop/arc, provisional
clock/pieces, course status, global/actor responses, clearance, accepted and
working poses, velocity, spin and normal. The message allocates only when the
race has already failed; it does not catch, suppress, skip or pay a rejected tick.

The complete continuous body/course/phase proof still decides admission. No
course, body, clearance allowance, response limit, time tolerance, position,
heading, odometer, phase or paid-clock fallback was relaxed. No tuning file,
AI decision rule, gameplay capability, random source, texture or renderer was
changed. Failure still rejects and aborts the unpaid tick.

The manual-world context is explicitly v26
`passive_curved_snow_support`. New near-edge trajectories may change. Stored
command bytes, raw historical career payloads and recorded hashes are not
rewritten; incompatible historical contexts retain their existing explicit
rejection instead of silently inheriting changed physics.

## Evidence

Working evidence: `artifacts/m15-6/clock-diagnosis/`.
Review evidence: `docs/quality/m15-6/course-clock/`.

| Probe | Result |
| --- | --- |
| Nine full production1-athlete1800-tick sustained steering cases before the fix |9PASS; these did not reproduce the fault |
|400-state main-course deterministic search before the fix |FAIL at sample118; Course,32responses |
| Reduced normal5x3/full-field before |2FAIL with identical rejection class |
| Reuse geometric continuation without passivity |2FAIL energy guard; about1946J excess |
| Reduced case with final passive continuation |2PASS, complete tick admitted |
|400-state main-course search, every quarter-piece checked for new-curvature energy |PASS;398course responses; new branch coverage asserted |
| Final focused course/ribbon/physical runtime/journal/historical balance/atomicity regressions |67PASS,0FAIL,0SKIP;2m04s |
| Final production career Mass30, complete race and saved-reload repeat |PASS;2complete races,600shots in each |

The final400-state physical trace SHA-256 is
`fee767c3ef0a3e889b2338b18e6281951449b28a194016a66d2c323798107196`.
The earlier `2645dab5...` trace belongs to the rejected broader continuation;
it must not be presented as the final source result.
This is same-runtime trace evidence, not a cross-runtime bitwise guarantee.

The final actual Mass30 result SHA-256 is
`393a67c37633b758c4560bb0349beb37d508a4538bf97d1026668e32cf767a84`,
at tick19604; event hash5320497708773509095;600shots. Both runs after reloading
the identical career save produce identical result JSON/hash, event hash and
tick. The original source also finishes this case, at tick28344 with result
SHA-256 `a264c7465cb01209d59cd825a4368e44f67fd0d02e84bd7a8eb96d7eeb35c89e`.
The result change is material and explicit. Previously uncertified normal-only
force pieces now receive a different fully admitted curved continuation; changes
to edge recovery propagate through pack contacts, range arrivals and later
penalty recovery. Returning an already certified old candidate unchanged does
not imply that every trajectory or race result stays unchanged. No claim of
equal pre-fix and post-fix race hashes, or final balance acceptance, is made.

The comparison uses the original course-contact implementation from
commit`2f1d9c1002bd49a9e1c142b1702ec9dcd08a36d4`, Git source blob
`ab11be6259e78a340a9c04755a522433d3db7c2a`. Its archived CRLF source SHA-256 is
`16639c327f2ea42e7c3390db24d4355b952c9c4c6a2ca6d7f43105d9d19d28c7`.
MSBuild selects that source through an artifacts-only override; no shared
production source is temporarily reverted. The profile/fixture data and test
assertions remain the same for the controlled comparison.

Native EditMode, Console inspection, final balance report and the integrated
Windows build remain root integration gates. The agent did not run Unity.

Root integration update2026-10-15: native `OwnerCourseClockRejectionTests`
completed12/12PASS,0FAIL/0SKIP in339.775536s. The MCP request timed out at300s,
but Unity continued and completed; the actual NUnit XML is retained in
`docs/quality/m15-6/course-clock/native-tests.xml`. Console errors after the run:
none. Final balanced-preset race qualification is recorded separately from
these targeted fault checks.

## Two inherited regression-fixture failures

The first focused batch was65PASS/2FAIL. Both failures reproduce with the
original course-contact implementation and are unrelated to the new reaction:

- `MatFootPlacementIsExplicitBoundedAndRetainsTheReachedPoseAndPaidTravel`
  computed firing occurrence4 using the standalone catalog, then replaced the
  catalog with production, where the actual firing occurrence is5. Occurrence4
  is a98m `RangeApproach/Continue` row and correctly does not plant a rifleman.
  The fixture now resolves its firing occurrence after selecting the catalog.
- `PaidOrdinaryEntryMotionDoesNotStickAtZeroThenJumpOutsideItsPortalBand`
  described a main-to-common-range seam but hardcoded previous occurrence0.
  The separate production start spur made that index `StartApproach`, followed
  by `BaseRoute`. The fixture now finds and asserts the actual
  `BaseRoute/Continue` -> `RangeApproach` seam.

All original movement, endpoint, velocity, paid-travel, energy and timing
assertions remain. No failing test was disabled, threshold widened or snapshot
regenerated. The5 affected cases now pass; the final full focused batch is67PASS.
The pre-existing `ai_range_traffic.json` and `format_bindings.json` files were
inspected but not modified; neither caused these index mismatches.

`.NET` build completed with zero warnings/errors. Under the managed Windows
sandbox, default multi-node MSBuild failed without a useful diagnostic;
`--disable-build-servers -m:1 -p:UseSharedCompilation=false` succeeded. Testhost
needed approved local loopback access; the initial90s connection timeout was
a runner-environment failure, not a test verdict or evidence of game slowness.
