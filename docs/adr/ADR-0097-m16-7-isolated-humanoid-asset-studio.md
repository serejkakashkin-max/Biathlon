# ADR-0097 — Separate M16.7 Humanoid asset and inspection studio

2026-11-05. Owner closes M16.6 and requests a realistic, editable biathlete
according to `docs/M16_7_OWNER_TASK.md`. The existing G2 athlete uses the typed,
read-only presentation boundary in ADR-0020 and the hybrid animation boundary in
ADR-0069. Its gameplay controller and contact/ground/grip assumptions are already
qualified. A different anatomical skeleton must not silently replace that rig.

## Decision

Keep the new character under `Assets/Biathlon/Characters/Biathlete_16_7/`.
The editable Blender source retains separate garments and equipment. Export a
runtime FBX with a valid Unity Humanoid avatar, one weighted body per LOD and
independent rifle/skis/poles. Six explicit, serialized equipment sockets belong
to this asset. Root motion is disabled; the inspection poses cannot access a
race, clock, save, shooting outcome or physics state. This is an asset-specific
rig, not a new authoritative character controller.

Only the owner-provided CC0 anatomical source/skeleton/skin maps and retained
walk/run file are incorporated. Casual clothing is replaced by a separate
weighted compression shell; the sport equipment is authored in Blender. The
downloaded alpine skis, safety glasses and ordinary rifles are measured sources,
not final equipment. The paid CGTrader model is an art-quality reference only.
Preserve the source's AI-assisted disclosure and provenance.

Unity imports four-weight Humanoid skinning and project-owned URP/Lit materials.
Textile maps are genuinely 4096x4096; supplied skin maps are 2048x2048. Blender's
scene-linear material colors convert at the Unity property boundary. ORM/roughness
data remain linear and are packed as metallic/smoothness for URP. Normal maps
use normal import mode. Do not change global render assets to improve this model.

The FBX importer may create automatic LODGroups from mesh names. Remove those in
the authored prefab and create exactly one measured three-level LODGroup. Keeping
both registers the same renderers twice and produces native warnings. Unpack the
imported model before changing the rifle's parent so the saved prefab preserves
the independent socket attachment.

Provide a separate orbit studio scene and Windows review build. Its cloned UI
panel, camera, lights, probe and high-quality lease are local to the scene. The
ordinary game remains accessible from the studio. Four inspection poses test
basic deformation, foot-preserving knee flexion and rifle reparent/reset. These
are not skating or shooting animation qualification.

## Verification and limits

Native tests cover avatar, sockets, independent equipment, root motion, UVs,
material ownership/import, measured LODs and repeated finite deformation. Existing
G2 tests continue to verify the race/camera presentation boundary. Actual Blender
renders and standalone captures are required in addition to these numeric gates.

On this Windows driver a hidden window can omit the automatic world rendering
pass while its native UI still renders. A UI-only frame falsely passed a whole
image brightness check. The opt-in development review now explicitly submits the
actual camera, renders the native UI to a separate transparent stencil surface
and composites both on the GPU. The model area excludes the UI rail in the image
gate; a retained PowerShell verifier reproduces the former failure independently.
An additional visual probe caught a one-frame GPU-skinning delay: the label and
rifle socket had advanced while the skinned body still showed the preceding
pose. Submit the explicit camera request after WaitForEndOfFrame, when skinning
and native UI for that pose are complete. The image verifier separately checks
raised sleeves in pose2 and their absence in pose3, excluding the central cap.
The retained original probe fails and the final player probe passes. Ordinary
play has no capture targets or extra render requests.

The installed Unity-MCP package independently requires dependency generation34
instead of30. Preserve that resolver change and exclude it from this commit.
The remaining24 ProjectSettings files match task-start bytes after guarded
restoration of temporary build settings. This is not a character render change.

M16.7 stays IN PROGRESS. This checkpoint supplies editable geometry and a working
inspection build. Reference-equivalent photorealism, manual cloth/joint cleanup,
finger articulation/contact and the new rig's complete race skating/aim/ground
adaptation remain unqualified. The existing race athlete is not replaced. No
stat, propulsion, RNG, shot geometry, save schema or race outcome policy changes.

Evidence: `docs/quality/m16-7/qualification.json` and
`docs/M16_7_WP_STATUS.md`. No claim that the complete simulation suite was rerun.

API references: [PanelSettings clearColor](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/UIElements.PanelSettings-clearColor.html),
[URP render requests](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/User-Render-Requests.html).

## Refinement — 2026-11-05

