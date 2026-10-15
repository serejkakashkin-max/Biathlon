# M15.6 — Recorded v25 course policy compatibility

2026-10-15. Release audit found a concrete compatibility break introduced by the
owner's course-edge correction. The correction intentionally changes the manual
world context from v25 to v26. The journal binds its exact stepper SHA-256, which
includes the world context. Previously the replay viewer always constructed the
new world, so an existing v25 journal failed before its first tick with
`Replay definitions differ from the recorded race.`

## Explicit compatibility, with unchanged strict verification

`ManualCourseReactionPolicy` selects `LegacyV25` or `PassiveCurvedV26` during cold
composition. New gameplay, headless production races and unqualified callers keep
v26 as the default. The historical branch returns the original normal-only
arrested-spin reaction and reproduces the exact original v25 context strings.
The v26 passivity/curvature fix is unchanged. Unknown policy values are rejected.

Replay metadata adds optional `course_reaction_version`. Existing metadata omits
it and resolves to v25; omission remains omitted on serialization. New capture
records the actual world's value,26. Provider, prepared race, initial host and
backward-seek reconstruction carry that selection. Both playback constructions
use the same recorded policy. The journal reader still checks every exact
identity hash, roster, clock, command and footer. No hash bypass, header rewrite,
command rewrite or save migration is used to make an old recording play.

Compatibility is scoped to v25, the immediately preceding approved M15.5 policy.
It does not claim support for older physics contexts that the baseline already
rejected. Old recordings use old physics; current races receive the correction.

## Independent original-source fixture

`Assets/Tests/Fixtures/Replay/v25-original.brp` was recorded by a separate .NET8
executable compiled exclusively from the original Domain/Simulation/AI/Career/
Replay Git blobs at `9867b24bfcaed10dc354267477eb9d665fe50d14`. No current policy
compatibility source was included. The authoring utility and exact inputs are
retained in `scripts/replay-fixtures/` and the adjacent fixture JSON.

This is one real athlete's accepted2,000-tick mass-start prefix at20Hz, seed123,
ending603.048657101m along the route. It covers the start and turns; it is not a
complete race. It contains no natural course-edge responses. Consequently the
same original-source executable separately captures three golden force responses,
including outward support, arrested spin and unchanged inward force. The test
compares the retained v25 branch to those independently emitted numeric values.

The fixture also contains original serialized metadata with an embedded raw v2
JSON payload. That is a serialization-boundary fixture, not an invented career
race result. Canonical metadata and embedded whitespace remain byte-identical.

- Decompressed command SHA-256:
  `0f3e47da72f6c7bc88890d72f74b82505e56d57ec32743a58b46fd76cbcc96b8`.
- Original stepper SHA-256:
  `d94998795c30ec3727cb7e683c9055bb22044c8dabca7a97b8f098650ccfe2fc`.
- Original world SHA-256:
  `8f046cec4c6d244225f7f863bece8607102c4f03732b785851becf04b67af9e0`.
- Compressed fixture SHA-256:
  `555b84408b717bb98dbe80b72313c310e49a85205b297b02bbcbde71943a7474`.

Reproduce from the project root:

```powershell
./scripts/replay-fixtures/Generate-LegacyV25Fixture.ps1
```

The default authoring command writes only its isolated artifact directory. The
explicit `-UpdateFixture` switch replaces the tracked fixture after review. No
working source, package, build output or user save is replaced.

## Verification

`ManualReplayPolicyCompatibilityTests`:5/5 .NET tests passed. The original
v25 fixture opens with its original header hashes and reproduces every retained
pose/velocity/progress value exactly. A new v26 journal reproduces paid resources
and actual physical state exactly on two independent playback instances. Wrong
policy in either direction and a corrupted header are rejected before any tick.
Metadata and preparation validate historical omission, explicit26 and unknown
policy rejection.

Expanded focused .NET run:33/33 passed,0 skipped, including those5 tests, all12
owner course-edge cases, legacy v2 appearance/save migration, weather and replay
library tests, plus both ranked/historical mass-start roster journal tests. Build
completed with0 warnings and0 errors. Raw evidence:
`docs/quality/m15-6/replay-compat/replay-policy-regression.trx`.

The earlier native12/12 owner-edge pass predates this additive policy-selection
layer. It remains useful v26-fix evidence but is not labelled as a native test of
the new replay compatibility. Root performs final Unity compilation, Console
inspection and native compatibility checks before building.

The separate career-core read-only audit found no concrete release blocker in
ID/player retention, world bounds, observed-only history, independent grouped
potential, equal-budget allocation or the shared player/AI stat projection.

## Native fixture correction: preserve each historical runtime's exact bytes

The first native run executed all5 tests:4 passed and the original-source journal
test failed its exact initial-header assertion. This was retained as a failure,
not counted as a compatible recording. Investigation compared the full original
CoreCLR contexts with the independently composed current v25 Unity Mono contexts.
All differing apron JSON values parsed identically, including identical IEEE754
bits. Historical serializers print those doubles differently: for example,
`0.007081673302963822` on CoreCLR and `0.0070816733029638217` on Mono. This changes
the derived SHA-256 without changing the underlying double. The existing journal
contract is runtime-specific; the production reader must continue to reject the
different header. We did not change its normalization or verification.

To test the native compatibility contract correctly, a second independent
original-source fixture was recorded on Unity Mono. A standalone netstandard2.1
library compiles the same original Git blobs with only a mechanical namespace
prefix, preventing its old types from shadowing current gameplay types. It
references only`netstandard`, with no dependency on any current Biathlon assembly.
Before running it in Mono, its CoreCLR output was compared against the unmodified
original-source generator: both the exact context and raw command SHA-256 matched.
Thus namespace isolation does not alter the original serialized or simulated data.

The original-source library then ran in Unity Mono. Its v25 context matched the
current explicit-v25 native context exactly; the final physical state also matched.
`v25-original-native.{brp,json}` contains those original native bytes. Native tests
select this fixture, while .NET tests retain the original CoreCLR fixture. Every
header, command and state assertion remains exact. Neither fixture is rewritten
into the other runtime's format, and no production code changed during this
fixture correction.

- Native original stepper SHA-256:
  `7829585b764d412eb95427db94146aefbf43593836459218ed4d9124c23bf7c8`.
- Native original world SHA-256:
  `c32fcb7580e0fa0dd499264d23d97835dee2348c296f45a8dce3ff2c9ae33431`.
- Native decompressed command SHA-256:
  `b93936c03e4a6c80cc92ff03dbf6cdf8885725ac368a5916a9abd9d3a0e3b87c`.
- Native compressed fixture SHA-256:
  `f7b0c1de9a427b7d4c0c085f044fa3d5df2eb419b33e0391b86168322d11e74e`.

Reproduction: run`Build-NativeLegacyV25Fixture.ps1`, then invoke
`OriginalV25Fixture.Run(projectRoot)` from its isolated library in the Unity
runtime. The authoring assembly and outputs stay under artifacts; they are not
included in the player. Machine-readable context diagnosis is retained beside
the test results. The root's final native rerun of the previously failing case
passed in21.978414 seconds. It used the original Mono fixture, with every hash,
command, endpoint pose, velocity and progress assertion exact. Raw NUnit evidence:
`docs/quality/m15-6/replay-compat/native-original-v25-replay-final.xml`.
Together with the four previously green native cases, all five unique native
compatibility tests have passed across the two checkpoints; the first failed
cross-runtime-fixture attempt remains recorded above.
