# OWNER M11 VISUAL ADDENDUM v1.0

**Project:** BIATHLON  
**Applies to:** M11 — Animation and Athlete Presentation, WP-141 .. WP-151  
**Status:** Owner-supplied production guidance; becomes active for M11 after OWNER GATE G1 approval  
**Authority:** Supplements, but does not replace, `docs/BIATHLON_MASTER_PLAN_FINAL_v2.0.md`, the GDD/TDD, approved ADRs, and verified task reports.

---

## 1. Purpose

M11 must replace the M10 capsule/proxy presentation with a coherent third-person humanoid athlete presentation pipeline without changing authoritative gameplay.

The owner-provided visual references under:

`docs/art/reference/m11/`

define the desired **visual direction, silhouette, pose language, equipment placement, camera readability, and material intent** for M11.

They are not authoritative sources for real-world dimensions, licensed branding, exact mechanical engineering, or animation timing. Where an image conflicts with the Master Plan, simulation state, verified gameplay, or an approved asset's rig/geometry, the project specification and verified implementation win.

---

## 2. Product direction that M11 must preserve

BIATHLON remains a **third-person sports game**.

During skiing, the main player-facing view is the established chase camera behind and slightly above the controlled athlete. The player should clearly read:

- their own athlete body and posture;
- skis and poles;
- rifle on the back;
- nearby opponents;
- drafting / overtaking situations;
- terrain and direction of travel.

Range gameplay may use a dedicated shooting / aim camera.

Do not redirect the project toward first-person skiing.

M11 must not radically redesign controls approved at G1.

---

## 3. Visual target

### 3.1 Athlete silhouette

Target a believable modern biathlete:

- athletic, lean winter endurance-sport build;
- fitted technical race suit;
- race bib layered over the suit;
- gloves, race boots, sport eyewear/headwear;
- rifle harness visibly integrated with the torso/back;
- skis and poles readable at normal gameplay distance;
- no exaggerated arcade proportions.

### 3.2 Visual hierarchy

At chase-camera distance, prioritize:

1. readable whole-body pose;
2. ski stance and leg separation;
3. pole timing and contact;
4. torso lean / effort level;
5. rifle stability on the back;
6. silhouette separation from nearby opponents.

Close-up face detail is secondary to animation credibility in M11.

### 3.3 Motion credibility

Motion credibility matters more than polygon count.

Avoid:

- foot skating without ski response;
- poles penetrating terrain or floating visibly;
- rifle clipping through torso/head;
- abrupt locomotion state snaps;
- prone / standing transitions that visibly ignore the rifle;
- upper/lower body poses that fight each other;
- identical posture across flat, climb, sprint, tuck and cornering.

---

## 4. Reference pack usage

Canonical owner reference root:

`docs/art/reference/m11/`

Expected structure:

```text
docs/art/reference/m11/
├── M11_master_reference_board_v1.png
├── 01_athlete_identity/
│   ├── M11_athlete_hero_v1.png
│   └── M11_athlete_turnaround_v1.png
├── 02_locomotion/
│   └── M11_locomotion_pose_sheet_v1.png
├── 03_shooting/
│   └── M11_shooting_pose_sheet_v1.png
├── 04_equipment/
│   └── M11_equipment_reference_v1.png
├── 05_camera/
│   └── M11_camera_composition_v1.png
└── 06_materials/
    └── M11_materials_detail_reference_v1.png
```

Use `docs/owner/M11_REFERENCE_INDEX_v1.md` for the intended interpretation of each image.

---

## 5. Important legal / branding rule

The reference images are **visual direction only**.

Some generated reference images may contain accidental or pseudo-real branding, logos, markings, text, equipment shapes, or brand-like details. They must **not** be copied into production assets unless separately cleared and documented.

For Release 1.0:

- use fictional/original athlete identity and branding;
- remove or replace real/pseudo-real manufacturer text and logos;
- do not reproduce a distinctive third-party logo from a reference image;
- do not treat generated text appearing in a concept sheet as approved branding;
- any external asset must have source/license recorded before import.

The owner approves licensed/paid asset decisions separately.

---

## 6. M11 work-package guidance

### WP-141 — Humanoid presentation architecture

Goal:

- rig-independent presentation layer;
- simulation-to-animation state adapter;
- clear separation between authoritative simulation and presentation;
- reusable athlete prefab/presentation architecture;
- stable sockets/anchors for rifle, poles, skis and camera-related targets;
- LOD-ready structure from the start.

Requirements:

- no animation state may become authoritative race state;
- no root-motion dependency that changes simulation position;
- athlete Transform follows presentation/interpolation only;
- support swapping the final humanoid asset without rewriting gameplay logic;
- define naming/anchor conventions for hands, feet, ski bindings, rifle mount and aim alignment.

### WP-142 — Locomotion state model

Required presentation states:

- flat / sustainable skating;
- climb;
- acceleration / attack;
- sprint / maximum effort;
- tuck / high-speed descent;
- technical descent;
- transitions between effort levels.

Use `02_locomotion/M11_locomotion_pose_sheet_v1.png` for pose language, not exact frame timing.

Animation state selection should derive from authoritative/presentation values such as:

- speed;
- slope;
- effort;
- tuck state;
- curvature / turn state;
- fatigue / finish effort where relevant.

### WP-143 — Cornering presentation

Show:

- meaningful body lean;
- lower-body alignment with ski direction;
- preserved chase-camera readability;
- no impossible lateral snapping;
- safe blending into/out of cornering.

Cornering must reflect route/curvature presentation, not modify authoritative route movement.