Replace the inherited casual-garment surface with the official CC0 MakeHuman
hm08 anatomy and four male/young morphology targets, fitted to the same24 joints.
Preserve its clean torso/hip/finger loops and explicit source weights. Source
licenses/hashes, the fitted mesh and reproducible preparation live under
Models/Source~/MakeHuman. The old Grab3D head/skin remains; this does not introduce
MakeHuman application code or change the source walk/run into ski animation.

Biathlete167Motion is a presentation-only studio V2 cycle. It caches imported
transforms/limb lengths and measures independent ski/pole meshes once. Exact phase
sampling drives weight transfer, ski recovery and two pole actions per cycle;
analytic limb solves preserve bone lengths and do not write the model root.
Playback, pause, speed and scrubbing expose the actual evaluated skin in the studio.
No simulation assembly or race tuning changes. Tests measure full-cycle contact,
reach residuals, continuity, phase-order independence and zero managed allocations
in warmed EvaluateCycle. The new rig is still not installed in the race factory.

Surface quality is reviewed separately from numerical PASS. Standalone capture
now covers33 frames: six views, three technical poses and12 phases from each of
front/side. Retain the explicit after-EndOfFrame camera submission and independent
model-area gate. Art and full shooting/ground/race adaptation remain open.


## Superseding natural-anatomy / production-motion checkpoint — 2026-11-06

The owner rejected the warped neck, independent studio cycle and subsequent
fingertip-pinching grip. The active isolated studio now uses the unwarped CC0
MakeHuman body/head/neck/hands and official 2K skin; the rejected fitted body
remains only as a negative regression fixture. Head/neck preservation is tested
against the raw morphology after rigid alignment (old warp 57.27 mm; Natural
error below 0.001 mm). Garment edges, bib and zipper follow the source surface
and transferred weights. There is no replacement neck tube.

`Biathlete167MainMotion` evaluates the existing G2 production locomotion,
rifle, ground and grip adapters on a hidden donor, then transfers directions
and fixed limb lengths to the anatomical rig. Optional presentation contact
adaptation limits wrist bend/forearm roll and lifts the boot heel about its
toe binding while retaining ski pose. The earlier independent V2 driver is
retained for legacy regression only. No race factory installs this candidate.

The full-palmar tuck grip uses offline anatomical finger hinges and actual
finger-pad centres. The upper 120 mm handle rests diagonally in the palm;
all four fingers and opposing thumb wrap it. A closed padded wrist loop and
short connectors replace the external triangular strap. A single versioned
JSON definition drives grip obliquity/end offset and runtime pose adaptation;
native tests require matching export metadata. Two rigid glove variants are
switched with their contact frames; articulated runtime fingers remain open.

Verification uses saved mesh surfaces, not generated IK targets: palmar and
five-finger proximity, upper-end contact band, handle-interior rays and a full
32-direction wrist circumference. The retained rejected pinch fixture fails
this same test. Independent palm/dorsal surface markers detect the previous
FBX handedness reversal. Native tests also measure posed hand separation,
elbow width, forearm clearance and nine shaft rays against the actual skinned
body at four tuck phases. Every one of the twelve production states is sampled
for root/limb preservation, toe contact, bounded wrist motion and zero warmed
managed allocations. These bounds do not constitute artistic approval.

The rifle incorporates official CC0 Poly Haven geometry/maps by Mateusz Sadek
with project-authored decorative biathlon fittings; licenses, original input
files and hashes are retained. The paid CGTrader reference and public athlete
photos remain view-only references.

Actual Blender/Unity frames and machine receipts are under
`docs/quality/m16-7-natural/`; the latest qualification JSON controls test scope.
No new Windows build: owner BUILD HOLD remains while the asset is refined.
M16.6 is CLOSED; M16.7 is IN PROGRESS. Shooting poses/transitions, cloth polish,
LOD transitions and a 30-athlete field still need separate qualification.
No authoritative simulation, balance, shot geometry, career or save changes.

## 2026-11-06 wrist and distal-surface correction

Swing limits alone missed the rejected candidate's 130.66degree axial wrist
twist. Keep the authored hand/prop contact frame and distribute axial rotation
onto the lower arm before restoring hand orientation. Qualify swing and twist
against the forearm-relative bind frame, then independently check anatomical
hand points, elbow plane, separation and skinned-body/shaft clearance. Do not
clamp the hand after solving the prop frame; that can rotate the shaft inward.

Finger fitting uses source hand cages and Blender Catmull-Clark limit-surface
stencils, verified against Blender. Actual distal-palmar patches must touch;
proximal contact cannot qualify a claw grip. Rounded handles, separate axial
finger slots, opposing thumb, closed straps and sampled inter-finger collisions
are checked on saved geometry. Glove-shell overrides cannot alter palmar
patches. Native renderers use explicit external URP assets, including on first
import. These engineering gates do not replace art review.

