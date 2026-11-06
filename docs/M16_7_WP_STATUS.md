# M16.7 — Hero biathlete

2026-11-05. Owner closed M16.6 and authorized M16.7. Owner specification:
`M16_7_OWNER_TASK.md`. This is a real editable 3D asset and Unity integration
task; beauty images cannot substitute for geometry or validation.

| Stage | State | Evidence / next gate |
|---|---|---|
|16.7.1 Project/source analysis|COMPLETE|Unity6000.6.0f1/URP17.6; G2 typed read-only presentation;11 imported source files audited in Blender4.5.15; licenses/provenance below|
|16.7.2 Body/face/skeleton|IMPLEMENTED; ART OPEN|1.80m CC0 anatomical base, repaired24bones, valid Humanoid;2K skin, further face/neck refinement needed|
|16.7.3 Tailored race suit|IMPLEMENTED; ART OPEN|Separate continuous weighted shell, seams/collar/zip/bib27; manual topology/folds/joint cleanup remains|
|16.7.4 Cap/glasses/gloves/boots|IMPLEMENTED; ART OPEN|Original sports silhouettes; static curled fingers, no articulated finger rig; full grip/pose matrix pending|
|16.7.5 Nordic skis/poles|IMPLEMENTED; RACE OPEN|1.94m skis/44mm waist, bindings/boots, pole grips/straps/baskets; moving ground/contact integration pending|
|16.7.6 Sporting rifle/harness|IMPLEMENTED; RACE OPEN|Independent original prop, straps, back/hand sockets; full transfer/aim cycle pending|
|16.7.7 Rig assembly|TECHNICAL PASS|Humanoid/six sockets/root motion off, four finite repeatable poses, foot-preserving knee flexion; full ski animation not qualified|
|16.7.8 PBR|IMPLEMENTED; ART OPEN|Actual4K textile/2K skin, normal import and URP metallic/smoothness; reference-quality skin/fabric not achieved|
|16.7.9 Actual3D visual review|ITERATED; OPEN|Six Blender renders plus native/player views; geometry/material/parenting/UV/bib defects corrected; professional art gate remains|
|16.7.10 Unity integration|STUDIO READY; RACE OPEN|Editable.blend/FBX/prefab/orbit scene/Windows build; existing G2 race athlete retained pending animation adaptation|
|16.7.11 Qualification|TECHNICAL PASS; STAGE OPEN|Native10PASS, build0errors, Console/standalone/image gates; exact receipts in docs/quality/m16-7; no whole-suite or final art approval claim|

## Analysis

Source folder is already extracted and normalized. No extraction necessary.
The downloader did not obtain valid `.blend` files. No model is missing from the
required GLB/FBX set. Readme warning about an empty archive predates the successful
download report and is not the current state. Nothing in that source folder is
an instruction to change the Unity game.

The source man is262348 triangles before importer triangulation; one packed
2K material,24 bones, walk/run actions. Face has detailed geometry/base color,
but skin lacks scanned microdetail. The model wears a loose T-shirt and shorts:
it cannot be recolored as a race suit. Head/neck supply the anatomical base;
new separate garments/accessories are required. Imported bone heads fit1.80m,
but displayed tails extend tens of metres: correct tail orientation/lengths
before Humanoid qualification. Original walk/run remain source locomotion only.

Alpine skis11954tris, beanie8688tris, safety goggles10560tris, poles184tris,
placeholder rifle4420tris and PolyHaven rifle19985tris have been imported and
measured. None is accepted as final professional biathlon equipment. PolyHaven
FBX references texture filenames different from downloaded names: missing-file
repair is needed if reusing it. Prefer original sporting prop geometry and keep
these files as measured references. Source4K claims do not describe these actual
downloaded packed maps: they measure2048x2048.

Current presentation uses G2 Hero,17 typed bones,7 typed sockets, procedural hybrid
skiing, ground/contact/grip/rifle adapters and LOD. Animation cannot change race
state/root motion. Retain existing assets and global rendering settings. New
resources belong under `Assets/Biathlon/Characters/Biathlete_16_7/`.

## Sources and quality reference

