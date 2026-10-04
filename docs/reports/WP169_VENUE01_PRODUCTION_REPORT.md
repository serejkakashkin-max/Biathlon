# WP169 — Venue01 Woodland Basin production report

Closed 2026-10-04. **WP169 PASS / DONE.** Final native/build evidence was captured on 2026-10-03. Consolidated report under `OWNER_M13_VELOCITY_WP169_CLOSURE_2026-10-03.md`; G2 remains APPROVED and the Master Plan unchanged. Evidence uses final imported production sources.

## 1. Final venue identity

**Woodland Basin**, venue `woodland_basin_v1`, schema2 manifest, track `woodland_basin_track_v1`, visual `woodland_basin_visual_v1`. This original winter forest venue shares one stadium/range. Resources: `Assets/Resources/Biathlon/Venues/woodland_basin_v1/`. Final flows proof verifies 256 current sources, 59/60/62 format resources and six frozen assemblies per flow.

## 2. Supported formats

| Configuration | Distance / laps | Shooting | Total participants |
|---|---|---|---:|
| Sprint | 10,000m / 3 | prone, standing | 40 |
| Qualified Pursuit | 12,500m / 5 | prone, prone, standing, standing | 40 |
| Mass Start | 15,000m / 5 | prone, prone, standing, standing | 30 |

Counts include the player; five shots/bout and authored150m penalties. Pursuit uses the actual completed Sprint40 qualification/gaps. Historical generic60 remains regression capability.

## 3. Range configuration

**30 lanes, 3m pitch, left-side entry, right-side exit, 12m rear apron.** Mats are 1.5×2×0.015m. The firing-front datum is Z=0; mat centre is Z=−1 and accepted athlete stop Z=−0.88. **Targets are at Z=50, 50m from the firing front; do not describe this as 50m from the body root.**

Paid apron admission preserves full body/clearance, each region, receipt, seam, selected path and retained tail. Local post-shoot exit clearance fixes the demonstrated exit/Main deadlock. No teleport, visual recentering, body shrink or global queue. Prone support bounds supplement the unchanged body; off-mat arrival stops short pending ordinary paid steering. Six actual native Aiming poses PASS: prone/standing, nominal AI and both signed paid extremes; baked torso/elbows/boots fit. One-body poses do not replace full-field validation.

## 4. Route variants

Routes `woodland_basin_sprint_route_v2`, `woodland_basin_pursuit_route_v2`, `woodland_basin_mass_start_route_v1` share 3,000m Main. Sprint replaces180m with513.333m extension plus final paid1mm adjustment; Pursuit replaces1,383.344m with883.344m shortcut; Mass pays five Main laps. Actual directed paid paths drive motion; only active panels render, with authority/guards/replay preserved.

## 5. Visual/content summary

99 terrain tiles,90 forest groups,26,614 evergreen instances; approved G2 chase/range cameras, hero animation, stance-specific target rings, stadium dressing, simplified crowd and High visibility.

Continuous packed apron removes rectangular private surfaces/raised outlines while retaining physical paths. Bounded snow shoulders replace the Sprint fork curtains. Native yaw0/yaw45 contact2/2, route/contact5/5 and mat/shader5/5 PASS. Inspected ordinary GameViews: [actual paid chase](../quality/m13/wp169-production/final-review-layout-chase.png), [range](../quality/m13/wp169-production/final-range-repaint.png), [stadium](../quality/m13/wp169-production/final-stadium-repaint.png), [paid Sprint connector](../quality/m13/wp169-production/final-sprint-shoulders.png); six actual stance captures: `native-prone-support/results/`. No Camera.Render. Scope is inspected views, not every transition/weather.

## 6. External assets/licenses

No new downloads. Actual notices: `Assets/Resources/Biathlon/G2ThirdPartyCredits.txt`, SHA256 `52bc8e8f53a384a7f2e6b86dfd1591d77b3d1d07f4f319a7c37793941ab7c7bd`.

Blender/selected Poly Haven and3dassets.dev derivatives are CC0. BlurryRender goggles, LOLIPOP trees, William Prosser stand and three winter garments retain CC BY4 attribution/modification notices; lettering/ads/logo-like materials removed. Renderpeople crowd heads remain restricted compiled content, without public combined source redistribution. Arctic Explorer is metadata-only. Final shipped `Builds/WoodlandReviewWindows/THIRD_PARTY_CREDITS.txt` is byte-exact with that source; PASS.

## 7. Actual production race results

| Final imported flow | Complete tick | Unique finish rows | Authoritative field events |
|---|---:|---:|---:|
| Sprint40 | 62,247 | 40/40 | 670,805 |
| Pursuit40 from that Sprint | 57,248 | 40/40 | 784,533 |
| Mass30 | 58,974 | 30/30 | 628,686 |

