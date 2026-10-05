# TASK_M13_OWNER_REALISM_PRESENTATION_01

2026-10-05. Owner corrections after WoodlandTrafficGuidanceReview20261005.
Status: native acceptance PASS; Windows content checks PASS; final evidence publication in progress.

The tuck now bends at the hips with the hips behind the boots, compact hands,
bent elbows, and airborne poles trailing almost parallel to the local snow plane.
The shipped hero rig, boots, skis and grips were checked on level snow and two
downhill grades. Other locomotion poses retain their approved minimal body motion.
This changes presentation only; tuck activation, drag and its costs are unchanged.

Assigned-mat guidance starts within90m of the actual firing stop. Seven chevrons
appear ahead of the skier along the selected physical path, rather than remaining
at the distant mat. The final3m pulse/capture and initial1m departure assistance
retain their existing authoritative limits. The mat-number label was removed.

The target camera previously hid the entire athlete during Setup. It now activates
only during Aiming/BoltCycle. The chase view shows reaching/removing the rifle,
lowering into the prone stance or settling the standing stance, and recovery after
the last shot. The existing setup/exit deadlines and all shooting authority remain.
The chase camera also follows the actual sampled visual grade with smooth response,
so a downhill route is not framed like a horizontal straight. This is a presentation
improvement, not completion of the owner request for more readable terrain.

Changed systems: AthleteLocomotion/Driver; AthleteGroundContact; AthleteChaseCamera;
AthleteProxyPresentation; PlayerRouteGuidancePresentation; RangePresentation;
their presentation JSON and four PlayMode fixtures.

Native results: early-guidance2, ground-contact4, locomotion2, camera2,
range-camera1, full rifle pipeline3, range-assistance5, proxy1:20PASS, zero skips.
The real Woodland early-guidance test first failed with an86.934m nearest arrow;
the corrected trail starts within6m. The real hero tuck first failed with hips
not behind the boots. The Setup camera regression also failed before correction.
All three failures were reproduced and corrected without disabling their assertions.

Raw evidence is retained under `artifacts/owner-realism-20261005/` and
`artifacts/owner-traffic-guidance-20261005/`. Normal native Game View captures include
the actual30-body host, early trail and prone preparation. Paused stale render
captures were rejected; the accepted frames followed a real Editor render step.
The side/chase tuck images show a paid player tuck at tick1563/Main391m.
Hidden Windows rendering is suspended and does not establish FPS/GPU performance.

Known open owner work: natural physically coupled AI steering, rare deterministic
contact response, full playable manual heading, remaining approach/branch queues,
and broader terrain clarity. This task does not close M13/WP175–178 or those issues.
