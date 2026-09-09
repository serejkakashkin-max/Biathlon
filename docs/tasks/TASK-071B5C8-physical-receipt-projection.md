# TASK-071B5C8 — Physical receipt projection

Project one validated timed race receipt onto one physical branch with exact travelled distance
and quantized time bounds. Main-route origin must match the map; crossing a section within one
receipt is rejected. Penalty origins must explicitly come from traversal state. Slice projected
receipts on the common fractional grid with telescoping integer distances, retaining full
circuits even when endpoints coincide. Validate ownership, bounds and malformed inputs; verify
real Sprint segment distances, seams, invalid crossings and zero warmed allocations.
