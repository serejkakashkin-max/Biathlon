# TASK-063 — AI perception report

Status: PASS, 2026-09-08. TASK-063A/B implement SprintPerceptionSource and
PhysicalPerceptionSource with caller-owned buffers and immutable value snapshots. Simulation
only exposes existing immutable track/profile/route references and current range motion.
No gameplay stepping or tuning changes. Route lookahead uses caller-selected monotone offsets,
clamped at finish; current terrain uses the penalty branch while traversing penalties.

Snapshot includes tick, identity/phase, physiology, speed, remaining base distance, next
scheduled range entry, outstanding penalties, shooting state, route-progress neighbors and
latest known checkpoint elapsed gap/observed place. Observed placing is provisional and based
on exact checkpoint timing, not final registered result rounding. Range/base distances exclude
outstanding penalty travel and are labeled accordingly. Future controller must account for it.

Physical perception uses authoritative corridor placements supplied by its caller and the
existing DraftingSystem, with stable-ID output and explicit same-tick/athlete binding. Different
branches do not interact. M5 independent paths explicitly lack physical draft data; WP071 must
supply actual pack placements. No placement is guessed from equal race progress. Perception
reads on the simulation owning thread between completed ticks; it is not a concurrent snapshot.

331/331 standalone and Unity EditMode tests pass, Console zero errors / four upstream warnings.
Complete-race observation reproduces identical results/events; all previous pinned regressions
pass. New tests cover prestart, mat, penalty terrain, frozen finishers, clamped lookahead,
invalid queries, stale/duplicate physical data, shared draft equivalence and order independence.
Both warmed capture loops allocate zero bytes in .NET; Unity GC-counter-only results have the
calibration limitation recorded at M5. No new presentation behavior; M5 PlayMode 6/6 retained.

Next: WP064 tactical intent model, then data-driven utility evaluation/cadence/controllers.
