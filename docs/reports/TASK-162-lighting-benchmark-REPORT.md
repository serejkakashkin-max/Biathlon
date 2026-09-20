# TASK-162 — Lighting benchmark

2026-09-20. PASS for explicit lighting profiles; visual review actions remain for WP164/165 and G2.

Added versioned clear cold daylight, overcast, dawn and late-day profiles. The existing WeatherPresentation owns the combined sky, fog, ambient and directional light state; profiles supply validated angles, gains and tints. Releasing the owner restores the original light orientation as well as its other scene settings. Existing callers without a lighting profile retain their prior orientation and weather values. Cameras, authoritative weather, shot geometry and race inputs are unchanged.

LightingBenchmark reuses the original stadium, crowd and athlete assets. Its single athlete is an explicitly frozen observation fixture, not an integrated live race. Capture-LightingBenchmark.ps1 records twelve fixed Game View images: sector, athlete and target views for each profile, with camera definitions retained alongside them. No new external art or packages.

Verification: focused3/3 and full PlayMode86/86 PASS (84.88s full run); validated profile bounds, atomic invalid-input rejection, exact profile rewind, camera pose/FOV preservation, scene/light restoration and warmed zero managed allocation. Unity Console0 errors,1 MCP BufferedFileLogStorage disposal warning, compilation_failed=false. Native Console evidence retained. Latest playable Windows smoke remains WP159; integrated build is due at WP164.

Visual findings: all five target apertures remain distinguishable in all four captures. Late-day backlighting substantially reduces target face and dark equipment contrast; this needs review in the playable scene and quality/weather matrix before G2. Low-angle light also exaggerates the coarse procedural snow grain. The fixture still exposes a flat apron edge and stylized characters/buildings; these captures do not establish final production visual quality. Carry these specific findings into WP164/165. No authoritative balance impact; OWNER-FEEL-001 remains open for WP229/230/232.

Evidence: docs/quality/m12/wp162. Next: WP163 broadcast camera tooling, preserving the approved gameplay camera behavior.
