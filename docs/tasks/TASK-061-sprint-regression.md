# TASK-061 — Sprint regression fixtures and CLI

Author versioned fixed-seed scenarios: perfect shooting, one intentional geometric miss,
aggressive/conservative pacing and equipment/snow comparisons. A precise reference controller
must still issue bounded shared aim commands; it cannot set hit outcomes. Intentional miss
means aiming outside the target apertures, then firing through the normal shooting system.

Export full race CSV/events/results/command tape via Balance Lab, replay every scenario,
retain per-scenario summary and cross-runtime SHA-256 fingerprints. Tests assert fixture
outcomes/physical tradeoffs and .NET/Unity repeatability. Production AI tuning remains
unchanged: the reference controller is explicitly a deterministic regression harness.
