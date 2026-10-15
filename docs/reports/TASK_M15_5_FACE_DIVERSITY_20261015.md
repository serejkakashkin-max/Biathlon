# M15.5 — Stable face and skin diversity, 2026-10-15

The owner explicitly approves the latest athlete movement ("движение теперь
шикарное… апрув") and requests different faces, including light and dark skin.
That approval closes VU-B artistic review. This additional face follow-up keeps
the accepted body, movement and rifle contact and gives each stable athlete ID
a repeatable appearance.

Local tooling was verified: portable Blender4.5.9 and the existing G2 body/LOD
authoring pipeline. The retained hero is the audited CC0 body. The old015
restricted scanned-head integration was retired by an earlier owner directive
and remains archived; it is not required or reactivated. The new geometry is
original face-only morph data authored on the current model, not imported heads
or generated photographs. The established G2 art style remains.

## Behavior and implementation

`athlete_faces_v1` schema1 defines8 light-to-deep skin colors and6 face profiles:
oval, angular, round, narrow, broad and defined chin. They affect jaw, cheeks,
chin and nose within a bounded face envelope. Fixed salted hash channels choose
tone and shape independently from the stable athlete ID. Existing cosmetic
build choice is retained exactly. All48 new tone/face combinations are available;
colors are independent of national kit, athlete strength, race number and bib.

Appearance is recreated consistently when the same athlete returns in another
race or a loaded career/replay. No career fields or schema change; stability is
within this versioned palette. Adding/reordering future recipe entries requires
an explicit visual version decision. No character creator UI, new gender/body
model, facial scans or animation changes are part of this task.

`AthleteFaceAuthoring` appends6 face shapes to each existing mesh LOD. Base
vertices, indices, bones, weights, sockets and first4 joint channels are retained.
The3 existing build channels retain their body/shoulder portions; their facial
width term is corrected for continuity at the centre line as described below.
Normals store only the morph-induced difference, preserving the
authored base smoothing. Repeated regeneration replaces only these6 shapes.
The normal body import pipeline calls the same authoring step, so regeneration
does not silently discard face diversity. Per-athlete vertex edits or mesh clones
are not used in gameplay; one face morph is selected at identity assignment.

`AthleteAppearancePresentation` combines the existing shared national cloth
palette with8 shared skin materials. Changing nationality/bib/leader changes
the appropriate independent channel. Skin is no longer indirectly tied to the
national material's minor variation value. Materials and mesh assets remain
shared; repeated unchanged identity/nation calls allocate0 managed bytes.

## Review and acceptance

Fullscreen test launcher: `Builds/M15_5_Faces_Review/START_TEST.cmd`.
Native gallery: `docs/quality/m15-5/faces/review.html`.
14 native640x800 portraits show8 tones and6 front-view shapes. All use the same
neutral existing build and lighting; the six front portraits use the same tone.
The gallery contains only native images and layout/captions. No face retouching
or image generation is used as acceptance evidence. Real Mass30 captures retain
actual production athletes, race controls and the accepted motion.

The old joint-volume fixture initially expected exactly7 morph channels and
failed after the intended6 new face channels were added. Its expected count
now includes those channels; all original joint locality/volume/rest assertions
remain. The first failure receipt is retained. No failing test was disabled or
deleted. Technical verification results are recorded below after the final run.

This follow-up is presentation-only: no Domain/Simulation/AI/Career/Replay
authority, steering, stamina, shot resolution, route, weather, UI, audio or
texture-quality change. Deterministic race output is not intended to change;
a full authoritative race replay is not rerun for a cosmetic-only addition.
M16 remains unstarted. Next action is owner review of faces in the test build.


## Initial verification (superseded by the final midline check below)

Initial feature source e5c65d68 on codex/m15-5-visual-upgrade. Selected checks:68/68 PASS,
0 failures/skips across14 fixtures:66 native PlayMode cases and2 EditMode
cases. The417-case PlayMode inventory is not a full-suite rerun claim. Coverage
includes all LOD face geometry, fixed-ID choice/distribution, material sharing,
zero warm allocations, national/bib/leader independence, idempotent authoring,
neutral-normal preservation, existing joint geometry, approved motion, prone/
standing aim and rifle/back contact. The first fixed7-channel fixture failure
is retained. A shell post-run LASTEXITCODE guard reported a stale external
command code after all test summaries passed; direct aggregation of every
required structured test result confirms68 passes and no actual test failures.

All6 matched moving-root body/leg transform stream hashes are exactly equal
to the previous approved source (360 frames each at120Hz). Both shape and skin
changes are visual; they do not move the rig/bones or alter simulation RNG.
14 native portraits pass;3 actual Mass30 clips reach tick1000 without errors.
Final Console Error/Exception/Assert queries are empty. Win64 development build
succeeds with0 errors/0 warnings; historical unrelated shader warnings are not
claimed resolved. Default launch after windowed QA reports2560x1440
FullScreenWindow. Source/binary/frame hashes and startup receipt are retained.

Sequential matched Ultra1920x1080,30 athletes/2588 trees,120 warmup+240 measured
frames:main CPU mean3.9820→3.7752 ms,p955.8440→5.5498 ms,pose0.4874→0.4616 ms,
mean GC7585.72→7585.58 B/frame (effectively unchanged/noise). Both runs report
209963578 texture bytes; no new skin textures are introduced. This single
scoped pair shows no measured CPU regression; it is not a general speedup or
whole-game60FPS claim. GPU/render-thread timings remain unavailable. One
additional active face morph per athlete has a bounded cost, not a zero-cost
claim. The benchmark excludes HUD/swapchain costs.

Approval status: VU-B movement APPROVED by owner. Existing VU-C/D/E/F approvals
are retained; this additional face diversity is ready for owner visual review.
VU-G technical verification is complete for the face follow-up. M16 has not
started. Next action: owner continues career or launches a race in the fullscreen
Faces Review build and reviews the athlete faces.


## Final midline correction and verification (latest operational result)

Source74baab00 includes featuree5c65d68. Review of the complete native gallery
caught a chin/neck seam in the narrow face after the first68-check pass. The
cause was a discontinuous Sign(x) face-width displacement at nearly coincident
mirrored vertices. It existed in the older cosmetic face width as well as the
new jaw/cheek term. One smooth sinusoidal side field now approaches0 at the
midline in both authoring paths. Source topology/skin weights are retained;
first4 joint channels and body/shoulder portions of the old3 builds remain.
Face portions of those old3 channels intentionally change. This qualifies the
initial broad morph-preservation claim; the approved movement remains exact.

New actual-mesh midline tests fail all3 LODs before the fix and pass afterward.
The first preliminary test's centre sampling was expanded from1 to3mm because
LOD2's closest centre vertex is1.49mm away; the final bound covers all LODs and
fails the actual split/overlap pattern before the source fix. Raw red receipts
and the rejected narrow-face native image are retained. No failure disabled.

Final71/71 selected cases PASS,0 failed/skipped:69 native PlayMode+2 EditMode
across14 fixtures. Inventory420 is not a full-suite claim; repeated passes are
not added. All6 matched360-frame120Hz body/leg transform hashes still match
the approved source exactly. Regenerated14 portraits and actual Mass30 through
tick1000 pass without runtime errors. Native narrow front/oblique views now
have no midline split. Build0 errors/0 warnings; default fullscreen after
windowed QA remains2560x1440 FullScreenWindow.

Final matched Ultra1920x1080,30 athletes/2588 trees,120warmup+240 sampled frames:
CPUmean3.7733→3.8331ms(+0.060ms),p955.5706→5.5786ms,pose0.4603→0.4738ms,
GC7585.575B/frame identical; texture bytes209963578 identical. This scoped pair
shows a small bounded change, not zero cost or a general speedup. GPU/render-
thread timing unavailable; HUD/swapchain excluded. No texture/quality reduction.

Two Editor-only InvalidOperationException records came from the Unity MCP
UnityMainThread.Dispatch/TaskCompletionSource.SetException tooling path during
automated checks. Their stack traces are preserved in editor-tooling-exceptions;
no project/player stack frame is present. The final settled3-minute Console
Error/Exception/Assert queries are empty. All native review error arrays are
empty. The package was not patched as part of this avatar feature.

Latest launcher and gallery remain Faces Review paths above. VU-B movement
APPROVED; VU-G final face verification complete; added faces await owner review.
M16 unstarted. No race authority/balance/save changes.
