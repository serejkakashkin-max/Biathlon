# ADR-0074 — M16.1 natural snow transitions and decorative range flags

2026-10-16. In implementation; milestone acceptance remains open.

## Authority boundary

Preserve every original ribbon buffer, course definition, route, range mat,
physical corridor, race clock and replay input. The optional schema-2 snow policy
changes the generated joins between the existing ribbon and actual terrain.
It may create a separate rendered ribbon copy with centered cross-row directions
smoothed over a bounded distance. This copy retains every original row center,
height, distance, full width and triangle selection. Each side's movement is capped
by the visual policy (0.75m maximum); the original authoring buffer is untouched.
Typed private mat paths retain their exact geometry. Terrain joins are certified
against this actual rendered copy, with separate original-buffer/center/width
invariants. Physical route sampling does not consume the copy.
Schema 1 retains the legacy join path. No change to Domain/Simulation/AI/Career.

## Snow surface

Use shared cross-profile rows with rounded height and a smooth material blend to
the actual terrain's barycentric material weights and normals. Constrain candidate
widths against all packed surfaces before indexing neighboring shoulders. Further
neighbor checks remain conservative. Width and crest envelopes taper along the
road, including closed seams and optional window ends. Preserve all terrain
coverage, float-triangulation, original-boundary and apron-clearance guards.

Record each active edge panel's loop/distance/rejection reason. These counts are
pre-apron construction diagnostics, **not** a count of visible defects. Remaining
exposed seams must still be qualified; no zero-wall claim is made.

## Decorative flags

An optional range resource supplies six extensible national patterns and bounded
mast/fabric/animation dimensions. Placement derives from the actual target row,
range direction and backstop size; it is tested under translation and rotation.
GPU-deformed cloth pins its mast edge, droops in calm air and responds to existing
visual wind. This never writes shooting wind or creates gameplay colliders.
The shader has matching forward, shadow and depth deformation and participates in
fog. Shared cloth geometry, six owned textures/materials and reused property blocks
avoid per-frame object/material creation. Original patterns use paint rectangles;
no third-party logos or downloaded images are imported.

Tree exclusion uses actual fir/pine bounds across all LODs plus their wind margin
and the complete flag sweep. It filters presentation instances at initialization;
authored tree data and terrain remain unchanged. Empty filtered groups are omitted.

## Qualification

Native RangeFlagTests: 20 executed cases pass. Native SprintCurvedSnowBankTests:
3 pass, including actual terrain contact in original and rotated renderer frames.
The crest assertion now measures height directly instead of recognizing an old
material-alpha value; its geometric threshold and contact assertions remain.

The capture host explicitly owns render submission to avoid submitting the forest
twice. Editor offscreen counters are exploratory; missing counters are reported as
unavailable, and no isolated GPU timing is inferred from a global Editor sample.
Standalone performance, full-route visual acceptance and fresh complete race hash
regressions remain required before delivery.

### 2026-10-16 final geometry qualification

Constrain rim rotations against every actual terrain/road triangle overlap before
building joins: preserve at least the existing 25mm full-face ground clearance.
Failed preparation restores the previous meshes/renderers and diagnostics, so a
retry cannot publish half a venue. Exact position/normal/UV/color sharing after
apron clipping reduces redundant vertices without approximate welding or changed
faces. Normalize interpolated normals once after clipping.

Whole-composite tests exposed duplicate coincident curtains at the main/start
cut. Remove only completely covered, exactly coplanar, same-facing vertical faces
with a double-precision polygon-subtraction certificate. Partial, opposite-facing
and offset faces remain. Keep the pre-clipping wall counters; report actual removed
faces separately. Four strict coverage fixtures pass.

The independent contact oracle now also certifies a complete edge terminating
inside an actual neighboring triangle (including perpendicular apron T-junctions),
and actual roofs on both sides of an internal original curtain. It still rejects
partial/high curtains, buried faces, ground gaps and duplicate/non-manifold faces.
An apron triangle spanning both half-planes is classified by its three vertices,
not only its centroid. No clearance bound or native boundary proof is removed.

Native full-composite source/contact tests pass at yaw 0/45. Six apron tests cover
Sprint/Pursuit/MassStart with legacy and natural geometry in transformed frames.
Final focused presentation run 50 cases passes; latest six-case apron run replaces
its three earlier legacy cases. All 19 selected EditMode cases finished Passed;
the MCP wait timed out, and retained native testResults were recovered after idle.
Matched native review passes in both phases, 19 camera/tier captures per phase.
Actual five-race/14-week authority regression is byte-exact against approved M16
(121817 ticks, 2400 shots). Final Windows delivery and player measurements are in
`docs/quality/m16-1/README.md`: 0 build errors, categorized 518 warnings, ordinary
fullscreen acceptance and both 30-athlete comparison probes pass. Observed frame
time -2.47%..+2.28% across the two cameras/three tiers; GPU timing unavailable.
No ordinary-game/minimum-spec FPS or zero-GPU-cost claim. Owner art review remains.

