# TASK-040 — Breathing sway

Versioned vertical breathing oscillator, fixed-tick phase, data-driven resting/high-load
frequency and amplitude. HR normalization uses existing physiology definition (no duplicate
HR constants). Fatigue increases amplitude; Breath Control decreases it. Expose phase,
frequency, amplitude and vertical angle. Check cycles, monotonic factors, replay, atomicity
and no allocations in .NET/Unity; keep breath holding/debt for WP044 and stance policy WP043.
