# TASK-159 — Breath vapor

2026-09-19. PASS for contextual presentation VFX.

Added versioned temperature/load settings, a fixed 256-puff/256-source pool and camera-specific billboard rendering. Explicit visual temperature/wind is required; unset environment emits nothing. Cold and exertion strengthen vapor, distance fades/culls it, observed breath holding suppresses new emission, and range emission follows the observed breathing cycle. Gaps, duplicate observations and time rewind have defined reset behavior. The mouth anchor was lowered after close-up inspection. No third-party art was imported; the original snow-spray analytic mask is reused.

RacePresentationSnapshot now exposes two detached read-only observations: breath holding and breathing phase. Neither changes simulation, inputs, event logs, hit geometry or tuning. An observed/control complete race with breath-hold commands retains exactly equal results and events. OWNER-FEEL-001 remains OPEN for WP229/230/232.

Verification: .NET observation tests4/4; Unity EditMode observations4/4; full PlayMode74/74; final focused4/4 after the mouth-anchor adjustment. Fixed-budget emission and warmed mesh rebuild allocate0 managed bytes in focused tests. Some repeated MCP test queries returned Unknown/0 because the runner retained stale state; these were not treated as passes. Requesting a script reload restored discovery and successful execution. The initial EditMode query also used the wrong assembly name; corrected to Biathlon.Tests.

Game View captures use a real host at tick1200, with its observed pose/load held stationary to inspect a cloud at age0.25s. Close on/off comparison changes29303 pixels; chase view remains restrained. This is an isolated VFX inspection, not a claim of a live moving race capture or final character quality. Runtime actual-host integration is separately covered by PlayMode. The capture callback removes itself when its Play Mode component is destroyed.

Windows development build succeeded:225421306 bytes,0 errors/0 warnings. Automated smoke passed Sprint6 and Mass Start30 at tick400 and all six scene/quality combinations, exit0. Full build-file hashes and compressed player log are retained. Final Unity Console0 errors/0 warnings, compilation_failed=false.

Evidence: docs/quality/m12/wp159. Limitations: analytic puffs rather than fluid simulation; skiing cadence is visual load-driven, while range cadence reads the actual breathing oscillator. Existing playable scenes do not invent temperature: explicit environment integration and shooting-camera readability remain WP164–165. No change to G1 controls or shooting balance. Next: WP160 representative stadium/range art.
