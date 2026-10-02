# M13 production path integration

2026-10-01. WP169 prerequisite after explicit owner G2 approval. The authoritative Master Plan remains unchanged: WP169–174 produce six distinct original venues; WP175 weather matrix, WP176 calendar, WP177 balance and WP178 performance complete M13. This document records the implementation checkpoint and the remaining content work. It does not certify a production venue.

## Implemented authoring and adapter checkpoint

`VenueManifest` has an explicit schema2 branch. It requires `paths_resource` and `occupancy_resource`, and rejects `lanes_resource`. Schema1 remains legacy-lanes-only and emits no new optional fields. Resource IDs retain the existing extensionless safe relative-path contract. `VenueAuthoringPlan` loads the exact track, junction and connection definitions before binding `PhysicalRangePathDefinition`, and reads canonical `OccupancyDefinition` rather than copying body tuning into a venue policy.

The plan exposes `UsesPhysicalPaths`, `Paths`, `Occupancy`, `LaneCount`, `LaneId(index)` and `MatPoint(index)`. A schema2 mat point comes from its authored physical `MatAnchor`; it does not use the old shared firing scalar. Schema1 `Lanes` remains available to existing consumers; schema2 consumers must use the new branch explicitly.

`VenueValidator` dispatches schema2 to `VenueRangePathWorldValidation`; schema1 behavior remains unchanged. Schema2 checks actual world edge distance/grade, widths, configured body containment, mat slot alignment, side-entry/apron anchors, directed penalty access, coincident connection seams, missing assets and capacity. Private loops are distinguished from loops shared by multiple lane paths; a shared exit aisle retains the main corridor width minimum.

World conflict checks clip actual-width corridor rectangles, including configured personal clearance. They detect overlap between different physical loops and nonadjacent parts of the same loop, including parallel corridors whose centrelines never intersect. Vertically separated bridges retain the policy's vertical clearance. Every overlap requires guard coverage for both affected distance intervals in one shared conflict region. Required intervals include the configured body half-length and clearance; multiple overlapping guard windows within that same region may jointly cover an interval. Distant pairs are rejected by allocation-free bounds checks before polygon allocation. This is cold authoring validation, not a hot simulation loop.

An exclusive region actually containing multiple authored mat forks receives `range.shared_entry_gate`, including interval membership rather than only named port anchors. Adjacent collinear retracing and reversal are rejected. Renaming physical loops cannot hide their world overlap, and merely adding a decorative entrance/exit anchor cannot fix a gap. No width, distance, body definition or guard is silently changed.

Final I1 verification: .NET70/70 authoring/world/local-guard/legacy tests; Unity schema5/5, world11/11, local guards14/14 and isolated Editor authoring7/7. Mat4/4 and proxy2/2 PlayMode tests verify physical anchors, stable IDs, explicit shooting direction and read-only authority. The positive one-lane world fixture uses deliberately broad single-lane regions and a test-only capacity override; production policy correctly reports insufficient capacity. It is not a30-mat runtime admission proof. I1/I2 final full PlayMode176/176 passes. Evidence and source hashes: `docs/quality/m13/venue-path-v2-verification.json`.

## Existing pipeline boundaries

The WP167 authoring tool now consumes individual physical mat anchors for schema2 and preserves its schema1 branch. Generation uses an isolated preview scene and preserves caller selection and scenes. WP168 legacy geometry diagnostics remain available for its fixtures. Schema2 optionally binds a distinct open start approach with an exact directed end-to-main seam.

`ChampionshipRaceFactory` and `PlayableSprintHost` remain synthetic composition roots. Separate `ProductionVenueRaceFactory` and `ProductionVenueHost` bind exact profiles, routes, conditions, paths and actual start geometry. The production host follows the accepted physical corridor width and explicit shooting direction; snapshots sample the actual private-path grade and curvature. The factory currently requires one covered path per entrant. Production Pursuit capacity still needs explicit60-entrant reuse of30 mats, not60 authored firing lanes.

I2 authored-start8/8 focused and36/36 start/pack/replay/legacy Mass Start regression pass; factory/snapshot/AI10/10 pass. Unity authored-start8/8, factory3/3 and host2/2 pass. These narrow contracts do not validate production density. The actual Venue01 draft currently rejects its30-actor grid because one exclusive start region contains three otherwise safely spaced bodies. A separate opt-in authoritative world-corridor continuation proof is required before allowing parallel motion there; unrelated crossing/terminal traffic and retained tails must remain protected.

