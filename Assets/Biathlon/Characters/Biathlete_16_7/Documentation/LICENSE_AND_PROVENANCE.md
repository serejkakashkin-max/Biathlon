# M16.7 source provenance

Schema version 1; active asset ID `biathlete_16_7_natural_v1`.

## Active natural candidate

MakeHuman hm08 body/head/neck/hands, four official male/young morphology targets,
rig and skinning weights are CC0 graphical data. Official young male diffuse is
2048 x 2048, author `makehuman_system`, from the official CC0 system-assets pack.
No MakeHuman application/add-on code is included. The natural body is uniformly
scaled and rigidly placed; it is not stretched onto the rejected old pivots.
Original notices, source URLs and SHA256: `Models/Source~/MakeHuman/`, including
`Natural/provenance.json` and the full `LICENSE.ASSETS.md` CC0 legal text.

- https://github.com/makehumancommunity/makehuman/blob/master/LICENSE.ASSETS.md
- https://static.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html
- https://files2.makehumancommunity.org/asset_packs/makehuman_system_assets/makehuman_system_assets_cc0.zip

The revised rifle reuses visible stock/metal geometry and 2K maps from **Bolt
Action Rifle 7.62**, by **Mateusz Sadek**, downloaded from Poly Haven's official
asset CDN and pinned by MD5/SHA256. Modified for a decorative sporting game prop:
optical scope, wrap and ammunition omitted; revised external sport fittings,
stock treatment, diopter/front hood, holders and harness connections. UVs and
source PBR detail retained where applicable. No functional internals are authored.

- https://polyhaven.com/a/bolt_action_rifle_7_62
- https://polyhaven.com/license — CC0; commercial modification/redistribution allowed.
- `Models/Source~/PolyHavenRifle/provenance.json` — exact files and source URLs.

The downloaded original Blender file reports a newer Blender save version;
geometry was opened and checked using project toolchain Blender 4.5.15 LTS.
Original source files are retained, not represented as project-authored work.

Compression suit, bib/markings, cap, goggles, glove grip adaptation, boots/skis,
poles, external sport rifle fittings, harness, authoring scripts and motion
retargeting are project-authored/derived work. Finger joint data is used offline;
the exported 24-joint rig does not claim articulated runtime fingers. Fabric4K,
skin2K and rifle2K are actual map sizes, not a claim of scanned photorealism.

The current studio reuses the existing project's G2 pose/contact/grip pipeline.
It is presentation only and does not change race physics or shooting geometry.
The independent earlier V2 studio mode is retained only for legacy regression.

## Retained legacy source and reference

Owner-provided Grab3D base skeleton names/parent relationships and archived
walk/run source are retained in the authoring pipeline/legacy checkpoint:
https://grab3d.com/models/characters/man-base-character/ — CC0 1.0.
That source advertises AI-assisted generation; keep the disclosure where required.
The current natural candidate does not use its old deformed head or skin map.
The other owner-downloaded alpine skis, beanie, safety glasses and background are
measurement references and are not incorporated into this candidate's geometry.

Paid visual-quality reference only:
https://www.cgtrader.com/3d-models/character/man/biathlete-fully-equipped-rigged
No paid geometry, textures or logos are extracted or redistributed. Original
project branding is PULSE. Reference advertising images remain local research,
and are not shipped as project assets or used as proof of our model quality.

Blender is a separate development tool and is not shipped with the game.
Project URP packages and existing Roboto/Oswald OFL notices remain applicable.


## 2026-11-06 grip checkpoint

The full-palmar grip, closed wrist loops, contact solver and verification are
project-authored derivatives of the CC0 anatomical surface. Natural metadata
now includes real source finger-pad centres; its refreshed file hashes are in
`Models/Source~/MakeHuman/Natural/provenance.json`. Regression-only glove meshes
in `Models/Source~/Regression/` are prior project-authored/CC0-derived failures,
not third-party reference geometry. Current export hashes and actual renders
are pinned in `docs/quality/m16-7-natural/`.


2026-11-06: new paired-knit suit/bib normal and smoothness maps, national albedos, semantic UV mask and returning carry-webbing geometry are project-authored. Colours derive from Assets/Resources/Biathlon/athlete_appearance_v1.json. CGTrader/Trimtex/IBU references are view-only; their photos/paid meshes are not redistributed. Existing CC0 anatomy/wood and CC-BY optional mask provenance remains applicable. Exact new-source hashes and real Unity evidence are pinned in docs/quality/m16-7-textile-kits/qualification.json.
