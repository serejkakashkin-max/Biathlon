# TASK-071B5C3 — Retained transition footprint queries

Added PhysicalTransitionFootprints and immutable PhysicalTransitionFootprint in Simulation;
bound PhysicalSeamTransition to its creating connection map. The incoming footprint is the
interval [incoming anchor - remaining tail length, incoming anchor], with periodic wrapping.
Its initial extent is ceil((body length + personal clearance) / 2) in nanometres. Actual forward
travel shrinks the tail; exact zero-length seam contact remains occupied until one more nanometre
is travelled. Zero travel preserves it, and complete outgoing circuits cannot restore it.

Queries combine the retained incoming interval with a conservative full outgoing body. They
check arbitrary authored junctions, including other regions overlapping the incoming tail.
Swept queries retain the initial tail during the whole interval and query the outgoing sweep,
so clear endpoints alone cannot erase a crossing. Results are immutable preview values and
carry cumulative travelled distance with checked arithmetic; they mutate no race authority.

Added three tests to PhysicalConnectionTests: exact body-clearance boundary, stationary body,
closed incoming seam and outgoing full circuit, shared-region retention after tail clearance,
separate upstream junction clearance, invalid/default/foreign inputs, overflow and warmed
.NET allocation. Standalone and Unity MCP EditMode 389/389 PASS; Console zero errors/four
upstream warnings. Captures use wp071b5c3-*. Prior PlayMode 6/6 at B5B retained; no new visual
behavior. git diff --check passes with existing CRLF conversion warnings. Unity's known GC
counter limitation means warmed zero allocation is evidenced by .NET, not Unity zero alone.

No force/tuning, shooting, old race progress, replay events or regression fingerprints changed.
Limitations: one retained incoming branch; another transition while that tail remains needs
multiple retained footprints and must not overwrite this state. Very short incoming/open paths
that cannot contain the tail are rejected. Outgoing full-body checks are conservative, not an
exact bent-body geometry solver. Begin is a pure mapping query, not permission to pass or replace
an active admission state. JunctionAdmissionSession still rejects branch changes; integrate
these footprints with reservations and prepared race commits before marking WP071 complete.
