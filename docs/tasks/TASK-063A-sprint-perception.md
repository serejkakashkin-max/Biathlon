# TASK-063A — Sprint perception source

Read-only, allocation-free capture from a coherent completed field tick. Expose self race/
physiology/shooting state, exact base-route remaining distance, required penalty obligations,
next scheduled range entry, caller-selected route lookahead samples and signed route-progress
gaps. Latest checkpoint elapsed gap/observed placing must remain distinct from spatial progress.

Use caller-owned buffers and value copies, never mutable simulation references in the snapshot.
Draft availability is explicitly unavailable for M5 independent paths. Physical corridor/draft
capture is TASK-063B before WP063 closes; no artificial wake from route-progress coincidence.
Verify prestart, shooting, penalties, finish, replay consistency, malformed buffers and GC.