Official pattern/reference sources consulted:
- https://www.norden.org/en/information/nordic-flags
- https://www.biathlonworld.com/ru/discover-biathlon/why-we-love-it/fan-tv-favourite
- https://www.biathlon-oberhof.de/aktuelles/alles-im-lot/

### 2026-10-30 — M16.2 format-cut snow continuity

Owner reports remaining sharp snow ends in Sprint/Pursuit. A buried candidate rim
cannot reserve an impossible shoulder footprint: apply the existing complete
packed-footprint predicate before indexing that candidate. All original terrain,
neighbor, float and private-mat guards remain.

Main's actual selected panels have two transverse cuts where narrower format
branches join. Longitudinal side banks do not cover the complete width of these
ends. Extend the existing cold bank builder locally at selected cuts of a closed
loop. Split the original cut at actual packed-boundary crossings; subdivide using
the existing policy's width/cross-section count. Reduce candidate widths against
the complete selected packed/apron footprint. Generate rounded profiles from the
original edge toward actual terrain using the existing terrain-clip, coverage,
world-float and closure guards. Cancel equal opposing internal profile curtains
before publication; close exposed profiles and retain original curtains only
where rounded profiles cannot fit. Include the ends of the longitudinal banks
at these format cuts, so their raised outer contour cannot retain an abrupt wall.
Keep original packed buffers, triangle selections, row
centers/heights/full widths, private mats and authoritative physical sampling.

This adds geometry to the existing owned skirt mesh/material at initialization.
No extra renderer, collider, texture, data binding, resource cache, external runtime
dependency or per-tick geometry. Uncut MassStart has no transverse-cap work.

Also round the actual open format-branch ends. Qualify a branch using explicit
directed connection ports at both endpoints to closed race loops, excluding typed
private mat paths; start approach and range paths do not qualify. A single cold
index of all generated roofs removes an interior curtain only with complete
projected edge matches on both sides and sufficient endpoint heights. Match
endpoints at one native float step of the generated horizontal extent, and admit
one downward representable height step. Partial, visible-offset, lower and
single-sided roofs cannot remove a wall. This avoids retaining
a non-manifold internal wall where two accepted snow profiles already meet. Roof
geometry and source coordinates remain exact; no position welding. Nine new
unit cases and actual whole-format contact tests certify this filter. All pieces
of each cut reuse its original complete cross-row direction; independent short
chord normalization had moved otherwise shared feet by float bits. A targeted
producer trace confirmed that cause and was removed after the direction fix.

### 2026-10-31 — free-space fans and convex cap closure

Keep packed-boundary intersections in double precision for admission. First
compute both incident free-space fans independently, then constrain shared row
widths; do not greedily erase one endpoint and thereby erase the next free strip.
The existing width/minimum/cross-section policy bounds this process. A zero-width
profile collapses to its original top contact, and exposed outer fan edges close
against actual terrain. No snow may enter the complete paid/apron footprint.

Within the existing end-fade distance of actual format cuts, a prepared natural
bank can survive a convex terrain crossing at its end. Clip the closing curtain
to its above-ground portions, matching the already terrain-clipped roof; do not
reject the whole bank and restore a large vertical wall. The original packed
edge clearance guard remains mandatory. A decorative bank-profile chord can
begin below ground and is clipped there; it is not an authored packed edge.
All actual terrain-coverage, float-face and full contact/topology checks remain.

The photographed Sprint return now has an additional independent outward-roof
witness outside every paid ribbon and apron, plus an inside-road negative
control. The original trial point was proved to be inside the paid Main ribbon
and corrected from actual mesh data. The corrected witness is red for the
constant/greedy-width candidates and green for shared independent fan bounds.

Experimental whole-patch clipping and offline constrained authoring were rejected
by the unchanged contact/topology checks and visual review. They are not shipped.
The seven actual-format full-contact fixtures remain enabled, including rotated
Sprint/Pursuit, both career finish-runout variants and uncut MassStart. Their
original-source and whole-rendered-composite certificate remains unchanged; no
clearance/topology tolerance is relaxed. Native views and compiled fullscreen
ordinary-player qualification remain delivery requirements.