Opened clean preview scenes are replaced after the new scene is populated;
dirty previews are preserved. Current evidence and actual motion videos:
docs/quality/m16-7-wrist. 71 PlayMode + 2 EditMode PASS. Prior QA remains
historical. BUILD HOLD and the isolated asset boundary remain.

## 2026-11-06 ordinary grip and importer round-trip

The owner correctly rejected ordinary-grip images. Its fixed-angle bone-circle
fit survived because distal-palmar qualification covered Tuck only. Both source
variants now use the same surface solver; both must pass palmar/all-five-finger,
actual distal-patch, rounded-handle interior, closed-strap, finger-clearance and
upper-thumb tests. The retained rejected_working_grip.blend fails the new gate.
Native attachment tests sample both hands across all12states/96phases. Actual
normal/0.25x cycles and current images are in docs/quality/m16-7-wrist.

Read/modify/write ModelImporter.humanDescription, preserving importer-owned
globalScale/root/parent fields. Constructing a new struct reset private scale
to0, reproducing meta-write errors in repeated full authoring. The new loaded-
importer regression failed and now passes twice; caches are released and settings
persisted before synchronous reimport. No error is suppressed/expected away.
Final82PlayMode+3EditMode PASS,6positive/2expected-negative source checks PASS.
These checks qualify the technical checkpoint; M16.7 art acceptance is open.


2026-11-06 M16.7 clothing checkpoint: UV-printed bib replaces floating glyphs; GDI winding preserves overlapping contours. Shared suit/bib partition and normalized weights remove independent-layer cuts; covered runtime triangles excluded with full editable underlay retained. Surface-following harness, matte textile and continuous panels refined. Tuck pelvis presentation lift6cm with both foot bindings restored by IK removes torso/thigh crossings. Actual imported BakeMesh16poses:0strict interior garment crossings, negative former poses reject; source layer/bitmap negative fixtures retained.86PlayMode+3EditMode PASS, source grip6positive+2expected-negativePASS. ActiveLOD0149094tri vs197883; allstored229616, no field FPS claim. Evidence/actual videos and source hashes: docs/quality/m16-7-cloth/qualification.json. Full assembly and final art acceptance not claimed. Race G2/authoritative gameplay/balance/shot geometry/career/saves unchanged. BUILD HOLD, no new Windows player; M16.6 CLOSED, M16.7 OPEN, M17 not started. Equipment follow-up with owner downloaded CC-BY Skiing Lady and independently authored reference rifle remains active. Scoped publication pending; historical STATE bytes preserved.


2026-11-06 M16.7 equipment/owner eyewear choice checkpoint: original sports glasses retained as default, saved source6parts have0vertex displacement. Optional mask derives selected lens/frame/padding from Skiing Lady by lancehuang526(CC-BY-4.0), welded/refined/fitted, with project strap. Studio dropdown switches actual imported renderers exclusively through all poses/4motion techniques. Donor alpine boots/rig/animation excluded; Nordic toe hinge and verified full-palmar grip retained. New PULSE4K ski surface graphics, carbon poles, ferrules and correctly oriented3spoke baskets. Independently authored broad sport rifle furniture/cheek rest/butt/sights/4magazine props follows owner animod visual reference; no paid source mesh copied, no exact likeness/art acceptance claimed. Natural wood material isolated from legacy.
Windows loaded-meta truncation reproduced Win321224; byte-identical atomic replacement immediately before Unity settings write preservesGUID/settings, same repeated-authoring regression now PASS and checks current remap persisted to disk. Mapping holder unconfirmed; no error suppression. Final89PlayMode+3EditMode PASS, Console after427296 has0newError/Exception/Assert. Source grip6positive+2expected-negativePASS; actual equipment bitmap/geometryPASS, clipped-print negative rejects; BakeMesh16poses0strict interior cloth crossings. Current source SHA256/evidence in docs/quality/m16-7-equipment/qualification.json. Full assembly/continuous collision/field FPS not claimed. Previous clothing checkpoint621964599a7765f412799ed2b39002306e975e61 confirmed on origin;17LFS objects66MB uploaded. Historical STATE/QA preserved. M16.6 CLOSED; M16.7 OPEN; BUILD HOLD/no new Windows player; raceG2/authoritative gameplay/balance/AI/shooting geometry/career/saves unchanged. Next: further artistic equipment/cloth polish, shooting transitions and separate terrain/LOD/field qualification. Equipment scoped GitHub publication pending.
