# M16.2 — Sprint start draw

2026-10-30. Owner-requested correction on `codex/m16-2-live-shooting`.
M16.1 is approved; M16.2 remains open for owner testing; M17 has not started.

## Behavior and compatibility

`CareerSprintStartDraw` draws every eligible Sprint entrant, including the player,
with an unbiased Fisher–Yates shuffle. `CareerRaceProvider` supplies the project's
seeded RNG on a separate stream derived from the prepared race seed. Each ordinal
is possible, including first. Reopening, abandoning or reloading the same race
retains its draw. Different races can coincidentally give the same player number.
No saved career RNG is consumed by this draw.

The drawn order feeds the existing interval scheduler, stable athlete-ID lookup,
bib presentation and personal countdown. A delayed player can skip to three
seconds before their own start. Every skipped race tick still executes. No start
interval, athlete capability, physics, shooting outcome or graphics tuning changed.
Mass Start retains ranking seeding; Pursuit retains qualifying Sprint order/gaps.
Existing careers need no migration and completed results are not rewritten.

New replay captures use `start_order_version: 2`. Versions 0/1 explicitly restore
the old Sprint order, independently of existing weather and course-force policy
compatibility. Future unsupported start-order versions are rejected. See the
[ADR-0055 addendum](../../../adr/ADR-0055-m14-prepared-career-race.md).

New Sprint race/event hashes intentionally change because the start order changed.
This is a game-wide uniform draw, not a claim to implement IBU start-group draws.

## Verification

* **15 native Unity cases passed**: [draw and old/new journals, 7](draw-native.json),
  [HUD lifecycle and actual 30-athlete board, 2](broadcast-lifecycle-native.json),
  [native replay viewer/seek, 1](replay-viewer-native.json),
  [metadata validation, 4](replay-metadata-native.json),
  [retained HUD/input/layout, 1](gameplay-hud-native.json).
* **27 CoreCLR cases passed**, reported separately from overlapping native cases:
  [draw/regression, 8](sprint-draw-coreclr.trx) and
  [unchanged Mass Start/interval/Pursuit/metadata, 19](unchanged-start-formats-coreclr.trx).
* [6,000 fixed-seed draws](distribution.json): all 30 entrants exactly once in
  every draw; every player ordinal covered, with 176–223 occurrences per ordinal.
* [Two complete 30-athlete Sprints](regression.json): identical result JSON/hash,
  event hash and 29,760 ticks; 300 accepted shots per run; qualifying Pursuit order
  matches the resulting Sprint. Result SHA-256:
  `27efbdff4732e0adc94a4f6fcb4e5634bfcc9df1259ab649b6a966a7b1359eab`.
* [Compiled-player protocol](fullscreen/review.json): actual career creation,
  Continue, recovery, settings/bindings/checkpoint/export, delayed drawn start,
  matching bib, skip and personal 3–2–1–0, 2560×1440 borderless fullscreen;
  owner profiles unchanged. This uses isolated QA profiles and ordinary production
  race/menu code, not manually edited starts or athlete poses.
  The final player drew ordinal/bib 20, scheduled tick 11,401 at 20 Hz.

Native [wait window](fullscreen/08a-drawn-start-wait.png),
[countdown](fullscreen/08-countdown.png), [race HUD](fullscreen/09-race.png).
These explicit development captures composite the native world and native UI in
GPU. They are visual/state evidence, not human play or an FPS benchmark. The
earlier [world-only capture protocol](fullscreen-first-world-only.json) validated
state but did not show UI because its camera cleared the shared capture surface;
the final captures use separate targets. Ordinary rendering was not changed.

## Regression found during qualification

The native replay viewer initially stopped on tick 1: a disabled `GameplayHud`
remained subscribed to the accepted feed and tried to lay out the board after its
`UIDocument` root had been released. A focused lifecycle test reproduced the exact
failure before the fix. The HUD now unsubscribes when disabled, skips refreshes
without an active document, and restores current accepted state when reenabled.
The actual replay seek and 30-athlete board both pass after this fix.

Red evidence is retained in [minimal reproduction](hidden-hud-red-native.json)
and [full replay diagnosis](replay-viewer-diagnosis-native.json). No assertions
were removed or tolerances relaxed. One unrelated test-report write hit Windows
IO error 1224 on an old opened artifact; [failure retained](gameplay-hud-artifact-lock-native.json).
The HUD fixture now writes each run to a fresh artifact directory, preserving
review history and avoiding truncation of an opened report.

## Delivery

Run `Builds/M16_2_Live_Shooting_Review/START_TEST.cmd` in desktop fullscreen.
[Final build summary](build-summary.json), [binary hashes](build-fingerprint.json),
[Console](console-final.txt), [authority guard](authority-guard.json),
[append-only state history guard](history-guard.json).
Final build succeeded with 0 errors, 24 existing API-deprecation warnings and
385,286,159 bytes. Native Console: 0 errors, 24 warnings, compilation settled.
The common builder's historical receipt ID remains `m15_5_visual_review`; the
folder and milestone identify this M16.2 candidate. Build warnings are recorded
in the summary, not treated as a zero-warning result.

Among 655 protected starting-workspace files, only the intended race composition,
replay metadata and the already-delivered player-audio definition differ. The new
Career draw helper is additional. Domain/Simulation/profile files remain exact.
Unrelated owner/Unity changes are preserved and excluded from this publication.
Previous board/audio evidence describes its stated earlier candidate; current
binary hashes are here. Next task is the owner's Sprint and M16.2 gameplay test.

Collector: `scripts/quality/report_m162_sprint_draw.py` asserts the evidence and
history/source guards; it never changes source, profiles or state history.
