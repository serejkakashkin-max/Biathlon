# TASK-056 — Spatial range and penalty traversal

Bind one ordered competition bout to its unique route section and authored range/penalty
anchors. Consume authoritative physics displacement in integer nanometres. Stop at the firing
line until the shared shooting resolver finishes its five-shot bout and exit timing. Each
unclosed target requires one full authored penalty loop; retain all ordinary course distance.
Expose accepted/unconsumed motion, route/penalty coordinates, phases and traversal events.
Test actual shooting geometry → misses → travelled distance, no movement credit while
shooting, atomic invalid inputs, exact boundaries, replay and allocations in .NET/Unity.

The adapter starts at range entry. The race orchestrator (WP059) owns activation and physics
substeps. Range anchors precede the lap boundary; WP054's combined boundary notification must
be separated into range arrival and lap completion before complete-race orchestration.
