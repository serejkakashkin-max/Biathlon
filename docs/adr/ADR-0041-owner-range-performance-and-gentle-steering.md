# ADR-0041 — Owner range feedback and gentle steering return

2026-10-10. Accepted under the current owner directive; supplements ADR-0038,
0039 and 0040. M13 awaits the next hands-on owner decision.

Production A/D remains signed physical steering while held. After release, the
owner now requests a gradual return toward the course trajectory. The default
`player_course_assistance_v1` starts after 0.25 seconds, limits return intent to
220/1000 and changes it by at most 400 units/second. Manual input takes immediate
precedence. The shared deterministic ski solver still owns X/Z, yaw, speed and
contacts. Explicit callers without this optional policy retain manual release.
Effective signed controls, rather than presentation transforms, are replayed.

A read-only geometric trail is shared by drawing and physical arrow capture.
Arrow stations are fixed on the route grid, so they can actually be ridden onto;
they no longer stay a constant four metres ahead of the player. Their separate
snow corridor and mat clearance remain. Capture latches on proximity within the
versioned 1.2 m radius, independently of flashing renderer visibility. It ends
for shooting; automatic departure continues until at least one metre is paid
and the physical heading is within 12 degrees of the continuing private exit.
Control returns on that tick. IBU lane allocation is unchanged.

Shared range AI targets retain the authored aisle until the selected private
fork. Production requests a minimum 0.8 m aisle offset away from the mat row;
arbitrary passing offsets cannot aim across neighbouring mats. Private mat
targets retain their centre and the common physical contact rules.

`world_hold_through_turns` allows a requested low posture to survive signed-world
steering. Actual grade, minimum speed and risk still apply. Actual yaw and its
physical grip/braking costs remain in the prepared motion sample; the posture
query alone excludes turn curvature. AI chooses whether to request tuck through
its existing policy and receives the same physical posture/costs when requested.

The lag investigation reproduced a 358,441,282-character runner context and a
108,849,949-character world context. Re-serializing accumulated JSON inside
another string repeatedly doubled escaping. Optional pack context extensions
now append tokens without discarding source definitions. Geometry/proof contexts
refer to exact immutable catalog/map attachments by SHA-256, using the existing
context-identity convention. Definitions still come from their versioned source
attachments; identity alone is not a standalone race reconstruction document.
Replays compare the new contexts and reject foreign ones; no old tape is silently
reinterpreted. This changes context identity, not physical motion.

The Unity host uses a bounded wall-clock frame scheduler: up to four fixed ticks,
a 10 ms work budget, and 0.2 seconds of backlog. Excess wall time is exposed as
`DiscardedWallSeconds`; every paid simulation tick retains its fixed duration.
During severe stalls simulation can run slower than wall time; it never skips
race ticks or awards time/progress for a pause. Presentation snapshots are
published once after each frame batch. Direct test/control steps still publish
immediately. Rendering quality stays High by default.

Focused regression includes actual first-bout departure, bounded context size,
snapshot allocation, signed control/replay, held tuck, physical arrow capture,
exit hand-back, and bounded catch-up. Hidden tests do not qualify sustained GPU
FPS; the owner will review the resulting Windows build before closing M13.
