# M13 — achieved world drafting and original force-piece portal roots

Date: 2026-10-09. Component qualification PASS; M13 remains OPEN.

The scoped manual field now computes aerodynamic shelter from actual world
positions and achieved velocities. Each leader's physical travel direction defines
its wake. The existing Pack draft definition and formula remain authoritative;
desired lines and counted route distance cannot invent shelter. Stationary bodies,
opposing travel and draft-prohibited surfaces do not provide it. The actual
interval force input receives the computed drag multiplier and the existing
scalar headwind transformed by the observed route tangent. Delivered power and
physiology receive no bonus or refund. Wind-vector, stable tie and zero-allocation
controls pass. This does not yet qualify the production weather matrix.

The changed arrival speeds exposed two retained failures. At tick16210 the
athlete's forward component stopped inside the tick while substantial physical
side-slip continued across the mat-exit portal. The boundary scheduler now searches
the original drive stop/cap/restart intervals and validates the complete original
trajectory prefix. It uses analytic roots of known force pieces, never an endpoint
chord or a fitted arbitrary curved-heading law. Minimal roots before and after
the real stop pass. The existing breakpoint test now compares whole-drive and
first-force-piece roots exactly; its previous UnsupportedTrajectory expectation
became obsolete when the scheduler itself gained this split. Its nonconstant-law
assertion and all unresolved curved-heading negative controls remain.

At tick16253 a real moving-body contact pushed the exiting skier towards the
course edge. Ahead moving range obstructions now request a braking fraction from
the achieved relative forward speed, existing AI response time and shared braking
cap. The AI reduces propulsion while steering around the neighbour. Stationary
obstacle passing and player signed input remain unchanged. No queue mode, pose
correction, velocity clamp, smaller body or relaxed course/phase proof was added.
Broad slip-alignment and rotating-envelope trials failed and were removed; their
receipts remain alongside the original failures. Temporary debug logging was
removed. Context versions identify the changed force and AI decision paths.

Validation:

- Full manual .NET selection: **528/528 PASS**, zero skipped (3m23s).
- All four bouts: one athlete tick16360, three tick16814, zero penalties;
  maximum five moving intervals in the three-athlete case.
- Native Unity: achieved drafting5, obstacle corridor/braking10, original force
  portal pieces2, event/boundary controls21, exact two-run four-bout witness1,
  Mass30 stationary-player400ticks1 — all PASS, zero skipped.
- Unity compilation false; Error and Exception Console empty.
- Existing legacy drafting5 and persisted AI-free Mass30 replay1: **6/6 PASS**
  (3m1s). The complete recorded/replayed result, poses and event hash agree.

Native discovery totals are not executed-test counts. Exact repeat compares actual
poses, velocities, motion, physiology, interval counts and complete event entries.
Mass30 validation covers the first course bend; it is not a full Mass Start race
or a GPU/performance approval. All retained FAIL receipts are diagnostic history,
not final failing acceptance assertions.

Changed systems: DraftingSystem, scoped field publication/stepper, original drive
trajectory pieces, boundary scheduler, AI moving-range closing brake and their
unit/integration/deterministic controls. Race balance changes in the scoped manual
candidate because real shelter and moving-obstacle braking now affect physical
arrival speeds. The legacy draft law stays numerically unchanged.

M13 / WP175 / WP177 / WP178 remain OPEN; WP176 PASS. Production host still uses
lateral-v1. Actual penalty debt, finish/runout, reusable mats, manual command
replay, full2–3m capture/presentation and host selection remain required before
closing the stage. Owner shooting-feel feedback remains pending appropriate
tuning. Final Windows owner build only after ALL M13. PROJECT_STATE is appended
without changing any historical byte.

Evidence: `docs/quality/m13/field-stepper-20261008/`, including
`manual-world-draft-and-force-pieces-all-20261009.trx` and native JSON receipts.
