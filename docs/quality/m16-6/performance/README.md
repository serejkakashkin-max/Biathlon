# M16.6.10 — skiing performance investigation

2026-11-03. Owner reports intermittent stalls during skiing. M16.6 remains open
for owner review. No M17 work, tuning, race-duration or visual-quality reduction.

## Reproduction

`RuntimeSkiProfileReview` is an explicit development-player diagnostic enabled
only by `-biathlonSkiProfile -biathlonSkiProfileOutput <folder>`. It builds the real
30-athlete ManualStepper and original production environment, High quality,
1280×720 explicit render target. After 90 warm-up ticks it measures 360 frames
(180 in the native PlayMode test) with exactly one tick per frame and a repeatable
moving stadium/forest camera. Fixed straight manual input deliberately puts the
start pack/course edge under pressure. The camera is independent of the player;
this is a controlled stress case, not a claim about average lap FPS.

`SkiSimulationPerformanceTests.ProductionThirtyAthleteSkiSimulationProfile`
separately drives 2,400 ticks (120 seconds) with 30 athletes for automatic and
fixed-manual input. The automatic athlete covers 795.486m of paid route after its
start approach. Before/after state hashes are pinned from the original code:

| Input | State hash, before and after |
| --- | --- |
| Automatic | `1889c670a7c3587a` |
| Fixed manual | `f8169cb1f5e027d1` |

Each hashes every athlete at every tick: position, encoded heading, raw spin and
velocity, occurrence/observed arc/credit, official progress, reserve/fatigue and
AI steering/braking. This is CoreCLR-specific evidence, not a cross-runtime hash
promise. Native Unity separately runs the geometry/replay policy tests.

## Findings and fixes

Initial standalone medians: race step 4.627ms, environment 1.569ms, athletes
0.802ms. Race-step p95 14.691ms and maximum 26.224ms. Whole environment, athletes
and snapshot are submitted once, not repeatedly in a frame. HUD refresh already
has a cadence; hidden TAB does not rebuild its table. No speculative UI rewrite.

Temporary attribution found actual contact/course proof cost in the pack and
complete snow inversion cost near range approach. Three red regressions captured
the redundant work before each fix:

| Operation | Before | After / invariant |
| --- | --- | --- |
| Saturated snow inverse witness | 2,001 physical previews | 89; same global command `-44`, original full scoring order |
| Four-actor arrival group | 12 endpoint samples | 4; same actual simultaneous arrivals, refresh on next query |
| Two unchanged current-body queries | 2 complete geometry builds | 1; changed pose/participants invalidate, failed geometry uncached |

Frozen trajectory endpoint queries now return their already calculated `State`.
The complete course-ribbon proof also reuses an exactly identical coupled force
law/source/apron between support planning and final admission. Changed force,
duration, source or apron invalidates it; original full geometry is still proved.
Interior/slice integration, continuous exclusion, physical support, tolerances,
work limits and event ordering remain intact. No context/recording version bump
because outputs are unchanged. See [ADR0092](../../../adr/ADR-0092-m16-6-exact-ski-query-reuse.md).

## Measurement limitations

A paired run with uninstrumented original M16.6.9 core assemblies and optimized
core used the same final presentation/harness and old AI policy: race-step
median4.335→4.078ms, p9513.475→12.511ms; frame maximum26.724→25.163ms. This is a
modest local improvement, not proof that all stalls disappeared. Initial and
intermediate runs are retained, including those that did not improve. Final
delivery is measured independently; see comparison.json and raw frame receipts.

Ryzen 9 9950X3D / RTX 5080, Windows, Unity 6000.6.0f1, D3D11, High. The renderer
submits approximately ten million triangles in the stress view, including stable
forest/crowd/dressing shadows. Those casters and shadow distances are preserved.
GPU and per-camera GPU timing counters were unavailable in this player/backend
and are recorded as -1. Editor global GPU timings are not used as world GPU cost.

The diagnostic verifies the per-thread allocation counter with a positive
allocation before using it; unsupported counters report -1. Profiler frame GC
includes snapshots, renderer and host work. Earlier diagnostic zero readings are
not proof of zero allocation in Unity. CoreCLR no-allocation tests remain active.

Wall times include frame pacing; capped medians near 16.67ms do not prove a higher
uncapped FPS. Timing maxima are noisy. Report distributions and exact work counts
alongside state parity. Single expensive physical proofs can still exceed a frame
budget; this bounded change does not replace those proofs with weaker ones.

## Delivery and qualification

Final receipts, same-case player measurements and build hashes are included beside
this report. Three original red results are retained. Temporary core stopwatches
and deep-profiling experiments were removed; five low-cost markers remain in
Unity adapters. The optional diagnostic creates no career and normal launches do
not instantiate it.

Review launcher: `Builds/M16_6_10_Performance_Review/START_TEST.cmd` (fullscreen).
Owner review should repeat the affected skiing section and rotate the camera
through forest/spectator shadows. M16.5 closed; M16.6 open; M17 not started.


Additional final-player qualification: explicit1000-frame profile reaches tick3090/904.149m and captures all5shots. The241 shooting frames have median wall16.664ms/p9516.730ms/max16.899ms; tick median2.063ms/p959.010ms. No per-frame file writes. Camera follows the accepted body in third person, not AimCamera; frame cap/GPU-counter limitations still apply. Raw compressed receipt: ../performance/shooting-profile.json.gz; summary: ../performance/shooting-summary.json.
