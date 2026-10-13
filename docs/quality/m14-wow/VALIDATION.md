# FROSTLINE validation log — 2026-10-13

Branch: codex/m14-ui-wow. Baseline: 262f3406402c1e65e5c020ae72db0771472cd27d.
M14 stays OPEN for owner visual/gameplay acceptance.

## Passed checks so far

- .NET Release: 44 focused FrontEnd, GameplayHud, replay-library, personal-start and full-field replay tests; `frostline-core.trx`.
- Native PlayMode FrontEndMenuTests: 10 passed (keyboard/gamepad, profile/career/race flows).
- Native RaceReplayPlayModeTests: 1 passed, real recording/seek/lifecycle.
- Native PlayerStartWaitTests: 3 passed, personal countdown and skip authority.
- Native GameplayHudTests: 1 passed, retained shooting observations and input picking.
- Native SportVisualReviewTests: 3 passed, actual art pixels, purchase and split flags.
- Native FrostlineFieldTests: 2 passed, rendered field contrast and reduced-motion navigation without preference writes.
- Native EditMode FrontEndPanelTemplateTests: 1 passed.
- FrontEndAccessibilityMatrix: 384 main-screen cases passed.
- SecondaryAccessibilityMatrix: 240 secondary-screen cases passed.
- HudAccessibilityMatrix: 192 cases passed in iteration-1; unchanged 10% ordinary / 17% large-text coverage budgets.
- Three concepts and baseline/after captures are actual UI Toolkit renders. Concept sample values are labelled design studies; they are not production gameplay screenshots.

## Repaired failures and evidence caveats

- Focus navigation expected 5 px selection edge: restored 5 px; all 10 menu checks then passed.
- First 1024x768 shooting HUD measured 10.2% coverage: padding reduced 10→8 without reducing font; entire HUD matrix passed.
- Offscreen capture originally used depth=0. UI Toolkit stencil clipping produced white input rectangles in images even though resolved backgrounds were correct. Capture now uses depth24. Initial baseline/iteration screenshots of editable fields have this known capture artifact; do not treat it as game behavior.
- Existing artwork tests captured during the new 180 ms fade. Added 300 ms settling and stencil target, preserving pixel-range assertions; all three checks passed.
- The combined 13-screen matrix exceeded the MCP 300 s response limit; split into independently verified 8-screen and 5-screen tests. No failed test was deleted or weakened.
- Matrix JSON records layout findings. All current after-layout records have zero findings; this is not a substitute for artistic review or a guarantee of zero text clipping in all possible names.
- Result screens use an explicitly identified recorded-result boundary fixture; live HUD is produced from actual deterministic race states. Standalone evidence will separately show the actual venue.

## Same-machine UI CPU observations

| Work | Before total ms | After total ms | Samples |
|---|---:|---:|---:|
| Unchanged retained HUD | 0.1089 | 0.1086 | 2048 |
| Changed HUD observation | 1.6190 | 1.6684 | 128 |
| Menu rebuild | 1.8977 | 2.3045 | 8 |

Changed HUD differs by about 0.00039 ms/call; menu rebuilding by about 0.051 ms/call.
These narrow measurements exclude physics, snapshot construction and GPU rendering.
They do not establish race FPS or a full-race frame-time improvement. The Unity Mono
allocation counter returned zero even for a positive-control allocation; it is marked
unsupported. Recorded zero allocation values must not be represented as measured
zero-GC evidence. No texture, quality-tier, shadow or gameplay tuning is changed by
this branch's UI implementation.

## Final Windows evidence

Final build code `88e433d2`: Win64 succeeds with 0 errors/warnings. The isolated Windows
review passes with 12 native images, 108 frames, actual production environment, personal
start and actual aiming state; profile hashes are unchanged, and no Error/Exception
was collected. A 7.2-second native MP4 is saved under `docs/ui/m14-wow/`.

Two probe-only failures were repaired: a hidden window returned a blank backbuffer,
so the native camera/UI capture now uses a stencil-enabled target and explicit URP
render requests; the player-bound runner rejected a commandless Step, so the isolated
driver now supplies a paid same-physics player command every tick. No pose teleport or
gameplay exception bypass was added. Failed receipts remain in the local evidence.

Native replay captures now also use depth24; the actual replay/seek test passes again.
Final Editor Console Error/Exception inspection is empty. Offscreen capture logs retain
URP warnings for stripped DOF/Panini shaders; no quality workaround was applied.
All 28 distinct selected PlayMode checks and 1 EditMode panel-template check passed;
44 core tests passed. Full layout evidence is 816 cases with zero recorded findings.
See the final task report for scope, caveats and acceptance status.
