# M16.2 — Gameplay cursor and checkpoint history

2026-10-30. WP-H, owner follow-up on `codex/m16-2-live-shooting`.
M16.2 remains open for owner testing; M17 has not started.

## Controls and behavior

During active play the native mouse cursor is hidden and locked, including start
preparation and TAB. Pause/deactivation, menus, replay controls, post-finish
spectator controls and loss of application focus release it. Resume/focus gain
capture it again for a live player. Shooting still consumes relative mouse delta.
Waiting for a delayed start can still be skipped with Enter/A.

TAB opens the latest passed checkpoint. PgUp/PgDn select earlier/later passed
occurrences; End returns to latest and follows subsequent arrivals. On gamepad,
use right-stick left/right and R3. These are overlay UI navigation keys. Existing
remapped TAB controls are honored and named in the footer; A/D and ski/aim bindings
retain their meaning. The table ignores mouse picking and does not unlock the
cursor. It does not pause the ongoing race.

The heading identifies the selected occurrence and available history count.
Latest/history mode, lap and cumulative race distance make repeated gates clear.
Paging an earlier line retains it when new checkpoints arrive, through pause and
language/text/contrast changes. Reopening TAB selects latest. Empty history and
navigation bounds are explicit; race reset/rewind cannot retain future selection.
No new checkpoint timing archive is copied: the existing accepted trackers provide
each table. Earlier lines remain provisional as later starters arrive, rather than
freezing standings at the player's crossing. Sprint comparisons use elapsed time;
Pursuit/Mass Start comparisons use global time.

The Frostline flags, two columns and all 30 rows remain. An initial large-text 4:3
test caught a 33.98 logical-pixel overflow; [red evidence](table-layout-red-native.json)
is retained. Row spacing/panel padding were tightened with the same font sizes,
then the original layout assertions passed. No graphics quality was reduced.

## Verification

* [Selection model, 6 native cases](history-model-native.json): empty, bounds,
  retained selection, follow mode, rewind/reset and invalid arguments.
* [HUD/input/cursor integration, 3 native cases](history-hud-native.json): real
  accepted fixture splits across laps, keyboard/gamepad edges, actual row times,
  preferences/pause, input transparency, no UI clock/pace advancement, native
  cursor hide/release across TAB, focus, disable and disposal.
* [Actual 30-athlete layout](thirty-athlete-table-native.json) in RU 1600×900 and
  EN large 1024×768; flags/IDs/times and complete table bounds verified.
* [General HUD](gameplay-hud-native.json), [mouse/stick shooting input](shooting-input-native.json),
  [live board lifecycle](broadcast-lifecycle-native.json),
  [replay UI and seek](replay-viewer-native.json) retain their existing assertions.
* [7 CoreCLR cases](history-coreclr.trx), reported separately from overlapping
  native tests, include selection and existing interval elapsed/global timing.

The final [compiled-player protocol](fullscreen/review.json) checks actual native
cursor properties at explicit production focus boundaries and real career pause/
resume. For history captures it supplies ordinary player effort/aim/trigger/bolt
commands with existing AI steering to the real 30-athlete Sprint until accepted
checkpoints are reached, with no edited poses, times, outcomes or stats. Each view
is frozen for a native world/UI GPU composite;
new accepted progress retains the earlier selection and End restores latest.
These are automated state/visual checks, not human play or an FPS benchmark.
Owner profiles remain unchanged; QA uses isolated local profiles.
The final probe drew bib22, viewed ordinal3, selected earlier ordinal2 with27
accepted arrivals, then reached ordinal4 while keeping ordinal2 selected. It
restored latest ordinal4 at accepted tick21774. Its scripted off-target shots
produce actual geometric misses; those screenshot gaps are not balance evidence.
The [initial probe receipt](player-first-command-guard.json) retained a harness
error: after live player binding it attempted an AI-only `Runner.Step`. The
existing command guard correctly rejected that call. The QA now uses the existing
`StepWithManualPlayer` boundary; no production guard was changed or bypassed.

Native captures: [pause menu](fullscreen/08b-pause-menu.png),
[latest](fullscreen/10-latest-split-ru.png), [earlier split](fullscreen/11-history-split-ru.png),
[history EN large/high contrast](fullscreen/12-history-split-en-large.png),
[all 30 rows EN large 4:3](layout/timing-en-large.png).

## Delivery and impact

Run `Builds/M16_2_Live_Shooting_Review/START_TEST.cmd` in desktop fullscreen.
[Build summary](build-summary.json), [current binary hashes](build-fingerprint.json),
[Console](console-final.txt), [qualification](qualification.json).
The common builder retains its historical `m15_5_visual_review` receipt ID;
the folder/milestone identify this M16.2 delivery. Warnings, if any, are recorded
in the build summary, not represented as zero warnings.
Final Windows build: succeeded, 0 errors, 24 existing API-deprecation warnings,
385,300,419 bytes. Native Console: 0 errors, 24 warnings, compilation settled.

Domain, Simulation, AI, Career, Replay and all authoritative JSON remain byte-exact
to this follow-up's starting workspace: [guard](authority-guard.json).
No balance, result hash contract, replay command, save schema or graphics tuning
changes. No extra per-athlete component or per-tick history copy is introduced.
The complete state history remains a prefix: [history guard](history-guard.json).
Unrelated owner/Unity changes are preserved and excluded from this publication.
See [ADR-0056 addendum](../../../adr/ADR-0056-m14-gameplay-hud.md) for behavior
and the Unity cursor reference. Next task is the owner's gameplay review.
