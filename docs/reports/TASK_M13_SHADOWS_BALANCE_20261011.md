# M13 — restored shadows, keyboard corner cost and timing-query work

2026-10-11. Owner reports practically no remaining hitches in CornerAim build.
M13 remains OPEN for owner review; M14 not started.

High restores soft shadows: 4096, 4 cascades, 100m. Textures, render scale and
original body/equipment meshes remain. Native normal Game View was inspected:
player rig has 6 shadow-enabled renderers and a clear body/ski/pole silhouette.
Only the three requested High asset fields were committed over HEAD; unrelated
existing owner URP/quality/package edits remain unstaged and preserved.

Production racers have identical physical stats, form and equipment. The actual
30-athlete production field/device W/effort path reaches the first player mat at
tick 2707/place 1, reserve 0.927/fatigue 0.065, when all navigation is held to the
existing AI steering seam. This does not reproduce the owner's human steering
or prove a complete-race balance verdict. It rules out an inevitable held-W
deficit in that controlled scenario. No AI speed/power/stats/difficulty changes.

The real device-to-physical-turn fixture exposes an ergonomics problem: one
keyboard tap immediately requests maximum deflection. At 14m/s that requests 300N
corner braking before much rotation develops. `device_control_gameplay_v3`
increases keyboard steering by 2000 units/s: first 20Hz tick 100 units,
full 1000 after 0.5s. Release is zero; reversal starts toward its new sign. Short
corrections request 0N extra corner braking in the fixture. Shared forces and
full hard-turn costs remain. Analog gamepad and historical device data stay exact.
No course-centre following or automatic corner navigation is introduced.

The remaining code audit found repeated leader scans in timing queries used by
AI/HUD. Immutable checkpoint arrivals now update a per-ordinal leader once.
Both elapsed/global clocks, athlete-ID ties and later faster starters remain
covered. A warmed 30-athlete, 300000-query .NET probe falls 421.6912→47.5947ms,
checksum 1619.0476250048598 unchanged, zero managed allocation. This is query CPU
work, not an FPS measurement or evidence that every possible hitch is eliminated.
Earlier contact/ribbon/forest caches and source equipment batching remain.

Validation:

- Red keyboard-to-force fixture fails with tap 1000/braking 300N before the fix.
- .NET controls/physics/owner shooting: 57 PASS; final targeted controls, timing
  and perception: 63 PASS. Fixed-tick sampling/rejected-command atomicity,
  release/reversal/gamepad/replay serialization/zero allocation covered.
- Equal-navigation W diagnostic: 1 PASS; timing CPU before/after: 1 PASS each.
- Signed tape: 12 PASS, including full MassStart/Sprint event/physical-state replay.
  One unchanged Pursuit construction failure occurs before movement: directed
  passages reject the factory's startContinuation composition. It is recorded
  in signed-replay-final.trx; no test was disabled or relaxed.
- Native EditMode device: 16 PASS, timing: 7 PASS; PlayMode quality: 3 PASS.
- Native compile settled; post-runtime Console Error/Exception queries empty.
  Third-party Unity AI subscription messages were Log-level and not game faults.
- Windows build: Succeeded, 0 errors/warnings, 355756440 bytes, High default, max 30.
- Standalone hidden Mass30/Sprint30 startup, 400 paid ticks, controls and real scene
  transition: PASS, exit 0. Rendering suspended, so no standalone FPS claim.

Owner build: `Builds/WoodlandM13ShadowsBalance-20261011/START_WOODLAND_REVIEW.cmd`.
Russian controls and suggested checks are in READ_ME_RU.txt. Previous builds kept.
Next owner check: gentle A/D taps versus held turns, first-range Mass Start pace,
restored athlete shadows and active shooting. High shadows require more GPU work
than the temporary reduced profile; sustained full-race FPS was not measured.

Decision: ADR-0050. Evidence: docs/quality/m13/shadows-balance-20261011/.
PROJECT_STATE remains append-only with its complete Foundation onward prefix.
