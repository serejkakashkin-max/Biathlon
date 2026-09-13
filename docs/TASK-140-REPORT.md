# TASK-140 — Windows development build and launch smoke

Implementation: `PlayerBuildPipeline.BuildWindows` explicitly includes PlayableSprint and PlayableMassStart, builds Windows x64 with Development enabled, and writes a versioned build summary. It does not replace EditorBuildSettings scene selection. `scripts/Invoke-PlayerBuildSmoke.ps1` builds through Unity MCP, launches the player hidden with a bounded timeout, retains the player log, checks exit status and both scene markers, and writes a SHA-256 file manifest and verification JSON. `-SkipBuild` checks the existing successful build.

The opt-in `-biathlonSmoke` development-player bootstrap loads both scenes, checks initialized hosts, advances 400 fixed ticks with scripted effort, checks presentation proxies/camera and snapshot progression, renders subsequent frames and exits. Normal launches retain player controls. This is startup/integration smoke, not a complete race or an automated assessment of human control feel.

The first executable launch correctly failed: dynamic Shader.Find calls found URP Unlit in Editor but the shader was stripped from the player. Range/penalty materials now clone explicitly referenced Resources materials; athlete Lit material uses the same explicit dependency pattern. No gameplay tuning or authoritative simulation changed.

Windows validation: successful Unity6000.6.0f1 development build, zero build errors/warnings, 219339148 reported bytes. Two subsequent executable launches passed: Sprint6 athletes and Mass Start30 athletes each at tick400, exit0. D3D11/RTX5080 initialized; no Error/Exception entries in the successful player log. Evidence: `quality/m10/wp140/verification.json`, `build-files.json`, and `player-log-excerpt.txt`. Full logs remain at the recorded local artifact path. The manifest fingerprints player data and managed assemblies as well as the launcher executable. BuildSummary time is Unity's reported value, not total external pipeline wall time.

PlayMode validation: focused smoke1/1 after shader fix; full assembly25/25 PASS in84.47s. Final Unity Console0 errors/1 warning, compilation_failed=false. An initial full-suite discovery attempt after building returned Unknown/0 tests; this was not counted as a pass. Explicit script compilation restored discovery before rerunning.

Local executable: `Builds/WindowsDevelopment/Biathlon.exe`. Launch normally for keyboard/mouse Sprint and the result-screen Pursuit transition. Mass Start is included in the build and exercised by smoke; a user-facing format-selection menu is not implemented. Keep the adjacent data/runtime folders with the executable. Build output is local and gitignored; source, tests and compact evidence are published.

Limitations: build evidence comes from this working tree, including pre-existing local Unity/package configuration changes (dirty-tree flag is true), not an isolated clean-checkout build. Greybox ribbon overlap documented by WP139 remains visible. Controller feel, control discoverability and gameplay direction require owner hands-on review. M10 technical delivery reaches WP140; owner gate G1 remains pending and must not be represented as approved.

Next: owner G1 review of core controls/gameplay direction per Master Plan after WP140. No radical control redesign is authorized by technical smoke success.
