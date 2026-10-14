# M15.5 — Athlete animation refinement, 2026-10-14

The owner approved all M15.5 work except athlete animation. VU-B was reopened for
unrealistic arm rowing, sideways waiting-start poles and missing pole use on turns,
lane changes and climbs. This follow-up changes only presentation motion and its
validation/build tooling. VU-C/D/E/F approval is retained; M16 has not begun.

Five native regression cases first reproduced the reported symptoms. Waiting and
zero-pole profiles were incorrectly routed to the aerodynamic tuck pole geometry.
Corner selection overrode uphill propulsion and used a zero-pole profile. Lateral
steps attenuated arm swing even when moving forward. The hand curve had very little
vertical movement, while contact timing used an unrelated half-cycle rule.

The schema2 motion recipe now authors forward/vertical hand paths, compression and
recovery clearance with one shared plant/drive/recovery phase. The waiting pose
braces the poles in the snow. Powered corners retain paired poling; curved uphill
sections retain their uphill technique. Forward lane changes keep poling while
their ski step still follows the existing lateral envelope. Elbows lead recovery
and move behind the torso at release. Cadence is bounded to2.25 pole cycles/s.
Stable athlete IDs provide individual, repeatable visual phases without RNG.

The first actual-field footage exposed another issue on tight turns: hand targets
stayed centred on the route root as the chest leaned sideways. Two additional red
cases measured only0.08792m inside-glove clearance. Targets now follow the chest's
lateral displacement and elbow hints follow the lean. Both turn directions retain
their own-side grip clearance through the complete moving Hero/contact/grip chain.

Changed systems/files:

- `AthleteHybridLocomotionDriver`, `ski_motion_v2.json`: selection, authored paths,
  ready pose, pole phase/cadence, individual phase and leaning-torso arm targets.
- `AthleteLocomotionDriver`: optional forward-poling preservation for the hybrid
  adapter; the explicit legacy fallback keeps its original output.
- `AthleteGroundContact`: authored contact/recovery timing; legacy direct sampling
  retains its previous behavior. Root placement, ski support and limb lengths remain protected.
- `HybridSkiMotionTests`, `AthleteProxyTests`, `G2MotionTests`: reproduction and
  regression coverage, actual Mass30 ready pose, independent phases, authority
  protection, and both stance transfers through legacy and authored motion.
- `PoleMotionReview`, `SkiMotionComparison`, `PlayerBuildVisualUpgrade` and
  `animation_refinement_review_report.py`: explicit development-only footage,
  comparison cases, packaging and a separate test-build directory.

Native PlayMode validation passed65/65 selected tests across16 relevant classes,
with0 failures/skips. The total assembly inventory is378; this follow-up does not
claim a new full378-test run. The prior complete365/365 result remains historical.
The focused run covers zero-time interrupted transitions, rewinds, warm-loop zero
allocations, root/authority protection, support/length/finite bounds, pole/grip
contacts, locomotion/exertion/corners, distant detail, prone/standing alignment,
rifle carry/transfer, and the actual production30-athlete field. Initial failures
are retained in `red-summary.json` and `corner-red-summary.json`; no test was disabled.
Unity Console has no new Error entries after the final validation.

The latest Win64 development build succeeded with0 errors and0 warnings in the
cached incremental build. This does not imply the previously recorded Sentis
variant warnings in the earlier full visual build were fixed. Build launcher:
`Builds/M15_5_Animation_Review/START_TEST.cmd`; existing career saves are supported.
The previous `M15_5_Visual_Review` build remains available as the rejected-animation
baseline and has not been overwritten.

Actual native Mass30 footage covers the waiting start, acceleration, first turn
and an uphill through tick1000. Waiting hand height was1.21996m and pole downward
projection0.87059; the ready state wasSlowStop. The captured uphill grade was0.10050.
Three5-second actual-field clips and seven3-second isolated comparison clips are
packaged in `docs/quality/m15-5/animation-refinement/review.html`. Isolated videos
compare the legacy pre-M15.5 fallback with the refined motion, not the rejected
M15.5 motion. All stills are unretouched Unity output; videos add only labels.

Sequential profiling used the same approved art, Ultra profile,30 athletes,
2588 trees and1920×1080 offscreen URP camera, with120 warmup and240 measured frames:

| Metric | Rejected M15.5 motion | Refined motion |
|---|---:|---:|
|CPU main mean|3.6458ms|3.6687ms|
|CPU main p95|5.3600ms|5.3347ms|
|Athlete pose mean|0.4256ms|0.4430ms|
|GC allocation/frame mean|7585.43B|7585.43B|

The small CPU difference is within ordinary measurement variation; no meaningful
regression was observed. This capture excludes HUD/swapchain cost and cannot
guarantee all gameplay frame times. GPU timing remains unavailable. No graphics
asset, texture quality, weather preset or authoritative race/balance source was
changed. Read-only proxy tests also verify rendering does not advance ticks or
distance. Deterministic race rules, shooting geometry, saves and replay authority
retain their approved implementations.

Motion remains original authored/procedural IK, not motion capture. Technique
research references remain [US Ski & Snowboard training resources](https://www.usskiandsnowboard.org/news/training-resources-xc-skiers)
and the previously audited skiing-technique papers; no research dataset or footage
was imported as a commercial asset. Artistic acceptance belongs to the owner.
Next action is testing this animation build; VU-B/M15.5 and M16 wait for that review.

Final compatibility guard: the smooth exhausted-finish envelope fades the new
ready hand/elbow weight so stopped finishers do not raise their hands back into
the start pose. A red wrist-height regression (1.24554m) is retained in
`finish-ready-red-summary.json`; the final hybrid15/15 run passes.