- Owner-provided CC0 base: https://grab3d.com/models/characters/man-base-character/
- Other original downloaded sources/licenses are retained in source audit.
- Art direction only: https://www.cgtrader.com/3d-models/character/man/biathlete-fully-equipped-rigged
- Official portable Blender4.5.15 downloaded and SHA256 verified; toolchain
  manifest and actual source Blender renders retained in local analysis.

The paid reference's geometry, textures, logos and design are not redistributed.
Its silhouette, fabric layering, sport-specific boots and vertical harness carry
guide the review. New character uses original PULSE branding and bib27. No claim
of reference-equivalent photorealism or owner visual approval has been made.

## Current checkpoint — 2026-11-05

Editable asset, scripts, studio and review build are available. ADR-0097 records
the boundary and the rejected UI-only capture. Technical qualification is recorded
in `quality/m16-7/qualification.json`; art quality and race motion remain OPEN.
See the asset's `Documentation/README_RU.md` for paths, regeneration and specific
remaining work. M16.6 CLOSED by owner; M16.7 IN PROGRESS; M17 not started.


2026-11-05 owner correction: M16.7 art gate FAILED (neck/jaw and studio motion rejected). BUILD HOLD until actual geometry and transferred main-G2 motion are visually checked. Existing21PASS/Windows build is technical evidence of the rejected candidate, not art acceptance. Natural anatomy and existing production motion transfer are in progress. Race G2 unchanged. PROJECT_STATE latest append controls.


## Latest checkpoint — 2026-11-06 (supersedes earlier candidate rows)

M16.6 CLOSED by owner. M16.7 IN PROGRESS; BUILD HOLD; no new Windows player.
The active studio uses Natural anatomy, official CC0 skin, transferred actual
G2 production motion and presentation-only contact adaptation. Head/neck are
continuous/unwarped; garment weights follow the body. The rifle reuses licensed
CC0 geometry/PBR, with decorative biathlon fittings. The latest owner-directed
full-palmar UPPER-handle grip replaces the rejected fingertip pinch.

Focused native tests: 57 PASS. Saved source-surface full-palm/5-finger/handle
interior/wrist-loop tests PASS; former pinch fixture FAILS as expected. Numeric
PASS is not art acceptance. Actual current frames, full PlayMode-run scope,
source hashes and limitations: `quality/m16-7-natural/qualification.json` and
`quality/m16-7-natural/REVIEW_RU.md`.

Open: manual cloth/finger/equipment polish, full standing/prone shooting and
rifle transitions, terrain/LOD/30-athlete-field integration and visual approval.
The race factory still uses G2. No simulation/balance/career/save changes.


2026-11-06 final verification: 69 native PlayMode PASS (57 asset/rig cases plus affected regression checks). Expanded 686-case run was cancelled after a saved 161-case snapshot (158 PASS/3 FAIL); all three found failures are fixed and included in the 69-case retest. Full-assembly PASS is not claimed. Details: docs/quality/m16-7-natural/regression-followup.json. BUILD HOLD; M16.7 remains open.

## Latest operational checkpoint — 2026-11-06 wrist correction

The owner rejected the preceding wrist orientation. The old swing-only test
missed 130.66 degrees of axial twist; current Tuck twist is <=0.087 degrees,
swing <=21.98 degrees. Forearm rotation, anatomical hand points, actual distal
palmar patches, separate fingers and rounded handles are checked together.
A retained claw-grip fixture fails the new distal test. Glove shell/URP
materials and safe opened-preview regeneration are included. 71 PlayMode +
2 EditMode PASS; final Console interval has no new Error/Exception/Assert.
Actual 37 PNGs and complete four-technique cycles at normal/0.25x speed:
`quality/m16-7-wrist/REVIEW_RU.md` and qualification.json. This supersedes the
active candidate in m16-7-natural; that folder remains historical evidence.
Full-assembly PASS and art acceptance are not claimed. M16.7 OPEN, BUILD HOLD;
race G2 and authoritative gameplay unchanged.

## Latest owner follow-up — working grip, 2026-11-06

