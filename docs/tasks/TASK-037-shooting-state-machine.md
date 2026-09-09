# TASK-037 — Shooting state machine

Implement a pure fixed-tick range lifecycle: enter, wait for externally assigned stable lane,
setup, aim, accepted shot/bolt cycle, exit. Durations and ammunition count are versioned data.
Lane ownership remains the competition coordinator contract (M5). No hit/miss outcome is
invented before WP038/WP046 geometry. Emit lifecycle and shot-acceptance events, test timing,
invalid-transition atomicity, deterministic replay and no allocations. Run full .NET/Unity
regressions and inspect Console before report/state completion.
