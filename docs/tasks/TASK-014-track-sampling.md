# TASK-014 — WP-014 deterministic track sampling

Dependency WP-013 PASS. Binary-search ordered segment data by exact millimetre distance. Linear grade;
per-segment curvature/width/snow/wind/semantic/overtake/draft metadata. Boundary policy is explicit:
internal boundaries select next segment; terminal endpoint is last segment end; wrap only closed loops.
Tests: every boundary and 1 mm offsets, interpolation, terminal/clamp/wrap/huge coordinates, open-loop
rejection, metadata, repeated full-loop numeric baseline and warmed zero allocation. Run .NET/Unity/Console.
No movement or smoothing/tuning changes; discontinuities reflect authored data. Rollback sampler/tests/docs.