The owner rejected the ordinary grip: handle in front of fingers. The old
distal test covered Tuck only; all ten ordinary pads failed by7.18–20.47mm.
Both variants now use the same surface fitter and pass the same full-palm,
all-finger, actual distal-patch, rounded-handle, closed-strap and inter-finger
checks. The retained old working source fails the extended test. Upper-end
thumb positioning also passes the retained thumb-side test. Actual runtime
handle attachment is sampled in all12states/96phases, both hands.

Final82PlayMode +3EditMode PASS; source gate6positive +2expected-negative PASS.
Repeated full authoring preserves importer-owned HumanDescription scale/root
metadata; new-struct initialization reset globalScale and caused meta-write
errors, caught by a new regression. Final Console interval has no new errors.
51currentPNG+1rejectedPNG and actual complete normal/0.25x motion videos:
quality/m16-7-wrist. No new Windows build, full-assembly PASS or art acceptance.
M16.7 OPEN/BUILD HOLD; race G2 and authoritative gameplay remain unchanged.


2026-11-06 M16.7 clothing checkpoint: UV-printed bib replaces floating glyphs; GDI winding preserves overlapping contours. Shared suit/bib partition and normalized weights remove independent-layer cuts; covered runtime triangles excluded with full editable underlay retained. Surface-following harness, matte textile and continuous panels refined. Tuck pelvis presentation lift6cm with both foot bindings restored by IK removes torso/thigh crossings. Actual imported BakeMesh16poses:0strict interior garment crossings, negative former poses reject; source layer/bitmap negative fixtures retained.86PlayMode+3EditMode PASS, source grip6positive+2expected-negativePASS. ActiveLOD0149094tri vs197883; allstored229616, no field FPS claim. Evidence/actual videos and source hashes: docs/quality/m16-7-cloth/qualification.json. Full assembly and final art acceptance not claimed. Race G2/authoritative gameplay/balance/shot geometry/career/saves unchanged. BUILD HOLD, no new Windows player; M16.6 CLOSED, M16.7 OPEN, M17 not started. Equipment follow-up with owner downloaded CC-BY Skiing Lady and independently authored reference rifle remains active. Scoped publication pending; historical STATE bytes preserved.


2026-11-06 M16.7 equipment/owner eyewear choice checkpoint: original sports glasses retained as default, saved source6parts have0vertex displacement. Optional mask derives selected lens/frame/padding from Skiing Lady by lancehuang526(CC-BY-4.0), welded/refined/fitted, with project strap. Studio dropdown switches actual imported renderers exclusively through all poses/4motion techniques. Donor alpine boots/rig/animation excluded; Nordic toe hinge and verified full-palmar grip retained. New PULSE4K ski surface graphics, carbon poles, ferrules and correctly oriented3spoke baskets. Independently authored broad sport rifle furniture/cheek rest/butt/sights/4magazine props follows owner animod visual reference; no paid source mesh copied, no exact likeness/art acceptance claimed. Natural wood material isolated from legacy.
Windows loaded-meta truncation reproduced Win321224; byte-identical atomic replacement immediately before Unity settings write preservesGUID/settings, same repeated-authoring regression now PASS and checks current remap persisted to disk. Mapping holder unconfirmed; no error suppression. Final89PlayMode+3EditMode PASS, Console after427296 has0newError/Exception/Assert. Source grip6positive+2expected-negativePASS; actual equipment bitmap/geometryPASS, clipped-print negative rejects; BakeMesh16poses0strict interior cloth crossings. Current source SHA256/evidence in docs/quality/m16-7-equipment/qualification.json. Full assembly/continuous collision/field FPS not claimed. Previous clothing checkpoint621964599a7765f412799ed2b39002306e975e61 confirmed on origin;17LFS objects66MB uploaded. Historical STATE/QA preserved. M16.6 CLOSED; M16.7 OPEN; BUILD HOLD/no new Windows player; raceG2/authoritative gameplay/balance/AI/shooting geometry/career/saves unchanged. Next: further artistic equipment/cloth polish, shooting transitions and separate terrain/LOD/field qualification. Equipment scoped GitHub publication pending.
