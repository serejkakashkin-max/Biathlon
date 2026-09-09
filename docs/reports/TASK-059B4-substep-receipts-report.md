# TASK-059B4 — Atomic receipts and substep timing

PASS, 2026-09-08. CompetitionLifecycle.StepBatch validates ordered same-tick receipts without
publishing events, restores all state on failure, then commits each receipt once. Multiple
penalty completions/lap boundaries need no artificial extra simulation ticks. FinishTime
supports canonical timestamps inside a tick; CheckpointTracker.ObserveMotion uses validated
timed route segments, excluding penalty travel from main-route progress.

312/312 standalone and Unity EditMode pass; Console zero errors/four upstream warnings.
Evidence: artifacts/physiology/wp059b4-{standalone.trx,editmode.json,console.json}.
Late invalid receipt rollback, no duplicate events, allocation-free batch/idle tracking,
surface-boundary timing and earlier stationary-time accounting verified. Existing single-tick
APIs and all deterministic regression fingerprints remain passing. No tuning or commit.
Next: TASK-059B5 complete athlete session with continuous physiology and shared shooting.
