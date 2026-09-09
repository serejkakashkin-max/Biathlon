# TASK-059B4 — Atomic same-tick receipts and timestamps

Allow ordered lifecycle notifications inside one fixed tick after movement crosses several
boundaries. Validate the entire receipt batch before emitting events, restoring all state
on failure. No intermediate event publication during validation and no allocations.

Extend checkpoint tracking to consume validated timed main-route movement segments so time
spent on a penalty segment within the same tick is not incorrectly treated as route progress.
Retain existing full-tick API/fixtures. Canonical tick fractions use existing nanounit policy.
Verify time continuity, stopped/penalty intervals, batch rollback, replay and Unity Console.
