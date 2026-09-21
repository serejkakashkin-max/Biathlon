# G2 environment uplift report

2026-09-20. **G2-VR-004A DONE: controlled fir integration. G2-VR-004 remains ACTIVE.** Benchmark-only remediation; no production venues created or visually approved.

## Selected source and adaptation

G2-010, LOLIPOP's Realistic Fir Trees Pack, CC BY4.0: one largest fir variant, source LOD1/2 as near/mid, own18-percent reduction of LOD2 as far. Runtime geometry is **6,014/2,997/1,090 vertices;6,633/3,268/588 triangles**. Previous procedural geometry was12,104/3,368/568 triangles. Four maps only: bark albedo/normal≤1K, foliage albedo2K and normal1K. Source gallery offsets are removed, all LOD ground pivots corrected, maximum height8m and actual radial envelope below2.5m. Source UV repeat is retained; per-vertex bark/leaf channels consolidate each LOD to one instanced material/submesh.

The source cross-card LOD3 failed the2.5m envelope test and showed a horizontal intersecting plane. It was rejected, not exempted from the budget. Far geometry was rebuilt from the volume LOD. Decimation also moved the lowest point2.1mm below ground; authoring now derives the ground offset independently after simplification. Final envelope tests pass without relaxing their tolerances.

`AuditedFir.shader` clips texture alpha consistently in forward/depth/shadow passes, uses mapped normals, two-sided foliage, winter coverage and existing observed visual wind. Original deterministic placement, route exclusion, density, camera culling, instancing batch cap and warmed0GC are unchanged. Only `VisualBenchmarkPresentation` opts into the new assets; legacy vegetation/weather fixtures retain their original resources. No Domain/Simulation data changes.

Attribution accompanies source/derivative manifests and ships beside the player. Adapted geometry/textures remain CC BY4.0; raw FBX/archives remain staged and are not published. [Derivation](../licensing/g2_visual/fir-derivation.json), [runtime hashes](../licensing/g2_visual/fir-runtime-manifest.json), [shipped notice](../../Assets/Resources/Biathlon/G2ThirdPartyCredits.txt).

## Validation

Full PlayMode **107/107 PASS**,92.72s, including new envelope/material/culling parity/0GC tests and hidden isolated/integrated captures. Final Unity Console **0 errors/0 warnings**,compilation_failed=false. Windows Development build **252,904,984 bytes,0 errors/0 warnings**; hidden player **9/9 actual rendered scene/tier cases PASS**. [Tests](../quality/g2/fir-a/playmode-full.json), [Console](../quality/g2/fir-a/console.json), [player](../quality/g2/fir-a/player/verification.json). Build manifest SHA-256:`022c7cc1a18ad5194a2d7738dccf303e545f1797c920fc64e01f278e4c1f4d0e`.

Three same-view isolated LOD captures and six integrated sector/athlete/range captures were recorded. Close near/far foliage and representative athlete/sector/range views were inspected. The foliage silhouette/density improves the previous procedural branch shapes. The far LOD is visibly sparse at an intentionally close inspection distance; it is used only at the existing distance threshold. Uniform species/repetition and snow shading still need the planned composition/lighting pass. This is not final G2 visual acceptance.

## Re-profiling with unchanged WP166 budgets

Same hidden1920×1080 DX11 Development player, ten cases,128 warmup frames and300 real rendered samples per case; RTX5080/Ryzen9950X3D. [Raw evidence and analysis](../quality/g2/fir-a/profile/analysis.json). CPU main p95, draw and SetPass checks pass; three triangle failures remain. GPU/render-thread/batch counters remain UNAVAILABLE, so overall status is **ACTION_REQUIRED**, with no FPS/minimum-hardware qualification. This build includes prior hero/rifle changes too; comparison with WP166 is not an isolated causal A/B for the forest.

| Case | CPU main p95 ms | Maximum draws | Maximum triangles |3M triangles|
|---|---:|---:|---:|---|
| low-approach | 1.177 | 598 | 1,030,564 | PASS |
| low-athlete | 1.23 | 596 | 1,744,054 | PASS |
| low-range | 0.853 | 277 | 1,013,138 | PASS |
| balanced-approach | 1.266 | 1015 | 1,832,781 | PASS |
| balanced-athlete | 1.376 | 742 | 3,903,385 | FAIL |
| balanced-range | 1.092 | 482 | 1,722,047 | PASS |
| high-approach | 1.443 | 1181 | 2,132,967 | PASS |
| high-athlete | 1.375 | 778 | 5,711,579 | FAIL |
| high-range | 1.198 | 518 | 1,849,109 | PASS |
| high-snowfall-athlete | 1.698 | 817 | 5,793,269 | FAIL |

