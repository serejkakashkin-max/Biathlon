# TASK-078 — Repeated 30-athlete AI soak

WP-078. Repeat three complete 30-athlete fields (flat expert seed123, rolling developing seed456,
flat developing seed789). Require identical context/results/streaming regression digest/CSV tail,
two bouts per athlete, exact penalty-inclusive travel, completion, no endpoint overlap or sustained
lateral oscillation. Enforce a 120-second individual no-progress bound outside shooting/staging.
Keep fixtures, thresholds and failures; fix observed starvation, do not relax acceptance.

Atomic correction: FIFO junction arrival after incumbents, scratch tickets committed with the
physical tick, versioned context and observable wait timestamps. Update affected golden results
only after independent physical assertions and deterministic replay, retaining prior evidence.
Status: DONE — 2026-09-09. See the corresponding report and WP078 final evidence.
