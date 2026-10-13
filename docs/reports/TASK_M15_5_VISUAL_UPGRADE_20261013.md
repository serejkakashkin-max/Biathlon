# M15.5 — visual and animation upgrade

Implemented between M15 and M16 for owner testing. Main merge and M16 have not
started. Work branch: `codex/m15-5-visual-upgrade`. Owner artistic acceptance is
separate from automated verification.

## Delivered changes

- Original authored hybrid skiing with12 observed states, speed cadence,
  continuous technique/phase transitions, retained exertion/tuck layers and
  bounded limb/contact/grip correction. Flat V2 alternate, uphill V1 and fast V2
  timings are distinct. The original procedural path remains selectable.
- Retained G2 body/17-bone/equipment contract and three LODs. Improved cloth seams,
  compression panels, skin/lens/rubber/boot materials; three small stable cosmetic
  builds preserve national colors, leader bibs and purchased ski appearance.
- Existing PBR snow detail with reduced repeated patterns, packed snow/groove
  ridges, grain-shaped push spray, observed edge/effort/surface response and
  bounded quality budgets. No texture-resolution reduction.
- A small grounded, culled/instanced winter detail recipe: reused shrubs/rocks/
  firs plus original fallen logs, stumps and snow drifts. Course clearance uses
  all physical corridors. Existing forest content is retained.
- Clear, overcast, light snowfall, snowfall and winter haze presentation. Existing
  physical weather IDs remain compatible. Snowfall density varies smoothly over
  a visual120-second cycle; wet overcast at the visual temperature threshold
  chooses haze. Performance/High/Ultra labels retain existing saved quality IDs.
- Native checks exposed old current-terrain dressing errors: corrected stadium
  footings,27 crowd pockets and tape footings within bounded nearby snow pockets;
  removed one identical fence. This changes decorative positions only. Terrain,
  paid paths, source meshes/materials and gameplay physics are retained.

No simulation, AI, balance, steering, shot resolution, penalty, career/save/replay
authority, M14 UI redesign or M15 sound redesign is included in this stage.

## Review package

Start `Builds/M15_5_Visual_Review/START_TEST.cmd`. Use
`Builds/M15_5_Visual_Review/COMPARE_MOTION.cmd` for the12-state comparison lab:
screen left original, screen right new. Both receive identical inputs.

`docs/quality/m15-5/review.html` contains ten matched production before/after
cases, five atmosphere frames and three native motion videos. PNGs are untouched
Unity captures; MP4s encode the original90-frame/30Hz sequences with explanatory
labels. They are not generated concept art or reconstructed dataset motion.
`capture-hashes.json` identifies the20 matched images.

The before build uses source art from `d95ef065` with the retained original pose
driver, current instrumented adapters and shared dressing corrections. It is an
art/motion comparison, not an exact rebuild of the entire historical code tree.
Earlier captures with missing rendering counters or stale offscreen skins are
excluded. `before-verified/` and `after-final/` are the final evidence directories.

## Measured performance

Ryzen9 9950X3D, RTX5080, Direct3D11, Unity6000.6.0f1. Two sequential native
development players, identical30-athlete field/2,588 trees, Ultra,1920×1080 URP
SingleCameraRequest.120 warm frames then240 measured frames, accepted race step
every third rendered frame; detached snapshots are captured only on a new tick.
Neither HUD nor display swapchain cost is included. Gallery-only skin matrix
recalculation starts after timing and is not enabled on production athletes.

| Metric | Before | After |
|---|---:|---:|
| Mean main CPU |3.773ms|3.837ms|
| Main CPU p95 |5.557ms|5.813ms|
| Main CPU maximum |6.460ms|6.791ms|
| Mean field pose marker |0.401ms|0.437ms|
| Maximum pose marker |0.654ms|0.541ms|
| Mean submitted draws |673.3|677.8|
| Mean setpass calls |113.6|118.6|
| Mean submitted triangles |6.221M|6.219M|
| Mean managed frame allocation |7,585.58B|7,584.66B|
| Runtime loaded texture estimate |224.33MiB|231.00MiB|

