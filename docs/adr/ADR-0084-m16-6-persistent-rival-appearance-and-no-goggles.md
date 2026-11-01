# ADR-0084 — Persistent rival cosmetics and complete goggles absence

2026-11-01, M16.6.1. Extends M15.6's existing AppearanceProfile and creator;
does not replace its data, rig, national kit or approved movement.

## Identity and persistence

CareerRivalAppearance reads the same athlete_faces_v1 and
athlete_creator_appearance_v1 resources used by the creator. Stable identity plus
the cosmetic property name supplies a deterministic FNV-1a selection. There is
no gameplay random stream or draw, no additional ability field, and no duplicated
option list. The complete existing schema-2 AppearanceProfile is saved for each
new rival. New world cohorts, catalog worlds and replacement/prospect births
use this generator; explicit existing profiles and the player's choice remain.
Loading never initializes or rerolls old profiles. Legacy identities continue
to render by their original face/body policy. The existing save version 3 and
appearance schemas remain sufficient for this stage.

## All parts of the accessory

The approved model distributes goggles across three material slots: rubber
frame, headwear strap and lens. Existing CreatorGogglesRaised blendshape ownership
identifies every part. Add one cosmetic UV tag to those existing vertices in
all three LODs, preserving the vertex positions, skin weights, eight material
slots and all eighteen morph channels. A material property clips tagged fragments
in the shared forward, depth and shadow paths. Absence uses zero raised-goggles
morph weight: it does not move or recolour the accessory to disguise it.
Eyes, brows, scalp and hair keep their separate original tags.

Owned material-cache keys include the goggles choice for every affected slot,
so an uncovered athlete cannot change another athlete's appearance. No new
renderer, model, texture, collider or per-frame geometry is introduced.

## Qualification and prior test correction

Require every headwear/goggles combination, all LOD ownership masks, independent
athlete materials, genuine native eyes/headband captures, same preview/race
appearance and saved profiles. Core tests compare player look, all abilities,
money, nationality and career RNG before/after cosmetic initialization.

The existing catalog-to-race test assumed roster ordinal was Sprint start slot.
Exact exported baseline 1717c530 reproduces its expected13/actual29 failure.
Keep the test and its exact identity/count assertions; update its fixture start
slots to the already-approved seeded Sprint draw. No seeding code changes.
Receipts and final status belong under docs/quality/m16-6/appearance.
