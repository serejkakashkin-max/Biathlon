# TASK-006 — WP-006 anaerobic reserve

Dependency WP-005 PASS. Fixed-tick W'-like normalized resource with watts/capacity conversion,
above-threshold nonlinear drain and below-threshold recovery, recovery modifier/fatigue interaction.
Telemetry reports value and actual clamped change/second. Reject invalid inputs and unrepresentable rates.
Tests: analytic constant load, threshold boundaries, finite clamp at exhaustion/full, larger capacity,
recovery comparison, effort integration, deterministic long intervals and no allocation; .NET/Unity/Console.
No skiing/AI multipliers. Tuning provisional. Roll back only this task's additions.
