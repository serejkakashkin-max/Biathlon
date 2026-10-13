# M15.5 — motion provenance and review

All new skiing motion in this build is original manual curve data with procedural
adaptation. No motion capture, NSD pose reconstruction or Rokoko export was
obtained or imported. The retained G2 rig, joint correction, grip and ground
contact adapters remain in use.

| Observed state | Authored / adapted motion | Technique |
|---|---|---|
| Flat | Closed skate/lift/poling curves, paired poling every second skate | Gear4 / V2 alternate |
| Uphill | Shorter stride, more compression, staggered uphill hand reach | Gear2 / V1 |
| Steep uphill | Shorter, faster cycle with greater compression | Adapted Gear2 / V1 |
| Fast skate | Paired poling on each skate, longer stride | Gear3 / V2 |
| Max sprint | Paired poling on each skate, stronger arm envelope and cadence | Adapted Gear3 / V2 |
| No pole | Existing skate envelope, no pole cycle | Procedural adaptation |
| Double pole | Paired arm motion, narrow skate envelope | Procedural adaptation |
| Tuck | Retained hip hinge, compact hands, trailing poles and smooth transition | Existing procedural layer |
| Left/right corner | Retained observed curvature layer with bounded ski edge correction | Procedural adaptation |
| Fatigued | Authored cycle plus existing fatigue/effort envelope | Mixed manual/procedural |
| Slow/stop | Shortened cycle, no cadence progression while stopped | Procedural adaptation |

Terms are verified against primary technique literature linked in
[the asset audit](M15_5_ASSET_AND_MOTION_AUDIT.md). They describe timing choices;
they do not establish professional mocap fidelity. Final artistic acceptance is
the owner's playtest.

The runtime path is simulation observation → detached animation frame → hybrid
state selection → continuous phase/profile blend → bounded analytic limb pose →
existing ground/pole correction → ski edge correction → typed grip binding.
It never applies root motion or feeds pose into race physics or shot resolution.
The original path remains selectable in `SkiMotionComparison.unity` and in
`COMPARE_MOTION.cmd`. Screen left is original; screen right is the new motion.
Both rigs receive identical speed, grade, effort and time. This lab compares
motion using current materials; the production gallery compares old and new art.

## Optional owner-supplied motion source

This is optional and does not block the delivered original motion. If later
using Rokoko Create, sign in at https://create.rokoko.com/ and use only owned or
explicitly licensed footage permitting commercial animation derivatives.
Choose a fixed front/side camera, full body and equipment in frame, clear feet,
no edits/occlusion, two or more complete stable cycles. Keep clips within the
account's free processing allowance; do not purchase automatically.

Export FBX with its skeleton and animation, preserving the export's actual
sample rate and units. Do not claim a specific unavailable exporter option was
used. Save the original FBX at
`artifacts/m15-5-owner-motion/inbox/<technique>.fbx`, and put permission/source
URL, date, export settings and sample rate in `<technique>.license.json`.
Keep source files out of public Assets until redistribution rights are confirmed.
Retarget in Blender to the existing 17-bone rig, bake skeletal keys, remove root
translation, trim complete cycles and retain ski/pole grip correction. Candidate
Unity import belongs under `Assets/Art/Animation/M15_5/OwnerLicensed/` only after
the rights and mapping review. Set clip looping only after checking the seam;
do not enable animation root motion. Validate contact, transition/rewind,
equipment and zero authority changes in the comparison scene and existing tests
before switching the production driver.
