# M13 owner motion / reference review — 2026-10-04

Status: measured diagnosis; fixes and manual steering acceptance OPEN.

## BIATHLON measurements

Actual loaded Hero rig and `AthleteLocomotionDriver.Sample`, sampled at60/120/240Hz for8seconds per condition, establish a fast reproducible native loop. `artifacts/owner-motion-review-20261004/unity-motion-baseline.json` retains the original measurements. At240Hz Sprint pelvis lateral velocity reached4.125m/s, acceleration155.8m/s² and jerk20,378m/s³. Full-fatigue tuck entry/exit at zero elapsed time changed hip position by approximately5cm and torso rotation by10.7°. These are presentation defects; authoritative effort, fatigue and shooting need no retuning.

A separate probe compiled and called the actual Domain visual-route API. Woodland main has11.25° tangent steps; current draft Pine main has24.665°. Applying the production lateral normal at1m offset produces approximately19.6cm/42.7cm position discontinuities around those knots. The current proxy directly assigns route heading and the chase camera directly aims using that heading. Rotation smoothing alone cannot satisfy manual steering or remove the lateral-normal position discontinuity.

The new actual-rig continuity fixture ran before implementation: **0passed / 5failed**, including all three sampling frequencies and both zero-time tuck transitions. `native-red.json` retains the failures. Unity discovery count249 is not an executed-test count. Existing support/rhythm/contact tests remain required.

## Read-only Biathlon 2006 reference

Inspected local `Data/ai/atlanims.txt` and archive entry names in `Data/pak01.zip`. The animation-selection table distinguishes fast/slow skating, separate two-skate variants, skating without poles, parallel-ski/braking states and shooting transitions. Proprietary binary clips were not decoded or imported; names alone do not establish their joint curves or control law.

Decoded and viewed the local9.91second `Data/video/skiing.avi`: it contains real skier footage used by the game, rather than captured gameplay. Observations include coordinated pole push and weight transfer, followed by compact downhill glide. The analysis decoder and frames remain outsideAssets/Git.

To inspect actual presentation, viewed this original [Biathlon 2006 gameplay recording](https://www.youtube.com/watch?v=81nEbden3D4) through a muted background browser, without downloading its media. Inspected approximately1:12,1:40,2:48 and4:03. The visible course combines forest banks/rocks, safety nets, boundary markers, spectators and banners at selected corners; the low downhill posture is distinct from active skating. These sampled views support posture and scenery comparisons, not measured cadence, frame-perfect joint curves, collision correctness or proof of manual keyboard control. The player's key presses are not visible. The reference is not evidence that BIATHLON already implements manual direction.

## Bounded implementation scope

Correct pelvis transfer and blend exertion suppression during tuck using presentation data. Preserve the existing readable ski support and paired pole rhythm; run the new failing fixture and relevant original animation tests, repeat the same native measurement and inspect ordinary chase/downhill gameplay.

Implement owner-approved manual steering separately in pure simulation with a versioned command/context and shared player/AI motion, actual body heading, speed/distance projection and conservative collision/edge constraints. Publish observed heading to the visual layer. Do not rename the existing absolute lateral command or turn only a Transform and call that manual steering.

Course and dressing changes use the existing content pipeline. Dense curve authorship alone can remove an internal geometric heading discontinuity, but it is not manual control. Real completed Sprint/Pursuit/Mass flows and replay still govern venue acceptance. This report does not close WP170 or M13.
