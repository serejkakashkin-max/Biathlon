# TASK-163 — Broadcast camera tooling

2026-09-20. PASS for presentation tooling.

Added six versioned shots covering sector, range, finish, tracked athlete, explicit-time rail and crowd zoom. BroadcastCameraDirector clones/validates definitions, requires an idle dedicated camera, rejects chase camera ownership, binds subjects by explicit stable IDs, validates before mutation and restores camera pose/projection/clips on release. No gameplay camera is switched by this tooling. Missing/destroyed subjects fail explicitly. Fixed/tracking cuts and rail sampling have no accumulated smoothing history; rail time clamps at its endpoint.

BroadcastBenchmark and its capture script exercise the tooling on the stadium, spectator crowd and detached athlete pose fixture. Eight2560×1440 captures include rail2s/6s/2s: movement changes3606013 pixels, rewind changes0. Athlete tracking is verified with moved subjects in tests; screenshots use the frozen subject. Existing Unity camera APIs cover these requirements, so Cinemachine or another extension is not installed.

Opt-in ProjectionAwareLod promotes athlete, vegetation and spectator mesh/pose detail under perspective zoom using angular size relative to the existing60-degree distance policy. Physical forest/crowd cull distances and VFX distances remain unchanged; unmarked gameplay cameras retain their previous behavior. Orthographic cameras retain distance LOD. Spectator integration tests verify zoom promotion and continued physical culling. Geometry crossfade and offscreen shadow retention remain limitations.

Focused4/4 and full PlayMode90/90 PASS (86.17s). Tests cover schema/degenerate path rejection, camera lease/restoration, stable subject bindings, invalid-input atomicity, exact rail rewind, zero warmed camera allocations and zoom/cull integration. Final Console0 errors,1 upstream MCP disposal warning; compilation_failed=false. Evidence: docs/quality/m12/wp163. No authoritative race/shot changes or balance impact. Latest playable Windows smoke remains WP159; WP164 supplies the integrated benchmark build.

Visual review confirms usable compositions and also the existing stylized athlete/crowd and flat fixture boundary. Camera tooling does not close character-fidelity action M11-VIS-002. M11-VIS-004 zoom priority is addressed; discrete LOD transitions remain for review. Next: WP164 representative integrated scene with8–12 athletes, retained M11/M12 visual actions, Windows build and G2 evidence preparation.
