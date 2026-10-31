# M16.4 WP-G — continuous visible audience and forest

2026-10-31. COMPLETE / OWNER TEST PENDING.
Fullscreen test build: `Builds/M16_4_Visibility_Review/START_TEST.cmd`.
Earlier test builds are preserved.

Visible grandstands keep their spectators; trees and winter dressing keep their
silhouettes across the previous110–120m cutoff, on all three graphics tiers.
Scenery outside the camera frustum/far plane or wholly behind the existing weather
fog is still culled. No new fog, shorter horizon, lower-resolution texture or
alteration of the authored venue/tree/seat positions hides the issue.

Systems changed: `ProductionVisibility` supplies a versioned optional production
policy and conservative whole-bound fog test. `ProductionVenueEnvironment` binds
it to the actual weather horizon, while legacy adapters without the policy keep
their original behavior. `VegetationPresentation`, `SpectatorCrowd` and
`ProductionForestDressing` preserve coverage, selecting cheap far models instead
of deleting people/trees. Spectator matrices/bounds are cached until their seating
transform changes; existing forest caching and instance batches remain. The
environment JSON adds one optional binding, and a new policy resource supplies
its schema/stable ID. ADR-0078 explains the compatibility/authority boundary.

The original near/far spectator assets, all trees/textures/materials/shaders and
graphics-tier resources remain unchanged. Two additive distant spectator meshes
reuse the original seated/standing pose, body proportions, clothing colours and
shader channels with **136triangles each**, versus816for the original far model.
They are selected only beyond the former disappearance threshold, adjusted for
camera projection; zoom keeps the original more detailed model. Lower tiers keep
their detailed-instance budgets while retaining the rest as far silhouettes.
Distant submissions add no detailed spectator/tree shadows.

## Reproduction and regression

- Before implementation, the native four-case regression reproduced **three
  failures**: actual168-seat grandstand at270m became0spectators; three visible
  native trees at210–240m became0on Low/Balanced. High's prior fog policy retained
  those trees. Receipt: `native-reproduced.json`.
- Final **41/41 PlayMode PASS**: eleven continuity/policy/budget cases plus the
  existing forest visibility, vegetation, crowd, environment, rock and course-
  dressing suites. Includes all tiers, full audience occupancy, rotated actual
  grandstands, behind-camera rejection, whole-object fog support, orthographic/
  camera-far-plane fallback, conservative canopy/wind bounds, invalid policy,
  original silhouette proportions/colours and exact6×triangle reduction.
- Additional **1/1 legacy audited-crowd PlayMode PASS**: original variant counts,
  LOD/density/culling/zero-allocation behavior retained. No restricted asset changed.
- **1/1 EditMode PASS**: environment ownership and global-state lifecycle.
- Warm crowd preparation: **zero managed allocation** across100calls; unchanged
  camera/time/quality reuses cached matrices; seating movement invalidates once.
  Existing forest zero-allocation/cache tests also pass. No claim that the complete
  game/native renderer has zero allocations.
- Windows Unity6000.6.0f1 build **Succeeded**,0errors,430631603bytes,26.583s.
  The520existing warning instances are24CS0618,460UnityAIInference ConvGeneric and
  36Sentis. Final actual Console audit/clear is recorded separately.

## Matched standalone resource measurements

The same final binary runs legacy vs complete visibility, at1920×1080, on an
RTX5080/Ryzen9950X3D using D3D11. The same30real athletes, accepted tick608,
camera/target/FOV, venue, graphics tier and clear weather are held. Two scene
views × three tiers ×240samples/phase = **2880samples**, after90warmup frames for
each case. Native counters and submitted instance/triangle/shadow counts are
retained. All12actual before/after screenshots are included; representative
stadium/forest views inspected. This is an offscreen scenery comparison, without
menu/HUD/swapchain or ongoing race-tick cost, **not minimum-spec FPS certification**.

See [measurements.md](measurements.md) and `comparison.json` for every case.
At the stadium on High, spectators rise **642→1548**, adding906far silhouettes;
trees stay1058. Total native triangle count rises4787771→4917929 (**+2.72%**),
versus5534009in the first816-triangle distant-crowd candidate. Optimizing the
newly visible people removes616080triangles/frame while preserving occupancy.
That intermediate candidate and its binary hash remain in raw artifacts; only
the final measurements here qualify delivery.

Median frame intervals in these isolated measurements:

- Stadium: High1.533→1.824ms; Balanced1.270→1.742ms; Low1.286→1.727ms.
- Forest bend: High1.609→1.482ms; Balanced1.364→1.469ms; Low1.377→1.387ms.

The increased Low/Balanced work restores their formerly absent audience/forest;
it is recorded rather than described as free. Whole-forest population is444in
the bend on all tiers, rather than136/83on Balanced/Low. Detailed near counts and
shadow source triangles do not increase in any matched case. Median recorded GC
allocation is34bytes/frame in both phases, arising outside the zero-allocation
preparation seam. Dedicated GPU/frame-timing data are unavailable in this driver/
player run and are reported as unavailable, not inferred from the frame interval.
The small High-bend improvement is measurement variation, not a claimed speedup.

## Delivery and authority

Compiled fullscreen startup, owner-profile preservation, source/core-binary guards
and append-only history are recorded alongside this report. Final startup passes
at2560×1440 FullScreenWindow, with real Sprint bib24, personal3–2–1 and wait skip;
all four loader stages paint before the race clock/countdown is released. No
owner-profile changes or captured C#/shader errors. The optional advanced
navigation probe is not enabled in that startup; its default receipt fields are
not new navigation evidence. Actual Console review retains the existing package/
obsolete-call warnings, then final counts are0errors/0warnings with Editor idle.
671of672task-entry protected core/resource files are unchanged; the one existing
JSON change is the optional visibility binding and every other environment member
matches. All five core DLLs are byte-identical to the prior MiniMap Review build.
Recheck with
`python scripts/quality/report_m164_visibility.py`.
There is no physics, route, AI, balance, shooting, scoring, replay or save change.
Next: owner tests the distant stadium/forest in the new build. M16.4 remains open
for visual acceptance; ordinary LOD detail changes and natural fog remain.
