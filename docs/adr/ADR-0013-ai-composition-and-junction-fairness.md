# ADR-0013 — AI composition and junction arrival fairness

2026-09-09; extends ADR-0012. Accepted and verified through WP078.

PackAiRaceRunner captures every actor from one completed physical snapshot, makes cadenced utility
decisions and per-tick pace/lane/aim commands, then advances PackSprintFieldSession once. The
field alone applies power, lateral work, drafting, contact and shot geometry. Difficulty changes
decision cadence/horizon, reaction and control errors only. Profiles, actor IDs, seeds and exact
JSON definitions are recorded in the runner context. Independent per-actor seeded streams are
stable across tactical ticks. Shooting controllers initialize at the global bout clock; the
first observation uses neutral pressure until a first physical shooting frame exists.

Owned value traces include all eight eligibility/utility decompositions, target and rejection
reasons, recovery reduction, shooting risk/tolerances and controller output. Bounded buffers
report overwrite counts and export invariant CSV. Race events 37/38 identify changed intent and
shooting policy; detailed target and score reconstruction uses the trace. Event sinks must not
throw/reenter. AI controller scratch is not rolled back after a failed plan: the runner fails
closed and must be reconstructed from exact context and commands. Field planning remains atomic.

The seed456 developing rolling 30-athlete soak exposed starvation at tick57457: athlete29 waited
2401 ticks at penalty147.249650556m, while later main-route traffic repeatedly took the junction.
ADR0012's progress priority was safe geometrically but had no fairness guarantee. The admission
ordering now retains incumbents first, then oldest unserved approach ticket, then the existing
route progress/ordinal-ID tie break. A ticket starts when the proposed body sweep first requests
a junction; it survives submillimetre stopping prefixes and clears after physical admission or
retirement. Tickets are prepared separately and committed with the physical tick. No unsafe
trajectory is accepted to satisfy fairness. GetJunctionWaitSince exposes pending arrival ticks.
The appended context policy is junction_fifo_after_incumbent_v1. Old tapes/contexts remain
distinguishable; golden shared-pack outcomes may change. Independent M5 races are unaffected.

Acceptance remains finite evidence, not a universal liveness proof for arbitrary authored maps.
Synthetic shared shooting footprints serialize access; authored multi-lane stadiums and the
WP079 believable-competitors/performance report remain separate roadmap work.

