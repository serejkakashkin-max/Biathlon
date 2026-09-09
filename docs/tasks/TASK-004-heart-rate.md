# TASK-004 — WP-004 HeartRateSystem

Dependency WP-003 PASS. Implement GDD HR load/target/overload/ceiling and exact exponential inertia
at fixed frequency. RecoveryModifier scales recovery time constant (stat curve follows WP-009).
Return target/load/next HR telemetry without Unity or allocations. No cardiac exposure dynamics yet.
Tests: 180s workload vs analytic response, 60s recovery, faster recovery modifier, no instantaneous
jump, saturation, invalid input, deterministic repeated workload, zero hot allocations; Foundation regression.
Validate shared tests in .NET/Unity and inspect Console. Tuning remains JSON-only/provisional.
Rollback: this system/result/tests/documents, preserving prior work.
