# M14 — implementation ready for owner testing

2026-10-12. WP-179–197: 19/19 implemented and verified. Owner acceptance pending.

The front end now completes the player loop from a durable profile through training/equipment, preparation, the actual production race, saved protocol/standings and the next event. Career pages use saved facts; they do not invent results, potential or rival drama. The menu uses the original biathlete background, compact cards, RU/EN, scalable text, contrast and visible keyboard/controller focus.

The final slice adds actual pre-race weather; optional F2 live TV standings; versioned, compressed recordings of committed commands; separate replay storage; event highlights; pause/seek/speed and athlete/camera controls; observation-only broadcast timing/shooting/penalty/finish panels. Dense events hold a readable shot instead of cutting constantly. Backward seeking restores a fresh matching venue/field. Recording uses bounded buffers and never stores per-frame scene transforms; a recording failure cannot invalidate career progress.

Relevant files are under `Assets/Scripts/Replay/`, `Assets/Scripts/Unity/Presentation/FrontEnd*`, `CareerRacePresentation`, `CareerReplayCapture`, `RaceReplay*`, `Gameplay*Hud`, the UI resource catalogs, and focused EditMode/PlayMode tests. Windows packaging uses `PlayerBuildM14`; the explicit startup probe runs only with `-biathlonM14Smoke`.

## Verification

- Full recorded/replayed Sprint30 and stream/highlight/storage checks: .NET **9 PASS**. Complete result JSON identical; 31136 ticks, SHA256 `701cabf4c0e01295abd425d7f1f2afb00611c20f421b6372e8cb7ef2eb21ff15`.
- Saved production Sprint → Pursuit: **1 PASS**, 30 entrants in both saved protocols; Pursuit finishes at 24586 ticks.
- Pursuit constructor/directed passage regression: **9 PASS** after reproducing the original failure.
- Compressed storage and broadcast cut policy: .NET **2 + 2 PASS**.
- Native EditMode: journal **4**, library **2**, highlights **2**, shot-hold policy **2 PASS**.
- Native PlayMode: menu/profile/career/pause/accessibility/real gamepad **10**, replay seek/visible independent UI/no career mutation **1**, broadcast camera lifecycle **4 PASS**.
- Replay UI inspected at 1600×900 and 1024×768. Menu/dashboard/results/preparation retain their earlier RU/EN and large-text captures. Unity Console: no current code errors.

Evidence: `docs/quality/m14/completion-20261012/`. Unity MCP reports the assembly-wide discovered total in its summary; the pass counts above refer to the filtered tests actually executed.

## Compatibility and scope

The live physics, AI capabilities, shooting geometry, graphics textures/meshes and accepted M13 tuning are unchanged. The obsolete Pursuit guard excluding an independent start approach was removed. Headless production career execution intentionally uses the same manual movement composition as the playable host; this replaces the legacy queue path and is covered by the complete career transition.

Replay data is separate from profiles, created for newly completed races, and rejects incompatible definitions/corrupt commands. A training draft is committed with the training week, not persisted on every click. Existing owner-dirty render/project/package files are outside this change.

Build: `Builds/M14Review20261012/START_M14_TEST.cmd`; build succeeded with 0 errors/0 warnings; standalone 1600×900 startup/render passed. M14 remains awaiting owner acceptance, not automatically closed.

Standalone packaging qualification: the UI now clones a persistent PanelSettings resource, including built-in UI shaders and ICU international text data. Native template regression 1 PASS and menu regressions 10 PASS. Final Windows build has 0 errors/0 warnings; the hidden startup probe renders the menu into a texture with 1438 non-black sampled pixels and exits successfully. Player log has no exceptions or ICU warning. This verifies startup/rendering, not sustained whole-race FPS.
