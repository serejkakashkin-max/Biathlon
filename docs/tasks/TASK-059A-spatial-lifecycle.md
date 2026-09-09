# TASK-059A — Separate range and lap notifications

First atomic task within WP059. Replace WP054's combined lap/range transition with explicit
EnterRange on the configured shooting lap, then CompleteBout and required penalty receipts,
then ReachLapBoundary at the real lap endpoint. Keep event IDs stable; add the new action at
the end of the enum. Update lifecycle integration tests and result receipt fixtures. Reject
early lap completion, repeated range entry and finish before all obligations. Verify precise
elapsed time and all deterministic regressions through .NET/Unity.

Then TASK-059B composes global clock, continuous physiology, ski motion, traversal and shared
shooting; TASK-059C adds field start/finish orchestration and scripted/AI completion tests.
