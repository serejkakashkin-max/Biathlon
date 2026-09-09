# TASK-071A — Simultaneous pack force motion

Build a fixed-roster, owned-placement corridor kernel: shared draft from one start snapshot,
shared SkiForceSystem proposals, bounded lateral motion with existing overtake cost, and one
SeparationResolver commit for all participants. Caller supplies physically derived delivered
power and validated whole-tick terrain/width; no caller-supplied position resets. Validate all
rows before committing state/events. Return draft/force/contact/placement telemetry.

This atomic task does not connect Sprint range/penalty crossings or lane queues. WP071B must
compose route-boundary substeps and continuous physiology before full race pack acceptance.
Test dense catch-up, simultaneous lateral changes, shared-force equality, per-ID order symmetry,
invalid-last-row atomicity, event replay and allocations.