The G2 native art resources are reusable, but benchmark placements are not venue-generic. `G2BenchmarkSurface`, `G2GroundDressing`, `G2Understory`, `G2ForestEdge` and `G2BenchmarkBackdrop` contain fixed route/extent/height assumptions. `TrackRibbonBuilder.Build`, `TerrainBlendMesh.Build`, vegetation initialization with explicit authored instances/height, weather/lighting, athlete/contact/VFX and target assets provide existing implementation hooks. Terrain must be chunked around the100,000-vertex builder limit; vegetation can use bounded spatial clusters rather than one venue-wide array.

## Stadium topology and geometry

Use a shared right-to-left entry aisle behind the mats, equal-length translated private mat paths, and a shared right-to-left exit aisle leading to penalty access. For fork coordinate `x_i`, equal physical travel follows `x_i + D + (W - x_i) = W + D`. The final-lap bypass needs the same actual travel through an outer connector. Distances and grades should derive from the authored world polyline, with whole-millimetre segment and slice coordinates.

Two parallel aisles behind the firing line require private incoming or outgoing traffic to cross one aisle in a planar layout. Every such crossing needs a local guard, as do forks and merges. A global exclusive region shared by all30 forks is not independent mat access. The actual Pack integration must prove incoming tails, occupied mats, accepted motion, simultaneous penalty traffic, stable accepted arrival ordering and replay behavior. Static world PASS alone cannot prove these properties.

`TrackVisualLoop.Sample` selects the outgoing tangent at a knot. A rear-apron mat turn can therefore return a departing heading while the athlete must shoot toward the targets. Mat dressing should use the authored physical position and explicit `RangeLayout.Forward`; shooting setup may ease presentation yaw to that direction without moving authoritative body coordinates. A skiing path must not continue through the firing corridor merely to preserve the locomotion heading.

Production route distances need explicit format/lap bindings. One repeated whole-millimetre lap cannot divide10,000m Sprint equally by3. Authored first/final connector configurations can produce an exact10,000m total; silently shipping9,999.999m or the existing3,000m test profile is not acceptable. Pursuit12,500m/5 and Mass Start15,000m/5 divide exactly. Every composed route section still needs world seam checks.

## Original venue identities

1. Venue01: expand the approved woodland-basin benchmark into a complete course, with mixed climb/descent and a sheltered recovery approach.
2. Venue02: sustained climbing in a dense fir valley with an elevated stadium.
3. Venue03: exposed flats/drafting on an open snowy plateau with sparse pine groups.
4. Venue04: technical descent and restricted passing in a rock-lined forest gorge.
5. Venue05: repeated rolling effort transitions in broad pine woodland.
6. Venue06: a late climb and exposed final approach in a highland bowl.

These are implementation candidates within the existing original-venue scope, not new owner commitments. Their authored geometry, elevation, exposure, passing opportunities, semantic zones and composition must differ. A new seed or palette alone does not prove identity.

## Licensed resources and blockers

Audited G2 hero, goggles/rifle/bib, snow, fir/pine LODs, rock/shrub LODs, grandstand/canopy, event dressing, architecture materials and modified sky are available. Existing attribution and derivation records remain authoritative. CC BY notices ship in `G2ThirdPartyCredits.txt`; Poly Haven and selected3dassets derivatives retain their documented CC0 sources.

Restricted Renderpeople crowd/head derivatives remain local ignored content, with documented generation paths and compiled-build use. Do not publish their source geometry/textures. Simplified crowd was already owner-approved. The unavailable Gumroad crowd pack and metadata-only Asset Store candidates are unnecessary to continue M13. No owner/license blocker was identified by this audit.

## Remaining acceptance

Pack path support and path-aware Unity authoring/mats are implemented through I1/I2. The original Venue01 draft has a3000m closed course,150m penalty loop,30 individual mats and a40m actual start approach. Its static production policy passes, but dense start flow and environment rendering remain unverified. Finish safe start continuation, reusable mat admission and exact format routes. Prove CLI/Unity static report parity,30-athlete Mass Start and60-athlete Pursuit completion, occupied mats, penalty concurrency, deterministic replay and fixed in-motion/camera captures. Then author the other five venues with the same proofs and distinct tactical/visual identities.

WP175 requires each venue in clear/overcast/snowfall and relevant lighting variants, with readability, contact and cleanup checks. WP176 binds14–18 season events to actual venue IDs and valid Sprint/Pursuit dependencies. WP177 compares course times, climbing, drafting and difficulty under fixed seeded workloads. WP178 profiles worst-case30-athlete and broadcast scenes. Hidden-only GPU/frame-counter limitations must remain explicit; they are not evidence of a measured60FPS result.

The optional authored path/start contexts change which actual course is traversed. Legacy contexts, race tuning, physics, shooting equations and the current playable delivery remain unchanged. G2 remains APPROVED; WP169 and M13 remain IN_PROGRESS. OWNER-FEEL-001 remains OPEN.
