# TASK-157 — Terrain material blending

2026-09-19. PASS for terrain material benchmark scope.

Extended original PBR snow shader with opt-in authored RGBA weights for loose snow, packed trail, ice and churned edge. Versioned material data controls albedo, roughness, grain strength and distance-filtered groom grooves. Plain cold/wet snow remains opted out; explicit reconfiguration restores that state. A reusable authoring mesh samples a readonly route polyline, caller-provided height and optional visual ice mask. One shared surface avoids coplanar material overlays. Vertex/definition bounds reject non-finite values and excessive authoring geometry.

Generated TerrainBlendBenchmark uses64561 vertices/128000 triangles, a gently sloped40x100m surface, a7m-wide curved trail and one explicitly authored ice patch. The fixture has no collider and does not alter authoritative surface bindings, course width, friction, shooting or AI. Actual-condition-to-material mapping for the integrated sector remains WP164; the fixture's visual ice mask is not a gameplay condition.

Focused Unity tests3/3 PASS: normalized/continuous weights across1001 lateral samples and three ice levels, interior/exterior cases, invalid data, repeat geometry and input preservation, upward normals, material opt-in/out and generated ice/edge coverage. Four fixed balanced-quality captures inspected: overview, packed center, transition edge and grazing ice highlight. Grain/groom detail fades with pixel footprint; no overlapping surfaces. Evidence: `docs/quality/m12/wp157/`.

Full Unity PlayMode66/66 PASS. Windows Development build succeeded (225390920 bytes,0 errors/0 warnings); smoke exit0, Sprint6 and Mass Start30 at tick400, all six scene/quality combinations. Final Console0 errors/0 warnings, compilation_failed=false. Full build-file manifest and compressed Player log are retained; launcher EXE hash alone is not build identity. Standalone smoke validates existing gameplay scenes; integrated new forest/terrain scene rendering remains WP164.

Limitations: authored vertex-weight resolution, no displaced/deformable snow, no persistent accumulation, no claims of final venue art. Full sector and camera/weather evaluation remain WP164–166. OWNER-FEEL-001 remains OPEN. Next: WP158 weather presentation.
