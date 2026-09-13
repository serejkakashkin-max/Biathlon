# TASK-144 — Rifle carry transitions

PASS after WP143 (80e4c28). Explicit rifle/pole bindings, stable back carry, controlled hand transfer and return implemented from detached phase elapsed/duration ticks. Full PlayMode36/36 and Console0 errors; see TASK-144-rifle-carry-REPORT.md. Contact refinement remains WP148.

Acceptance: repeatable setup/aim/exit samples; no prop duplication/reparent accumulation, no harness/head clipping in inspected transfer samples, hands track the held rifle during transfer, no changes to the camera/authoritative aim. Test terminal endpoints, timing at multiple tick rates, reversal/reset and world placement invariance. Inspect both fixed and chase views through Unity MCP. Prone body lowering and standing shoulder alignment follow WP145–147.
