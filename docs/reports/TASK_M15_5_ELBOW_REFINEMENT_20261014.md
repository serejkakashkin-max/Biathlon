# M15.5 — Elbow anatomy correction, 2026-10-14

The owner rejected visibly bent-out elbows in the previous animation refinement.
This correction removes the changing hinge plane and the forearm's independent
shortest-arc rotations. The side-by-side native frames visibly show the change:
elbows below the shoulders, forearms flexing forward from the down bind pose,
and no sideways elbow horns during pole recovery.

Source: b15c8c08, branch `codex/m15-5-visual-upgrade`.
Test launcher: `Builds/M15_5_Elbow_Review/START_TEST.cmd`.
Gallery: `docs/quality/m15-5/elbow-refinement/review.html`.
VU-B remains awaiting owner visual acceptance. VU-C/D/E/F remain approved;
M16 has not started.

## Diagnosis and changes

The complete pose → rifle → ground contact → grip chain reproduced the defect.
Old flat-cycle samples put elbows up to 0.129 m above the shoulders and rotated
the forearm about multiple axes; the late grip solve repeated that twist.
The first provisional regression used the wrong sign for elbow flexion. Checking
the actual G2 bind axes (-Y along each arm, +X across the elbow) established that
negative X flex brings the forearm forward. Corrected that assertion; retained
the shoulder-height and flare checks. No failing test was disabled or removed.

- `AthleteLocomotionDriver.SolveHingedArm` constructs the shoulder frame and
  flexes the lower arm about local X, preserving both bone lengths and root pose.
  The existing generic solve remains unchanged for legs, rifle fitting and legacy motion.
- `AthleteHybridLocomotionDriver` places moving hands relative to the shoulder
  instead of allowing root-relative hand height and flipping bend hints to lift elbows.
- `AthleteGripPresentation` uses the same hinge for the late hybrid pole fit,
  so contact correction cannot reintroduce elbow yaw/roll.
- `ski_motion_v2.json` is now schema3 (stable resource ID retained): flex limits
  20–140 degrees, explicit plant/release hand paths, a longer .18–.55 drive interval
  and a 0.025 m authored pole-anchor correction ceiling. Legacy contact tuning is preserved.
  This avoids a rapid elbow snap at lease release and softens the previously very short sprint push.
- Development-only front/side close-capture and separate build methods retain
  the complete native chain. The fixture snow plane follows the observed grade;
  URP targets warm before retaining frame zero. Normal gameplay creates none of this QA setup.

## Verification

81/81 distinct selected native PlayMode tests PASS across18 classes; no failures
or skips. Whole-assembly inventory386 is not a full-suite rerun claim. Coverage
includes7 full-cycle anatomical cases (flat, sprint, uphill, steep uphill, downhill
poling and both tight turns), hinge reach/length/root invariants, stable profiles,
zero warm-loop allocations, contacts, the G2 mesh/joint correctives and both
legacy/authored prone and standing transfers. The final hybrid23 checks were
repeated after restoring baseline-capture source changes and passed again;
repeated checks are not added to the81 total.

An intermediate continuity run failed3 cases, retained in `continuity-red.xml`.
The failures exposed an overly large anchor-release correction and excessively
short sprint drive. They were corrected in data/contact policy and rerun green.
One test-tool call was rejected while the preceding run was still releasing its
request; the delayed retry passed. The transient baseline-source restore hit a
file lock; a byte-for-byte retry succeeded, the editor recompiled and the final
candidate was rebuilt. Neither tool error is counted as a successful check.
Unity Console has no new errors after those tool retries; no compile errors remain.

Native numerical probe:3 moving-root cases ×360 frames ×2 arms at120 Hz.
Maximum observed elbow height relative to shoulder -0.06246 m, lateral flare
0.03691 m, lower-arm deviation from its anatomical hinge0 degrees at recorder
precision, consecutive forearm world-angle change12.824 degrees, grip residual
0.01395 m. These are sampled presentation bounds, not a universal anatomy claim.

Native review contains8 paired close clips (flat/uphill/max sprint/tuck ×front/side)
plus3 actual production Mass30 clips (start/turn/uphill), with untouched PNG
sequence hashes. The actual field reaches tick1000 with no errors. The baseline
uses the rejected revision24d27dd8 motion source, identical approved art and the
same updated QA camera/plane; the added unused hinge helper does not change old
motion execution. Previous delivered builds and the original captures remain preserved.

## Budget and scope

Sequential matched Ultra native1920×1080 captures:30 athletes,2588 trees,
120 warm-up +240 measured frames each; excludes HUD/swapchain. Main CPU mean
3.6729→3.6273 ms, p955.3471→5.3767 ms; pose mean0.4512→0.4429 ms. Mean managed
allocation7585.43 B/frame is unchanged (not a whole-frame zero-GC claim).
Small differences are measurement variation; no material CPU cost increase was
observed. GPU/render-thread timings are unavailable, not zero. No general FPS
or hardware-independent performance claim.

Final Win64 development build Unity6000.6.0f1:0 errors/0 warnings,382475054 bytes.
Standalone UI/race startup review also passes:12 captures,14 navigation actions,
actual venue loaded and started, owner career hashes preserved.
Earlier historical Sentis shader-variant warnings have not been independently
resolved by this incremental build.

No simulation, AI, steering, stamina, shooting resolution, race event/replay,
career/save schema, graphics assets/quality, weather, UI or audio tuning changes.
No deterministic race output change is introduced; a full race replay was not
rerun for this presentation-only correction. Original authored procedural motion,
no imported mocap or licensed footage derivatives. The next action is owner
playtest/visual acceptance of the elbows, then an explicit M16 start decision.

GDD section29.2 and ADR-0069 govern the read-only presentation boundary.
Frame construction follows the official Unity [Quaternion.LookRotation API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Quaternion.LookRotation.html).
