# M15.5 asset and motion audit — 2026-10-13

Baseline: `d95ef065`, branch `codex/m15-5-visual-upgrade`. Existing local settings
and prior evidence were retained. Budget: $0. Existing asset credits remain at
`Assets/Resources/Biathlon/G2ThirdPartyCredits.txt`.

| Resource | Existing integration | Rights / action |
|---|---|---|
| G2 Hero body | CC0 Blender-authored, 17 bound bones, three LODs | Improve existing rig; do not replace it with a primitive |
| Suit/bib micro maps | Poly Haven bi_stretch / terlenka | CC0; reuse shared materials |
| Rifle, skis, poles | Existing unbranded meshes and bindings | Keep saved colors, bindings and firing geometry |
| G2 fir/pine | LOLIPOP adaptations, texture atlases, three LODs | CC BY 4.0; retain attribution |
| Winter shrub / boulder | Poly Haven shrub_02 and boulder_01 | CC0; reuse instead of downloading duplicates |
| Snow | Poly Haven snow_02, native snow/contact/spray shaders | CC0 maps; improve existing bounded renderer |
| Sky | Passendorf snow 2K, authored sun-neutralized derivative | CC0; use through exclusive WeatherPresentation owner |
| Crowd/stadium | Existing licensed adapters | Keep credits; restricted head source is not a new public asset |
| Nordic Skiing Dataset | Research material, not imported | Commercial motion-derivative rights are unconfirmed; exclude dataset files |
| Rokoko Vision/Create | Evaluated free workflow | Account/eligible footage needed; no mocap export was obtained |

## Research and provenance

The [Nordic Skiing Dataset](https://www.nordicskiingdataset.se/) describes front
and side observations of G2/G3/G4. It is not an asset license. The supplied CVF
paper endpoint returned HTTP 403; no access bypass was attempted.

The primary review [Exhaustive Classification and Review of Techniques](https://www.sciencedirect.com/org/science/article/pii/S1875399X17000044)
identifies G3 as V2: paired pole push on each skate. G4 is V2 alternate: paired
poling on every other skate. [Factors that Influence the Performance of Elite
Sprint Cross-Country Skiers](https://www.diva-portal.org/smash/get/diva2%3A1059424/FULLTEXT01.pdf)
identifies G2/V1 as a moderate/steep uphill technique. These inform timing, not
copied animation data. Technique labels differ between countries; use both names.

`ski_motion_v2.json` is original project-authored normalized key data. Limb paths
use closed quintic interpolation, speed-dependent phase and bounded analytic IK.
It is **manual/procedural animation, not mocap**. The original driver remains
available for a like-for-like comparison and fallback.

The [Rokoko Vision page](https://www.rokoko.com/products/vision) currently advertises
30 seconds of monthly free video processing and FBX export in Create. An optional
owner workflow is: sign into https://create.rokoko.com/ → upload an owned or
commercially licensed stationary-camera full-body skiing clip → process → export
FBX → retain footage permission and export receipt → clean/retarget in local
Blender to the 17-bone mapping. Disable root translation; verify cadence, ski
contact and pole grips in the comparison scene before enabling it. This is an
optional later source, not a prerequisite for this original motion upgrade.

## Baseline evidence

41 selected native PlayMode checks passed; raw results are in
`docs/quality/m15-5/baseline-tests.json`. Ten production-field captures and 240
rendered samples are in `docs/quality/m15-5/before/`.

Hardware: Ryzen 9 9950X3D, RTX 5080, Direct3D11, Unity 6000.6.0f1. The measurement
renders an actual 30-athlete field and 2,588 authored trees into 1920×1080 through
URP SingleCameraRequest; no HUD or display swapchain cost. Baseline mean main
thread 3.491 ms, p95 5.203 ms; mean 682 draws, 115 setpass calls, 6.250M submitted
triangles, 22,398 bytes GC/frame; loaded texture memory 235,226,738 bytes.
GPU timers are unavailable (-1), not zero. This is not a 60 FPS gameplay claim.
The earlier hidden-window measurement lacked rendering counters and is excluded.
