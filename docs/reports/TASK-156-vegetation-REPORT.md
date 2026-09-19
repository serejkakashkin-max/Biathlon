# TASK-156 — Vegetation system

2026-09-19. Technical benchmark scope verified; final visual acceptance remains G2.

Original conifer meshes, winter material and generated VegetationBenchmark scene provide a repeatable forest sector. Versioned placement uses a bounded jittered grid and stable hash, with explicit route-polyline clearance including canopy radius and maximum wind excursion. A598-tree fixture supplies nested low/balanced/high density, three geometry LODs, camera-frustum and120m distance culling. GPU instancing submits fixed matrix arrays in batches of at most500; distant trees do not cast shadows. Explicit presentation time drives restrained vertex wind in visible and shadow passes. No external art, textures, packages or brands were imported.

Validation: focused tests3/3 and full PlayMode63/63 PASS. Placement/clearance, invalid definitions, nested density, bounded capacity, assets/LOD, culling, rewind counts and warmed zero-allocation preparation are covered. Fixed close, chase, far and three-quality captures inspected. Wind time12→14 changes1109456 pixels; returning to12 gives zero changed pixels against the original near capture. This tests actual rendered rewind, including shadows.

CPU selection/preparation over500 warmed iterations: low0.0280ms/150visible/275824triangles; balanced0.0453ms/255visible/460344triangles; high0.0664ms/405visible/672888triangles. All tiers0 managed bytes. These are base geometry budgets, excluding extra shadow passes. Editor frame counters showed21 draw calls in chase captures; they include other passes/Editor overhead and are not an isolated GPU measurement. Full GPU/frame qualification remains WP166.

Evidence: `docs/quality/m12/wp156/`. Full63/63 preceded a defensive draw-batch split; post-change runtime confirmed598 visible far-LOD trees, split into two draw submissions, with Console0 errors/0 warnings. Earlier post-compilation Console had0 errors and five warnings: one pre-existing PlayerBuildSmoke obsolete lookup and four MCP disposal/reconnect warnings (native entries retained). Latest Windows playable remains verified WP155; this forest fixture has not yet been integrated into playable venues.

Limitations: flat benchmark terrain, one procedural tree silhouette with scale/yaw variation, visible stylization of close foliage, hard LOD changes and camera-dependent offscreen shadow culling. Assets are replaceable; close foliage/LOD review is required in WP163–165 before G2. Sector root supports translation/yaw at unit scale. Representative terrain/athlete integration is WP164. No authoritative race, AI, shooting or balance changes; OWNER-FEEL-001 remains OPEN.

Next: WP157 snow/ice/packed trail/edge material blending.
