# TASK-016 — WP-016 venue/lap/race-route composition

Dependencies WP-013..015 PASS. Compose ordered loop occurrences into laps and a finite race route.
Validate track/profile IDs, profile lap count/base distance and required range access. Use the route-facing
projection of CompetitionProfile; full competition rules remain WP-053. Only synthetic profile data now.
Integer cumulative distances, observable lap/section coordinates and allocation-free sampling.
Tests: mixed loop lengths, exact lap/junction/finish boundaries, mismatched references/count/distance/range,
penalty exclusion from base route, deterministic repetition and allocations. .NET/Unity/Console.
No real IBU profile or hidden rule changes; no physical junction/3D closure claim. Rollback route-only files.
