# TASK-043 — Prone and standing

Versioned stance profiles define distinct breathing/heartbeat/muscular multipliers,
setup/exit duration, aim movement and recovery contract. Wire transition and aim modifiers
into existing systems without changing neutral callers. Provide deterministic sway composition;
prone has less sway, slower transitions/aim and better recovery. Test both profiles and
cross-system timing/aim behavior; recovery multiplier is consumed by later bout physiology.
