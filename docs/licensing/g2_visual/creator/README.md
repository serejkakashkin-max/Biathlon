# M15.6 creator appearance derivation

2026-10-15. Reuses the approved G2 Hero rather than replacing its body or motion.

- G2-001, Blender Human Base Meshes bundle v1.4.1, CC0-1.0. Existing audited source
  SHA256 `3c121505651140ceb4d69fd1d8923f7788ffadd81672f5be14845a5f2c75c137`.
  The selected source objects are `GEO-body_male_realistic`,
  `GEO-body_male_realistic.eye.R`, `GEO-body_male_realistic.eye.L`.
- `scripts/art/Build-G2CreatorEyes.py` runs in Blender with factory settings and
  autoexec disabled. Only selected geometry is loaded. Source materials, textures,
  modifiers, scripts and unrelated objects are not imported into Unity.
- Eyes are fitted to the retained G2 Head mapping, inset2mm and reduced for3LODs.
  Original project-authored eyelid rings conform to the retained face surface and
  use interpolated face normals. Small brow strips and a closed scalp reuse the
  existing Skin slot. A separate fitted headband uses the retained Cap slot.
- `AthleteCreatorAppearanceAuthoring` appends vertices/channels without altering
  the original vertex prefix, skin weights, bindposes, triangles or first13 morphs.
  Five additive cosmetic morphs: narrow/wide shoulders, crew/swept hair, raised goggles.
  The retained CC BY4.0 G2-032 goggles geometry keeps its existing attribution.
- Runtime keeps one SkinnedMeshRenderer,8 material slots and3 LODs. Hair/material
  colors and bounded height are cosmetics; no equipment purchase/stat is granted.
  National kits and bibs retain authority in official races. Training accent is a
  preview/career choice and cannot repaint official national race clothing.
- Deliberately limited short crew/swept choices; no facial hair is advertised. The
  creator adds no real-person portraits/likenesses or restricted hero-head derivative.

Generated intermediates and source archives stay under artifacts/external_downloads;
only Unity derivatives and this provenance ship. Native tests preserve first13
morph channels and independently verify scaled skin coordinates/stock-back contact.
