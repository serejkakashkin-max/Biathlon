# TASK-086 — Complete thirty-athlete Mass Start PPSS

Objective: verify the shared field through five laps/four PPSS bouts, geometric shots, actual
penalty distance, final-lap bypass and exact complete arrival results for30 AI athletes.
Dependencies: WP085 DONE (ef705df). Reuse authoritative field; no separate physics or hit policy.
Acceptance: all30 finish under the existing120000tick safety bound,20 accepted shots each,
correct bout stance/laps, full actual travel including grid offset and penalties, no overlap,
no120second skiing stall. Persist trace/result fingerprints and validate through Unity MCP.
Follow with WP087 explicit deterministic lane/order policy. No tuning changes in this audit.
