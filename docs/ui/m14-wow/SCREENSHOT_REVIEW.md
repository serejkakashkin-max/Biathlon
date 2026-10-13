# FROSTLINE native screenshot review

All images below were rendered by Unity. Design-study mock data is confined to
the separately labelled concepts folder. M14 artistic acceptance belongs to the owner.

## Main menu and career

| Screen | Before | After |
|---|---|---|
| Home | [baseline](../../quality/m14-wow/before/home-ru-1600x900-100.png) | [Windows player](../../quality/m14-wow/standalone-release/01-home.png) |
| Create career | [baseline](../../quality/m14-wow/before/new-career-ru-1600x900-100.png) | [Windows player](../../quality/m14-wow/standalone-release/02-new-career.png) |
| Dashboard | [baseline](../../quality/m14-wow/before/dashboard-ru-1600x900-100.png) | [Windows player](../../quality/m14-wow/standalone-release/03-dashboard.png) |
| Training | [baseline](../../quality/m14-wow/before/training-ru-1600x900-100.png) | [Windows player](../../quality/m14-wow/standalone-release/04-training.png) |
| Shop | [baseline](../../quality/m14-wow/before/equipment-ru-1600x900-100.png) | [Windows player](../../quality/m14-wow/standalone-release/05-equipment.png) |
| Calendar | [baseline](../../quality/m14-wow/before/calendar-ru-1600x900-100.png) | [Windows player](../../quality/m14-wow/standalone-release/06-calendar.png) |
| Pre-race | [baseline](../../quality/m14-wow/before/pre-race-ru-1600x900-100.png) | [Windows player](../../quality/m14-wow/standalone-release/07-pre-race.png) |
| Results | [baseline](../../quality/m14-wow/before/results-ru-1600x900-100.png) | [Windows player](../../quality/m14-wow/standalone-release/11-results.png) |

Result images are an isolated recorded-result boundary fixture used to inspect layout;
the test does not pretend to have completed that synthetic result as a live race.
The original before field capture has the documented depthless-stencil artifact.
Corrected field pixel tests and actual Windows captures show readable input values.

## Race and broadcast

| Surface | Before native panel | After native panel |
|---|---|---|
| Skiing | [baseline](../../quality/m14-wow/before/hud-ski-ru-1600x900-100.png) | [Frostline](../../quality/m14-wow/after/hud-ski-ru-1600x900-100.png) |
| Shooting | [baseline](../../quality/m14-wow/before/hud-shooting-ru-1600x900-100.png) | [Frostline](../../quality/m14-wow/after/hud-shooting-ru-1600x900-100.png) |
| Tab splits | [baseline](../../quality/m14-wow/before/hud-tab-ru-1600x900-100.png) | [Frostline](../../quality/m14-wow/after/hud-tab-ru-1600x900-100.png) |
| F2 broadcast | [baseline](../../quality/m14-wow/before/hud-f2-ru-1600x900-100.png) | [Frostline](../../quality/m14-wow/after/hud-f2-ru-1600x900-100.png) |
| Replay controls, 1600 | [historical baseline](../../quality/m14-wow/before/replay-ui-1600.png) | [new replay regression capture](../../quality/m14-wow/after/replay-ui-1600.png) |
| Replay controls, 1024 | [historical baseline](../../quality/m14-wow/before/replay-ui-1024.png) | [new replay regression capture](../../quality/m14-wow/after/replay-ui-1024.png) |

The black-background images isolate a native HUD panel, driven by accepted actual
race observations. For the real scene see Windows [personal countdown](../../quality/m14-wow/standalone-release/08-race-countdown.png),
[skiing HUD](../../quality/m14-wow/standalone-release/09-race-hud.png),
[broadcast](../../quality/m14-wow/standalone-release/10-race-broadcast.png) and
[shooting HUD](../../quality/m14-wow/standalone-release/11-shooting-hud.png).
The replay before files are native 2026-10-12 evidence recovered from baseline commit
`262f340`, not newly captured scenes in this revision.

## Secondary pages and accessibility

Final native screenshots: [standings](../../quality/m14-wow/after/standings-ru-1600x900-100.png),
[history](../../quality/m14-wow/after/history-ru-1600x900-100.png),
[rivals](../../quality/m14-wow/after/rivalries-ru-1600x900-100.png),
[replay library](../../quality/m14-wow/after/replays-ru-1600x900-100.png),
[settings](../../quality/m14-wow/after/settings-ru-1600x900-100.png).
There is no newly collected before pair for these secondary screens.

Small-screen examples: [English creation, 130%](../../quality/m14-wow/after/new-career-en-1024x768-130.png),
[English high-contrast settings, 130%](../../quality/m14-wow/after/settings-en-1024x768-130-contrast.png),
[large shooting HUD](../../quality/m14-wow/iteration-1/hud-shooting-en-1024x768-130.png).
The local `after/` and `iteration-1/` folders contain the full resolution/language
matrix. Git publication includes representative images and aggregate JSON receipts
instead of hundreds of redundant large PNGs.

[7.2-second native Windows demo](frostline-native-demo.mp4): menu transition, then
skiing and shooting clips. It consists of 108 native frames; the cuts and test driver
are documented in the report. The video is not an FPS benchmark.
