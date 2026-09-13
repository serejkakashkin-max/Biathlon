# TASK-144 — Rifle carry transitions

PLANNED after WP143. Add explicit asset bindings for the rifle and pole props; stable carry under the back socket, controlled hand transfer to the range-ready socket and return. Use detached shooting phases and their exact elapsed/duration tick spans; no independent animation clock may delay readiness or shots. Pole staging must make hands available, with contact refinement reserved for WP148.

Acceptance: repeatable setup/aim/exit samples; no prop duplication/reparent accumulation, no harness/head clipping in inspected transfer samples, hands track the held rifle during transfer, no changes to the camera/authoritative aim. Test terminal endpoints, timing at multiple tick rates, reversal/reset and world placement invariance. Inspect both fixed and chase views through Unity MCP. Prone body lowering and standing shoulder alignment follow WP145–147.
