# M16.7 вЂ” technical asset checkpoint, art goal OPEN

2026-11-05. M16.6 was closed by the owner. M16.7 is IN PROGRESS.
`qualification.json` is the current receipt. Technical pass does not mean final
art acceptance. The editable Blender file and Unity prefab really exist; every
included image is an actual model render, not generated artwork.

Native PlayMode:10PASS/0FAIL/0SKIP,7 new asset cases plus3 existing G2 rig cases.
The entire simulation suite was not rerun. Console0Error/Exception/Assert.
Windows build:Succeeded,0errors/526warnings,
607220878bytes,22.563s. Incremental build warning totals
vary with cached compilation; this is the actual final BuildReport, not a zero-warning claim.
Standalone:exit0,2560x1440 FullScreenWindow,9 nonempty model/UI frames, valid
Humanoid, exactly1LODGroup. Source Blender review:6 views in `blender/`.
Active LOD0:167771triangles. All three stored bodyLODs
plus equipment:253721triangles. Do not confuse stored and active budgets.

`asset-files.json` and `build-files.json` bind the evidence to the actual files.
The local build is not committed. Source files use existing Git LFS rules.
The owner checkout includes pre-existing unrelated render/package/venue settings;
they are preserved and excluded, so clean-clone binary equivalence is not claimed.
24settings files match task-start bytes; the sole independent MCP dependency gate
30в†’34 is required by the installed package, left intact and excluded from Git.

Rejected duplicateLOD and UI-only captures are retained in `rejected/` with an
explanation. Relative render paths initially wrote outside the working directory;
all generator inputs/outputs are now resolved at startup. The final6 Blender
images were regenerated from the current source and visually inspected.

The current asset still has visible collar/skin overlap, jagged panel borders,
coarse cloth/joint topology and a band-like bib. Fingers/grips need redesign.
Skin lacks scanned microdetail. Four inspection poses do not qualify ski motion,
pole-to-snow contact, weapon transfer or race aiming. The procedural shell and
available2K skin source have not reached the professional reference's fidelity;
manual sculpting, retopology and texture work are required. G2 remains the race
athlete until this art work and the new rig's complete animation/contact/ground
integration pass. No race balance or deterministic policy change in this task.

Start test:`Builds/M16_7_Biathlete_Studio_Review/START_TEST.cmd`.
Asset paths/regeneration:the asset's `Documentation/README_RU.md`.
ADRs:ADR-0020/0069/0097. Next:art cleanup, repeat the same views, then full motion
and field-performance qualification. M16.7 must not be marked CLOSED on these results.

Final capture correction: the actual native pose3 comparison exposed a one-frame
GPU-skinning delay in early capture. Submit now occurs after WaitForEndOfFrame.
The independent image probe went red (pose3 raised-sleeve samples79) and green
(pose2=90,pose3=1), with all9 model areas nonempty. Final10 native cases pass,
Console0 errors, new build0errors/526warnings. Four poses are still illustrative;
this correction does not claim a completed shooting animation or correct grip.
