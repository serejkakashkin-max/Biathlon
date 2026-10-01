# TASK169A–C — production range path prerequisites

2026-10-01. WP169 remains IN_PROGRESS. Venue01 is not complete.

TASK169A: immutable versioned PhysicalRangePathDefinition with ordered physical slices, directed seam validation and exact track/junction/connection snapshots. Every lane covers the same canonical distance; firing coordinates derive from actual mat anchors. The two-lane contract fixture has firing offsets28/48m for equal100m physical travel and explicitly has no world-geometry/production approval.

TASK169B: pure PhysicalRangePathMap for exact incoming/outgoing mapping, actual terrain samples, terrain/slice boundary distance, individual firing coordinates and full replay source context. Shared entry/exit aisles retain shared physical identities and existing occupancy queries detect bodies sharing the exit. Warm queries allocate zero bytes.

TASK169C: optional map in RacePathMotion preview takes actual physical samples for ski forces/curvature and splits substeps at every slice/terrain boundary. Default callers retain the existing path. A new map cannot drive a shooting traversal until the individual stop integration is supplied: unsupported map+RangePenaltyTraversal calls fail explicitly. No live athlete/AI/pack adapter currently selects the new path.

Verification: .NET **39/39 PASS**: contract13, map7, motion3 and16 legacy projection/range/replay/regression tests, including six pinned scenario trace/event hashes. Unity contract13/13, map7/7, motion3/3 PASS;616 discovered in the final focused run, only selected tests executed. Final full PlayMode **168/168 PASS**,0failed/0skipped (198.008s); Console0errors,5existing warnings, compilation_failed=false. Independent review found no actionable defect within this scope. Machine evidence: ../quality/m13/wp169abc-verification.json. This is a technical prerequisite checkpoint, not Venue01 completion.

Known remaining work: actual world geometry/seam tangents, individualized firing stops in transactional athlete preparation, actual sample in physiology/posture/drafting, corridor width steering, receipt projection/retained incoming tails, fractional Mass Start lane order, local intersection/admission guards,30-athlete/occupied-mat/penalty flow tests, production terrain/stadium dressing/cameras and a visual build. None is marked complete by the contract fixture.

Next: TASK169D integrate selected physical layout and personal mat stop in event-free prepared movement, then physical pack commit/admission. Preserve v1 fixtures and the owner-approved G2 visual build.
