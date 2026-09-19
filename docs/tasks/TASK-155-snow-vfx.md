# TASK-155 — Snow interaction VFX

Status: DONE. Full PlayMode60/60 and Windows two-scene/three-quality smoke PASS; see TASK-155-snow-vfx-REPORT.md.

Implement bounded ski/pole snow spray driven by observed motion/contact, with restrained contextual intensity, distance scaling, rewind/reset and explicit particle budgets. Presentation-only: no simulation coupling, gameplay RNG or friction changes. Prefer original procedural particles and already installed render APIs; record provenance.

Validate contact emission, capacity/expiry, disabled/distant behavior, warmed allocation and real-host integration. Review close/chase captures and measurable cost through Unity MCP; keep accumulation modest and subordinate to leg/equipment readability. Preserve G1 cameras and OWNER-FEEL-001. Update report/state and push, then WP156 vegetation.
