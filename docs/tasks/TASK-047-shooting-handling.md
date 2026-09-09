# TASK-047 — Shooting speed and handling

Versioned skill/weapon modifiers for acquire/setup, bolt duration, recoil and settling.
Wire timing profile into real lifecycle; expose deterministic recoil offset to later full
shot harness. Gear effects bounded by shared envelope; same geometric ray must still hit
regardless of handling skill. Test timing trade-offs, decay, bounds, replay and allocations.
