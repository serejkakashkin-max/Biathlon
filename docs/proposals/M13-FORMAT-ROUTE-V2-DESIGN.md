# M13 exact format routes on one physical venue

Status: owner-authorized implementation design, 2026-10-02. This supplements the Master Plan; it does not complete WP169 or approve a production venue. Root owns Domain/Simulation route integration. The content generator owns actual world geometry and resource proofs.

## Problem and contract

One 3,000 m physical Main loop cannot represent all of men's Sprint 10,000 m/3 laps, Pursuit 12,500 m/5 laps and Mass Start 15,000 m/5 laps using whole-loop repetitions. Copying Main into several overlapping loop identities loses physical traffic identity; relabeling the distances changes travel without changing world geometry. Existing schema1 routes remain valid for their authored whole-loop fixtures.

Schema2 `RaceRoutePlan` uses `laps[].slices` with `{loop_id,start_m,end_m,enter_connection_id?}`. Endpoints are whole millimetres, positive, directed, nonwrapping and within the actual physical loop. A lap may reuse portions of the same physical Main identity. Open branch identities exist only where the route actually departs from Main.

Each different-loop transition names an existing directed connection whose from/to ports exactly match the preceding slice endpoint and next slice start. World knots must also coincide. Same-loop adjacent slices and a closed-loop length-to-zero wrap remain continuous travel. Neither connections nor lap boundaries grant distance. The first slice has no entering connection; the bound start approach still joins Main0 and contributes no counted competition distance.

For a route section with cumulative start `R` and physical slice start `L`, physical coordinates are `L + (routeDistance - R)`. A physical anchor `a` maps back to `R + (a - L)`. All range, checkpoint, penalty, perception, motion, replay and presentation code must use these maps. In particular, repeated Main slices must not each be classified as range access merely because their loop IDs match.

A shooting lap contains one full range bundle, including the personal path replacement and penalty rejoin, then actual finish travel. Venue01 retains Main range overlay2714.592–2950 m and a50 m counted finish lead. Its thirty mats, side approach, explicit +Z shooting direction, equal235.408 m lane paths and150 m penalty circuit are shared unchanged by all formats. Pursuit60 entrants reuse thirty mats; that is allocator/admission work, not a sixty-mat asset requirement.

## Authored Venue01 slices

The original `main` remains exactly3,000 m. Two open branches are connected at real interior Main anchors, with one local guard at each departure/return. The branch ends include short actual edges aligned with the incident Main tangent; nearby corridor overlap is explicitly protected and is not extended into a venue-wide gate.

| Format | Counted slices per ordinary lap | Lap distance | Race distance |
| --- | --- | ---: | ---: |
| Mass Start | Main0–3000 |3000 m |15000 m /5 laps |
| Pursuit | Main0–597.048; `pursuit_shortcut`0–883.344; Main1980.392–3000 |2500 m |12500 m /5 laps |
| Sprint | Main0–549.048; `sprint_extension`0–513.333; Main729.048–3000 |3333.333 m |10000 m /3 laps, including below |

Pursuit replaces1383.344 m of the outer bowl with an883.344 m curved traverse, saving exactly500 m. Both seam endpoints lie at58% of the Main climb rise; their world Z difference is5.04 m. The two inward turns and traverse occupy the basin interior. Their actual Y is derived from the incident climb/descent planes, preserving connected height at the junctions.

Sprint replaces a180 m mid-climb Main interval with an actual513.333 m western forest excursion, adding333.333 m. Its four rounded turns and two traverses follow the original hill-height plane as a function of world Z. It adds a different physical tactical section rather than an extra start lap. Curvature and grade derive from the final authored world chords.

At whole-mm resolution10,000,000 mm is not divisible by three. Sprint's third lap therefore has the ordinary slices plus a final closed-Main wrap and actual Main0–0.001 m continuation. Its lap length is3333.334 m; first two laps are3333.333 m. This is owner-authorized paid finish travel. The finish marker/camera must derive the actual route endpoint. No sub-mm rounding, free distance or9999.999 m profile is accepted.

The Pursuit return Main section has cumulative section start1480.392 m and physical `LoopStart`1980.392 m. Its mapping origin is therefore−500 m: logical range entry2719.592 maps to race2219.592 m. The corresponding Sprint mapping origin is+333.333 m. These nonzero offsets are mandatory regression cases.

## Resources and static binding

The generator retains `track`, `visual`, `paths`, `junctions`, `connections`, `manifest`, `start_approach`, `start_continuation` under `Biathlon/Venues/woodland_basin_v1`. Full-format draft output is separate from the imported Mass-only checkpoint until validation succeeds. It adds profiles and schema2 route resources for Sprint/Pursuit, and appends only the two actual open branch loop identities and their four directed seams.

An explicit venue format catalog binds profile and route resources. Recommended manifest field: `format_bindings_resource`; catalog shape: `{schema_version:1,id,venue_id,track_id,formats:[{id,profile_resource,route_resource}]}`. Cold venue loading validates track/profile IDs, exact route distance/laps, directed topology and world connections for every binding. The world checker treats an open race loop as authored only when used by a validated catalog route, range path or bound start approach. An unbound extra open loop remains an error. Until this binding is implemented, the full-format draft must report that dependency; it must not set `closed:true` or suppress `loop.open`.

All protected corridors, including branches, enter terrain-height sampling and forest exclusion. Separate arrays prevent phantom links. The height generator uses actual counted route edges and their world Y, then blends off route into the basin profile; it does not use the old G2 surface. Terrain bounds expand from the actual full set of knots. Existing licensed fir/pine/snow/stadium resources and attribution records remain authoritative.

## Exact world chord authoring

Draft smooth control points establish shape. The generator chooses actual whole-mm chord lengths whose sum equals the prescribed branch distance. It keeps alternating control points fixed and solves intervening points from the intersection of two spheres with those chord radii. The nearest point on that circle preserves the original curve plane. A bounded small amount of real geometric curvature on a long traverse pays any mm remainder; declared distances are never substituted for unchanged world chords.

Every generated edge is independently checked for actual3D distance against declared whole-mm length, Y/horizontal grade, endpoint world gap, incident tangent alignment, and route sum. Curvature derives from the final signed horizontal turn, not the pre-quantization ideal arc. Local overlap checks use full authored corridor widths and canonical body clearance. If a small geometric adjustment produces a new overlap, it must receive an explicit local guard or a corrected shape.

## Required acceptance

- Preserve schema1 route regression and legacy fixtures.
- Reject unknown/out-of-bounds/non-mm slices, open implicit joins, wrong/reversed connection IDs, partial/duplicate range bundles and missing real travel after penalty rejoin.
- Prove exact10000/12500/15000 m route sums, actual world seams and complete30-path replacement for each shooting lap.
- Exercise offset sampling, checkpoints, range arrival, private lane movement, penalty/rejoin, last-lap bypass, final finish projection and recorded replay context for all three formats.
- Run deterministic repeats using actual production definitions; prove Mass Start30 dense-grid release and Pursuit60 reuse of thirty mats separately.
- Compare static CLI/Unity diagnostics; inspect full terrain/forest corridors, cameras and actual side range arrival in Unity. Static recipe PASS alone does not certify playable or visual acceptance.

Related decisions: ADR0003 track coordinates/routes; ADR0011 physical identities/seams/tails; ADR0024 personal range paths; the M13 start-continuation contract and TASK169I1/I2/J reports. No authoritative shooting/effort balance constants are changed by this content design. Actual route geometry changes race outcomes and requires deterministic production regressions.
