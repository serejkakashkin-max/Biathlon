# ADR-0092 — Exact reuse in ski simulation queries

2026-11-03. Owner reports intermittent stalls during skiing and requests a deep
performance investigation. M16.6.10 follow-up; no new gameplay policy.

## Evidence and decision

Production fixed-tick markers separate race work, snapshots, athlete presentation,
environment and HUD. A repeatable 30-athlete case uses the real ManualStepper,
original venue, High quality and a moving explicit camera. Whole-environment and
athlete submissions occur once per frame. The initial player profile contains
race-step spikes up to 26.224ms; environment median 1.569ms and athlete presentation
median 0.802ms. Rendering counters show approximately ten million triangles per
frame with the existing stable scenery shadows. GPU timings are unavailable on
this player/backend; no GPU-time or minimum-FPS claim is justified.

Temporary CPU attribution isolated contact/course proofs in the start pack and
the complete snow-command inverse when approaching the range. The latter repeats
identical physical requests after grip saturation. Repeated current-body queries
also enumerate an unchanged contact group, and arrival pairing resamples the
same immutable prefix endpoint for every pair.

Keep all proofs, geometry, tolerances, fixed ticks and command scoring. Reuse only
identical deterministic values:

- Complete snow search still scores the original integer domain in original
  neutral/negative/positive order. Per-call signed caches reuse a snow result only
  for exactly equal bounded target yaw and angular acceleration budget. Unbounded
  requested yaw is diagnostic; it does not enter the snow-force calculation.
  Neutral remains separate. Legacy search/context is retained.
- Current contact groups are reused only while every pose and physical
  participation flag compares exactly equal. A changed pose, provisional motion,
  abort/restoration or changed participant causes a rebuild. Failed geometry is
  never cached. Stable pair order and the original full exclusion checks remain.
- Arrival endpoints are sampled once per participating actor for that query's
  exact earliest prefix, then shared across pairs. Every later query rebuilds its
  endpoints. EndpointPoseSamples and OwnedContactGeometryBuilds expose this work.
- Frozen contact, driven and coupled trajectories return their already computed
  State at fraction 1. Constructors compute that same expression once; interior
  samples, slices, force integration and validation retain their original laws.
- Complete ribbon proofs are reused for an exactly equal coupled law, source
  projection and apron. Source, duration, slice and all force/pose/velocity fields
  are compared. Cached diagnostics and threatened pose are restored. Interior
  geometry and contact-direction queries retain their complete original fields.

No context/version change is required because selected commands and physical
outputs are unchanged. Before/after CoreCLR witnesses cover 2,400 ticks × 30
athletes for both automatic and fixed manual input. They hash every pose, raw
velocity, cursor/credit, physiology and AI steering/brake: automatic
1889c670a7c3587a; manual f8169cb1f5e027d1. Regression tests pin these prior values.

## Verification and limits

Three regressions first failed on the original behavior: 2,001 snow evaluations
for a saturated query; 12 endpoint samples for four actors; two geometry builds
for two identical current-body queries. Their final assertions preserve the
original global command choice, actual arrival group and invalidation behavior.
Existing no-allocation, geometry, numerical and replay-policy tests remain active.

Evidence and final measurements: [performance report](../quality/m16-6/performance/README.md).
The stress camera is independent of the player's fixed input; it deliberately
includes start-pack/course-edge pressure, not a representative average race lap.
Headless 120-second cases separately exercise ordinary skiing and range approach.
The optional diagnostic never runs on normal startup and never writes a career.
Deep-profiling and temporary core timing probes are removed. Runtime markers
remain in Unity adapters only; core assemblies have no Unity dependency.

Single expensive physical proofs can still exceed a frame budget. This change
does not lower proof work limits or remove shadow geometry to disguise that cost.
Owner review on the original problem route remains useful after delivery.
