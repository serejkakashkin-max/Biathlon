# M13 — conservative Woodland route corridor cells

The manual-heading movement kernel needs physical course edges before it can
replace route-following movement. The local route projection map now precomputes
four inward planes for each straight visual edge: left, right, entry and exit.
Each cell uses the narrowest authoritative track width crossed by that visual
edge. A query returns a cell only when the occupied body plus clearance fits
strictly within it. Authored knots and short/narrow sections require separate
transition admission; the query never grants a free step across a seam.

The stable edge IDs and planes are built once. Warm lookups allocate nothing.
A combined test uses the actual Woodland route and a physical outward velocity;
the joint sweep returns the right edge as the first limiting boundary. Static
tests cover all sampled selected route occurrences, widths, body support and
rejection at knots.

Validation: 33/33 focused .NET tests PASS, native Unity route/corridor class
16/16 PASS; scripts compiled and the recent Error Console was empty. Evidence:
`docs/quality/m13/owner-manual-corridor-20261006/`.

This is a local convex-cell check, not a whole-course proof or a live race
integration. Switching between cells, real branch ownership, range/penalty
paths, simultaneous contacts and race event commit remain. The existing
playable auto-turn movement is unchanged. M13/WP175–178 remain open. No Windows
build was made before the complete owner correction batch.
