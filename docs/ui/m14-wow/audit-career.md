# FROSTLINE — career, results and replay audit

2026-10-13. Source and existing native-image audit; fresh baseline capture and final
artistic selection are owned by the main task. No gameplay or production UI edited.

## Sources checked

- Full owner MASTER TASK attachment (all 17 sections), AGENTS rules, complete root
  `BIATHLON_GDD_TDD_v0.1.md`, frontend-design skill.
- `PROJECT_CURRENT`, latest append-only `PROJECT_STATE` entries, `M14_WP_STATUS`;
  master plan sections 14 and WP-179–197.
- M14 completion, visual review, compact HUD, owner review and player-start reports.
- Career dashboard/pages/results, shared design helpers, replay TV, theme/panel
  settings; career EditMode tests and native menu/review/art/replay test harnesses.
- Inspected existing native PNGs: visual-review calendar RU, review training,
  completion replay UI. These are historical evidence, not new screenshots.

## Concrete visual findings and redesign hooks

| Surface | Evidence / weakness | Coherent implementation direction |
|---|---|---|
| Dashboard | The large pale rounded panel dominates; three equal empty numeric cards receive nearly as much emphasis as the next race. Readiness is deliberately hidden. Age, season and progression are not presented as a unified athlete identity. | Dark, restrained event-led composition. Athlete identity with real nation/age/category; clear next-start hero; compact real form/fatigue instruments; genuine completed-calendar progress. Reuse `CareerDashboardSummary`, never fabricate a trend. |
| Calendar | Identical panoramic strips repeat on two small event cards per page; eight pages hide seasonal chronology. | A compact chronological season rail plus current/next detail. Keep all sixteen `career_event_<id>` elements and accessible paging; identify completed/current/ineligible by wording and shape as well as color. |
| Training | Two sessions per page; long cost sentence; forecast bars show only predicted value with no current baseline. Large unused panel area. | Session choices as concise discipline tiles, weekly-slot track, selected-plan summary, real current→preview form/fatigue comparison. Every action must still call session draft/commit APIs. |
| Equipment | Existing generated skis are consistent and useful, but same generic white rounded cards and short images mute product presence. Delta scales always use packed-neutral comparison. | Taller dark product stage with retained full ski tips, named comparison context, compact symmetric delta axes and strong selected/owned/purchase state. Preserve catalog economics and image↔ski ID mapping. |
| Standings | Useful flags/bibs and player highlight already exist. `ResultTableRow` reuses the same six widths for differently shaped tables; long headers and times truncate. | Broadcast rank rail, aligned numeric columns, quiet alternating rows, nation and bib indicators, explicit player stripe. Allow context-specific column definitions while preserving truthful rank/gaps. |
| History / rivals | Repeated text tiles; history lacks pagination, rivalry is mostly one sentence. | Actual race timeline and restrained result numerals. Rival comparison uses only shared races, close finishes, podiums and standings battles; do not invent personality/drama. Empty state should direct next action. |
| Results | Player headline is in sidebar, shooting data above protocol consumes height, only 3/5 rows fit. Typography does not distinguish medal finish, elapsed time and statistics sufficiently. | Finish masthead, one compact shooting strip, clearly selected protocol/splits tabs and denser aligned rows. Keep default page containing player and original saved result data. |
| Replay library | Generic sections, default Foldout and full-width repeated buttons. | Race poster/date strip and deliberate highlight list. Preserve recordings from real library, loading guard and compatible launch/seek behavior. |
| Replay TV | `Label` strings concatenate rank/name/gap/shooting; default Slider/Dropdown controls and horizontal ScrollView look like tooling. | Retained broadcast rows with individual columns and flags, compact contextual shooting panel, unified transport bar and styled dropdowns. Keep authoritative snapshot and seek callbacks untouched. |

## Shared helper contracts requested from main task

Main task owns `FrontEndMenu`, `FrontEndDesign`, theme/tokens and HUD. Career patch
should use a common surface/ink/muted/accent palette and typography via helpers,
not introduce another hardcoded palette. Useful shared helpers:

- Semantic `Tile` variants (surface, hero, selected) and compact spacing.
- Display, body and numeric font roles; `AddLabel` must remain localized and scaled.
- Reusable measured comparison bar / slot strip / small section marker.
- Consistent buttons, focus/hover/disabled states and paging bar.
- Reduced-motion preference available to all animated views.
- Shared stylesheet application callable from replay's independent UIDocument.

Preserve existing element IDs because tests and navigation target them. Several
legacy tests additionally rely on first/second Label order; adapt their selectors
to stable named semantic labels if hierarchy changes, preserving the same data
assertions. Do not retain invisible duplicated text solely to satisfy old layout.

## Existing test/capture entry points

- `FrontEndMenuTests`: real keyboard/gamepad profile/continue/settings, career
  draft flow, production launch/pause, saved results and dashboard screenshots.
- `SportVisualReviewTests`: ski image rendering/purchase, calendar+pre-race
  viewport/pagers, actual career split table and nation flags.
- `M14ReviewPresentationTests`: dashboard/training/equipment screenshot loop and
  post-finish cameras/authoritative completion.
- `BalanceReviewUiTests`: new-career age/category and actual ski color painting.
- `RaceReplayPlayModeTests`: independent replay UI, both-direction seek and no
  save writes; screenshot helper already redirects PanelSettings.targetTexture.
- `GameplayHudTests`: accepted shooting lifecycle, zero aim interception, actual
  rectangle area and center-clearance checks. Existing limits must survive.
- `FrontEndCareerPagesTests`: atomic training/equipment, read-only pages, no
  saved RNG consumption by draft/forecast.

Coverage gaps for this overhaul: fresh screenshots of every history/rivalry/
standings/replay-library state, all four requested resolutions, 115% text,
high-contrast, reduced-motion, meaningful populated and empty states. Existing
replay screenshots contain only UI on black; final review also needs composited
game imagery. Tests should write a new evidence folder, not overwrite historical
before images. No native tests were run by this audit agent.

## Assets and licensing

- No project `.ttf`/`.otf` assets found. `front_end.tss` imports Unity's default
  theme; current faces are implicit engine UI defaults, not an authored font pair.
- `docs/reports/M14_VISUAL_ART_PROMPTS_20261012.md` records exact built-in imagegen
  prompts for three unbranded matching ski images and one venue illustration.
  Generated image source PNGs are under `Assets/Resources/Biathlon/UI/Art`.
- Venue illustration is promotional and not an exact playable course map.
- National flags and pulse trace are original retained vectors. These do not
  require new raster image downloads.
- New fonts need explicit Cyrillic coverage, source URL, distributable license
  text and asset provenance. Do not redistribute locally installed proprietary
  fonts just because their glyphs render in the editor.

## Non-negotiable scope constraints

UI observes the existing simulation, career data and accepted events. No physics,
balance, AI, shooting geometry, start gaps/countdown algorithm, save/replay schema
or High graphics changes. Training forecast must not reveal hidden potential.
Rank/points/bibs are actual current-season facts, not decorative mock values.
No UI hierarchy rebuild on simulation ticks. Animation needs reduced-motion and
bounded update work. M14 remains OPEN pending the owner's visual acceptance.
