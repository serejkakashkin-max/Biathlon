# TASK-127 — Greybox venue

WP127 complete. BIATHLON/Generate Greybox Venue creates a separate saved scene with32 displayed loop meshes (main-short alternative omitted), ground/stadium platform,30 target banks,150 target proxies, penalty loop, semantic labels, lighting and an overview camera. Materials/meshes are generated assets, with no Inspector setup required. No authority reads scene names or transforms.

Validation: Unity PlayMode scene test1/1 (9 discovered) verifies all mesh references,30 banks, penalty geometry and camera. Game View screenshot inspected and retained at docs/quality/m10/wp127-game.png. Final Console0 errors/1 warning, compilation_failed=false. The manual Camera.Render MCP capture missed URP mesh surfaces; the actual Game View rendered them correctly, so the accepted screenshot is Game View.

This is schematic greybox geometry: piecewise linear circuit, broad shared range approach, static target proxies and overview camera. It is not final terrain/art or a playable athlete view. Physics/tuning unchanged. Next: WP128 authoritative athlete proxies.
