# TASK-032 — WP-032 steering/line intent

Dependencies WP031 PASS. Convert normalized line intent into usable-width target and fixed-tick
lateral response. Optional center deadzone, speed-dependent response and tuck steering multiplier.
Query swept occupancy; expose blocked/target/desired placement, line quality and lateral cost.
Final simultaneous movement belongs to separation resolver. Test width, symmetry, rate/tuck,
blockers, line-quality consequence, invalid inputs and GC; .NET/Unity regressions.
