# M14 visual review — 12 October 2026

Owner-requested visual revision implemented. M14 remains **OPEN**, pending owner
testing and explicit acceptance. No M15 work started.

## Delivered

- WP-188: generated three consistent, unbranded ski product images. Deck/accent
  colors match the actual equipment catalog. Retained comparison meters and
  purchase/equip controls; selected item has readable contrast. Full skis fit
  inside their image viewport without cropping their tips.
- WP-181/185: one Woodland Basin venue illustration reused in the upcoming-event
  hero, all sixteen calendar event cards and pre-race screen. Horizontal hero and
  two events per page preserve accessible navigation. Original menu backdrop kept.
- WP-184/189: TAB splits now have separate rank/name/time/gap columns, six native
  vector national flags, alternating rows and a highlighted player row. Actual
  career roster and authoritative checkpoint results supply all values. Both
  columns align at the top; lower HUD cards hide while the table is open. Flags
  also appear in final results and season standings.
- WP-182: compact pulse trace, animated heart, clear BPM numeral and fine upper
  panel borders. Trace cadence is bounded at 20 Hz and follows observed game HR.
- WP-196/197: RU/EN normal and 130% text at 1600x900 and 1024x768 inspected.
  Pre-race control reminders moved next to start/back so they remain accessible.

## Validation

Native PlayMode: SportVisualReviewTests **3/3**, FrontEndMenuTests **10/10**,
GameplayHudTests **1/1**, BalanceReviewUiTests **2/2**. Native EditMode:
FrontEndCareerPagesTests **7/7**. Console clean after final compilation/tests.
The tool's TotalTests is the registered suite size; PassedTests is the executed
filtered count. No test disabled or weakened.

New coverage verifies actual image rendering (not just a non-null resource),
catalog asset mapping, successful purchase/equip, shared venue artwork on every
event, viewport fit and an actual career Mass Start checkpoint table with
roster-backed flags. UI refresh leaves the race tick unchanged. Existing HUD
checks retain screen-coverage and shooting-transition constraints.

Visual inspection caught and fixed Unity Image rounded-clip masks rendering
white, calendar overflow, product aspect distortion, table-column vertical
centering and large-type HUD overlap. PNG source assets remain untouched;
Unity import uses shared BC7 textures, 1024 maximum dimension, original aspect,
no mipmaps and no CPU-readable copy. New art loads only during menu composition.

No authoritative physics, AI, balance, race definitions or replay protocol changed.
Full deterministic race regressions were not repeated for presentation-only edits;
the previous full-race/replay evidence remains in the burst-pack report. Earlier
experimental planner failures are outside this revision and not represented as fixed.

## Build

`Builds/M14VisualReview20261012/START_M14_TEST.cmd`

Unity 6000.6.0f1, Windows x64 Development, DX11. Build succeeded with **0 errors,
0 warnings**, 370460870 bytes. Standalone startup smoke exited 0, rendered
1600x900 with 1437 visible samples, success=true; no exception/error in startup log.

Evidence: `docs/quality/m14/visual-review-20261012/`. Generated art and exact prompts:
[art manifest](M14_VISUAL_ART_PROMPTS_20261012.md),
`Assets/Resources/Biathlon/UI/Art/`.

The venue image is promotional illustration, not an exact playable course map.
Equipment art matches colors; in-race ski mesh detail is unchanged. Supported
career nations have flags; unknown country codes get a neutral marker rather
than an invented flag. Next action: owner tests this build and decides whether
M14 needs further corrections. Git publication receipt is appended to PROJECT_STATE.
