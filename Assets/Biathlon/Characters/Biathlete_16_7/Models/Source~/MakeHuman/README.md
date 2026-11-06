# M16.7 — CC0 anatomical production input

The active candidate uses `Natural/athletic_fitted.blend` and its JSON metadata.
It preserves the original MakeHuman head, neck, torso and limb proportions after
one uniform 1.80 m scale and a rigid Y +0.09 m translation. The 24 mapped joint
names are retained, but their pivots follow the anatomical source. There is no
head/neck bone-fit stretching, radial clamp or replacement backing tube.

`athletic_fitted.blend/json` at this folder's root are the **rejected legacy rig
fit**, retained only as a red regression fixture. They are not the production
anatomy. The preservation probe measures 57.27 mm of non-rigid head/neck warp
there; the natural source differs by less than 0.001 mm after rigid alignment.
That numerical comparison does not certify artistic quality.

## Data and license

Official hm08 base, four male/young morphology targets, skeleton and weights are
CC0 graphical assets. Original notices are retained in `LICENSE.ASSETS.md` and
`license.txt`. No MakeHuman application/add-on code is bundled. Source URLs and
SHA-256 are pinned in `provenance.json`; current derived data and official 2K
skin are documented in `Natural/provenance.json`.

- [Official asset license](https://github.com/makehumancommunity/makehuman/blob/master/LICENSE.ASSETS.md)
- [Base mesh](https://github.com/makehumancommunity/makehuman/blob/master/makehuman/data/3dobjs/base.obj)
- [Rig data](https://github.com/makehumancommunity/makehuman/tree/master/makehuman/data/rigs)
- [Official system-assets catalog, including CC0 young male skin](https://static.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html)

Only the body group is used: 13,380 vertices, 13,378 quads, 26,756 triangles before
subdivision. Morph blend: male-young 1.0; average-muscle/average-weight .55,
max-muscle/average-weight .20, max-muscle/min-weight .25. These are explicit
additive target weights, not a claim of equivalent UI slider settings.

Metres, Z up, -Y forward. Original body UVs and source weights are retained.
Runtime export maps to 24 body joints; 40 finger-joint records support offline
phalange grip authoring. A runtime articulated finger rig is not exported.

## Regeneration

Use official Blender 4.5.15 LTS. From the repository root:

```powershell
& $blender --background --python-exit-code 2 --python scripts/art/m16_7/anatomy_prepare.py -- --source Assets/Biathlon/Characters/Biathlete_16_7/Models/Source~/MakeHuman --audit docs/quality/m16-7/source-audit.json --output artifacts/m16-7-refinement/natural-anatomy --natural
& $blender --background --python-exit-code 2 --python scripts/art/m16_7/verify_anatomical_proportions.py -- --body Assets/Biathlon/Characters/Biathlete_16_7/Models/Source~/MakeHuman/Natural/athletic_fitted.blend --source Assets/Biathlon/Characters/Biathlete_16_7/Models/Source~/MakeHuman --output artifacts/m16-7-refinement/anatomy-warp-green.json
```

The audit supplies existing bone names/parent relationships. Natural joint
positions are derived from the original MakeHuman rig. No internet access,
MakeHuman installation or add-on is needed once these input files exist.
Copy reviewed `.blend/.json` outputs into `Natural/` explicitly. `Source~` is
intentionally excluded from Unity import and is not a second runtime character.


## 2026-11-06 surface contact metadata

The Natural JSON now records `finger_pad_centres`, computed from the actual
source cage vertices with distal skin weights. A virtual bone tail can lie
outside the finger skin and must not be treated as a contact point. The CC0
anatomical vertices and their proportions are unchanged. Natural provenance
hashes include this metadata enrichment. Saved glove regression fixtures live
in the sibling `Regression/` folder, outside the active runtime asset.
