# ADR-0078 — M16.4 continuous visible scenery

2026-10-31. Owner reports empty sections of visible stands and trees appearing on
approach, and requires continuous visibility without wasting rendering resources.

## Reproduction and cause

`VenueDistantContinuityTests` initially fails3/4cases in2.66s on the native path:
an actual168-seat production grandstand at270m has0visible spectators, and three
in-frustum native fir/pine silhouettes at210–240m have0visible instances on Low
and Balanced. High's existing optional fog policy keeps those three trees.
`SpectatorCrowd` hard-culls individual seat pivots at110m; ordinary forest adapters
hard-cull at120m. These distances cut scenery while its stand/world remains
visible. The old density tiers also remove authored people/trees. LOD changes and
frustum bounds were considered separately: the counts reproduce the missing
audience/forest even with the complete stand/canopy inside the camera frustum.

## Production-only policy

Add the optional versioned `visibility_resource` to the environment recipe. The
new resource's `complete_fog_frustum_v1` mode preserves every authored crowd/tree
instance on all tiers. Lower tiers control detailed LOD/shadow work rather than
leaving empty seats/forest holes: instances previously removed by the stable
density rank use the existing far model. Existing close high-quality geometry,
textures, shaders, quality pipeline and shadow decisions stay unchanged for the
previously retained instances. No added detailed shadows for far silhouettes.

Initial matched measurements showed that keeping the old816-triangle spectator
LOD beyond its former110m limit added unnecessary distant work. Bake an additive
136-triangle seated/standing silhouette from the same original authoring body
proportions, pose, garment palette and shader channels. It uses six vertices per
original body part. Select this third LOD only when projection-adjusted distance
passes the former culling limit. ExistingLOD0/1meshes are not regenerated, and
zoom retains the more detailed original models. Distant spectator triangles drop
by83.3% without deleting people. The original crowd shader receives the existing
far/no-cheer setting for both distant levels; no reversed arm motion from passing
a third-LOD number into its two-state animation channel. The optional audited
legacy crowd retains its own original two meshes and behavior.

The new bound is the existing weather's camera-depth linear-fog horizon, including
its existing scale and snowfall/light-snow cycle maximum. Retain any complete
canopy/wind or spectator-bound support in front of the fog plane. The far camera
plane and ordinary frustum still reject invisible work. Do not add fog or reduce
its distance to disguise cutoffs. Orthographic/physical/custom projections that
cannot certify the standard fog-depth mapping retain their whole frustum.
This extends the old High-only opt-in to explicit all-tier production continuity;
legacy benchmark/fixture adapters without the new policy keep their old contracts.
Old High-only tests explicitly omit the new binding and keep all original
invariants; separate regressions certify the new production behavior.

Small winter-dressing clusters use the same new horizon and their existing near/
far models, replacing their old radial cutoff on opted-in venues. Extend the
submitted draw bounds to include every retained decoration; do not leave the old
radius-sized batch bounds around a longer-distance batch. Existing route/coach
clearance and placement remain unchanged.

## Resource control

Reuse native meshes/materials, bounded matrix arrays, instanced batches and spatial
forest groups. Cache spectators' static world matrices and conservative bounds,
invalidating only when their seating transform changes. Keep the old all-LOD
forest transform cache. Do not allocate per tree/seat/frame or add GameObjects,
cameras, transparency passes or runtime mesh baking. [Unity's instanced draw
contract](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Graphics.RenderMeshInstanced.html)
supports group-level bounds, so the CPU's actual-instance frustum/fog filtering
remains necessary. No expectation of zero extra GPU work: preserving previously
missing silhouettes has a finite measured cost.

The opt-in player benchmark can compare legacy vs complete visibility using the
same compiled binary,30real athletes, accepted tick, weather, two camera poses and
three tiers. It records native frame counters and exact submitted crowd/tree/
dressing counts, LOD triangles and shadow source triangles. This isolates scenery
cost; it is not a full HUD/menu/swapchain race FPS certification. Evidence, actual
screenshots and limitations: `docs/quality/m16-4/visibility/README.md`.

No Domain/Simulation/AI/Career/Replay, paid course/terrain/tree/seat placement,
athlete animation, race balance, shooting, replay events or save schema changes.
