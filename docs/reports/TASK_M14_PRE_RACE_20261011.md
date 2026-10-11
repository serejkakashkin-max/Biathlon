# M14 — illustrated menu and prepared career race

2026-10-11. M13 remains owner-accepted. M14 remains in progress. No player build.

## Delivered

- Original biathlete menu background, dark translucent navigation, readable
  focus/disabled states and white information cards. RU/EN and 100/115/130%
  text retained; wide and 4:3 screenshots inspected in native Unity rendering.
- Pre-race screen reads actual venue/profile, distance/laps, stances, targets,
  penalty distance, start interval, snow, selected skis and prepared readiness.
- Explicit recovery weeks and preparation; actual career roster/seed/stats
  enter `ProductionVenueHost`, with the existing manual steering and shooting.
- Pause/resume and abandon-to-preparation own one race root. Error paths stop
  active work rather than flooding the console. A completed field returns the
  real result payload/hash, updates the week/accounting and saves before showing
  Results. A failed write offers retry without discarding the completed field.
- Atomic profile replacement with `.bak` and an expected-state check. Cancelled
  preparation/races and failed writes do not consume saved weeks or RNG state.

## Files and design

`CareerSeasonEngine` separates preparation/completion while preserving the
synchronous API. `CareerRaceProvider` separates runner construction and result
collection. `FrontEndSession`, `FileCareerProfileStore`, `PreRaceSummary`,
`FrontEndMenu`, `CareerRacePresentation` and `ProductionVenueHost` connect them.
UI theme/text resources and original `menu_background.png` supply presentation.
See ADR-0055 and `docs/quality/m14/pre-race-20261011/menu-art.md` for the design,
saved image path, built-in image-generation provenance and exact prompt.

## Verification

- .NET **17 passed**: profile rollback/backup/stale writes, navigation,
  prepared readiness, fixed-seed career foundation and weekly repeatability,
  and completed production career race → save → restore → next event.
- The real race used **30 athletes**, the same manual movement composition as
  the live host, AI driving for automated completion, **31136 ticks**. Result
  SHA-256: `701cabf4c0e01295abd425d7f1f2afb00611c20f421b6372e8cb7ef2eb21ff15`.
- Native EditMode **16 passed**; native PlayMode **14 passed**, including
  keyboard/gamepad, scaled UI and career launch/pause/abandon. Updated smoke
  with the full environment also **passed**. No script compile errors or
  unexpected game exceptions in these runs; package shutdown warnings persist.
- An initial completion probe accidentally selected the older autonomous
  movement path and was stopped after running slowly. It is **not** counted as
  passed. The corrected test explicitly binds the production manual movement
  composition and completes in about 54 seconds. No pre-existing failing test
  was deleted or disabled.

Screenshots: `frontend-ru.png`, `frontend-en-large-4x3.png`, `pre-race-ru.png`,
`pre-race-en.png` under the evidence folder. `delivery-summary.json` records
hashes and append-only state-history integrity.

## Impact and limits

No physics, AI advantage, shooting geometry, steering or gameplay graphics
tuning changed. Career uses its existing shared stat/readiness projection and
accounting formulas. Tests cover deterministic saved state; this is not a new
whole-game FPS or whole-season claim. The static hidden menu adds no animated
scene or per-tick career work during races.

Results are a first summary, not the complete WP-185 screen. Detailed training,
equipment selection, standings and replay/broadcast UI remain. Weather shows
authored snow only; there is no invented temperature forecast. Mid-race resume
is not implemented. Legacy venues/ineligible categories are unavailable in
the live front end. Results wait for the complete field.

The previously recorded Pursuit directed-passage/startContinuation issue remains
separate: Sprint result persistence and the next-event reference are verified,
but this report does not claim a complete playable Sprint→Pursuit season loop.
Next: WP-182/183 race and shooting HUD, expanded results and that Pursuit entry
before the full M14 gate. Owner's existing settings/package/render edits remain
untouched. Existing build remains the owner-approved M13 BotRecovery build.
