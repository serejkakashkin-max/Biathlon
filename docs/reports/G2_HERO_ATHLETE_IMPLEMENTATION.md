# G2 Hero Athlete Implementation

2026-09-20. **G2-VR-002A DONE: controlled source adaptation, native Unity prefab, M11 integration and technical verification. G2-VR-002 remains ACTIVE.** This is an intermediate implementation checkpoint, not final character quality acceptance or G2 approval.

## Imported scope and derivation

Only five audited candidates contribute to this checkpoint: G2-001 Blender CC0 realistic male body, G2-003 CC0 small-pore stamp, G2-025/026 Poly Haven CC0 fabric normals, G2-032 BlurryRender CC BY goggles. No source bundle, external animation library or restricted crowd data was imported. [Body/goggles manifest](../licensing/g2_visual/hero-derivation.json), [material manifest](../licensing/g2_visual/hero-material-derivation.json), [actual distributed credits](../../Assets/Resources/Biathlon/G2ThirdPartyCredits.txt).

`scripts/Build-G2Hero.ps1` exports the actual M11 bind positions through Unity MCP, runs Blender 4.5.9 in a hidden background process with auto-execution disabled, and imports only selected derivatives. The Python process has a nonzero exception exit code. Intermediate JSON/source data stay outside Assets and Git. Reproduction requires the audited local staged pack and portable Blender; offline authoring is reproducible as a procedure, not promised byte-identical across Blender/platform versions. Published hashes identify the actual selected output.

The body receives automatic heat weights, restricted/normalized to four influences from the existing 17 M11 bones, then an offline A-pose-to-M11 rest-pose adaptation. Runtime geometry is continuous across shoulders, elbows, torso, hips and knees. Source multires and materials are removed. Own changes include garment smoothing, explicit collar/hem boundary edges, bib/cap shells, boot geometry, material panels, glove curl and surface-fitted harness ribbons. Three 1K runtime normal maps provide fabric and mild skin detail.

Goggles use geometry only. All original materials/images, including lettering/normal atlases, are discarded. Neutral materials replace them; geometry is resized/fitted to the head and participates in LOD. Front and rear close views were inspected: no source markings remain visible. A first lens placement intersected the face and was corrected; rejected iterations remain local, not final evidence. Source author/link/license and actual modifications accompany the derivative. `PlayerBuildPipeline` also copies `THIRD_PARTY_CREDITS.txt` beside the Windows executable.

## Integration and budgets

Native prefab: `Assets/Resources/Biathlon/Athletes/G2Hero/HeroAthlete.prefab`. `AthleteRigFactory` selects it in gameplay proxies and the ten-athlete benchmark; the original development rig remains a separate fallback and legacy test fixture. No Inspector work is required.

| Body + clothing + goggles | Vertices | Triangles |
|---|---:|---:|
| LOD0 | 17,224 | 29,224 |
| LOD1 | 8,476 | 13,064 |
| LOD2 | 2,874 | 3,482 |

Eight material slots. Original skis, poles and rifle remain separate equipment meshes, so the table is not the total per-athlete draw/triangle count. The new typed skin LOD binding uses projection-adjusted distances 12/45 m with 2 m hysteresis; shooting keeps near geometry. Existing distant pose scheduling and shadow policy remain. No warmed per-frame allocation was introduced by detail selection.

Typed rig/socket contracts, ground-level model origin, camera pivot, root-motion protection and completed-tick presentation inputs are preserved. Authoritative Domain/Simulation/AI/Career/Replay code and shooting tuning were not changed. OWNER-FEEL-001 remains OPEN.

## Verification

- Full PlayMode assembly **99/99 PASS**, 90.49 s, including race/proxy/benchmark integration and allocation checks. Run before the final art-only lens/harness fitting and mesh-name correction.
- Final focused hero checks **3/3 PASS** after those refinements: normalized weights, LOD reduction/hysteresis/shooting override, zero allocation, finite posed vertices, placement-root preservation and actual hidden offscreen captures.
- Final Windows Development build: **243,108,592 bytes, 0 errors / 0 warnings**. Hidden player smoke **9/9 actual rendered scene/tier cases PASS**: Sprint, Mass Start and VisualBenchmark, low/balanced/high. Credits are present alongside the player and in its build manifest.
- Final Unity Console snapshot **0 errors / 0 warnings**, `compilation_failed=false`. Earlier script recompilation also exposed existing obsolete-API diagnostics in legacy Editor/test helpers; these are not new hero import failures. Mesh filename/name warnings in the first authoring iteration were fixed.

Evidence: [full tests](../quality/g2/hero/playmode-full.json), [final tests](../quality/g2/hero/playmode-focused.json), [Console](../quality/g2/hero/console.json), [Windows smoke](../quality/g2/hero/player/verification.json). Seven isolated captures `docs/quality/g2/hero/hero-pose-0..6.png` show front/back, skiing, prone, standing and close front/rear. Nine player captures are under `docs/quality/g2/hero/player/`. Isolated shots validate asset fitting; they are not a substitute for the final combined benchmark review.

## Remaining character work

The continuous silhouette and facial geometry improve on the technical segmented mannequin, but this checkpoint does **not** meet final premium character presentation. Glove/finger shapes and pole/rifle grip need further work; close elbow deformation, bib finishing/identity, material response and the retained technical rifle require another pass. Goggles are deliberately unbranded but remain broad and stylized. Athlete appearance variation is not implemented in this checkpoint. Motion readability and body/equipment contact must be reviewed together in G2-VR-002B/003.

The existing environment/crowd remain visibly weak. No claim is made that hero import alone fixes the benchmark. WP166 GPU-counter limitations and three triangle-budget failures are still OPEN pending integrated re-profiling. Next: G2-VR-002B hand/equipment/cloth refinement, then G2-VR-003 motion presentation before forest/environment uplift.
