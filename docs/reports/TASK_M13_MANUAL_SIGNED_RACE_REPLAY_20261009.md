# M13 — complete signed command replay — 2026-10-09

`ManualScopedRaceCommandTape` persists the complete fixed-clock roster: effort,
equipment/technique/form intent, signed integer yaw, explicit physical brake,
tuck/sprint, aim, trigger, breath, wind and shooting pressure. Playback calls the
manual field stepper directly and makes no AI decisions. The legacy lateral tape
keeps its schema and meaning. Both tapes share only command payload encoding,
retiming and intent comparison; signed runs use a distinct required member and
kind, so legacy decoding cannot silently discard yaw/brake.

The initial context fingerprints the exact field definition, stepper physics and
initial world state, with a stable roster. These identities require the original
loaded definitions; the tape alone is not a venue/save reconstruction package.
Recording binds its exact initial stepper and clock. It does not re-hash the
mutating selected-lane session context on every tick. Playback validates a fresh
independently reconstructed context before advancing it. External clock changes,
bad final controls, insufficient run capacity, malformed schema and incomplete
coverage reject. Tape runs publish after an accepted movement tick only.

Independent complete Mass Start3, Sprint1 and Pursuit3 witnesses compare every
accepted source/recorded body pose, velocity, odometer, occurrence, observed and
credited arc, route credit, session frame, physiology, drafting, lane and finish.
JSON round trips exactly. Fresh playback reproduces final state, all recorded
field events and reusable reservation records with zero AI tactical decisions.
AI planning telemetry is deliberately not a field-replay event stream. The
Pursuit fixture still uses explicit ten-second lifecycle qualification gaps,
rather than an actual previous Sprint result.

Validation: .NET signed/legacy/device replay selection **32/32 PASS**, no skips.
Native complete Mass Start3 and Pursuit3: **1/1 PASS each**. Native atomic bad-row/
capacity control **1/1 PASS**. Native warm RLE recording and playback **1/1 PASS**,
zero managed bytes after warm-up. Earlier manual physics baseline **569/569 PASS**.
Compilation settled and Error/Exception Console empty. Getter added for the AI
runner's actual brake intent; no gameplay/AI force or shooting tuning changed.

M13 / WP175 / WP177 / WP178 remain OPEN; WP176 PASS. Production host remains
lateral-v1. This is complete small-field persisted movement replay, not full
40/30 field, tied-start, weather, graphical, performance or owner acceptance.
Next qualify the production-sized manual fields, actual Sprint-to-Pursuit flow,
then connect player input/final mat capture/presentation and finish M13 review.
The owner shooting-feel issue stays open for its designated tuning stages.
No owner Windows build is produced before ALL M13.

Evidence: `docs/quality/m13/field-stepper-20261008/manual-signed-race-replay-*`.
