# M16.6.4 — illustrated training and bounded regression repairs

2026-11-01. Feature implemented; owner review and further legacy repair pending.
Branch `codex/m16-6-equipment-weather-training`, after weather `046e000e`.

Six original code-native sport illustrations use the existing six training tasks.
The detached week plan shows actual slots, individual stat deltas, form, fatigue,
health and risk from TrainingPreview. No training formula or adaptation change.
RU1600x900 and EN1024x768 large text/high contrast captures were inspected; plan
removal wraps, all six tasks remain available by scrolling. Actual gamepad input
focuses and adds a session without applying the week or mutating saved state.

Qualification receipts:

| Check | Result |
| --- | --- |
| .NET training subsystem | 17/17 PASS |
| .NET start corridor, weather and original v25 replay | 26/26 PASS |
| .NET economy lock, unchanged numerical draft calibration and purchase atomicity | 9/9 PASS |
| Native EditMode, lock/draft/weather/career pages | 16/16 PASS |
| Native training/UI/gear capture | 6/6 PASS |
| Native actual training gamepad + weather recording v1 seek | 2/2 PASS |
| Native start corridor + original v25 replay | 7/7 PASS |
| Native actual 30-body start, including stationary player | 2/2 PASS |
| Native current Mass Start replay and seek | 2/2 PASS |
| Native independent visible replay UI, forward/backward seek | PASS,118.586s |

Groups overlap; these counts do not imply a unique aggregate. The final replay
tool returned `plugin_reconnected`; the test continued. Actual `testResults`
after completion proves Passed, archived in native-replay-recovered.json.

ADR-0088 explains the narrowly scoped complete start/main corridor proof and
near-zero wait fix. New journals explicitly version that policy. Weather recording
v2 binds authored world heading for headless vector wind; v1 and original v25
journals preserve their old context. No body/contact/power/route-credit tuning.
Career tuning lockv2 preserves historical lockv1 and every M8 input. WP097 keeps
the original archived hash; exact comparison excludes only context_sha256 and
proves every numeric value/event hash unchanged, also natively.

Separate Windows build `Builds/M16_6_4_Training_Review/START_TEST.cmd`:
Unity6000.6.0f1 Succeeded,0errors/520existing warnings,430940476bytes,32.255s.
Actual fullscreen standalone PASS/exit0,2560x1440FullScreenWindow,bib23,
start13201/tick13205,personal3-2-1-0,owner profiles unchanged,errors empty.
Native Console0errors/exceptions/asserts. Optional navigation probe was not
enabled; its false fields are not passing claims. No FPS benchmark inferred.

The historical equipment-stage full assembly is still running and has old
failures; no globally green claim. Exact1717c530 reproduces the five additional
failures retained in baseline-other-legacy.trx (force-boundary2,world identity,
base-route transaction,range arrival). Owner explicitly authorized continuing
their diagnosis and repair in this task. No failing test or threshold disabled.
M16.6 remains OPEN; M17 not started. STATE history bytes remain an exact prefix.
