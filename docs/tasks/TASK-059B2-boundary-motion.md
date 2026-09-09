# TASK-059B2 — Fixed-tick boundary motion

Expose a bounded duration within the existing fixed tick and split movement at exact route
coordinates. Preserve full-tick behavior and all pinned movement traces. Solve the first
quantized boundary arrival deterministically, return consumed/remaining time, retain speed,
then let the orchestrator resample the next surface before advancing the remainder.
Cover stopping, caps, coordinate overflow, elapsed budget, surface change and allocation.

Boundary solving uses 48 fixed bisections, a numerical algorithm constant rather than gameplay
tuning. Crossing accuracy is the existing nanometre coordinate lattice, not a claim of real
physical precision. Physiology and commands still advance once per configured simulation tick.
