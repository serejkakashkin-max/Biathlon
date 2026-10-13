# M14 — personal start preparation and waiting skip

2026-10-13. Correction delivered; M14 remains **OPEN / owner acceptance pending**.

The player now receives a personal 3–2–1 countdown, including delayed Pursuit
starts. While waiting, **К старту / Skip to start**, **Enter**, or **A on the
selected gamepad** fast-forwards to the final three seconds. Rivals keep racing
through the same accepted simulation ticks. The player is visible at the launch
point and the chase camera immediately follows it. The same behavior covers a
late interval Sprint start.

## Systems / work packages

- WP-182: personal preparation, waiting timer, compact countdown and skip affordance — COMPLETE.
- WP-191/192: every skipped tick retained in command recording; bounded stepping and safe interpolation landing — COMPLETE.
- WP-194: waiting player visibility and camera snap — COMPLETE.
- WP-196/197: RU/EN, enlarged text, actual button/Enter and pause checks — COMPLETE.

Changed sources: `RaceStartDefinition`, new `PlayerStartPreparation` and
`race_start_v2.json`, `ProductionVenueHost` and its start-preparation adapter,
`GameplayHud`/`GameplayPaceStart`, `AthleteProxyPresentation`,
`AthleteChaseCamera`, build entry and focused EditMode/PlayMode tests.
No race physics, athlete balance, rendering quality, weather, save schema or
registered start gap changes. Existing replay context remains unchanged.

## Checks

- .NET Release: **19/19 PASS**, no skips — personal countdown, pace, scheduled
  Pursuit starts, complete repeated Pursuit matrix and accepted-journal replay.
- Native Unity EditMode: **9/9 PASS** — personal preparation policies, including
  zero managed allocations in repeated observations.
- Native Unity PlayMode: **7/7 PASS** across personal waiting (3) and original
  countdown/device regressions (4). The final interpolation correction also
  reruns the command/pose comparison successfully.
- Native images inspected: RU 1600×900, EN 1024×768 at 130% text; button contrast,
  selected player's launch pose/visibility, countdown and pause verified.

The two-athlete production Pursuit fixture schedules the player at tick **601**.
Skipping stops at **541**, then executes **60 real-time ticks** before departure.
Both ordinary and skipped runs record **601 ticks / 139,714 bytes** with identical
commands and athlete poses/frames. Journal SHA-256:
`ba50ef26eb4db671f76b11a29faa675b1dceb023a0631f3965bfa8513313049c`.

The native tool's `TotalTests` field lists assembly inventory; executed counts
above use its passing/failing results. Initial compile diagnostics and a screenshot
assertion against a deliberately hidden panel were fixed. The initial CoreCLR
allocation harness reported 24 bytes; a non-inlined measurement boundary separates
observed calls from boxed assertions, and both CoreCLR and Mono now report zero allocations.
No assertion was disabled or tolerance weakened.

## Test build

`Builds/M14PlayerStart20261013/START_M14_TEST.cmd`.
Win64 Development, Unity 6000.6.0f1: **Succeeded, 0 errors / 0 warnings**,
370,497,489 bytes. Standalone DX11 startup/render at 1600×900 passed
(1,437 visible samples, exit code 0). Final Unity Error/Exception queries are empty.
Evidence is recorded in `docs/quality/m14/player-start-20261013/`.

Skipping computes the actual race in bounded batches, so a long delay may require
a short calculation. Environmental dressing is omitted in isolated native UI
fixtures; production graphics and textures retain their previous settings.

Next owner check: continue a career into Pursuit with a nonzero gap, try waiting
and Enter/button skipping, confirm the personal 3–2–1 and unchanged rival head start.
M14 is not closed by this correction.
