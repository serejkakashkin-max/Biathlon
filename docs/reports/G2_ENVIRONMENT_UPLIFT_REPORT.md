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
