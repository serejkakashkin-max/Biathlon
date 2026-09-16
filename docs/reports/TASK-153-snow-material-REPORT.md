# TASK-153 — Snow material system

2026-09-16. WP153 PASS. New material tests2/2 and full PlayMode53/53 PASS. Final four-view Game View matrix inspected after the 3D-noise fix; Unity Console0 errors/0 warnings, compilation_failed=false.

Added original `Biathlon/Snow` URP PBR shader, JSON-owned cold/wet presets, generated materials and a repeatable material scene. Macro albedo/roughness variation and analytic grain normals use world-space three-dimensional noise, so snow does not depend on mesh UV density or stretch on vertical slopes. Pixel-footprint filtering fades subpixel grain. Slope darkening exposes orientation; cold snow is lighter/rougher, wet snow darker/smoother. Opaque forward lighting, main/additional lights, directional shadows, fog, depth and normal passes are supported. Presentation parameters never change authoritative friction, conditions or shooting.

The first visual review caught vertical stretching from XZ-only sampling; the final shader uses 3D sampling and tangent-projected gradients. Installed URP source also confirmed that the depth pass must return device depth; this was corrected before acceptance. The initial material test found sub-micro-unit Color round-trip error; channel checks now use1e-6 tolerance and the original failure evidence is retained. No failing assertion was removed.

Material fixture shows both presets on identical rolling ground and curved slope samples under a low winter directional light. This verifies material response, not final venue quality. Multi-surface blending remains WP157, contact tracks WP154 and snow spray WP155; scene art/lighting and benchmark approval remain later M12 tasks. No external art import; provenance in licensing/m12/asset_licenses.md.

Evidence: quality/m12/wp153 (JSON/XML, retained initial float-roundtrip failure, final captures and Console). No race simulation implementation changed. This material fixture was validated in Unity; the most recent standalone build remains WP152. Final Windows benchmark build is still required in M12.
