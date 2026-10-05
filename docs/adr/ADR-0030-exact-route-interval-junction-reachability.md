# ADR-0030 — Exact physical slice reachability for production junctions

Status: accepted implementation; 2026-10-05.

ADR0025 captures reachable loop identities. That is insufficient for a route
which uses Main before and after a shortcut but skips its intervening interval.
The owner-compressed Woodland course has a measured Main/shortcut clearance
region inside that skipped interval. Loop-only binding incorrectly activates
both ports. Two following shortcut athletes then claim an exclusive region
whose Main port cannot be occupied in this race, causing a permanent queue.
The actual qualified Pursuit40 record stops; a focused failing test proves the
incorrect active region without running a complete race.

Production route-bound fields now capture and merge exact positive physical
intervals from every race section and every possible range lane, plus the full
penalty and optional start loop. A port is reachable when its authored guard,
padded by the complete longitudinal half-body, intersects those intervals.
Closed-loop guards check both periodic images. A region remains active when
at least two incident ports are reachable, including real same-loop crossings.
Unknown points or sweeps entering skipped intervals throw.

The old null and explicit loop-list constructors retain their legacy behavior
and context. The production exact constructor records sorted merged nanometre
intervals, body definition and the v2 reachability policy in replay context.
It does not alter any authored guard, body, collision projection, retained tail,
paid travel, lateral costs, speed, shooting or admission priority. An unreachable
port supplies no competing occupancy; reachable guards still require the same
authoritative simultaneous safety proof.

New regression coverage includes the actual skipped Main crossing, inclusive
body-touch boundary and 1mm outside case, skipped-point/sweep rejection,
reachable range paths, self-crossings and zero-allocation hot queries. Complete
current races and exact persisted replay are required separately.
