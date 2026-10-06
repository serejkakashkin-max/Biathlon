# M13 — continuous turning contact and simultaneous response

Implemented continuous exclusion for accelerating/turning manual trajectories and
a separate simultaneous-contact velocity solver. This advances the replacement
for legacy route-aligned separation; it does not enable it in the owner build yet.

Changed systems:

- Heading frames expose force-weighted velocity, acceleration and numerical error
  bounds; existing trajectory integration and legacy command behavior are unchanged.
- Pair sweep finds the first interval not proved clear, including rotating skis
  that cross between separated endpoints. It reports unresolved work explicitly.
- Group response processes stable-ID-ordered angular impulses, retains raw spin,
  rechecks the entire group and reports residual closing speed/work exhaustion.
- Two versioned numerical definitions and distinct contexts bind the new algorithms.

Validation: .NET `sweep-group-core-replay.trx` passes 106 tests, including 23 legacy
replay tests. Native Unity: sweep 11, group 8, pair regression 16, all pass.
Unity is out of Play Mode, compilation complete, and the final recent Error Console
query is empty. New tests cover analytic integration error, within-step tunnelling,
stationary rotating corners, acceleration/stopping, close parallel passage,
simultaneous contacts, permutation determinism, a 40-actor field, energy/momentum
conservation and no warm allocations. No full-suite or owner playtest claim.

No authoritative race caller, production preset, race balance or legacy replay
format changed. No Windows build was made, in accordance with the accumulated
M13 batch directive. Earlier allocation-anomaly evidence remains untouched; it
did not recur in the current combined run.

Limits: potential contact needs arrival refinement; reached group response needs
continuous post-impact slip/spin integration, edge checks and resting-contact
handling. Paid route/lifecycle, AI, v2 replay and host integration remain open.
The owner queue and full manual steering are not fixed in the playable game yet.

Contract: [ADR-0036](../adr/ADR-0036-continuous-manual-contact-and-group-response.md).
Receipts: [contact evidence](../quality/m13/owner-contact-sweep-20261006/).
M13/WP175–178 remain OPEN. Next work is joint movement after contact and course
edges, followed by the outstanding race integration; then one consolidated build.

## Follow-up — snow impulse and continuing contact

Added passive side-slip/spin resistance using the existing cornering grip budget.
Simultaneous front/rear snow impulses dissipate energy without inventing spin in
pure lateral slip or stopping forward travel. Impulses retain unrounded physical
spin; the new output includes energy loss and applied grip. This is a fixed-pose
velocity operator; it does not yet integrate the resulting displacement.

Added explicit registered-contact continuation with a tagged receipt and distinct
context. Parallel contact and forward separation can continue without a zero-time
stop; rotating a ski rear into a neighbour stays unresolved. Initial deep overlap
and work exhaustion cannot authorize travel. Ordinary new-contact rules are intact.

Final focused .NET/replay run: 116 PASS, no failures/skips. Native Unity: updated
sweep 13 PASS and snow 8 PASS. Prior unchanged group 8 and pair 16 native receipts
are retained. An initial test expected a nose-away turn to be clear; sampling
demonstrated actual rear overlap. The test now asserts that collision, with a
straight departing athlete as the positive case. No production relaxation hid it.
The 300-case snow matrix verifies energy/impulse bounds and forward momentum.

Evidence adds `continuation-snow-core-replay.trx`, `native-continuation.xml` and
`native-snow.xml`. This remains disconnected from the playable race. M13, owner
queues, manual steering and the consolidated build remain open. Next: temporal
arrival refinement and joint spatial continuation/edges, then race integration.
