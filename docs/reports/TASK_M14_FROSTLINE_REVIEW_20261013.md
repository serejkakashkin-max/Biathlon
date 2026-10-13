# M14 FROSTLINE — Windows visual review build

2026-10-13. M14 remains **OPEN for owner testing and artistic acceptance**.
Branch: `codex/m14-ui-wow`. Baseline: `262f340`. Build code: `88e433d2`.

Launch **[START_M14_TEST.cmd](../../Builds/M14_UI_WOW_Review/START_M14_TEST.cmd)**.
The separate Win64 development build uses the existing profile/replay location.
No new career is required. Previous builds remain available.

## Delivered visual change

Three actual Unity design studies were compared; Frostline Broadcast was selected.
The existing biathlete background is retained, with restrained motion and an editorial
menu. Local Oswald/Roboto fonts, glacier selection edges, navy surfaces and gold event
accents now connect the menu, career pages, race, shooting and replay interfaces.

Career now presents nation/age/season identity, the next event, real readiness meters
and a season rail. Training displays actual current→forecast comparisons and weekly
slots. The shop retains generated ski art and shows selection and comparison context.
Results use a finish masthead, compact shooting strip and flagged, aligned protocols.
Season standings, history, rivals, replay library and transport use the shared system.

Race HUD has distinct sport numerals, speed units, segmented resource/pace bars and
a small observed-HR heartbeat/trace. Shooting has clear target rings, bolt/breath
status, wind and contextual breathing guidance away from the optical centre. Tab/F2
and replay tables keep genuine ranks/gaps and roster-backed national flags.
The 180 ms entrance, 140 ms button/target and 120 ms meter transitions are bounded;
Reduce motion removes these transitions and the decorative drift/pulse.

| WP | Final state of this owner-requested revision |
|---|---|
| 179–180 | COMPLETE: shared presentation, main menu, creation fields, settings and focus |
| 181–185 | COMPLETE: pre-race, skiing/shooting HUD, split graphics and result presentation |
| 186–190 | COMPLETE: dashboard/calendar, training, shop, standings/history and rivals |
| 191–194 | Existing authority/highlight/camera mechanics preserved; replay presentation integrated |
| 195 | COMPLETE: shared live/replay broadcast tables and controls |
| 196–197 | COMPLETE: RU/EN, text/contrast/reduced-motion and native navigation validation |

All 19 implementation WPs remain delivered. The remaining step is owner acceptance,
including the visual quality judgement; no M15 work or automatic main merge begins.

## Mechanics and compatibility

Simulation, AI, Career and Replay authority source was not changed in this visual
revision. Shared physics, balance, shooting geometry/hits, breathing cost, manual
controls, personal start/skip, range assistance, weather, national suits/bibs,
selected skis, results, durable saves and recorded command/replay semantics remain.
No High texture, shadow, quality-tier or 3D material change belongs to these commits.
Pre-existing unrelated local settings/rendering edits were preserved and excluded
from the branch's commits.

## Verification

| Check | Result |
|---|---|
| .NET Release focused menu/HUD/start/library/full-field replay | 44 PASS |
| Native FrontEndMenuTests | 10 PASS |
| Native RaceReplayPlayModeTests | 1 PASS, actual recorded replay/seek/lifecycle |
| Native PlayerStartWaitTests | 3 PASS |
| Native GameplayHudTests | 1 PASS, accepted shooting state/input and contextual hint |
| Native SportVisualReviewTests | 3 PASS, actual image pixels/purchase/checkpoint flags |
| Native FrostlineFieldTests | 2 PASS, rendered RU/EN values and reduced-motion navigation |
| Native Frostline visual/concept methods | 8 PASS, baseline/final captures, matrices, CPU observation |
| Native EditMode panel-template check | 1 PASS |
| Front-end layout combinations | 624, zero recorded layout findings |
| HUD layout combinations | 192 PASS, unchanged ordinary 10% / large-text 17% area budgets |
| Win64 build, Unity 6000.6.0f1 | Succeeded, 0 build errors / 0 build warnings |
| Windows standalone isolated review | PASS: 12 native images, actual venue/start/shooting, 14 navigation actions, owner profile hashes unchanged |

The layout matrix uses 1024×768, 1600×900, 1920×1080 and 2560×1440; RU/EN;
100/115/130% text; ordinary/high contrast. Tests hold authority unchanged during
preference/layout capture. Latest Console inspection has no Error/Exception entries.
The separate ordinary-menu startup probe also verifies the shipped scene without
the Frostline review flag; its receipt is stored with the evidence.

The UI refresh CPU comparison is [recorded here](../quality/m14-wow/VALIDATION.md):
128 changed observations total 1.6190→1.6684 ms; eight menu rebuilds total
1.8977→2.3045 ms. These differences are small in absolute per-call terms and do not
measure full-race FPS. The Mono allocation counter failed its positive control;
its zero readings are explicitly marked unsupported, not a zero-GC claim.

## Review artifacts

- [Owner brief](../ui/m14-wow/OWNER_BRIEF_RU.md), [audit](../ui/m14-wow/audit-career.md),
  [three concepts and selection](../ui/m14-wow/CONCEPT_SELECTION.md).
- [Design system](../ui/m14-wow/DESIGN_SYSTEM.md),
  [ADR-0066](../adr/ADR-0066-m14-frostline-presentation-system.md).
- [Before/after index](../ui/m14-wow/SCREENSHOT_REVIEW.md).
- [Native Windows demonstration](../ui/m14-wow/frostline-native-demo.mp4): 108 native
  frames at 15 fps, menu transition plus skiing/shooting clips, 7.2 seconds. Cuts are
  intentional; this is not a continuously recorded complete race or a performance benchmark.
- [Build receipt](../quality/m14-wow/build-summary.json),
  [standalone receipt](../quality/m14-wow/standalone-release/standalone-review.json),
  [layout summary](../quality/m14-wow/layout-summary.json), core TRX and native screenshots
  under `docs/quality/m14-wow/`.
- Fonts and their OFL notices ship locally; existing generated menu/venue/ski artwork
  remains under `Assets/Resources/Biathlon/UI`. OFL notices are also copied into the build.

## Limits and repaired failures

Owner artistic review remains necessary. Secondary pages share components, and hidden
vertical scrolling remains an overflow/navigation fallback for enlarged or long content.
No fabricated minimap, athlete trend, result or nation was added. Result review screenshots
use a labelled isolated result boundary fixture; gameplay HUD observations come from
real deterministic field states. Native panel-only HUD/replay images have a black scene
background; Windows venue captures are identified separately.

Initial editable-field screenshots used a depthless RenderTexture and painted white
stencil masks. The capture now uses depth24. The original baseline images retain that
known capture defect; it was not a production field failure. Replay baseline captures
come from the recorded native 2026-10-12 images at baseline `262f340`; secondary career
screens have final images but no newly collected before pair. Artwork tests now wait
300 ms for the new entrance transition without weakening their pixel assertions.
The initial shooting HUD exceeded its area budget; padding was corrected and the full
HUD matrix passed. The combined long matrix hit the MCP response timeout and was split
into passed independent suites. Failed exploratory receipts are retained locally.

The offscreen Windows capture path reports URP warnings for stripped Gaussian/Bokeh
depth-of-field and Panini shaders. They are recorded in `player.log`; the safe gameplay
profile disables depth of field. No graphics-quality workaround was applied. Full-race
frame-time/GPU profiling and allocation measurement remain outside the narrow UI CPU
comparison, so this report does not certify a new full-race FPS improvement.

Next: owner tests this build, evaluates the visual direction and reports required
adjustments or explicitly accepts M14.
