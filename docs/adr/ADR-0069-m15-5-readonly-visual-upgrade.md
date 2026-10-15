# ADR-0069 — M15.5 read-only visual upgrade

2026-10-13. Implemented for owner review between M15 and M16.

## Context and decision

The owner requests stronger skiing animation and winter art without changing
the accepted mechanics, M14 interface or M15 audio. Keep G2 Hero and its typed
17-bone/prop contract. The new `AthleteHybridLocomotionDriver` consumes the same
detached animation frame as the original driver. Retain the original as an
explicit fallback and side-by-side review path.

Use original normalized manual curve keys, smooth numeric profile transitions,
speed-derived cadence and bounded limb IK. Terrain chooses V1 uphill, V2 alternate
on the flat and V2 during fast effort; no source motion dataset or mocap is imported.
Twelve presentation states share existing exertion, ground-contact and grip adapters.
Ski edge correction runs once after ground alignment. The adapter protects model
and race roots. No pose result feeds simulation, steering, stamina or shooting.

Keep the four existing joint-volume shapes as the first channels. Append three
small original cosmetic face/shoulder shapes, chosen once from the stable athlete
ID. National kit, bib and purchased ski bindings remain independent.

Improve the existing snow shaders and bounded tracks/spray. New original log,
stump and drift geometry reuses audited fir bark, shrub and rock assets. A small
versioned cluster recipe projects onto the existing terrain, checks clearance
against every course corridor and uses explicit-camera instancing/culling/LOD.
It adds no gameplay collider or terrain/path edits.

`WeatherPresentation` remains the sole scene lighting/sky/fog owner. The original
three physical career weather IDs are retained. Presentation adds light snowfall
and winter haze: existing wet overcast at/above the visual temperature threshold
uses haze; snowfall density crossfades over a data-driven 120-second visual cycle.
The first 45 seconds retain the base look; the light interval fades in/out over
eight seconds. Physical weather, wind, snow friction and saves/replays are unchanged.
High forest visibility reserves the maximum fog reach of both snowfall looks.

Legacy quality IDs remain `low/balanced/high` for saved compatibility, displayed
as Performance/High/Ultra. All retain the same hero and texture assets. Existing
shadow/forest profiles and bounded spray budgets distinguish the three levels.
Graphics preference is local presentation data, not career state.

## Validation and limits

The work has native zero-allocation/root/contact tests and a comparison scene.
Production native before/after capture uses identical observation inputs, cameras,
FOV and base light preset. Gallery-only manual skin matrix recalculation avoids
stale meshes during repeated offscreen rendering; it is not enabled on every
gameplay athlete. CPU/render counters are measured separately from screenshots.
Unavailable GPU/VRAM timings remain unavailable, not zero. Full evidence and
owner acceptance are tracked in `docs/M15_5_VISUAL_WP_STATUS.md`.


## 2026-10-14 — Anatomical pole-arm hinge follow-up

Owner rejected the forearm twist/raised elbow in the refined animation. Use a
single negative-local-X elbow hinge for the -Y arm bind axes, with a stable
shoulder frame. The late hybrid grip fit must use the same solve. Shoulder-relative
hand paths, flex/reach bounds, longer pole drive and smaller authored anchor
correction are defined once in schema3 `ski_motion_v2`. Generic leg/legacy/rifle
solves and all authoritative systems remain unchanged. Native close captures
and numerical tests supplement, but do not replace, owner visual acceptance.
See TASK_M15_5_ELBOW_REFINEMENT_20261014 and the elbow-refinement gallery.


## 2026-10-14 — Arm clearance and velocity follow-up

The owner approves legs/torso but observes close-fitting arms and a hammer-like
stroke. Schema4 adds a 0.065 m powered hand outset and a signed 20-degree
shoulder-to-wrist hinge-plane splay. Splay fades with existing arm strength;
the forearm remains on the same negative-local-X hinge, with no added local
yaw/roll. Generic leg/rifle/legacy solves retain their previous defaults.

Only hand curves use cyclic monotone cubic interpolation. Extend drive from
.18–.55 to .12–.58, shorten forward/drop travel and spread upward recovery
across the return. The existing pole-cycle frequency, compression curve,
leg paths, body transforms and locomotion choices are retained. Thus the
change softens rapid portions of the arm cycle rather than slowing the
whole athlete animation. All tuning stays in the one motion resource.

Native paired close captures, actual Mass30 footage, anatomical/contact/
continuity tests and matched CPU profiling govern technical acceptance.
The review probe compares SHA256 of body/leg transforms over six matched
input streams; this is a scoped sampled preservation check. The 950 deg/s
regression ceiling is a project smoothing bound, not an IBU or anatomy rule.
See TASK_M15_5_ARM_REFINEMENT_20261014 and the arm-refinement gallery.
Owner arm acceptance remains pending; approved visual areas and M16 stay unchanged.


## 2026-10-14 — Sprint frequency and flat rifle carry follow-up