Real fixed-seed races cover paid range flow/ownership, penalties and finish/results. `artifacts/wp169closure/final-{sprint40,pursuit40,mass30}-record.json` and replay reports verify all40 real Sprint qualifiers/ordered gaps/current bindings. Failed prefixes remain preserved; no all-seed balance claim.

## 8. Replay validation

Three `final-*-replay.json` reports **PASS_COMPLETE_EXACT_REPLAY**: exact initial context, accepted commands, per-tick state, ordered field events and results, **zero AI steps**. `final-production-flows-proof.json` audits tape/context/result/source/resource/assembly hashes, SHA256 `7913cba6ae8a38e362d7fd8f24bfdd65b44810fcf5c1e65139ea6363fa9eb6bd`. Paired replay reports supersede the record-stage “replay not yet proved” label.

## 9. Performance snapshot

Headless record/replay wall seconds: Sprint148.74/132.11, Pursuit162.44/130.01, Mass142.66/113.22, including compressed I/O: faster than realtime for these workloads. Hidden Windows runs initialize and verify the actual production content and paid-control prefixes. The OS suspends normal-camera rendering in these hidden windows: `HIDDEN_WINDOW_SUSPENDED`, zero rendered frames. GPU/frame counters and Windows rendered screenshots are unavailable. An actively rendering camera with available zero draw calls still fails. Native visual acceptance is supplied by ordinary Unity GameView captures separately. No gameplay FPS/GPU claim. Final performance/LOD stays WP178.

## 10. Known deferred issues

Pursuit60 30.05% stopped ski time remains WP177; the concrete Venue01 deadlock is fixed. OWNER-FEEL-001 shooting too easy remains OPEN for WP229/230/232; no hidden RNG/silent rebalance. Weather: WP175; performance/polish: WP178/Alpha/Beta. Restricted crowd regeneration remains local; final poses/views do not qualify all transitions/weather.

## 11. Windows build path

**`Builds/WoodlandReviewWindows/Biathlon.exe` — Development review build PASS.** Unity6000.6.0f1; 359,411,877 bytes; build12.389s; zero errors/warnings. Initial Mass30/High; interactive entries Mass30 and Sprint40; quality High/Balanced/Low; controls and instructions shipped. Qualified Pursuit smoke consumes this venue's actual completed Sprint record/replay; the interactive qualification/menu flow remains M14 work.

Final hidden Sprint40/Mass30 content smoke and exact-qualified Pursuit40 smoke all PASS/exit0, each including real400-tick paid-control prefixes, current route/resource bindings,30 mats/banks and tier configuration. Receipts: `artifacts/production-player-smoke/20261003-161201-355/verification.json` and `qualified-20261003-161201-407/verification.json`. Hidden rendering is explicitly unavailable as described above.

Executable SHA256 `718444f6a718fa668da96eb0a58445b009673310e6129152a63dbf7a92b8de00` identifies the Unity launcher only. Final managed assembly hashes and complete build file-manifest receipt in [acceptance.json](../quality/m13/wp169-production/acceptance.json) identify this content build. Credits packaging PASS. Build-lease EditMode6/6 PASS; all19 pre-existing owner files and native settings/dirty values are preserved exactly. Temporary timing/InputSystem preload changes are restored only if the complete candidate file matches the captured original; independent owner edits are retained.

Earlier failed smokes remain preserved: immediate mat-collider disable corrected the same-frame defect; unavailable hidden frame counters are recorded honestly. These failures are not claimed as successful runs.

## 12. WP169 PASS/FAIL

**WP169 PASS / DONE.** [Consolidated acceptance](../quality/m13/wp169-production/acceptance.json) records the final content, race/replay, native visual/test, build and hidden-content evidence. Core **1015/1015 PASS**, zero failures/skips (`artifacts/wp169closure/core-regression-final-results/final-core.trx`); relevant replay tools20/20 PASS. Native full PlayMode **234/234 PASS**, zero failures/skips: [actual saved results](../quality/m13/wp169-production/full-playmode.xml), SHA256 `e736229e6028672a579af864658138a4b6e3bd3358b3794d851aa9e4916776fc`. The finish-transport timeout did not prevent Unity from completing and saving all234 cases. Subsequent review-layout focused tests3/3 PASS, build-lease6/6 PASS. Final Console0 errors/0 warnings, compilation_failed=false. Earlier failures were fixed without disabling tests or weakening strict terrain-contact tolerances.

Deterministic impact: compact course/range content and explicit local exit clearance change race trajectories. All three final imported flows have complete exact command/context replay proofs; player shooting geometry/tuning and AI physical parity remain unchanged. These fixed-seed proofs are not an all-seed balance certification.

Report/state and the coherent production bundle are published together. Next: **WP170 Pine Ridge**, importing and qualifying its distinct course/environment through the accepted common range/content pipeline. G2 remains APPROVED; no additional owner gate between venues. Complete weather/balance/performance work follows in WP175/177/178.
