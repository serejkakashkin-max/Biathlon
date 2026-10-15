# M15.5 — Attack rhythm and rifle contact, 2026-10-15

The owner observes rapid arm/pole hammering during attack and asks for the
rifle's broad stock face to rest against the back. The first carry correction
fixed orientation but left the weapon hovering in tuck; the owner rejected
that result. Final source221c4ccd fixes actual contact with the deforming back
and retains the slower arm rhythm from e5b2db84. Orientation checks alone were
insufficient, so the regression now measures the actual hero body surface.

Branch: `codex/m15-5-visual-upgrade`.
Test launcher: `Builds/M15_5_Rifle_Contact_Review/START_TEST.cmd`.
Gallery: `docs/quality/m15-5/sprint-carry-refinement/review.html`.
VU-B awaits owner visual acceptance. Other approved WPs remain approved.
M16 has not started. Both prior test builds are preserved as reference.

## Attack rhythm

Earlier hand-curve smoothing reduced peak arm speed within a stroke, but left
complete attack strokes at2.25 Hz. Regression observes the actual phase stream
for10 seconds at120 Hz at9/12/15 m/s. Initial6-case run:1 PASS/5 FAIL, including
3 frequency cases and2 carry orientation cases. Changing cadence alone yielded
4 PASS/2 FAIL; orientation still failed. Both corrections then passed. No failed
test was deleted, disabled or weakened. Attack/ordinary-rate ordering and
same-time Shift switching have additional regression coverage.

`ski_motion_v2` schema5 has one per-profile `pole_cadence_scale`: .72 MaxSprint,
.68 FastSkate,1 elsewhere. Existing leg phase/cadence calculations, hand paths,
anatomical hinge, pose amplitudes and root movement remain. Arms, pole contact,
recovery and small torso compression share the slower pole phase. Therefore
exact torso compression timing in the two fast profiles changes to coordinate
with the slower push; the preceding follow-up's exact fast-profile body hashes
are not claimed. PoleCadenceHz is exposed for review without per-frame logging.

Sampled sprint frequency2.25→1.620 Hz (−28%); peak forearm rate912.9→658.3 deg/s.
Fast skate816.7→555.9 deg/s. Four ordinary flat/climb/steep/corner body streams
retain their earlier hashes. These are project presentation choices and scoped
observations, not IBU frequency or anatomy standards.

## Rifle contact

The mesh is thin on local X, broad in Y/Z and points its muzzle along +Z.
Schema2's90-degree muzzle-axis carry roll correctly oriented that broad face,
but its constant mounting offset did not follow the actual skin deformation.
The new regression bakes the real G2 skinned mesh after posing and intersects
triangles from two points on the stock face. Old placement reproduced45–72 mm
gaps across ordinary skiing, attack and tuck. All3 motion cases failed.

`athlete_rifle_v1` schema3 replaces that offset with two data-authored weighted
anchors. Their typed bone transforms follow the existing hero back skin near
the lower and upper stock. The carry frame follows the line between those
anchors; its broad-face contact rests on the lower anchor. Seven weighted
TransformPoint operations per sample replace the constant offset. Actual mesh
baking and triangle queries run only in tests, never in gameplay. Stable IDs
and the shared rig contract are retained; these are not career save schemas.

All8 sampled gaps per motion case pass the unchanged18 mm regression ceiling
after the fix. The shooting socket, sight orientation, hand grips and existing
transfer interpolation remain intact. Existing transfer tests exercise return
to carry, continuity, stance geometry and the authoritative phase deadline.
Native side/back frames also show the corrected placement in tuck and attack.
Numeric bounds supplement visual inspection and owner acceptance.

Visual reference: [Larsen's harness photograph](https://www.skidskytteexperten.se/en/varumarken/larsen-biathlon/larsen-biathlon-barsele)
shows the broad stock side against the harness/back. The image was viewed,
not imported into game assets. [IBU September2026 ECR7.1.2](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf)
requires back/harness carry with the barrel up; it does not prescribe this
model's numeric roll, contact tolerance or animation cadence. The
[ANSCHÜTZ harness description](https://shop.anschuetz-sport.com/Tragegestell-Biathlon-Feedback-Version-4.0/015390)
identifies side-rail/rear-stock attachments. Quaternion composition follows
the [official Unity manual](https://docs.unity3d.com/Manual/class-Quaternion.html).

## Native review and scope

Final candidate:12 close sequences,90 frames each: front/side/back ×flat,
uphill, attack and tuck. Eight paired clips compare identical inputs, cameras
and times with the previously delivered arm-review binary. Three further
paired side clips compare the rejected hovering carry directly with the final
contact correction. Four clips show the final back view. Three production
Mass30 clips cover start, first turn and uphill:150 frames each, tick1000,
no runtime errors. Native frames are unretouched; comparison videos only join
matched frames side by side. Capture/video hashes are retained. The comparison
CMD still uses the older procedural fallback; the gallery labels its actual
previous-binary baselines separately.

No authoritative simulation, AI, movement/steering, energy, hit resolution,
replay/events, career/save, graphics assets/quality, weather, UI or audio change.
There is no intended deterministic race output change; full race replay was
not rerun for this presentation-only correction. Motion is authored procedural,
not mocap. VU-B stays open for owner playtest of arm rhythm and rifle contact.
See ADR-0069 and GDD29.2. Final test/build/performance receipts are recorded in
the gallery and the verification entry below.


## Final verification

Full native Biathlon.PlayModeTests assembly:410/410 PASS,0 failures/skips,
25m45s. This is the complete410-case run, including the3 actual back-contact
cases and7 sprint/carry cases. The MCP transport request timed out before
completion; an independent Unity TestRunner callback retained the final NUnit
XML. The timeout receipt is preserved separately and is not a test failure.
Console Error, Exception and Assert queries after the run are empty. All prior
red receipts are retained; failed tests were not removed or relaxed.

The Win64 development build succeeds with0 errors/0 warnings in this incremental
build; historical Sentis warnings are not claimed resolved. Native startup
following windowed QA reports2560x1440 FullScreenWindow. Real field review,
paired close captures and startup exit0, no runtime errors.

Sequential matched Ultra1920x1080 runs:30 athletes,2588 trees,120 warm-up and
240 measured frames. Main CPU mean4.1776→3.8539 ms, p956.0813→5.6939 ms;
mean animation0.4871→0.4749 ms; GC mean7584.95→7584.59 B/frame (rounding/noise,
not a new memory optimization). This scoped pair shows no measured regression;
it does not prove a general speedup or whole-game60FPS. GPU and render-thread
timing remain unavailable. The benchmark excludes HUD/swapchain costs. Textures,
models and quality settings are retained. Exact receipts and binary/source
hashes are in the gallery folder.

Next action: owner tests this fullscreen build, especially Shift rhythm and
rifle placement in tuck, then confirms visual acceptance of VU-B. VU-G technical
follow-up is complete. M16 remains unstarted.