Owner still sees rapid hammering during attack and an edge-on carried rifle.
Smoothing the individual hand paths did not reduce the complete stroke count:
9/12/15 m/s sprint observations remained2.25 Hz. Schema5 adds a separate
per-profile visual pole-rate scale, .72 for MaxSprint and .68 for FastSkate,
1 elsewhere. Leg phase/cadence calculation and every pose amplitude remain
unchanged. Arm, contact, recovery and small torso compression share the slower
pole phase, preserving their coordination. Expose PoleCadenceHz for review.
This changes compression timing in the two fast profiles; it does not retain
the previous follow-up's exact body-transform hashes for those profiles.

Rifle presentation schema2 rolls only the carried frame90 degrees around the
local muzzle axis and offsets it20 mm rearward in the mounting parent frame.
The mesh is thin on local X, broad in Y/Z, muzzle along +Z. The broad face
therefore lies along the back, while the barrel stays upward. The shooting
socket, sight orientation, hand grips and authoritative shooting are unchanged;
existing transfer interpolation connects the corrected carry frame to ready.
Stable resource IDs are retained; these definitions are not career save schemas.

The broad-side placement follows the actual Larsen harness product photograph.
IBU September2026 ECR7.1.2 confirms muzzle-up harness carry; it does not specify
our model's numeric roll/offset or the chosen animation frequency. Reference
links and native before/after evidence are in the sprint/carry follow-up report.
No simulation/AI/physics/energy/balance/art-quality/UI/audio change; M16 remains
pending owner acceptance.

## 2026-10-15 — Correct contact with the deforming back

The owner rejected the schema2 carry placement: the broad side was correctly
oriented, but the stock visibly hovered in tuck. A test against the actual baked
G2 skin reproduced 45–72 mm gaps at two stock contact points, across ordinary
skiing, attack and tuck. Orientation checks alone were insufficient.

Schema3 replaces the parent-space offset with two authored attachment points,
each a weighted combination of typed skeleton bone transforms. Their local
positions/weights follow the existing hero back skin near the lower and upper
stock contact. Build the carried orientation from these two points and retain
the 90-degree muzzle-axis roll; place the stock's broad-face contact on the lower
anchor. No per-frame mesh baking, raycasts, allocations or new gameplay state.
The same existing skeleton contract supports the legacy presentation fallback.

Actual skin baking/triangle intersection is test-only. All eight sampled contact
gaps per motion case must stay below18 mm; this is a project regression bound,
not an IBU rule. Native side/back footage, the existing transfer/aim/contact
tests and owner review remain necessary. The slower schema5 pole frequency is
retained. Shooting socket, sight, authoritative timing, art/quality and all race
systems remain unchanged. The earlier schema2 paragraph records the rejected
intermediate state, not final acceptance. M16 remains pending.

## 2026-10-15 — Stable face and skin diversity after motion approval

The owner approved the refined movement and requests different athlete faces,
including light and dark skin. Retain the approved G2 CC0 base, skeleton,
equipment fit, animation, body/joint shapes and national kit. The local Blender
and existing authoring pipeline are available; the retired restricted015 head
source remains archived. Do not replace the approved hero with that old head.

One versioned face recipe defines8 skin colors and6 original bounded face
profiles. Resolve independent face/tone channels from the stable athlete ID
using fixed FNV-1a/avalanche hashes, without simulation RNG or nationality/bib
inputs. Retain the old3-build hash choice exactly. Same-ID reloads and race
recreation give the same face within this recipe version; saves need no new
schema or Unity references. A future recipe reassignment must be an explicit
visual version change, not an accidental palette reorder.

Append6 face-only morphs to each of the3 existing LOD meshes. Keep the first4
joint corrective and next3 cosmetic build channels in their original order.
Authoring touches only skin vertices in the face envelope, preserves base
vertices/indices/weights/bindposes and all existing morph frames, and stores
normal differences against identically recalculated reference normals so the
original artist smoothing is retained. Regeneration is idempotent. Normal
gameplay selects one face morph once; no procedural mesh editing or head-bone
updates run per frame. Same ordered channels retain identity through LOD swaps.

Separate8 shared skin materials from the national cloth palette. One reused
material-slot array per renderer combines those bounded caches; bib property
blocks/leader colors stay independent. Repeated identity/nation application
performs no managed allocations. Native portraits use the neutral pre-existing
build, identical lighting and actual player rendering. No generated photographs
or additional restricted assets are imported. Validation includes mesh/normal/
LOD invariants, actual Mass30, standing/prone sight and the approved motion
transform stream. These are cosmetic changes only; M16 is not started.

Native portrait inspection subsequently exposed an actual centre-line split/
overlap in the narrow face. Both the new jaw/cheek morph and the older3-build
face-width term used Sign(x), discontinuous at mirrored near-coincident vertices.
Use one smooth sinusoidal side factor tending to zero at the midline. Apply it
to face width in both authoring paths; keep shoulder/build amplitudes outside the
face and all joint channels unchanged. A real-mesh centre-line regression goes
3 FAIL→3 PASS across all LODs, including the legacy face channels. Existing
source vertices/topology/bones remain intact. This supersedes the earlier blanket
claim that every pre-existing face delta remains exact: only face portions are
deliberately corrected; the approved body/leg motion streams stay identical.
