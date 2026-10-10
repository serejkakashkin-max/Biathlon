# ADR-0047 — M13 scenery submission and snow support

2026-10-10. Accepted for owner testing; M13 remains OPEN.

## Context

Owner reports hitches at shooting preparation and floating roadside scenery.
The compressed course retained terrain heights from the older layout. Exact
ribbon clearance alone removed protrusions but left deep cuts and raised edges.
The range view also submits thousands of draws for stationary decoration.

## Decision

Fit the existing visual terrain to the current corridor heights using the
versioned `woodland_shoulders_v1.json` authoring policy. A six metre shoulder
outside each road stays close to road height; surrounding snow approaches the
original terrain with a bounded 0.28 bank grade. The existing exact triangle
clearance pass runs afterwards. Reproject tree heights; retain the existing
runtime projection of individual standing spectator anchors onto actual ground.
Race paths, scoring, ski surfaces and course widths do not change.

Combine explicitly selected stationary MeshRenderers into material/shadow/layer
groups in 32m spatial cells. Preserve world geometry and source materials.
Exclude dynamic target faces, moving wind fabric, characters, TextMeshes and
per-object property blocks. Keep original renderers for ownership/cleanup and
release generated meshes with their owner. Spatial grouping bounds the culling
cost instead of merging the complete venue into one mesh.

Set a data-driven 60 FPS presentation target through the existing quality lease;
restore the previous application target when the last lease releases. The
current desktop profiles disable vsync. This avoids continuously spending the
available CPU/GPU budget on uncapped rendering. Fixed 20Hz simulation is intact.

## Evidence and limits

Native geometry/exclusion tests2, quality/lease tests3 and environment tests7
pass. Three focused core startup/control cases pass. Terrain checks now reject
the previous deep road/ground gaps. Two native roadside captures were inspected.

On the frozen first-range view, initial High draw counters averaged about3953
during preparation and2835 during aiming. After batching these were2567 and1829
(approximately35% fewer submissions). Coarser culling increases submitted
triangles in this view; the measured GPU work still stays around1–2ms.
The final deduplicated 59-frame windows had maximum frame times below4ms while
physics was frozen. They are rendering evidence, not a full-race FPS guarantee.
The separate active-host probe measured mean6.13ms / max11.03ms for90 range
steps including publication. This workload did not reproduce the owner's long
visible hitch. Keep owner acceptance pending; do not claim all hitches eliminated.

The first exploratory frame CSV used editor callbacks rather than unique frames
and captured screenshots, so its wall timings cannot be compared to the final
capture. Only its draw counters are used above. GPU timing columns in that raw
CSV used the local decimal separator; the final CSV uses invariant formatting.
The temporary frame probe is retained as evidence outside Assets and removed
from the shipping editor scripts. See `docs/quality/m13/perf-ground-20261010/`.
