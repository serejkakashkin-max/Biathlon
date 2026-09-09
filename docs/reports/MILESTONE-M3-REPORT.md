# MILESTONE M3 — Player controls

Status: PASS — 2026-09-07.

Master Plan gate: controllable headless skier input stream is deterministic and replayable.
WP030–036 deliver immutable tick commands, effort presets/assist, constrained steering,
technique assist, physical draft feedback, exact-context command tape and device harness.

Evidence: TASK-035 and TASK-036 reports; 198/198 .NET and Unity EditMode tests, 4/4 Unity
PlayMode tests. Device-generated complete rolling course reproduces motion, resources,
lateral position, finish timestamp and event stream after tape serialization. Existing
canonical Foundation and .NET/Unity ski trace regressions pass. Active command building,
recording and controlled simulation loops retain zero-allocation checks.

Gamepad and keyboard paths are checked against real virtual Unity Input System devices.
Frame sample repetition/batching does not advance keyboard effort or multiply action pulses.
Axis deadzone and response exponent are versioned data. Host selects the input device.

This is the technical M3 gate. Visual handling approval, shipping input UX, rebinding and
pack presentation remain later work. M2 physics/balance is unchanged for neutral commands;
steering costs from M3 are included in full replay. No current owner decision or blocker.
Next unlocked package: WP037, shooting state machine.
