# TASK-055 — Interval start scheduler

Immutable stable roster order and exact global fixed-tick start slots from CompetitionProfile.
First start explicitly configured; require an exact whole-tick interval and reject
nonrepresentable intervals rather than silently accumulate drift. No start before due tick,
no duplicate emission, capacity/invalid-tick atomicity. Connect due starts to lifecycle timing
in tests, preserve replay ordering and zero allocations. Non-interval formats follow M7.
