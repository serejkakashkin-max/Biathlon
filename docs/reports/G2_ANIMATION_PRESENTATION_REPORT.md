# G2 animation and presentation remediation

2026-09-20. **G2-VR-003A technical checkpoint DONE; final motion quality remains OPEN.** This work is post-WP166 remediation, not M13 or G2 approval.

The new glove geometry exposed a mismatch with the old technical glove centre. The hero now finishes rifle contact using its typed grip socket, with a support-hand orientation along the fore-end and a vertical trigger grip. Correction blends over the existing rifle ready-alignment interval. The original rifle adapter exposes its already-computed grip points/alignment; there is no duplicate timing or shooting tuning. The correction preserves rifle world pose and stowed pole positions, and leaves legacy development rigs unchanged.

`Build-G2Rifle.py` generates an original, unbranded stock, butt plate, cheek piece, fore-end, receiver, barrel/sight details and grip in the retained M11 prop coordinates. The static mesh has **3,580 triangles / four material slots**, replacing the hero's ellipsoid-based technical rifle. It is a visual prop, not a new gameplay weapon definition. No external rifle source or texture is imported. [Derivation](../licensing/g2_visual/hero-rifle-derivation.json). Hero generation also regenerated the existing body LODs; geometry counts are unchanged, actual output hashes are refreshed. Offline heat-weight output is not claimed byte-identical.

The M11 preview fixture can now instantiate the actual hero, including bib and late grip correction. Its original-rig mode remains for existing regression checks. Four sets cover locomotion, prone sequence, standing sequence, and exertion/corners. Sixteen hidden 1920×512 captures sample four timestamps per set; nine 1024×1024 isolated hero shots provide closer inspection. Front-facing matrix images show roster slots07→01 from left to right; the sequence samples are pose stages, not an authored motion-capture clip.

## Verification and evidence

- Focused **3/3 PASS**: both stances at negative/zero/positive aim offsets preserve the rifle sight and stowed poles; grip residual below20mm; warmed fit0GC; 400-step setup/exit transitions retain continuous hands and return the rifle to carry; matrix skin vertices finite and poses repeatable.
- Full PlayMode assembly **104/104 PASS**,89.12s, including real-bout observation/phase/sight integration. One intervening MCP call returned an empty `Unknown/0` run; it was not counted as a pass. After test-runner script reload, the complete run above executed and is preserved.
- Windows Development build **244,120,996 bytes,0 errors/0 warnings**. Hidden player **9/9 actual rendered scene/tier cases PASS**. Final Console **0 errors/0 warnings**,compilation_failed=false.
- No authoritative Domain/Simulation/AI/Career/Replay code or shooting parameters changed. Existing full-suite race integration passed; this is not a new balance qualification. OWNER-FEEL-001 remains OPEN.

Evidence: [focused tests](../quality/g2/motion-a/playmode-focused.json), [full tests](../quality/g2/motion-a/playmode-full.json), [Console](../quality/g2/motion-a/console.json), [player](../quality/g2/motion-a/player/verification.json). Build manifest SHA-256:`a8788d4cdf7042de8991351145169fbc5fc638ef6f96ee4285d05d557b60e162`.

## Visual review and remaining actions

The rifle silhouette is clearer; ready-state support/trigger grips align with explicit contacts, and full-body stance/transfer images remain readable. Close elbow/knee deformation is still angular, the procedural motion remains mechanical, and pole contact is a local presentation-plane correction rather than world-locked friction. This checkpoint does not claim production animation quality. Natural weight transfer, skin/joint shaping, equipment/body intersections across more close angles, cloth/skin response and athlete variation require another integrated visual pass.

Next at003A: **G2-VR-004A controlled fir LOD/material import**, then snow/terrain composition. Preserve002/003 visual actions for the combined benchmark review; do not scale this still-incomplete presentation to production venues. GPU-counter availability and unchanged WP166 performance budgets remain tracked in G2_OPEN_ITEMS.

## Integrated review008A —2026-09-23

Записаны48кадров/2s с production chase camera на detached benchmark subject и близкие prone/standing fixtures; отдельно dedicated camera на двух реальных замороженных AI bouts. Клип подтверждает механический шаг, слабый перенос веса и неубедительный контакт палок. Крупные планы сохраняют угловатые суставы и необходимость доработки weapon/body fit. **Visual actions003 OPEN**, [recommend G2 FAIL](G2_VISUAL_BENCHMARK_REVIEW.md); STOP на owner gate. Capture harness не меняет runtime animation или authoritative shooting. Запись24fps не является измерением FPS.