The observed main-thread increase is0.063ms (~1.7%); no whole-frame allocation
increase is apparent at this sample size. The new pose and winter dressing warm
loops have explicit zero-allocation tests. The entire game is not zero-GC.
Loaded texture size uses Unity's runtime allocation estimate, not measured VRAM.
GPU/render-thread timings were unavailable and remain null/-1. These are scoped
render measurements, not a full-game60FPS guarantee or a low-end GPU benchmark.
Raw samples and method: `performance-summary.json`, both `review.json` receipts.

## Verification

The initial full PlayMode assembly executed365 tests:355 passed and10 failed.
The initial XML is retained. No failing test was disabled or deleted. Fixed
fixtures retained geometric/pixel/immutability assertions: elapsed-time menu
painting, existing numbered mat flags, explicit bolt cycles and current
departure/start/portal definitions. Regenerated cold/wet materials from the
visual source data and corrected the actual decorative grounding errors.

The final full PlayMode assembly passed365/365 tests with0 failures/skips in
25m47s, recorded in `docs/quality/m15-5/playmode-final.xml`. Do not interpret the
300s MCP transport timeout receipt as a test result: Unity continued independently.
Focused motion, hero, snow, vegetation, weather and dressing repeats passed.
The stadium authoring10 tests passed with current native geometry, including
invalid section-policy rejection and preservation of omitted legacy fields.

Core deterministic/replay/persistence selection:24 PASS in
`m15-5-core-replay.trx`. Actual30-athlete Mass Start completed20,109 accepted ticks
and replayed195,444 compressed command runs; event digest9212986234871512072
matched. Sprint/player replay and career persistence cases passed in that run.
No race-affecting change was made after this regression.

Native ordinary-player UI/race review passed:12 screenshots,14 navigation actions,
actual production race loaded/started, shooting HUD captured, owner profile
fingerprints preserved and no runtime errors. Its results screen is an isolated
recorded-result boundary fixture, not a claim to have manually completed that
native Sprint. Actual completion/replay is covered by the deterministic tests.

The Win64 development build succeeded with0 errors. The final fresh shader build
reported636 warnings from the existing Unity AI Inference/Sentis2.6.1 shader
variants; the observed warning classification contains no new game-art shader.
The installed package is preserved. This is an upstream warning limitation,
not a claim that the build has0 warnings. Native comparison, motion capture and
ordinary-game review all exited0. See `build-warnings.json` and build summary.

## Resources, provenance and reproduction

No paid art, subscription, NSD video/pose derivative or Rokoko animation was
imported. Existing Poly Haven CC0 maps/models, LOLIPOP CC BY4 fir/pine and original
G2/equipment resources are reused. New motion keys, blend shapes, log/stump/drift
geometry and shader work are original. Existing crowd/license restrictions and
attribution are preserved. Credits are copied into the test build.
The optional owner mocap path is documented in `M15_5_MOTION_PROVENANCE.md`; no
owner login/action is required to run this build. All new motion remains
manual/procedural and does not establish professional mocap fidelity.

Editor generation: `VisualUpgradeHeroAuthoring.Generate`,
`VisualUpgradeSceneryAuthoring.Generate`, `SnowMaterialAuthoring.Generate`.
Keep existing audited source art; do not regenerate the authoritative venue as
part of the visual build. Cosmetic footing repair is reproducible with
`scripts/visual_upgrade_stadium_grounding.py`. The optional first-stand pose is
also in the existing stadium authoring recipe. Final build method is
`PlayerBuildPipeline.BuildVisualUpgradeFinal`.
Native evidence/encoding tooling is under `scripts/visual_upgrade_*`.
The local ffmpeg tool is not shipped or committed as game art.

The next action is the owner's visual/gameplay test of this M15.5 build. Long
sessions and hardware outside this measured configuration still require testing.
