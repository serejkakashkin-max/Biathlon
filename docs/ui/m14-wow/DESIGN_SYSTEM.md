# FROSTLINE — implemented M14 visual system

2026-10-13. Presentation specification for the current implementation; owner
acceptance of M14 remains open. This document describes code, not a promise of
features or performance results. Validation results belong to the task report.

## Identity and materials

The signature is a narrow glacier timing edge on a dark navy instrument. It
connects the editorial menu, race invitations, energy rails and HUD. Snow and
the athlete remain the visual subject; ordinary skiing leaves the optical
centre free. Corners are 2 px, not large rounded dashboard cards.

| Role | Value | Actual use |
| --- | --- | --- |
| Ice / `Ink` | `#E8F5F8` | Primary readable foreground |
| Muted | `#B0C4D0` | Captions and secondary information |
| Deep | `#0B1928` | Hero surfaces and HUD foundations |
| Surface | `#132A3B` | Career cards and fields |
| Glacier | `#85D5DF` | Primary action, selection, timing edge |
| Medal gold | `#D6AA69` | Race invitation and short-burst emphasis |
| Danger | `#F3988D` | Error/warning foreground |

`FrostlineStyle.cs` holds shared presentation colours/fonts; the versioned
`front_end_theme_v1.json` holds page metrics and legacy theme roles. In this dark
theme, `ink` is light and `paper` is dark. HUD foreground and target interiors
are therefore explicit, avoiding an accidental inverted target or unreadable
number when legacy theme roles change. Gameplay tuning is not stored here.

## Typography

Oswald is used for display headings, compact sport numerals, countdown, clock,
speed and pulse. Roboto carries body copy and utility captions. Both ship as
local TTF assets under `Assets/Resources/Biathlon/UI/Fonts`; there is no runtime
font download. Cyrillic and Latin are exercised by the RU/EN capture harness.

The base menu theme specifies title 62, section 30, body 18 and caption 14;
individual home/career layouts use restrained explicit overrides. Existing HUD
settings remain the metric source (body 17, caption 13, clock 26); speed is 33
and pulse 26 before text scaling. Text settings retain 100%, 115% and 130%.
Compact layout is selected below 1250 logical pixels. Readability is judged
through rendered geometry and screenshots rather than font metrics alone.

Font provenance and redistribution:

- Oswald: Google Fonts distribution, upstream identified in the bundled notice
  as `https://github.com/googlefonts/OswaldFont`; copyright 2016 The Oswald
  Project Authors. `Oswald-OFL.txt` contains SIL Open Font License 1.1.
- Roboto: Google Fonts distribution, upstream identified in the bundled notice
  as `https://github.com/googlefonts/roboto-classic`; copyright 2011 The Roboto
  Project Authors. `Roboto-OFL.txt` contains SIL Open Font License 1.1.
- Keep both notices with redistributed font assets. No paid UI library or
  remote font service was introduced.

## Menu composition

The home keeps the existing generated biathlete background. Its left editorial
headline and navigation use a narrow dark field; a low venue signature sits to
the right. Career pages share a 23% navigation rail and a flexible content area.
The dashboard places an actual next-event venue preview beside event details,
then form/fatigue meters and a paged season calendar. Equipment retains existing
ski product art and the actual selected-item state; results/standings retain
their model-backed observations.

Shared surfaces, fields, focus, buttons and typography apply to secondary pages
as well. Calendar, training, equipment, standings, history, rivalries and results
use page controls where implemented. Vertical `ScrollView` containers still
exist as an overflow/focus fallback, with hidden scrollbars; this is not a claim
that every long or enlarged-text page fits without scrolling.

## Race instruments

- Upper left: clock, lap, current timing observation and contextual phase.
- Lower left: a display speed value with separate unit, selected pace and a
  segmented rail. Shift's existing short-burst overlay replaces that readout
  within its fixed 76×text-scale height; reserve is never invented by the UI.
- Lower right: animated heart, compact pulse trace, BPM, reserve/fatigue rails;
  wind appears when relevant to shooting.
- Shooting replaces the speed instrument with five closed/open targets,
  observed bolt state and breath debt. Light closed targets and dark open
  interiors use explicit contrasting colours. The crosshair stays unobstructed.
- Readiness is contextual above the lower-right condition card, away from aim.
  Its real physiology classification keeps both words and colour.
- Tab retains the actual latest checkpoint table with national vector flags,
  stable rank/name/time/gap columns and player emphasis.
- F2 retains the broadcast toggle but displays roster-backed flags and separate
  rank, name, gap and shooting fields. Unknown nations use the neutral flag;
  a nation is never inferred from an athlete's name.
- F1 help, F3 debug, shooting Tab behavior and all skiing/shooting controls retain
  their existing meaning. The personal-start skip button is pickable/focusable
  only while its waiting card is visible.

Normal HUD coverage budgets remain below 10% of the viewport, and below 17%
at the existing enlarged 4:3 acceptance size. Contextual tables and explicitly
opened help/broadcast panels are additional intentional overlays. These limits
are assertions in tests, not a substitute for inspecting each final capture.

## Motion and performance policy

`frostline.uss` defines a 180 ms page opacity transition and 140 ms button colour
transition. Home art has a small scheduled 33 ms drift; rebuilding the page
pauses the previous schedule. The Reduce motion setting removes those
transitions/drift. HUD reads the same `biathlon.ui.reduced_motion` preference
when built and stops heartbeat/trace animation; static vector shapes and status
colours remain visible.

The pulse trace is a tiny retained vector, capped at 20 Hz; the heart at 30 Hz.
HUD observation refresh keeps its existing bounded cadence and no-work path
for an unchanged snapshot. Energy dividers and flag objects are constructed
when the UI is built, not per tick. Text still follows the existing observation
formatting path, so this document makes no claim of an allocation-free UI.
No blur, fullscreen shader, per-frame texture upload or new gameplay camera was
added by this design pass. No world texture/quality reduction is involved.

## Acceptance and honest limits

Native concept specimens live in `concepts/`; actual game captures and measured
results are separate. `FrostlineVisualTests` exercises real career and race data
across resolutions, locales, text sizes and contrast settings. Some secondary
pages inherit the shared system rather than a bespoke composition. The HUD has
no new minimap; the authored map remains the existing pre-race visualization.
Race simulation, saves, standings, replay payloads and fairness rules are
outside this visual change. Owner review remains required to accept the final
appearance and close M14.
