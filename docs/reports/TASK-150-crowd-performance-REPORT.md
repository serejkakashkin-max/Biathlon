# TASK-150 — Athlete distance detail and30-body cost

2026-09-15. WP150 PASS. Next WP151 animation validation/integration; M11 not yet complete.

The original athlete now uses a shared generated distant body mesh. Bone-isolated vertex clustering preserves rigid skin weights, bindposes and material slots:3012→618 vertices,5424→1364 triangles. Original equipment remains visible. Distant renderers stop casting shadows; returning near restores the original mesh/shadows. External rigs retain their own meshes. Authoring regeneration also regenerates the distant asset; no third-party assets or licensing changes.

Presentation uses a45 m distance threshold with5 m hysteresis, and20 Hz distant pose updates while root placement continues every render. Close athletes and every active shooting phase receive full detail/pose updates. Rewind, athlete identity and phase/tuck/sprint changes force refresh. The host explicitly supplies its chase camera; an absent observer defaults to full detail. No simulation/collision/aim/difficulty behavior changes.

Full Unity PlayMode **46/46 PASS**: geometry reduction/rig compatibility, reversible hysteresis/shadows, cadence, rewind and warmed zero-allocation rendering; actual range observations force full detail even with a distant observer. Prior race/control/replay tests also pass. Console after captures: **0 errors/0 warnings**, compilation_failed=false.

`docs/quality/m11/wp150/` records a detached Mass Start tick400 snapshot on a fixed30-athlete grid. All30 centers are in frame in near and far screenshots.600 pose samples after60 warm-up frames: near mean0.2795 ms / max0.3117 ms; far mean0.09585 ms / max0.3073 ms; both0 allocated bytes. Hardware: Ryzen9 9950X3D, RTX5080, Unity6000.6.0f1.

Separate180-frame Editor samples at2560x1440 and VSync0 are stored in frames-near/far.json with mean/p95/max time and Unity draw-call/triangle counters. These include Editor rendering overhead; the pose timing excludes simulation/rendering. This is a presentation microbenchmark, not release1080p validation or a whole-race60 FPS claim. See native profiler memory/rendering snapshots for context. Reproduce with Capture-AthleteCrowd.ps1 and Measure-CrowdFrames.ps1 in the running animation validation scene.

Limits: two detail tiers, no geometry crossfade, distance-only selection (zoomed broadcast cameras will need projected-size priority); reduced mesh is specific to the original rigid skin. Detailed shooting remains an intentional worst-case cost. Material submeshes still produce many draw calls; production rendering budgets follow M12/WP164. Distant pose cadence may remain detectable under unusual zoom. The fixed grid does not certify actual pack overlap or final visual quality; WP151 checks transitions and real race presentation. No new Windows build claim yet.