The profile completed without error/assert/exception and emitted PASS for **execution**, not budget qualification. One `Releasing render texture that is set to be RenderTexture.active!` warning occurs on player shutdown after execution PASS; retained in the compressed raw log. The final Editor Console snapshot and build0/0 are separate observations and do not erase this warning.

Next: **G2-VR-004B snow detail and terrain/route composition**. Remaining environment work includes pine/understory/rocks and variation; later integrated performance work must reduce actual submitted/shadow geometry rather than raise budgets. Stadium/crowd/lighting and final owner review remain outstanding. G2 NOT APPROVED; M13/WP167 BLOCKED_G2.

## G2-VR-004B — snow detail, guarded banks and ground fit

2026-09-20. **Technical checkpoint DONE; G2-VR-004 remains ACTIVE.** G2-017 Poly Haven Snow02 (CC0) contributes only diffuse/OpenGL normal/roughness at 2K/2K/1K. The original layer palette, groove filtering and roughness logic are preserved. World-metre scanned detail is strongest on loose snow and weakest on ice/packed trail. Existing materials default to detail weight zero. No collider, route physics, athlete contact plane, race tuning or authoritative data changed.

Small edge banks (maximum14cm) and off-route relief (maximum18cm) are authored in the existing surface mesh. A1.5m guard beyond the7m ski corridor prevents one-metre triangles from interpolating raised vertices beneath the lane. Tests sample the generated triangles against the original interpolated contact surface, including edges, rather than relying only on the analytic height function. The stadium platform remains unchanged. Forest placement now samples the world-space surface once during initialization, retaining tree yaw, scale, density rank and allocation-free camera preparation.

All four new tests PASS; full PlayMode **111/111 PASS in97.30s**, final focused **4/4 PASS**. Windows Development **261,997,752 bytes**, build0 errors/0 warnings, hidden actual renders **9/9 PASS**. [Tests and captures](../quality/g2/snow-b/), [player verification](../quality/g2/snow-b/player/verification.json). Player build-manifest SHA-256: `a88def6a3b0bd2372b80f62cb1664c0a48019dea15472f496fd39a9a3597b7a6`.

Four high-tier hidden views (approach/sector/athlete/range) were visually inspected. Fine snow variation is now visible, while the route and stadium are readable. Repeated texture motifs remain visible from elevated views; broader terrain composition, species variation, stadium dressing, crowd and atmosphere are still outstanding. This checkpoint is not a G2 visual pass.

The final Console is0 errors/3 MCP lifecycle warnings, compilation_failed=false. These warnings concern a disposed buffered log and closed/disposed SignalR connection during the requested domain reload; the native console entries are retained. They are tooling warnings, not snow/forest runtime failures. An invalid diagnostic MCP snippet (`return` in its void body) generated three tool-error entries after the successful build; these are retained in console-errors.json. MCP clear-logs encountered a locked plugin log file; the Editor Console was cleared through its native API, then the focused tests were rerun successfully and console-state.json captured. No failing game test or source compile error was suppressed.

Repeated hidden profiling: ten cases,128 warmup and300 samples each, unchanged WP166 budgets and hardware. CPU main/draw/SetPass remain within limits; three triangle failures and unavailable GPU/render-thread/batch counters remain. Overall **ACTION_REQUIRED**, not FPS/minimum-hardware qualification. The same render-texture shutdown warning follows execution PASS and is preserved in the raw compressed log.

| Case | CPU main p95 ms | Maximum triangles |3M budget|
|---|---:|---:|---|
| low-approach | 1.222 | 1,030,564 | PASS |
| low-athlete | 1.214 | 1,744,054 | PASS |
| low-range | 0.863 | 1,013,138 | PASS |
| balanced-approach | 1.415 | 1,832,781 | PASS |
| balanced-athlete | 1.529 | 3,903,385 | FAIL |
| balanced-range | 1.100 | 1,722,047 | PASS |
| high-approach | 1.451 | 2,132,967 | PASS |
| high-athlete | 1.373 | 5,711,579 | FAIL |
| high-range | 1.116 | 1,849,109 | PASS |
| high-snowfall-athlete | 1.478 | 5,793,269 | FAIL |

[Source derivation](../licensing/g2_visual/snow-derivation.json), [runtime hashes](../licensing/g2_visual/snow-runtime-manifest.json), [profile evidence](../quality/g2/snow-b/profile/analysis.json). Next: **G2-VR-004C selected pine variation and bounded forest composition**. G2 remains NOT APPROVED; M13/WP167 BLOCKED_G2; OWNER-FEEL-001 remains OPEN.

