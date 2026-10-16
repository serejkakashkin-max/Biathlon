# M16.1 · Winter venue · owner review

2026-10-16. Implementation and test delivery complete; M16.1 remains OPEN / OWNER
REVIEW. M16 is CLOSED / OWNER APPROVED. M17 has not started. Branch
`codex/m16-1-winter-venue`, accepted starting point `a6bb3e2e`; no main merge.

Run `Builds/M16_1_Winter_Venue_Review/START_TEST.cmd` from the project root.
It opens the existing career game at desktop borderless fullscreen.
Unity 6000.6.0f1 Windows64 Development build, 384799261 bytes, 0 errors,
518 warnings: [actual build receipt](build-summary.json),
[categorized warnings](build-warnings.json). The shared builder's historical
`m15_5_visual_review` receipt ID is retained; [milestone.json](milestone.json)
identifies this M16.1 delivery. The warnings are 496 Sentis shader/compute variant
warnings and 22 FindObjectsByType API deprecations in review adapters. No snow or
flag shader error/warning was reported. This is not a zero-warning build.

## Visual result and geometry

[Open the native comparison gallery](review.html). 19 matched editor camera/tier
captures per phase (38 original PNGs), plus actual compiled-player field, approach
and aiming-camera captures. Images are unretouched Unity output. The static editor
field is frozen at its start; those views do not claim visible 30-athlete racing.
The supplemental player cameras observe the real advancing 30-athlete field.

The before policy is the accepted schema-1 shoulder configuration without the new
national flags. Both runs use the same compiled source and common exact apron
clipping optimization, camera poses, weather and visual animation time. Thus the
performance comparison isolates the new shoulders/flags, not every change since
an old executable. It does not claim an independent frozen-M16-binary benchmark.

* Bends previously switched to vertical curtains, and near-coincident source rows
  introduced sudden cross-row direction changes. A bounded separate rendered rim
  smooths these directions while preserving every original row center, elevation,
  full width and source buffer. Actual full-face terrain clearance constrains it.
  Private mat paths retain exact geometry. Simulation never consumes this copy.
* Shared eight-section rounded cross-profiles, adaptive widths, an eight-metre
  taper and low-frequency closed variation replace abrupt profile/window ends.
  Neighboring roads/shoulders, selected route cuts and apron passages remain guarded.
* Snow colors blend into actual terrain barycentric weights; normals meet the
  actual road and terrain normals. Existing textures, lighting, athlete shadows,
  motion, LODs and quality tiers are preserved.
* The connected start/main cut published coincident same-facing curtains. Exact
  plane/whole-area certificates remove only fully covered faces. Partial, offset
  or opposite-facing faces remain. Exact channel sharing after apron clipping
  avoids triplicating every triangle vertex; no approximate weld is used.

Current MassStart construction diagnostics: 1762 tapered / 2932 retained vertical
panels versus 1027 / 3667 before; 69306 join vertices versus 24905 with the common
clipping optimization (original unshared baseline had 111276). 16 completely
covered curtain faces removed. `MaximumEdgeGapMeters` remains 0.9091877m: it is
the largest source-to-ground separation, not an open-hole measurement. These are
pre-apron counts, **not visible defect counts**. Most retained curtains lie under
overlapping paid roads/private mats and are clipped or covered in the composite.
Main side panels: 1263 tapered / 347 retained; penalty: 186 / 54. Reasons and native
distances are retained in [CSV](edge-diagnostics.csv) and the full
[after geometry/camera receipt](after/review.json). `width_m` is the configured
maximum width; adaptive actual widths may be smaller. No counters were zeroed.

## Range flags

Six 10.5m metallic masts use the actual backstop and 30-bank row frame, including
translation/rotation tests. NOR/SWE/FIN/GER/FRA/ITA patterns use original rect-based
paint definitions. One bounded shared cloth mesh, six owned textures/materials,
reused property blocks and GPU deformation; calm droop, pinned edge, wind response,
fog, matching depth and properly biased two-sided shadows. No active collider or
cloth simulation, no change to shooting wind. National definitions are extensible.

All target planes remain in front of conservative fabric bounds for calm/strong
wind, so every prone/standing mat-to-target ray stays clear. Actual all-LOD tree
canopy bounds and wind margins are checked. This row excludes **zero** existing
trees because the authored clearance is already sufficient; the positive/negative
near-canopy fixtures prove the filter without claiming fictitious removed trees.

## Verification

