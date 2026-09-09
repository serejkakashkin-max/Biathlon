# TASK-045 — Wind model

Pure world-vector to lane-local projection, explicit exposure and bounded versioned projectile
scale/speed. Horizontal drift follows GDD crosswind × ballistic scale × flight time. Expose
physical diagnostics plus a coarse direction/strength indicator for presentation. No RNG or
implicit aim compensation. Test rotation, sign, zero/shelter, bounds, deterministic geometry
composition and zero allocations in .NET/Unity.
