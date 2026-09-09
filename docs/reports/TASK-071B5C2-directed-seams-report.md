# TASK-071B5C2 — Directed seam connectivity

Added Domain PhysicalConnectionDefinition, Simulation PhysicalConnectionMap/PhysicalSeamTransition,
synthetic_flat_connections_v1.json and PhysicalConnectionTests. The schema has stable definition,
connection, junction and port IDs. Each connection is explicitly directed and has kind
coincident_seam. Finite connectors require an explicit physical path; no implicit travel edge
is inferred from sharing a conflict region. Definitions bind exact track and junction snapshots.

Runtime checks require the exact incoming anchor and correct map; the result retains incoming
and outgoing points, the junction index and unchanged cumulative travel. It performs no admission,
ownership release, body removal, race progress mutation or event emission.

The synthetic fixture joins main entry at 900 m to the penalty seam in both explicit directions.
It intentionally returns to 900 m rather than jumping to rejoin at 920 m. Integration tests using
actual RangePenaltyTraversal verify 1/3/5 complete circuits, exact 150 m per miss and the remaining
20 m to rejoin. This preserves the M5 travelled-distance policy. The fixture is authored synthetic
topology, not a claim about surveyed world geometry or a new race policy.

Standalone and Unity MCP EditMode 386/386 PASS, including prior deterministic regression
fingerprints. Console zero errors/four upstream warnings. Results are captured under
artifacts/physiology/wp071b5c2-*. Prior PlayMode 6/6 at B5B is retained; no new visual behavior.
git diff --check passes with pre-existing CRLF conversion warnings. Added five test cases cover direction,
nanometre anchors, foreign maps, changed snapshots, invalid/ambiguous definitions, round trip,
actual traversal and warmed .NET allocation. No tuning or old race outputs changed.

Remaining: incoming-body retention, safe transition admission, follower replanning and coordinated
race commit. JunctionAdmissionSession still rejects branch changes; this map must not be used to
bypass that check. Full pack replay must bind this connectivity JSON plus exact track/junction
snapshots and initial occupancy. WP071 and TASK-071B5C remain IN PROGRESS; no owner gate applies.