### WP-144 — Rifle carry transitions

Required:

- stable rifle-on-back state during skiing;
- believable release from harness/back;
- transition toward range-ready position;
- return-to-back transition after shooting.

Use reference images for placement/silhouette only.

Do not copy unlicensed manufacturer branding or a distinctive proprietary rifle design literally.

### WP-145 — Prone enter / exit

Required:

- coherent lowering sequence;
- rifle remains controlled throughout;
- skis/legs remain visually plausible;
- camera handoff remains stable;
- simulation shooting state and presentation state stay synchronized.

### WP-146 — Standing shooting enter / exit

Required:

- readable move from travel/range-ready posture into standing shooting stance;
- rifle-to-shoulder alignment;
- stable camera transition;
- clean return to ski state.

### WP-147 — Shooting procedural / IK layer

Priority alignment:

- shoulder / buttstock;
- cheek/head line;
- support hand;
- trigger hand;
- rifle forward axis;
- aim direction.

The procedural layer must improve visual alignment without changing the authoritative hit calculation.

### WP-148 — Pole / ski IK and contact

Required:

- ski placement follows presentation terrain;
- pole tips should contact plausible ground locations;
- avoid obvious floating / deep penetration;
- preserve animation rhythm;
- handle slope variation;
- keep the system performance-conscious for 30 visible athletes.

Prefer graceful approximation over unstable over-constrained IK.

### WP-149 — Fatigue / exhaustion presentation

Use visual changes such as:

- more torso collapse;
- less crisp pole recovery;
- heavier posture;
- reduced motion sharpness;
- finish-line exhaustion.

Do not alter authoritative speed/power through animation.

### WP-150 — Athlete LOD / crowd performance

Requirements:

- full presentation quality for player / nearby rivals;
- reduced procedural/IK cost at distance;
- stable 30-athlete Mass Start;
- no visible identity popping near common gameplay camera distances;
- measurable CPU/GPU cost.

### WP-151 — Animation validation scenes

Create repeatable validation scenes/captures for:

- flat;
- climb;
- acceleration;
- sprint;
- tuck;
- descent;
- cornering;
- rifle carry;
- prone enter/aim/exit;
- standing enter/aim/exit;
- player chase camera;
- dense 8–12 athlete comparison;
- 30-athlete performance view.

Use fixed cameras where useful and retain representative screenshots.

---

## 7. Asset staging and import policy

Raw external downloads should be staged outside `Assets/`, for example:

```text
external_downloads/m11/
├── characters/
├── animations/
├── weapons/
├── skis_poles/
└── misc/
```

Before importing a third-party asset into Unity:

1. record it in `docs/owner/M11_ASSET_MANIFEST_v1.md`;
2. record source and exact license in `docs/licensing/m11/asset_licenses.md`;
3. classify it as:
   - `raw`
   - `reference-only`
   - `import-candidate`
   - `approved-import`
   - `rejected`
4. import only after its licensing/status is clear.

If final-quality humanoid/animation assets require owner action, stop only at the actual asset/license decision and provide a shortlist with recommendation.

---

## 8. Material direction

M11 may establish material slots and representative materials, but M12 owns the production rendering benchmark.

Desired material families:

- technical stretch fabric;
- bib textile;
- synthetic/rubber glove;
- plastic/composite boot shell;
- ski top-sheet and base;
- matte metal / composite rifle;
- straps / woven harness;
- mirrored sports lens;
- subtle snow/wet/frost response.

Do not over-invest in final snow shader / weather / venue rendering inside M11.

---

## 9. Camera direction

`05_camera/M11_camera_composition_v1.png` is composition guidance.

Primary gameplay:

- third-person chase;
- athlete visible;
- trail/terrain readable;
- opponents readable;
- sufficient look-ahead at speed.

Pack situations:

- do not frame so tightly that nearby competitors disappear;
- preserve lateral awareness for drafting/overtaking.

Range:

- dedicated shooting view is allowed/expected.

Broadcast framing shown in references is future-facing guidance; M11 only needs what helps validate athlete animation and primary gameplay presentation.

---

## 10. Validation behavior

During M11:

- use Unity MCP screenshots frequently;
- inspect motion in Game View, not only static Scene view;
- prefer fixed reproducible validation cameras;
- log visual defects by category;
- retain representative before/after captures;
- do not mark a WP complete solely because an Animator graph exists.

A state is complete when it is visually readable in motion and does not damage gameplay, determinism, or performance.

---

## 11. G1 feedback carried into M11

Owner feedback from the first hands-on greybox should be retained as an issue:

**Player shooting currently feels too easy.**

This is not a request to add hidden RNG or to redesign the shooting simulation during M11.

M11 should:

- preserve authoritative shooting behavior;
- make breathing, heartbeat, fatigue, stance and rifle stability visually legible where possible;
- avoid presentation choices that make the aim challenge artificially easier;
- leave numerical shooting difficulty/balance to appropriate tuning passes unless a presentation defect is clearly responsible.

---

## 12. M11 completion target

M11 is successful when the playable races can be viewed through the established gameplay cameras with:

- humanoid athletes replacing capsules/proxies;
- readable ski locomotion state differences;
- credible cornering;
- stable rifle carry;
- coherent prone and standing transitions;
- substantially improved shooting pose alignment;
- credible pole/ski contact;
- fatigue presentation;
- LOD behavior suitable for a 30-athlete field;
- repeatable animation validation evidence.

M11 is **not** the final visual-quality gate. M12 is responsible for the representative production rendering benchmark.
