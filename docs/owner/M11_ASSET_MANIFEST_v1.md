# M11 ASSET MANIFEST v1.0

**Purpose:** Track M11 reference material, external downloads, import candidates and licensing status.

## Status values

- `owner-reference` — owner-provided visual direction; not a Unity production asset
- `raw` — downloaded/staged but not reviewed
- `reference-only` — never import as production asset
- `import-candidate` — technically promising, licensing/review pending
- `approved-import` — cleared for import/use
- `rejected` — do not use

## Owner reference pack

| ID | Path | Category | Status | Intended use | Production-copy rule |
|---|---|---|---|---|---|
| REF-M11-001 | `docs/art/reference/m11/M11_master_reference_board_v1.png` | art direction | owner-reference | overall M11 visual language | guidance only |
| REF-M11-002 | `docs/art/reference/m11/01_athlete_identity/M11_athlete_hero_v1.png` | athlete identity | owner-reference | silhouette, apparel layering, equipment placement | do not copy logos/text |
| REF-M11-003 | `docs/art/reference/m11/01_athlete_identity/M11_athlete_turnaround_v1.png` | turnaround | owner-reference | proportions and multi-angle silhouette | guidance only |
| REF-M11-004 | `docs/art/reference/m11/02_locomotion/M11_locomotion_pose_sheet_v1.png` | locomotion | owner-reference | flat/climb/attack/sprint/tuck/cornering pose language | guidance only |
| REF-M11-005 | `docs/art/reference/m11/03_shooting/M11_shooting_pose_sheet_v1.png` | shooting | owner-reference | carry/draw/prone/standing transition intent | guidance only |
| REF-M11-006 | `docs/art/reference/m11/04_equipment/M11_equipment_reference_v1.png` | equipment | owner-reference | functional categories, proportions, attachment ideas | do not copy branding/design literally |
| REF-M11-007 | `docs/art/reference/m11/05_camera/M11_camera_composition_v1.png` | camera | owner-reference | chase/pack/overtake/range framing | composition guidance |
| REF-M11-008 | `docs/art/reference/m11/06_materials/M11_materials_detail_reference_v1.png` | materials | owner-reference | material breakup and weather-response intent | shader/material guidance |

## External asset candidates

Add one row for every external asset considered.

| ID | File / Asset | Category | Source | Exact license | Attribution | Status | Intended use | Imported files | Notes |
|---|---|---|---|---|---|---|---|---|---|
| EXT-M11-001 |  | humanoid |  |  |  | raw | base athlete |  |  |
| EXT-M11-002 |  | animation |  |  |  | raw | ski locomotion |  |  |
| EXT-M11-003 |  | animation |  |  |  | raw | prone/standing |  |  |
| EXT-M11-004 |  | rifle |  |  |  | raw | rifle prop |  |  |
| EXT-M11-005 |  | skis/poles |  |  |  | raw | equipment |  |  |

## Rules

1. No third-party asset becomes `approved-import` without a known exact license.
2. Paid/account-gated licensing decisions require owner approval.
3. Generated reference images are not proof that a depicted logo, rifle design, ski graphic or manufacturer mark is safe to reproduce.
4. Prefer original/fictitious branding for Release 1.0.
5. Record any modifications made to imported third-party assets.
