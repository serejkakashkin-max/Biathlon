# M13 — continuous selected-route inner tube

The Woodland Main visual line has 325 edges, 190 shorter than two metres. A skier
often spans multiple edges, so a four-plane straight cell cannot authorize
ordinary travel through every knot. Added an inner-tube sweep over a bounded
window of the selected route occurrence. The width is reduced by the whole
body's circumscribed radius, authored edge clearance and a reserve derived from
the visual turn angle. A continuous interval is accepted only if its motion and
numerical-error bounds fit inside one local tube segment; unresolved boundaries
and work limits do not grant movement.

The query does not choose a route branch, turn the athlete or award race progress.
It can see the immediate next occurrence only when the caller explicitly admits
it. Stable segment identities and turn reserves are precomputed, with no warm
per-query allocations. Tests traverse every internal Main knot at a fixed physical
velocity, detect an outward departure before the end of the step, distinguish
repeated laps, reject an off-course start and check deterministic output.

Validation: focused .NET route/spatial tests 33/33 PASS; native Unity tube tests
4/4 PASS. Unity compilation completed and recent Error Console was empty.
Evidence: `docs/quality/m13/owner-manual-route-tube-20261006/`.

A subsequent PlayMode cross-check passed 2/2, including 7,368 radial samples
inside the actual Woodland Main ribbon triangles at all interior native mesh
rows. The hidden PlayMode run emitted an FMOD output-device initialization
error; it did not fail tests or indicate a script compilation error. Its XML
receipt is in the same evidence directory.

This is a conservative authored-polyline model. Continuous equivalence to the
rendered ribbon at all joins, actual branch/range/penalty admission, group
contacts, route events, v2 replay and playable host integration remain open.
The present auto-turn game has not changed. M13/WP175–178 remain open; no
intermediate Windows build was made.
