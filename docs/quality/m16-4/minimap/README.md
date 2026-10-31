# M16.4 WP-F — live vector minimap

2026-10-31. COMPLETE / OWNER TEST PENDING.
Fullscreen test build: `Builds/M16_4_MiniMap_Review/START_TEST.cmd`.
Previous review builds are preserved.

A compact Frostline map sits below the timing card on the left. Cyan directional
arrow: player. Light dots: rivals. Hollow dots: starters still waiting. Gold:
penalty loop, five-target firing line and rivals currently shooting. Checkerboard icon:
the actual finish endpoint. Geometry follows the chosen Sprint/Pursuit/Mass Start
route and the same selected physical slices as the rendered course. There is no
invented course, camera-position marker or hidden speed/position inference.

`RaceMiniMapModel` owns cold geometry/projection and the bounded marker buffer;
`RaceMiniMapGraphic` paints separate retained route/actor layers; `GameplayMiniMap`
binds the existing HUD and preferences. One versioned visual recipe controls sizes
and limits. The existing course-selection helper changes visibility only; its
body is unchanged. `GameplayHud` integrates build/refresh/reset/enable boundaries.
`PlayerBuildM16_4` provides the separate review folder.

The palette and fonts match the approved interface. No second world camera,
render texture, terrain render, blur, new texture or graphics-quality reduction.
The route is cached; only actors repaint at the existing0.1s HUD cadence. Accepted
world positions/headings take precedence; legacy views use actual loop station
and lateral offset. The player draws last and uses a distinct shape. All elements
ignore input. No UI code steps the field, creates snapshots or changes race data.

Validation:

- **14/14 PlayMode PASS**: ten new map cases plus existing HUD and checkpoint/
  cursor/history regressions. All three native format geometries, landmark/
  aspect/fit assertions, invalid definitions, real30-athlete movement, accepted
  scoped-world coordinates/heading, same-snapshot deduplication, real shooting
  context, race reset, locale/contrast/enable/table transitions and layer caching.
- **13/13 EditMode PASS**: existing localized observation, individual-start clock,
  breath and weapon-prompt contracts.
- The warmed30-athlete marker buffer performs2000observation changes with **zero
  managed allocation** and no field tick advance. This is a buffer measurement,
  not a claim that the complete HUD/native painter allocates nothing.
- Native UI captures inspected at1600×900RU and1024×768largeEN/high contrast.
  Total visible HUD area **8.319% / 15.146%**, including the map, below existing
  10% / 17% limits. The map stays below timing, above speed and left of the centre.
  Moving actors repaint the actor layer without repainting the static course.
- Unity6000.6.0f1 Windows build **Succeeded**,0errors,430611219bytes,25.492s.
  The520existing warnings are24CS0618,460UnityAIInference ConvGeneric and36Sentis.
  Actual Console reviewed/retained before clear; final0errors/0warnings, idle.
- Compiled player **2560×1440 FullScreenWindow PASS**, with inspected actual
  world/HUD screenshot `player-qualified/09-race.png`. Real Sprint bib10, waiting
  skip, personal3–2–1, race entry, all four loader stages, profile/recovery/settings
  checks and owner profiles unchanged. No captured C#/shader errors. Optional
  advanced navigation probe is not enabled in this standalone startup; its default
  receipt fields are not used as new navigation evidence.
- **671/671 protected core/resource files unchanged**, including approved coaches
  and shooting definitions. Domain/Simulation/AI/Career/Replay DLLs are all
  **byte-identical** to the preceding Coaches Review build. No movement, balance,
  shooting, scoring, replay-event or save-schema change.

Recheck: `python scripts/quality/report_m164_minimap.py`. Native receipts, captures,
authority/source/binary fingerprints and append-only history guard are retained
here; raw player/Console logs remain under `artifacts/m16-4/minimap/`.

Scope: fixed whole-route overview; markers can overlap in a tight pack. Extreme
off-map markers clamp to the viewport boundary. To preserve the approved clear
aiming/table layouts, the map hides during shooting and TAB/F2, then returns.
No interactive zoom is introduced. Next: owner tests the new build; M16.4 stays
open for acceptance. Design references/current API sources: ADR-0077.