* 19 selected EditMode cases PASS: visual geometry, terrain planning, venue race
  bindings and five strict historical/current replay policy cases. The MCP wait
  timed out; all actual retained results were recovered after the editor returned
  idle: [native recovery receipt](editmode-recovered-native.json). No inferred pass.
* 56 selected PlayMode cases PASS across latest applicable checkpoints: 8 course
  lifecycle/contact, 13 adversarial/full-terrain, 20 flags, 4 curtain coverage,
  2 rim/policy, 6 legacy/natural all-format apron, 3 full-composite Sprint cases.
  [50-case run](presentation-final-native.json),
  [latest six-case apron run](all-format-apron-final-native.json),
  [full-composite source/contact](composite-contact-final-candidate.json).
  Three earlier apron cases are replaced by the latest six, not added twice.
* Two additional native review executions PASS, one per before/after phase:
  [before](captures-before-final-native.json), [after](captures-after-final-native.json).
* Whole-triangle affine overlay proves real terrain coverage/clearance, valid
  normals/winding, no packed-road intrusion, canonical topology and continuous
  complete boundaries in original and rotated renderer frames. Composite proof
  includes exact apron/route T-junctions and fully hidden original curtains;
  partly exposed/high curtain negatives still fail as intended. No contact bound
  or source boundary assertion was removed. Earlier red/diagnostic receipts remain.
* Warmed 300-call flag animation allocation test: 0 managed bytes. Owned resources
  survive disable/enable and are released on shutdown. Scene generation is cold.
* [Fresh authority regression](race-regression/summary.json): five newly executed
  full 30-athlete races (2 Sprint, 2 Pursuit, 1 MassStart), 121817 ticks / 2400 shots,
  14 real weekly save/reload boundaries. Complete race payloads, event hashes,
  ticks/shots and save hashes exactly match the approved M16 reference. Final save
  `5b9c15db8923236a520ab5888bba925bcf63e68671501716243dfcb466bcfad7`.
  This is five fresh races, not the historical 48-race study or five human races.
* Compiled-player before/after probes PASS with zero runtime errors. Separate
  ordinary desktop fullscreen acceptance PASS at 2560×1440: actual creator,
  save/Continue, disclosed backup recovery, controls/settings reload, diagnostic
  export, checkpoint, personal countdown, tick 6 and abandon. Owner profiles remain
  byte-identical: [receipt](fullscreen-acceptance/review.json).

## Performance and limits

Ryzen 9 9950X3D / RTX 5080 / D3D11. Same 1920×1080 offscreen camera, 30 real
athletes, 2588 trees, identical authority ticks; 90 warmup + 240 measured frames per
case. No menu/HUD/swapchain cost. [Raw receipts](player-after/review.json),
[all counters and distribution statistics](performance-summary.json), repeatable
analysis `scripts/quality/m161_player_metrics.py`.

| Camera | Tier | CPU main before → after (ms) | Frame-time change | Draws before → after |
| --- | --- | --- | --- | --- |
| Bend | Ultra | 1.427 → 1.391 | −2.47% | 402 → 402 |
| Bend | High | 1.290 → 1.287 | −0.13% | 373 → 373 |
| Bend | Performance | 1.162 → 1.149 | −1.03% | 287 → 287 |
| Range | Ultra | 2.183 → 2.233 | +2.28% | 1606 → 1643 |
| Range | High | 2.105 → 2.106 | approximately 0% | 1421 → 1433 |
| Range | Performance | 1.887 → 1.926 | +2.10% | 846 → 858 |

Triangles increased 6.8–8.4% at the bend and 15.5–18.0% at the range; the new static
profile geometry and flag/shadow passes account for the added work. Whole-frame
GC observations stay approximately 164 bytes/frame in both runs (review/profiler
overhead; do not claim the entire game allocates zero). Observed total used memory
is roughly 4.3–4.7GiB, lower in the after run by about 269MiB; collection/heap state
differs, so this is **not** a causal memory-saving claim. The snow vertex-sharing
optimization reduces actual generated vertex count at the prior candidate from
399465 to 69306, without a texture or shader-quality cut.

Unity did not supply valid GPU frame or camera timings for these offscreen D3D11
captures: the report retains null/unavailable. There is no claim of zero GPU cost
or complete GPU qualification. Short frozen-scene measurements on this PC do not
guarantee minimum-spec race FPS or prove every possible lap hitch absent. A single
URP active-render-texture warning at probe shutdown appears in both phases; no
runtime Error/Exception/Assert. Owner in-game art/performance review remains the
next step. An optional demonstration video is not included; original native PNGs
and the playable fullscreen build are provided.
