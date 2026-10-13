# ADR-0065 — M14 personal start countdown and waiting skip

2026-10-13. Owner-requested M14 correction; milestone acceptance remains open.

## Problem

The existing three-second preparation held the entire field at tick zero. In a
Pursuit it counted down before the first athlete's gun, not the player's delayed
departure. Interval athletes were also hidden until `OnCourse`, so the chase
camera followed an inactive, unpositioned pivot while the player waited.

## Decision

`PlayerStartPreparation` is a Unity-free presentation policy. It reads the
player's unchanged authoritative scheduled tick. The current versioned
`race_start_v2.json` defines three countdown seconds and bounded skipping
(256 accepted ticks / 8 ms per render frame). The original v1 definition remains
readable. These values are presentation policy, not an IBU rules revision.

An immediate start retains the existing three-second local hold: race time,
input clocks and the wall-clock accumulator remain at zero. If a registered gap
is shorter than three seconds, only the missing preparation time is held before
the first gun. Longer gaps start the field normally and show a personal waiting
card. The last three seconds display 3–2–1 before the player's scheduled start.
No qualification gap, start ordinal, athlete capability or result clock changes.

The waiting card offers a button, Enter and A on the explicitly selected gamepad.
Skipping runs the existing player/AI/manual-world step, with neutral player
commands, until `scheduled_start_tick - countdown_seconds * frequency`.
Every intervening tick, event and accepted command reaches the existing recorder.
There is no snapshot teleport, reseeding or artificial opponent progress.
The host publishes one detached observation per batch. On landing, only the
presentation interpolation history is reset, preventing a visual rewind across
the coalesced interval. A fresh wall-clock accumulator then drives the remaining
three seconds in real time. The host's normal fault handling remains authoritative.

Controls are released during preparation/waiting; W/Shift cannot charge a pace
or spend reserve before departure. Existing career pause deactivates the race
root and suspends both waiting and skipping. No save or replay schema migration
is required; preparation itself is not an authoritative race event.

The selected player alone may be rendered during `Prestart`, at its existing
launch pose. Other unstarted interval athletes retain their visibility policy.
The camera snaps behind the player when preparation is armed. This presentation
exception never changes authoritative `OnCourse`, body admission or collisions.

## Verification

Unit coverage includes immediate, short and long gaps, exact skip targets,
malformed policies and zero-allocation observations. Native coverage compares
ordinary and skipped Pursuit commands/poses, bounds batch size, checks Enter
with held W/Shift, clickable UI, pause, player visibility/position, RU/EN and
large text. The original Mass Start countdown/device tests remain green.
Complete deterministic Pursuit and manual-journal regressions also pass.

Evidence and build details: [owner report](../reports/TASK_M14_PLAYER_START_20261013.md).

## Limits

Skipping is bounded simulation and may take a short amount of computation for
long gaps/large fields. It remains interruptible through the existing pause.
Native visual fixtures omit environmental dressing to isolate the new UI;
the delivered game still loads the unchanged venue/weather/quality assets.
M14 closure remains the owner's decision after testing.
