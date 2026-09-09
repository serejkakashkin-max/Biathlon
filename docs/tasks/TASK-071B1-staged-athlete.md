# TASK-071B1 — Staged Sprint athlete step

Split SprintAthleteSession into event-free preparation and single-use commit while preserving
Step as their composition. Preview contains immutable physiology/movement values plus an
owner/revision handle for access to prepared segment copies. Prepare never advances shooting,
range, physiology, lifecycle, checkpoints or event logs. Shooting computation stays in commit.

Every preparation attempt invalidates the prior handle; foreign, default, consumed and replaced
handles are rejected before gameplay mutation. Rejected input does not advance authority. This
is simulation-thread-only scratch storage, not a concurrent/multi-version transaction.

Test full Sprint parity through every phase, pre-commit authority/event immutability, stale/
foreign handle rejection, immutable copied segments, allocation cost and existing pinned hashes.
Constrained trajectory application follows B2; no externally authored motion is accepted yet.
