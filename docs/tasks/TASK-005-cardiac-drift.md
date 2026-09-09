# TASK-005 — WP-005 cardiac drift

Dependency WP-004 PASS. Bounded high-load exposure accumulates above configured threshold and decays
at/below it using RecoveryModifier. Drift = fatigue × configured bpm strength × exposure fraction.
Return exposure and drift telemetry. Provisional decay coupling documented, no race movement.
Tests: long saturation, fresh vs fatigued, recovery decay, HR integration/ceiling, determinism and
zero allocation. Quantized exposure boundaries must be representable. Run .NET/Unity and Console.
Rollback only this task's source/tests/docs; prior tasks remain intact.