## G2-VR-004C — selected pine and mixed forest

2026-09-21. **Technical checkpoint DONE; G2-VR-004 remains ACTIVE.** One G2-011 medium pine variant from LOLIPOP (CC BY4.0) now replaces a stable35% share of existing benchmark trees. World-position hashing is separate from density ranking; no new trees, simulation state or colliders. Original positions, ground conformance, scale, density and camera LOD/culling counts are retained. Two species use six bounded instancing groups, with no warmed allocations. Legacy fixtures remain fir/procedural unless explicitly opted in.

Source LOD1/2 form near/mid; an80-percent reduction of LOD2 is far. Final geometry: **2,282/1,201/1,026 vertices;2,355/1,253/1,001 triangles**. The25-percent far experiment visibly broke branch silhouettes and was rejected. Using source LOD0/1/2 exceeded the comparative submitted-geometry test in one tier; the final moderate selection passes that test without weakening it. The far mesh is heavier than the fir's far mesh, so distant views can cost slightly more even when nearer views improve. Baked gallery offsets are removed using the low trunk centroid, each LOD is grounded and normalized to8m, and horizontal scale fits the existing2.5m canopy envelope.

Five source textures become four runtime maps: bark color/normal, leaf color combined with the explicit opacity map, leaf normal. Leaf color≤2K, other maps≤1K, coverage-preserving mipmaps, existing alpha/winter/wind shader. No billboard/source FBX or unrelated pack content imported. [Source derivation](../licensing/g2_visual/pine-derivation.json), [runtime hashes](../licensing/g2_visual/pine-runtime-manifest.json); CC BY notice/modifications ship in player credits.

Focused **3/3**, full PlayMode **114/114 PASS in100.08s**. Unity compile successful, final Console **0 errors/0 warnings**. Windows Development **270,358,989 bytes**, build0/0, hidden actual-render smoke **9/9 PASS**. [Evidence](../quality/g2/pine-c/), [player verification](../quality/g2/pine-c/player/verification.json). Player manifest SHA-256: `3caaa7f2b46311ef5b55f406fcaadeace9c5d62fd403631d3a538408d5bd1fcc`.

Near/far isolated captures and an integrated athlete view were visually inspected. The final far silhouette retains branch continuity; pine trunks/crowns add species contrast. Forest composition still needs ground detail and wider variation, and this does not resolve the weak stadium/crowd/atmosphere or constitute G2 approval.

Ten unchanged hidden profile cases again complete. High-athlete maximum triangles falls from5.712M to5.404M; balanced athlete from3.903M to3.771M. Far-dominant approach/range views rise modestly. CPU main/draw/SetPass budgets pass; **three triangle failures remain**, GPU/render-thread/batch counters UNAVAILABLE, overall **ACTION_REQUIRED**. The retained shutdown active-render-texture warning occurs after execution PASS. No FPS/minimum-hardware claim.

| Case | CPU main p95 ms | Maximum triangles |3M budget|
|---|---:|---:|---|
| low-approach | 1.130 | 1,035,933 | PASS |
| low-athlete | 1.233 | 1,703,128 | PASS |
| low-range | 0.860 | 1,019,070 | PASS |
| balanced-approach | 1.188 | 1,841,454 | PASS |
| balanced-athlete | 1.246 | 3,771,164 | FAIL |
| balanced-range | 1.003 | 1,734,274 | PASS |
| high-approach | 1.234 | 2,147,835 | PASS |
| high-athlete | 1.263 | 5,403,634 | FAIL |
| high-range | 1.030 | 1,871,661 | PASS |
| high-snowfall-athlete | 1.427 | 5,485,324 | FAIL |

Next **G2-VR-004D: selected CC0 boulder LODs and off-route composition**. G2 NOT APPROVED; M13/WP167 BLOCKED_G2; no owner action required to continue remediation.

## G2-VR-004D — controlled boulder derivatives and off-route dressing

2026-09-21. **Technical checkpoint DONE; environment visual acceptance remains OPEN.** G2-020 Poly Haven Boulder01 (CC0) source LOD3 has8,264 triangles; selected near/far derivatives have **1,652/494 triangles,1,578/671 vertices**. Source real-world scale is preserved with ground pivot/Unity axes. Only diffuse/OpenGL normal/roughness are imported at2K/1K/1K; EXR data maps are converted without an sRGB transform. Source FBX/EXR are not shipped. A project shader blends snow onto upper-facing surfaces while retaining scanned rock detail on exposed faces.

