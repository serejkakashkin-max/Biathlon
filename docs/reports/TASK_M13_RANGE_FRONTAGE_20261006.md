# M13 — range frontage and exit queue correction

The owner still observed a standstill after the second Mass Start shooting bout.
The production AI traffic definition commanded zero effort whenever range-line
alignment was blocked. Removing that cap alone exposed a different physical
conflict: on the paid Main passage beside the range, AI selected a line occupied
by athletes on a mat or returning from the penalty loop. The old safe-side goal
ended at the range exit, five metres before the penalty-entry merge.

The AI now plans its existing data-defined safe-side lateral goal along that
Main frontage, beginning before range entry and continuing past penalty entry.
It also checks world geometry on Main when there is no active range traversal,
including the final lap. The production zero-effort alignment override was
removed. These are requested *goals*: the shared simulation still charges each
lateral movement, validates whole bodies, and may shorten an unsafe step. It
never grants a free lane shift, speed multiplier, or collision exemption.

The field exposes the first block on each tick as `Width` or `Athlete` plus a
stable athlete ID. The reason is captured during the original safety check, with
no second collision pass or per-tick allocation. This makes a future reported
stall reproducible without changing authoritative movement.

The focused .NET run passed 15/15: the actual 40-athlete qualified Pursuit
finished at tick 28,923 with a maximum 28 stationary ticks, a repeated 30-body
penalty merge stopped at most 68 ticks, and all four nearby AI athletes passed
an effort-zero player at the Mass Start exit with no stopped follower tick.
The six-athlete paid-exit replay fixture also passed. The tests assert that the
Main frontage goal remains on the safe side of the penalty merge. The native
Unity owner-exit test passed 1/1 after import; compilation completed and the
recent Error Console was empty. Its XML receipt is in the evidence directory.

Evidence: `docs/quality/m13/owner-exit-alignment-20261006/`.

This changes seeded AI choices, timing and replay context, so older exact race
outputs should not be compared byte for byte. Player control and shooting rules
are unchanged. It does not establish that every possible in-game congestion
case is solved: the owner will test the eventual consolidated Windows build.
M13/WP175–178 remain open, particularly full manual steering and integrated
physical contacts. No intermediate Windows build was made.
