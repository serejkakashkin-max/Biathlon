# TASK-071B5C5 — Fractional admission clock

Add contiguous fractional substeps inside one simulation tick. Use integer billionths of a
tick, explicit start/end and a completion flag. Tick counts completed ticks only; expose pending
coverage separately. Require full coverage before sealing. Zero-time seam batches move no athlete;
moving substeps cannot include seams. Reject gaps, overlaps, negative/overflowing endpoints,
early completion and moving to another tick while the current one remains unsealed. Preserve
session/revision preview invalidation and legacy whole-tick calls. Test movement before/after a
zero-time seam within one tick, exact coverage failures, endpoint seam/seal and allocation.