Sixteen placements in data use unique stable IDs, bounded scale and route margin (at least2m beyond the existing7m corridor plus each rock's radial envelope). Bases are buried by12% of scale into the sampled visual terrain. No colliders, simulation identifiers or authoritative terrain changes. Two instanced groups, projection-aware LOD, frustum/distance culling, maximum32 placements and warmed zero allocations. [Source derivation](../licensing/g2_visual/rock-derivation.json), [runtime hashes](../licensing/g2_visual/rock-runtime-manifest.json); CC0 acknowledgement ships.

Focused **4/4**, full PlayMode **118/118 PASS in102.63s**. Final Console **0 errors/0 warnings**, compilation_failed=false. Windows Development **275,414,149 bytes**, build0/0, hidden actual-render smoke **9/9 PASS**. [Evidence and captures](../quality/g2/rock-d/), [player verification](../quality/g2/rock-d/player/verification.json). Player manifest SHA-256: `86ef69fc7c9b411493459c5d3485f9b8a12909ca3d5b20663152edf1fa63063b`.

Near/far isolated and integrated athlete views were inspected. Both meshes retain a usable silhouette and upper snow coverage. The integrated image exposes a remaining composition issue: rocks look detached from the distant forest edge. Their relationship to forest/terrain must improve in004E; this is not a finished natural venue or G2 approval.

Ten unchanged hidden profile cases complete with CPU main/draw/SetPass budgets satisfied; three athlete-view triangle failures remain. The added rocks do not increase the recorded maximum in those three cases; approach-view maxima gain4,446 triangles. GPU/render-thread/batch timings remain UNAVAILABLE. Overall **ACTION_REQUIRED**; the same shutdown render-texture warning is retained after execution PASS. No FPS/minimum-hardware qualification.

| Case | CPU main p95 ms | Maximum triangles |3M budget|
|---|---:|---:|---|
| low-approach | 1.065 | 1,040,379 | PASS |
| low-athlete | 1.079 | 1,703,128 | PASS |
| low-range | 0.854 | 1,019,070 | PASS |
| balanced-approach | 1.171 | 1,845,900 | PASS |
| balanced-athlete | 1.242 | 3,771,164 | FAIL |
| balanced-range | 0.996 | 1,734,274 | PASS |
| high-approach | 1.228 | 2,152,281 | PASS |
| high-athlete | 1.260 | 5,403,634 | FAIL |
| high-range | 1.033 | 1,871,661 | PASS |
| high-snowfall-athlete | 1.441 | 5,485,324 | FAIL |

Next **G2-VR-004E: forest-to-route composition and rendering-budget diagnosis** before stadium/crowd/lighting. G2 NOT APPROVED; M13/WP167 BLOCKED_G2; OWNER-FEEL-001 remains OPEN.

## G2-VR-004E — measured shadow geometry correction

2026-09-21. **Technical checkpoint DONE; visual acceptance remains OPEN.** The unchanged high-athlete single-case repro failed at5,403,634 triangles against the3M budget. Four isolated hidden-player ablations each changed one shadow source, using the same initial build/case/sample schedule:

| Diagnostic | Maximum triangles | Reduction from baseline |
|---|---:|---:|
| baseline |5,403,634|—|
| crowd shadows disabled |3,606,514|1,797,120|
| forest shadows disabled |4,197,490|1,206,144|
| terrain shadows disabled |4,731,634|672,000|
| athlete shadows disabled |5,265,146|138,488|

The dominant avoidable work came from repeated detailed geometry in shadow passes. Benchmark forest near/mid instances now cast from the next coarser LOD; near crowd casts from its far mesh. Visible geometry, transforms, density/population, pose and hero shadows are preserved. Existing other scenes keep their default shadow behavior. Source-shadow triangle ledgers are separate from visible geometry and do not pretend to count renderer cascade multiplication.

The168,000-triangle visible terrain is unchanged; a separate **55,968-triangle /28,320-vertex adaptive grid** casts its shadows. A first uniform42K mesh failed the8cm surface-height test (12.4cm error near snow banks) and was rejected. Full-resolution rows/columns near the route and turns preserve that original8cm limit. The final mesh uses33.31% of the visible terrain triangle count; no budget or height tolerance was raised. Integrated athlete/range/crowd close views were inspected: no visible geometry/population loss or obvious disconnected shadows. Fine shadow silhouettes are intentionally approximate and remain part of final visual review.

The original repro is now GREEN at **2,733,881 triangles**. The diagnostic harness requires an explicit single case, runs in isolated hidden players and labels every sample as diagnostic. The normal analyzer rejects these samples, even if they are copied into an otherwise complete matrix. Diagnostic runs do not count as performance qualification.

Validation: probe parsing **3/3**, shadow geometry/visible parity/zero-allocation/integrated capture **4/4**, full PlayMode **125/125 PASS in106.797s**, Python analyzer regression **5/5 PASS**. Console **0 errors/1 warning**, compilation_failed=false: the warning is MCP BufferedFileLogStorage "Flush called but already disposed, ignored" at test completion, retained in evidence. Windows Development build **277,453,877 bytes**, build0 errors/0 warnings, hidden render smoke **9/9 PASS**. No authoritative gameplay, simulation, shooting, replay or balance change; deterministic race regression is not required for this presentation-only change.

The full ten-case profile (no diagnostic selector/ablation) has **all observable budgets PASS**, including the unchanged3M triangle threshold. GPU/render-thread/batches remain **UNAVAILABLE**, so overall profile status remains ACTION_REQUIRED, without FPS/minimum-hardware qualification. The pre-existing render-texture shutdown warning is retained in player logs. [Evidence](../quality/g2/shadow-e/), [full profile](../quality/g2/shadow-e/profile/analysis.json), [isolated diagnosis](../quality/g2/shadow-e/diagnostics/).

| Case | CPU main p95 ms | Maximum triangles |3M budget|
|---|---:|---:|---|
| low-approach | 1.157 | 816,315 | PASS |
| low-athlete | 1.239 | 1,333,484 | PASS |
| low-range | 0.928 | 778,422 | PASS |
| balanced-approach | 1.428 | 1,397,772 | PASS |
| balanced-athlete | 1.344 | 2,329,564 | PASS |
| balanced-range | 1.045 | 1,231,538 | PASS |
| high-approach | 1.476 | 1,704,153 | PASS |
| high-athlete | 1.693 | 2,733,881 | PASS |
| high-range | 1.167 | 1,368,925 | PASS |
| high-snowfall-athlete | 1.703 | 2,811,809 | PASS |

Next **G2-VR-004F: forest-to-route composition** with explicit world-route/stadium clearance and the same performance budgets. Original environment layout still leaves a wide empty area between trail/rocks and tree line. Hero/motion and stadium/crowd/lighting actions remain open; G2 NOT APPROVED, M13/WP167 BLOCKED_G2, OWNER-FEEL-001 OPEN.

## G2-VR-004F — forest edge composition and camera clearance

2026-09-21. **Technical checkpoint DONE; integrated visual acceptance remains OPEN.** Added24 explicitly authored fir/pine trees in small uneven groves between the trail rocks and the three distant forest clusters. This reuses the already audited derivatives and shipped CC BY notices; no new third-party source was imported. Stable IDs drive density ranks independently of array order. Low/balanced/high remain nested populations. Placements own scale/yaw, copy input arrays, sample the same visual terrain once, and add no colliders or authoritative state.

Validation rejects nonfinite/out-of-range values, duplicate IDs, close trunks, canopy intrusion into either leg of the benchmark route, stadium encroachment, and near-camera obstruction. The first layout passed route checks but blocked most of the approach view; it was **visually rejected**, retained as `rejected-obstructed-approach.png`, and corrected by moving the eastern middle grove. A regression reproduces and rejects that obstructing coordinate. Camera positions/FOV were not changed to hide the problem. Final approach/athlete/finish/sector/range captures were inspected: the approach is clear and trail rocks now have nearby trees; the stadium remains visually sparse and the backdrop remains a simple forest arrangement. Understory and later stadium/atmosphere work are still needed.

Focused **3/3**, full PlayMode **128/128 PASS in108.647s**, allocation-free warmed rendering, copied input/terrain fit/route clearance/stable-rank tests. Console **0 errors/1 warnings**, compilation_failed=false. Windows Development **277,459,753 bytes**, build0/0, hidden smoke **9/9 PASS**. Ten unchanged profiling cases have all observable budgets PASS: high approach1,747,661 triangles (+43,508), high athlete2,733,881 and snowfall2,811,809. GPU/render-thread/batches remain unavailable; no FPS/min-spec qualification. [Evidence](../quality/g2/edge-f/), [profile](../quality/g2/edge-f/profile/analysis.json).

Next **G2-VR-004G: selected bounded understory**, followed by stadium/event work. No simulation/balance/shot/replay changes. G2 NOT APPROVED; M13/WP167 BLOCKED_G2; OWNER-FEEL-001 OPEN.
