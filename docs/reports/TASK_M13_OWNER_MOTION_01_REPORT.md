# TASK M13 owner motion01 — minimal natural torso motion

2026-10-05. PASS for this bounded presentation task; M13 and manual-steering integration remain OPEN.

The owner requested almost no sideways torso movement. The previous support-transfer curve rapidly crossed the grounded overlap; the first smooth replacement still had almost38cm lateral excursion in Sprint. The final continuous transfer uses the existing authoritative presentation resource with `hip_transfer_fraction=0.04` (previous committed value0.58). Leg lift/support, pole cadence, paired pushes, contact and root transforms retain their own values. No physics/physiology, shooting, difficulty or race-body tuning changed.

Tuck suppression of fatigue now follows the visible pose transition, including interruptions, rewind and long sampling gaps. It no longer instantly removes fatigue hip drop/torso lean when the selected state changes.

Actual loaded Hero rig/driver measurements at60/120/240Hz for8seconds per condition:

| State | Full lateral pelvis range |
|---|---:|
| Flat | 1.877cm |
| Climb | 1.685cm |
| Sprint | 2.920cm |
| Technical descent | 0.426cm |
| Tuck | 0 |

At240Hz Sprint peak lateral velocity is0.13757m/s, acceleration1.29679m/s² and jerk12.228m/s³. Full-fatigue tuck entry/exit at zero elapsed time has zero measured hip-position jump and zero reported chest-angle jump. These are actual rig samples, not copied curve calculations.

Validation:

- Native RED on the smooth but large presentation:3PASS/3FAIL, all60/120/240Hz minimal-transfer cases failed on Flat's24.398cm range.
- Native GREEN: continuity6, original ski technique6, locomotion2, exertion2 and ground contact3 = **19PASS/0FAIL/0SKIP**. Original support, pole rhythm, grip contact and no-allocation checks remain active. Discovery counts are not executed counts.
- Ordinary Woodland Mass30 GameView inspected at tick859, real field commands with no pose override. Saved capture `artifacts/owner-motion-review-20261004/captures/minimal-chase-20261005.png`, SHA256 `7452542ba87e5af235b8bd73c00c2eedcff3ae7e58a759278f1c694009147043`.
- Fresh normal production-host run at tick761: Console0errors/5warnings, compilation_failed=false. Old1691errors came from restoring a stale device command builder after an external test witness; later3errors came from an incorrectly named property in a temporary MCP evidence snippet. Both failed diagnostics/logs are retained outside Assets; no validation was weakened or production source workaround added. The five warnings remain recorded; no zero-warning native claim is made.

Full measurements and focused native results: [measurements](../quality/m13/minimal-motion-20261005/measurements.json), [native results](../quality/m13/minimal-motion-20261005/native-regressions.json). Runtime final Console evidence is kept with them.

Changed systems: AthleteLocomotionDriver, AthleteExertionPresentation, ski technique presentation JSON, new AthleteMotionContinuityTests and updated G2SkiTechniqueTests. Race results/determinism are unaffected by this presentation-only change. Hidden Windows FPS/GPU claims are unavailable and are not made.

Remaining work: integrate full manual heading into shared race physics, oriented-body constraints, AI/replay and Unity; improve Woodland course dressing/geometry and complete WP175–178. The [one-venue owner scope](../owner/OWNER_SINGLE_VENUE_SCOPE_2026-10-05.md) keeps other venue production deferred.
