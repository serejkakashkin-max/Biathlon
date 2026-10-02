# TASK169K — exact production format route slices

2026-10-02. Implementation checkpoint; WP169 remains IN_PROGRESS. ADR0025 records the contract and compatibility boundary.

Venue01 now binds three exact production profiles to explicit schema2 physical routes. Mass Start uses five 3000m Main laps. Pursuit replaces a real 1383.344m Main interval with an 883.344m shortcut, yielding five 2500m laps. Sprint replaces 180m with a real 513.333m extension; three 3333.333m laps plus 1mm of paid final Main travel total exactly 10000m. Original Main, start, penalty and thirty personal mat loops retain their physical identities. Branch travel is never a teleport or a distance rescaling.

`PhysicalRoutePlanValidation` is the shared cold Domain contract for the catalog and simulation. Positive whole-mm nonwrapping slices require exact named directed seams; same-loop boundaries must be adjacent or a closed terminal wrap. Every shooting lap includes the complete range interval and paid finish travel after rejoin. Sampling, physics terrain boundaries, checkpoints, perception, physical maps and penalty traversal apply each section's physical start offset. Integer-mm grade interval boundaries avoid a false overlap caused by summing binary floating values.

Production races capture all reachable route, possible mat, penalty and actual start loops for junction admission. Unused alternative-format forks do not serialize Main traffic. Multiple used ports on Main retain real self-crossings. Unknown physical points are rejected, and the exact sorted usage and full original sidecar indices remain in replay context. Legacy null bindings retain their behavior.

The production factory accepts only exact catalog profile/route pairs. Sprint/Pursuit explicitly select reusable reservations and support sixty entrants on thirty mats; Mass Start still requires a distinct covered mat for each entrant. This count test proves construction and source binding, not completed production races.

Validation on the frozen integrated core:

- .NET140/140 focused tests, including route7, catalog6, junction usage4 and factory5; `artifacts/wp169j/production-final-focused.trx`.
- Unity EditMode route7/7, catalog6/6, junction usage4/4 and factory5/5 (887 discovered; only those22 executed).
- Independent full-format world audit:1322 actual chords,69 zero-gap directed connections, exact three distances; production static validator zero diagnostics. Tracked proof: `docs/quality/m13/venue01-full-formats-verification.json` and `...static-report.json`.
- Complete rotated-route fixture replay, cold invalid-source rejection, changed context rejection, warm zero-allocation queries and canonical closed-seam identity pass.

The actual thirty-athlete Mass Start cleared its curved start but stalled at the first range fork. That failure is retained in `docs/quality/m13/venue01-mass30-range-stall.json`; it is not a completed race or route/content acceptance. TASK169N must correct paid AI steering for the narrower private corridor and repeat real dense races. Actual full Sprint/Pursuit, production replay, visual delivery and venue balance remain required before WP169 closes. No athlete capability, hidden multiplier, authoritative shooting tuning or approved player control changed.
