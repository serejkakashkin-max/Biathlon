# TASK-050 — Shooting training harness

Status: PASS, 2026-09-07. Added ShootingSession/Input/Frame, complete model/athlete data,
ShootingCommandTape, ShootingCsv, versioned training scenario/runner and CLI shooting command
via scripts/Run-ShootingLab.ps1. The private session composes aim → physiological oscillators
→ hold/pressure/stance → previous recoil → wind/geometry → new recoil. External tick/lane,
physiology and wind are validated before any subsystem advances. Aim is clipped only at the
shared numeric sight domain ±0.5 rad. No hidden player outcome RNG.

258/258 .NET and Unity EditMode pass; zero Console errors, four upstream warnings.
Evidence: artifacts/physiology/wp050-standalone.trx, wp050-final-{editmode,console}.json and
artifacts/shooting/training{,-repeat}. Both CLI runs match. Pinned full CSV/event SHA fixture
passes in both .NET and Unity: tests/fixtures/shooting-traces-v1.sha256.
Training: 214 ticks, 5 shots, closed_mask=4, 29 events. This deliberately near-centre script
exercises repeat-target accounting rather than pretending to aim at all five targets.

Serialized tape replays every input/frame/event including external physiology/wind/context.
Changed seed/context, malformed rows, capacity exhaustion and invalid late input reject before
mutation. Complete AI also drives the composed session. Active session+recording allocates zero
measured bytes. Earlier Foundation and both ski trace versions remain passing.

HR/fatigue inputs are supplied by the caller; the training scenario holds them fixed to isolate
conditions. Race physiology recovery integration belongs to M5. The CLI writes an incomplete
summary before simulation and only marks passed after successful completion/replay. CSV is a
cold allocating export. Model state/recordings are technical artifacts, not final UI/animation.
No ski balance change or commit. Next: WP051 statistical shooting Balance Lab.
