# M16.6.11 — Natural ski turn and first range overlay

2026-11-05 owner follow-up. New balanced pelvis transfer and distributed body
bank repair the reproduced sideways fold in the shipped hero rig. Read-only
pose settings:16degree bank ceiling,8cm maximum inward pelvis shift and1.5cm
compression. Both hybrid and legacy drivers use the same definition before IK.
Original roots, skis, physics, inputs and shooting remain unchanged.

The range board now participates in layout while Visibility.Hidden, prepares
its localized header before its first contextual window and reveals without
a Display.None-to-Flex cold layout. Geometry callbacks recompose only when
readable capacity actually changes. Queue, shot order, flashes, retention, TAB,
RU/EN/accessibility and input transparency retain their contracts.

## Evidence

- turn-red.json: both direction cases reproduce a stationary pelvis/sideways fold.
- turn-before.png / turn-after.png: actual hero, rifle, contact and grip corrections.
- board-red.json: unseen original board had zero measured height until first reveal.
- old-range-control.json: pre-existing no-miss fixture failure repeats on original
  HUD source; prepare a real miss with ordinary aim/trigger/bolt commands instead
  of relying on the AI to miss. No outcome injection or deleted/disabled assertion.
- regression-final-native.json:45PlayModePASS. Includes full hybrid arm anatomy,
  techniques, ground/grip, stance matrix, impacts, signed turn reversal, root
  placement, real30-athlete range coverage, queue/flags/miss/retention and HUD.
- responsive-final-native.json: final affected real-field range casePASS after
  conditional resize recomposition; includes a no-new-tick row-capacity assertion.
- presentation-core.trx:40.NET model/observation casesPASS.
- first-reveal-comparison.json / paired compressed profiles: actual30-athlete
  production field at exactly tick1712/510.790815835m,600measured frames each.
  First layout0.3179→0.1527ms, snapshot/publication0.5828→0.4960ms, reveal-related
  geometry events1→0. Baseline whole frame16.348ms and after16.673ms with60Hz cap:
  no large first-reveal whole-frame hitch reproduced on9950X3D/RTX5080. This
  establishes removal of cold layout work, not universal hitch-free performance.
  Explicit1280x720camera RT, no per-frame PNG/file writes; unavailable marker-1.
- build-summary.json: Succeeded,0errors/524warnings,484836810bytes,26.6345957s.
- player-fullscreen.json:2560x1440FullScreenWindowPASS,18artresources,
  isolated profiles/owner career preserved/errors[].
- console-final.json:0Error/Exception/Assert. All25ProjectSettings files restored
  byte-for-byte to task-start state; unrelated work excluded from publication.

Source decision and primary Unity visibility/profile references:ADR0095.
Owner launcher:Builds/M16_6_11_Turn_Range_Review/START_TEST.cmd.
M16.5closed;M16.6OPEN FOR OWNER REVIEW;M17notstarted. Next owner check: right/left
turns and first range-board appearance during normal skiing in this new build.
