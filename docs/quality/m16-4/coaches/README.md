# M16.4 WP-E — coaches' enclosure

2026-10-31. COMPLETE / OWNER TEST PENDING.
Fullscreen test build: `Builds/M16_4_Coaches_Review/START_TEST.cmd`.
Previous review folders remain available.

Behind the shooting-ramp exit aisle: a90m-wide,3.2m-deep fenced snow platform,
at least35cm above the firing datum, a rear access opening/step, national plaques,
twelve spotting scopes/tripods, cases and clipboards. Twelve coaches use the
approved Hero athlete mesh, six face shapes, eight skin tones, ages44–69, grey or
dark hair/beards, different builds/headwear/heights and six national winter kits.
Scopes face actual target banks; right-eye eyepiece contact is qualified.
The exact placement contract/reference and deliberate extra setback to preserve
the existing exit aisle are documented in ADR-0076.

Art is baked offline at the Hero's existing three LODs. Each body uses one shared
material; the deck, fence and equipment use one combined mesh. No new Animator,
SkinnedMeshRenderer, athlete frame scripts or physics colliders in the live world.
The normal maps are the original Hero textile/skin maps. No existing athlete mesh,
shader, motion, graphics tier or texture setting is changed. Small decorative
clusters overlapping the new area are filtered once; the native review excludes
zero existing full-size forest trees. This is bounded static rendering work,
not evidence of zero added GPU cost or a general FPS claim.

Validation:

- **34/34 PlayMode PASS**: seven new coach cases plus production-environment and
  range-flag regressions. Includes every authored format/private-mat corridor,
  segment crossings with both endpoints outside, rotated/translated range frames,
  invalid definitions, actual high-quality terrain/course render, eye alignment,
  shared asset survival and generated-resource cleanup.
- **1/1 EditMode PASS**: existing environment ownership/global-state lifecycle.
- Six actual1600×900 native world views inspected: overview, row and four faces.
  Includes real course/apron and joined ground. No injected screenshot/mockup.
- **Windows build Succeeded**,0errors,430595331bytes,29.989s. The520 existing
  warnings are24CS0618,460UnityAIInference ConvGeneric,36Sentis variants;
  `build-warnings.txt` retains classification. Actual Console reviewed before
  clearing, final0errors/0warnings, Editor idle and outside Play Mode.
- Compiled player **2560×1440 FullScreenWindow PASS**: isolated profile creation,
  recovery/settings roundtrip, real race loading, bib8 delayed-start skip and
  personal3–2–1 countdown, no owner-profile changes or C#/shader errors.
  All four loader stages painted with clock held/countdown unarmed. Advanced
  navigation probe is not enabled by this startup run; its default receipt fields
  are not treated as navigation evidence.
- All **five core DLLs are byte-identical** to the prior loader review build.
  669/670 protected source/resource files match the task-entry snapshot. The only
  existing resource delta is the optional `range.coaches_resource` binding; all
  other environment JSON members match the prior commit. One new visual recipe
  and static art are added. Race physics, scores, AI, balance and save schemas do
  not change; no new deterministic race output is expected.

Run `python scripts/quality/report_m164_coaches.py` to verify the checked evidence
and current source/build fingerprints. Native receipts, images and authority
checks are alongside this file. Raw player log and full Console audit remain in
`artifacts/m16-4/coaches/`.

Limitation: coaches are posed static venue art, without procedural cheering,
tracking animation or coaching gameplay. Next: owner art review in Sprint,
Pursuit and Mass Start before milestone acceptance; M16.4 remains open.
