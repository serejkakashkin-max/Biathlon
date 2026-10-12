# M14 stadium timing portal correction — 2026-10-12

The owner's screenshot shows the generated `0.94 KM` portal at main 935 m,
25.005 m before the retained stadium `TIMING` gantry. The retained gantry is
authored at main 0 / the closed loop end 960.005 m. This produced two neighboring
structures even though the second one had no authoritative split.

`range_exit_split` keeps its stable ID and now sits at the loop end 960.005 m.
This remains after the penalty rejoin at 910.005 m. The timing presentation reads
`reused_stadium_checkpoint_id` from its resource and adds only a snow stripe at
this station, reusing the native frame and label. The other two portals remain.
Initialization rejects a reused checkpoint that is not the matching physical
closed-loop end. No new colliders, per-frame work or reduced graphics quality.

Changed files:

- `Assets/Resources/Biathlon/Venues/woodland_basin_v1/track.json`
- `Assets/Resources/Biathlon/UI/course_timing_v1.json`
- `Assets/Scripts/Unity/Presentation/CourseTimingPresentation.cs`
- `Assets/Tests/EditMode/CheckpointTimingTests.cs`
- `Assets/Tests/PlayMode/CourseTimingPresentationTests.cs`

Verification:

- `gate-red.trx`: new production regression failed against the previous 935 m
  anchor, reproducing the duplicate station.
- `gate-green.trx`: 7/7 checkpoint timing tests passed. All three actual production
  formats bind the new stripe to the existing gantry, omit the old 935 m split,
  and do not manufacture a time-zero arrival. Existing event and deterministic
  zero-allocation timing checks pass.
- Native geometry tests are prepared for root execution: one retained native
  gantry, no added frame/header, stripe centered on all original mesh placements.
- `git diff --check` passed for these five files.

Impact: checkpoint observations and race context hashes change deliberately;
course length, athlete motion, power, standings clock policy and IBU-required
post-penalty timing semantics remain unchanged. M14 acceptance is still pending.
