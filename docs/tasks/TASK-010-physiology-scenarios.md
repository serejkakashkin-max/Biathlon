# TASK-010 — WP-010 scenarios and telemetry

Dependency WP-009 PASS. Versioned workload suite (required initial resources/stats/segments), headless
runner, invariant CSV with complete input and telemetry, separate canonical physiology fingerprint,
and .NET BalanceLab physiology command producing JSON summary and per-scenario traces/events.
No rendered simulation dependency. Core Step allocation discipline excludes explicit cold export.
Tests: suite validation, duration/segment boundaries, deterministic fingerprint sensitivity/replay,
CSV columns/culture and runner completion. Run CLI, .NET/Unity suites, inspect Console.
Scenario/tuning are provisional M1 workloads, not race balance acceptance. Rollback new files only.
