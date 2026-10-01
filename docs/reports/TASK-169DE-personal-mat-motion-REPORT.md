# TASK169D/E — personal mat stops and physical receipts

2026-10-01. WP169 IN_PROGRESS; Venue01 production is not complete.

TASK169D adds an optional immutable path association and derived firing stop to RangePenaltyTraversal. SprintAthleteSession can bind one physical lane for its full race; preparation uses actual terrain for physiology/posture and every ski-physics substep. Shooting begins only at the accepted personal mat coordinate. Rejected travel cannot start shooting. Path identity, lane ID and authored ordinal are checked before construction can emit events; exact source context is retained. A lane cannot be changed behind this fixed-path adapter without a prepared transaction.

TASK169E extends PhysicalMotionProjection with the same optional physical path. Receipts must stay inside one physical slice, with exact incoming/outgoing ownership; a whole range overlay or a later return to the same loop cannot hide a seam. Stationary seam receipts keep outgoing identity. Fraction slices conserve travel exactly, including penalty circuits. Shared aisles use shared occupancy coordinates, so independent mat paths do not hide overlapping exit traffic.

Review found and corrected a buffer-capacity risk: repeated path slices can traverse the same terrain segments many times. A checked cold count includes every terrain interval in every slice occurrence; the owned movement buffer uses that bound. The null-path branch retains its previous sizing/behavior. All hot path queries/preparation/projection measurements remain allocation-free.

Final validation: .NET **85/85 PASS** including all physical-path cases, original range/motion/projection/athlete preparation, Sprint replay/six pinned scenario hashes and Mass Start ordered-range regression. Unity traversal9/9, athlete6/6, motion3/3, capacity3/3 and projection6/6 PASS (640 discovered in final focused runs). Full PlayMode **168/168 PASS**,0failed/0skipped (194.369s). Console0errors/5existing warnings, compilation_failed=false. Machine evidence: ../quality/m13/wp169de-verification.json. Independent review passed after correcting its capacity finding. An initial test incorrectly compared escaped nested context text; it was corrected to decode and check the exact source context. An incomplete class-name filter ran zero Unity tests and was discarded; all listed runs used exact class names.

Limits: no field/pack factory selects the new path yet. One fixed lane per athlete proves stop/physics/receipt integration, not Mass Start dynamic lane order or range admission. World seams/tangents, lateral corridor control, retained body tails, local conflict guards, occupied mats/penalty traffic, dense30 race verification and rendered production Venue01 remain open. The approved G2 review build and authoritative shooting tuning are unchanged.

Next: TASK169F prepared lane/path/traversal choice and joint commit, followed by pack flow integration and production venue geometry. No owner gate is pending.
