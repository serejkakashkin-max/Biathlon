# TASK-060 — Persisted complete Sprint replay

Bind all race/model/track/roster/start/timing data into exact replay context. Record complete
field ski and shooting intents with run-length encoding of unchanged commands, retaining
their tick ranges. Persist a versioned JSON tape, validate full per-athlete coverage and
contiguous runs, and replay through the same field orchestrator. No raw result reconstruction.

Test full race frames/events/results, changing intents, concurrent independent replay cursors,
context and malformed-tape rejection, capacity atomicity and allocation-free unchanged runs.
The event stream diagnoses replay but commands/configuration remain sufficient to regenerate
the complete race. AI decision inputs are already resolved into recorded aim/effort commands.
