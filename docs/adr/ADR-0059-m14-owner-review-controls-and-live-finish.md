# ADR-0059 — M14 owner review: timing, controls and live finish

2026-10-12. Implemented and verified; owner acceptance remains pending.

The owner tested Sprint and requested a substantial visual redesign, more timing lines,
Tab standings, quicker steering and collision recovery, explicit S braking, HR guidance,
and live spectating/fast completion after the personal finish. These are authorized M14 corrections.

## Design

Keep the original menu biathlete. Use snow `#EDF3F6`, white `#FFFFFF`, stadium ink
`#183849`, race teal `#126F78`, ice focus `#BBDFDF`; warm warning is reserved for
actual negative equipment tradeoffs or high pulse. Bold italic display numerals,
plain readable body text, and restrained captions use Unity's bundled font pipeline.

The inner shell has a compact left navigation rail and a rounded snow workspace.
Next race is the single strong dark surface; season figures and calendar entries are
quiet cards. Calendar/protocol/standings use pages rather than long visible scrollbars.
Training shows the actual forecast in bars; equipment shows skis and signed comparison
bars. The distinctive graphic is an actual course map drawn from authored geometry,
not a decorative invented track. Responsive layout and text scaling retain all data.

Initial screenshot critique found clipped calendar controls and too much training
copy. The revision adds geometry-driven compact pagination, short navigation labels,
training pages and graphic forecasts, and equipment drawings/comparison bars.

## Race controls and cost

Keyboard reaches full deflection in 100 ms (500 units per 20 Hz tick); neutral input
still holds the released world heading without seeking the road centre. Shared yaw
acceleration is 6 rad/s², neutral response 10 rad/s², yaw ceiling 2 rad/s. Grip remains
physical and may limit turning at high speed. A/D introduces no automatic longitudinal
brake in the production preset. S explicitly requests the existing full brake fraction;
rearward gamepad stick requests the same brake. It is serialized in the manual command
and propagated through the shared planner, force solver and accepted replay journal.
Automatic range transit ignores manual braking while it owns the skier.

A passive ski constraint cannot pivot from rest. Below a data-defined 1.5 m/s, a powered
stepping/pole-assisted pivot may spend already delivered power on angular work. Its
angular acceleration and remaining support grip cap the response; its rotational
work is subtracted from propulsion. The frame exposes ActivePivotWorkJoules. There
is no heading teleport, collision bypass or free energy; player and AI use the same
physical operator. An unpowered stationary skier does not gain spin.

## Timing and shooting guidance

Woodland now has six lines per main lap: 180, 372.323, 510, 651.581, 735 and 935 m.
The last line follows the penalty rejoin at 910.005 m. Low-cost visual timing posts
are outside the ski corridor and have no colliders or per-frame scripts.

Tab while skiing opens arrivals at the player's latest crossed line, not a mixture
of each competitor's latest split. Sprint compares personal elapsed time; Mass Start
and Pursuit compare the common clock. Pending arrivals remain provisional; equal times
keep equal ranks. Tab on the shooting range retains the range overview function.

Primary reference: [IBU Event and Competition Rules September 2026, §9.2.7](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf)
requires intermediate timing after the penalty loop following shooting. Additional
forest/climb/descent timing lines are a course-specific game presentation choice.
The [official Datacenter](https://biathlonresults.com/?RaceId=BT1718SWRLCP02SWSP)
provides the race-rank, shooting, result and gap presentation reference.

Within 160 m before a scheduled range, and during shooting, a compact HR band shows
calm/elevated/high pulse at normalized HR loads 0.65/0.85. Normalization and continuous
breathing amplitude use the actual production physiology/breathing definitions.
These are explanatory presentation bands, not discrete accuracy bonuses; fatigue,
stance, skill, breathing and heartbeat continue to act through the existing geometry.

## After the player's finish

The unfinished field keeps running the same AI/physics. A separate camera and UI allow
previous/next unfinished athlete, follow/overhead/side views, and fast completion.
StepRemainingField requires the matching finished player and keeps every authority
tick, event, physiological update and replay command. Fast completion uses bounded
8 ms / 256 tick frame work and publishes only one detached presentation snapshot
per batch. Results are collected and saved only after the real field completes.
Pause/abandon and optional replay saving retain their existing ownership.

## Compatibility and risk

The steering physics and extra checkpoint observations change authoritative context
and race results openly. Existing career profiles remain valid; old command replays
with incompatible track/physics context are rejected rather than silently altered.
No quality preset, texture, shadow or render resolution is reduced.
