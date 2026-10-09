# Production-sized manual red fixtures — 2026-10-09

Mass30 initially rejects tick163, actor23, Course/UnresolvedStart: the conservative
disk cover reports -0.326mm clearance after 25 accepted provisional pieces.
Correction after exact geometry inspection: that disk result is NOT proof that
the actual occupied rectangle lies outside the course. The rectangle still fits;
the following physical continuation crosses its unchanged clearance boundary.
Sprint40 rejects tick3788, unstarted actor7, SourceMismatch. Both complete-race
assertions remain enabled. `manual-production-fields-first-20261009.trx` retained.

Ranked hypotheses before changes:

1. Sprint: stationary refinement/interval slicing may publish a conservative
   acceleration bound or tiny motion for an absent row. Inspect the exact guard
   and original/prefix trajectory before modifying any force/presence policy.
2. Sprint: source/window or interval duration mismatch at another actor's event.
3. Sprint: captured schedule incorrectly changes physical presence mid-tick.
1. Mass: AI outside-line turn/contact deflection needs earlier fitting heading
   intent. Do not move bodies inward or enlarge the corridor.
2. Mass: an accepted earlier contact piece may disagree with the next source
   ribbon/cell observation. Inspect piece continuity before changing AI tuning.
3. Mass: force/contact continuation or numerical bound error. The 0.326mm gap
   exceeds the existing contact encoding tolerance; it is not permission to relax
   full-body proof.

One-variable trials and exact first failure receipts follow. No queue fallback,
speed/pose reset, body shrink, skipped field row or test removal is authorized.

## Results and retained receipts

The exact stationary polynomial of an absent row can have a nonzero conservative
acceleration bound. Authenticate its zero polynomial, pose, velocity and yaw;
do not require that bound to be zero. The same attested mask must reach all
pair iterations in group impulse response. Public unbound group previews still
include every body. Exact rectangle containment additionally requires complete
edges disjoint from the simple polygon, not just four inside corners.

| Trial / receipt prefix | Mass30 first rejection | Sprint40 first rejection |
| --- | --- | --- |
| fields-first | 163 Course / UnresolvedStart | 3788 absent SourceMismatch |
| stationary-law-trial | 163 Course | 4858 ContactResponse |
| impact-presence-trial | 163 Course | 8724 Course |
| exact-rectangle-trial | 163 Course / PotentialBoundary | 8724 Course / PotentialBoundary |
| moving-passing-trial | 154 Course | 12835 MovingFiringArrival |
| achieved-pursuit-trial | 135 Course | 4201 private exit Course |
| shared-edge-alignment-trial | 233 Course | 10609 BoundarySchedule |
| one-tick-intent-trial | 163 Course | 8728 Course |
| early-pass-arc-trial | 274 first-bend Course | 7842 pushed shooting hold |
| low-slide-friction-trial | 198 Course | 7425 Course |
| shared-snow-inverse-trial | 292 Course | 6134 pushed shooting hold |
| forecast7-trial | 160 Course | 8789 collapsed arrival ordering fraction |

All prefixes are `manual-production-` and suffixes `-20261009.trx`. Experimental
AI recovery, moving-passing, forecast, friction and retry changes were removed.
Production contact sliding friction remains 0.2. Complete-race assertions stay
enabled. Final checkpoint full manual run: 586 PASS / 2 FAIL / 0 skipped / 588,
6m07s; failures are original large-field tick163/8724 course continuations.
No qualification of the host or M13 is claimed by those two enabled red tests.

Native actual executions: presence6/6, group9/9, ribbon16/16 PASS. Tool discovery
counts are not execution counts. Source/frame diagnostics were temporary and
removed. The new collapsed ordering observation needs its own minimized fixture;
the detached forecast variant is not a passing race or production selection.
