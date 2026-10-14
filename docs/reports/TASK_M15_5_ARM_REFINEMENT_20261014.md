# M15.5 — Arm clearance and smoothness, 2026-10-14

The owner approves legs and torso, but the arms still rub the body and briefly
look as though they hammer the snow. The corrected arms leave visible space
at the elbows and pass smoothly through push and recovery. Approved legs and
torso keep their existing timing and transforms. Visual acceptance of arms
remains with the owner. VU-C/D/E/F remain approved; M16 has not started.

Source: `7d4ad434`, branch `codex/m15-5-visual-upgrade`.
Test launcher: `Builds/M15_5_Arm_Review/START_TEST.cmd`.
Paired native videos: `docs/quality/m15-5/arm-refinement/review.html`.
The previous `M15_5_Elbow_Review` binary/evidence is preserved as the baseline.

## Diagnosis and change

The complete moving-root pose → rifle → contact → grip chain reproduced
both observations. Old outward elbow displacement relative to the shoulder
was only18–28 mm in the three principal states. Sprint forearm world-angle
velocity reached1538.9 deg/s at120 Hz. These measurements complement the
visible defect; they are not universal anatomy or sport thresholds.

- `AthleteHybridLocomotionDriver`: only hand curves use cyclic monotone Hermite
  interpolation, which carries velocity through intermediate keys instead of
  repeatedly stopping and bursting. Added a powered hand outset and weighted
  signed hinge-plane splay. Existing ready/tuck/finish weights are retained.
- `AthleteLocomotionDriver.SolveHingedArm`: optional shoulder-to-wrist plane
  rotation opens the elbows. Lower arms still flex about negative local X;
  no added forearm yaw/roll or bone stretching. The zero default retains
  generic/legacy behavior, and the separate leg/rifle solve is unchanged.
- `ski_motion_v2` schema4: hand outset0.065 m, plane splay20 degrees, longer
  .12–.58 drive, shorter hand forward/drop travel and gentler upward recovery.
  Pole-anchor limit stays0.025 m. Pole-cycle frequency, every body/leg profile
  and the compression curve are unchanged. This softens the rapid portion
  of arm movement rather than slowing the whole athlete cycle.
- `ArmMotionRefinementTests` adds6 full-chain movement regressions and2 cyclic
  interpolation invariants. `ArmMotionReviewDiagnostics` and the separate
  build/report scripts are explicit QA only; no runtime per-frame logging.

## Verification

89/89 distinct selected native PlayMode cases PASS across19 classes,0 failures
or skips. Inventory400 is not a full-suite claim. Coverage includes anatomical
hinge/reach bounds, no shoulder spikes/wide flares, contacts, continuity,
G2 skin/joints, fallback, real field setup, and prone/standing shooting transfers.
The warmed hybrid loop still allocates0 bytes in its existing allocation test.

The original native red run is retained:2/6 PASS,4/6 FAIL, exposing the three
close-arm cases and sprint velocity. An initial candidate left one fast upward
recovery segment; that was tuned before the final green run. No failed test was
removed/disabled and the950 deg/s regression ceiling was not relaxed.

The SDK class filter failed to discover the new fixture, although Unity's native
test inventory contained all cases. Exact fully qualified leaf names executed
all8 new cases successfully; the18 existing class selections worked normally.
A resource-only experiment read the Editor's cached recipe; those stale results
are not used as final evidence. After an explicit cache invalidation/domain reload,
final native results and the independently built player use the current recipe.

Six matched3-second streams at120 Hz pass through the entire chain, with raw
float local body/leg positions/rotations hashed. All six before/after SHA256
digests match exactly for hips, spine, chest, neck and both leg chains. This is
a scoped sampled preservation check, not proof for every possible race state.

|Input|Minimum outward elbow displacement from shoulder, m|Peak forearm world-angle rate, deg/s|Body/leg stream|
|---|---|---|---|
|flat|0.018 → 0.106|595.0 → 354.2|identical|
|uphill|0.023 → 0.100|719.0 → 434.4|identical|
|sprint|0.028 → 0.114|1538.9 → 912.9|identical|
|steep|0.023 → 0.096|831.2 → 473.8|identical|
|corner|0.010 → 0.099|595.4 → 429.3|identical|
|fast|0.018 → 0.106|1370.6 → 816.7|identical|

Final largest grip residual across those observations is0.00823 m (steep climb).
The950 deg/s ceiling is an internal smoothing regression bound. Native paired
front/side footage covers flat, uphill, sprint and tuck,90 frames per case; all
original PNGs are retained without retouching. Same phases and approved art,
true moving roots, warmed URP and grade-matched lab planes. The11 packaged
videos include8 paired close clips and3 actual Mass30 clips. Field reaches
tick1000 without errors; captures inspected at start, turn and uphill.
Gallery comparisons are previous delivered motion vs this correction. The
build's `COMPARE_MOTION.cmd` remains the explicit older procedural fallback
comparison, which is a different baseline; the README makes this distinction.

## Performance and delivery

Sequential matched native Ultra1920×1080:30 athletes,2588 trees,120 warm-up
frames plus240 samples. Main CPU mean3.8079→3.8211 ms, p955.4522→5.5985 ms;
pose mean0.4458→0.4538 ms. Mean GC7585.575 B/frame is unchanged. Small sampled
CPU differences do not establish an FPS gain/loss; no material budget increase
was observed. GPU/render-thread timing is unavailable. The capture excludes
HUD/swapchain, so this is not a hardware-independent or complete-game FPS claim.

Win64 development build Unity6000.6.0f1 succeeds with0 errors/0 warnings.
Existing historical Sentis warnings are not claimed fixed by the incremental
build. Unity Console error query after the final tests/build is empty. Native
startup exits0 and reports2560×1440 FullScreenWindow after the windowed QA
runs; menu frame was inspected. Both EXE startup and generated launcher retain
fullscreen policy. No Inspector configuration is required.

No authoritative race, AI, physics, steering, energy, balance, shooting resolution,
events/replay, career/save schema, graphics quality/assets, UI or audio change.
No deterministic race output change is introduced; full race replay was not
rerun for this presentation-only correction. Source and evidence published on
the existing branch. Next action: owner playtest of arms, then M16 decision.

GDD and ADR-0069 govern the read-only presentation boundary. Research guidance:
[US Ski & Snowboard technique resources](https://www.usskiandsnowboard.org/news/training-resources-xc-skiers)
and [Frontiers cross-country skiing biomechanics study](https://www.frontiersin.org/journals/physiology/articles/10.3389/fphys.2021.741573/full).
Original authored procedural movement; no imported mocap, dataset derivatives
or assertion that the numerical thresholds are research-prescribed.
