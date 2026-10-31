# M16.4 WP-H/I — forest LOD continuity and quiet UI

2026-10-31. Candidate is READY FOR OWNER TEST; M16.4 remains OPEN.

Fullscreen build: `Builds/M16_4_Forest_Hud_Review/START_TEST.cmd`.
Unity6000.6.0f1, development Windows build,0errors,520existing warnings,
430634687bytes,25.687s. The shared builder retains its historical receipt ID;
`milestone.json` identifies M16.4. Original close tree assets/textures are intact.

## Reproduction and fix

The native `ForestLodContinuityTests` originally fails: moving2mm across the24m
tree threshold changes37.56% of the rendered fir's canopy pixels. Source LODs
have different branch coverage and were substituted immediately. Retaining
whole trees in WP-G did not fix this separate replacement. Ranked hypotheses
were hard model replacement, imported far-branch loss, and density-tier effects.
The single native tree reproduces the failure independently of tier density or
frustum/cull removal. `forest_red.json` and `ui_red.json` retain both failures.

Full near detail remains to24m. Opaque complementary dither blends adjacent
original models over24–36m and55–75m, with projection-aware zoom. No meshes,
textures, alpha import settings, lighting, fog or placements were replaced.
Visibility counts are unique trees; triangle/submission counters include both
models where needed. Each visible tree draws once or, in its transition, twice.
Lower-tier density-ranked far silhouettes do not accidentally fade out nearby.
Preparation is allocation-free after warmup; original transform cache remains.

Twelve paired native image tests cover both species and the start, middle and
end of both bands. Changed-pixel fraction under the same2mm displacement is
0–1.37% (ordinary raster/projection changes), below the unchanged2% regression
bound. `epsilon/measurements.txt` and24actual PNGs retain the evidence. Native
shader forward/depth/shadow passes are supported and error-free.

The map retains its geometry/player/rivals but loses its title, legend and rival
count. Pace/burst cards lose key captions and unused space. Remove the permanent
help hint, menu/pre-race navigation footers, split-history key footer and OVR/
background-simulation explanations from career cards. Shooting, breathing and
approach labels now describe state without input names or implementation terms.
Keep useful race values, target evidence, readiness/sway, split history, Settings
bindings and deliberate F1 help. TAB/PgUp/PgDn/End and controller behavior remain.
RU/EN and large high-contrast native HUD captures: `ui/`. Normal full HUD footprint
is7.02%; large EN4:3 is13.03%, retaining the approved10%/17% maximums.

## Verification

79unique native tests PASS:54PlayMode and25EditMode. Raw receipts preserve
additional repeated runs after final small shader/cache/assertion changes.

- Forest continuity/visibility/legacy vegetation/crowd:28.
- Clean race HUD, accepted shooting HUD and short-burst presentation:6.
- Minimap, live range board, split history and cursor:15.
- Additional career-card/menu/accessibility cases:5.
- HUD model, localization and career-page transactions:25.

The final11-case run repeats the new forest/clean HUD tests and covers those
additional menu cases. No tests were disabled/deleted or tolerances relaxed.
Existing layout tests now measure actual retained content rather than removed
caption nodes. Console audit before clearing:0errors;75existing build warnings,
matching the prior build's warning classes. Final Console0errors/0warnings,
Editor idle. C#/shader errors and ordinary-player captured errors:0.

`player-qualified/review.json`: ordinary2560×1440 FullScreenWindow; actual Sprint
bib12/start ordinal12, wait skipped, personal3–2–1–0, accepted tick6605 after
scheduled6601. All four loader stages painted with clock held/countdown unarmed;
creator/appearance/settings/recovery/checkpoint checks PASS; owner profiles
unchanged. Advanced optional navigation fields are not enabled in this probe
and are not used as evidence. Inspect `09-race.png` for the cleaned actual HUD.

## Resource measurement and limits

Six matched native1920×1080 offscreen cases, same compiled binary, same2588-tree
authored inventory,30real athletes/frozen accepted tick608, weather/cameras and
three quality tiers. BEFORE disables only the new blend bands in the bound
definition; complete scenery remains enabled in both phases.90warmup frames,
240samples per case,2880total recorded samples and12world captures. Both
processes exited0. Exact arrays and counters: `player-before/`, `player-after/`;
summary: `comparison.json`. GPU:RTX5080; CPU:Ryzen9950X3D; Direct3D11.

| Camera | Tier | Visible trees, both | Blending trees | Median wall ms before → after | After p95 ms |
| --- | --- | ---: | ---: | ---: | ---: |
| Penalty stands | High | 1058 | 1 | 1.869 → 1.842 | 2.012 |
| Penalty stands | Balanced | 1058 | 1 | 1.837 → 1.962 | 2.110 |
| Penalty stands | Low | 1058 | 1 | 1.673 → 1.803 | 1.980 |
| Forest bend | High | 444 | 58 | 1.771 → 1.525 | 1.675 |
| Forest bend | Balanced | 444 | 40 | 1.783 → 1.579 | 1.755 |
| Forest bend | Low | 444 | 23 | 1.610 → 1.460 | 1.625 |

This is a finite extra draw/vertex cost, not a free graphics improvement. At the
forest bend High native whole-frame submitted triangles rise4.918M→5.870M,
including the extra depth/shadow submissions for58blending trees; Balanced
4.307M→4.608M; Low2.734M→2.873M. Far populations are not switched to full detail.
Median sampled GC is34bytes/frame in both phases; isolated forest preparation
tests certify0allocated bytes. Measured wall-time reductions are timing/warmup
variation, not a claimed speedup. Largest median increase is0.13ms on this PC.
Dedicated GPU timings are unavailable (-1); offscreen measurements exclude
ordinary HUD/menu/swapchain/ticking cost and do not certify minimum-spec FPS.

## Protected systems and reproduction

1026of1028task-entry source/resource files remain byte-identical. The only
existing-resource edits are UI wording and the two forest blend distances.
All five Domain/Simulation/AI/Career/Replay DLLs are byte-identical to the prior
Visibility Review build. No race/balance/input/shot/save schema change.
Full task-entry PROJECT_STATE prefix is preserved. ADR-0079 records the choice.

Run `python scripts/quality/report_m164_forest_hud.py` to verify archived native
results, the matched measurement, actual PNGs, fullscreen startup, protected
source/binaries and append-only state. Player benchmark flags:
`-biathlonWinterVenueReview -biathlonForestLodReview -biathlonWinterVenuePhase before|after -biathlonWinterVenueOutput <fresh absolute path>`.

Next: owner drives the forest bend and shooting HUD in the fullscreen candidate.
No owner acceptance or milestone closure is implied by these automatic checks.
