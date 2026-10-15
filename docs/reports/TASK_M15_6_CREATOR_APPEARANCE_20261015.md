# M15.6 — live character appearance

The creator reuses one native G2 athlete, one camera/RenderTexture and private
lights. It exposes6 faces,8 skin tones,3 builds,170–198cm height, shoulder width,
2 short hairstyles,6 hair colors,4 eye colors, cap/headband/no headwear,
worn/raised goggles, lens coatings, gloves, boots and training accents. The same
versioned appearance is restored from the career and applied to actual race rigs.
Official national kit and leader bib remain authoritative presentation choices;
training colors do not replace those roles or grant free equipment ability.

The model retains one SkinnedMeshRenderer,8 material slots and3 LODs. The first13
approved movement/build/face morph channels are preserved;5 cosmetic channels are
appended. Eyes derive from the same audited Blender Studio CC0 source as the
approved body. Eyelids, brow/scalp/headband derivatives and hair shading are
documented in `docs/licensing/g2_visual/creator/`. Hair is deliberately limited
to two short styles; no facial-hair control is advertised.

AppearanceProfile detail schema2 is additive. Historical explicit schema1 and
legacy identities retain canonical JSON, saved stats and random checkpoints.
Dimensions are visual only: scaled grip/prop offsets preserve the approved
M15.5 motion, rifle carry and shooting ray. No new capability multiplier exists.

Native verification:5 appearance cases,8 rifle/contact cases,1 authoring
idempotence case,8 approved arm-motion cases and the complete own/existing
UI flows pass. Own flow selects every cosmetic category, confirms, reloads and
checks the exact profile on the actual production race athlete. Its compact
variant also checks whole confirmation/back buttons inside clipping bounds at
1024×768 and130% text. CoreCLR appearance/migration9/9 pass.

Initial rifle-height4 failures were a measurement error: the default BakeMesh
overload plus TransformPoint applied ancestor scale twice in this Unity runtime.
The corrected conversion is independently checked against weighted bind-pose
skinning at both height extremes (over100 vertices per height,0.1mm tolerance).
The original18mm rifle/back bound is unchanged; production carry anchors were
not shifted to satisfy the test. Initial bulbous eyes/eyelid shading and hard
hair edges were visually rejected and corrected before final native captures.

Real screenshots are in `docs/quality/m15-6/`. Full native studio/face/integration
regressions and final Windows acceptance are recorded in the delivery evidence;
this report does not replace the owner's final visual decision.
