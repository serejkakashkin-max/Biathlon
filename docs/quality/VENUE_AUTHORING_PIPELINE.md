# Venue authoring pipeline — WP167

G2 APPROVED 2026-09-30. Master Plan M13 is active. See ADR0023.

## Inputs

Author `Assets/Resources/Biathlon/Venues/<stable_id>.json` with `schema_version:1` and a stable `id`.

| Field | Meaning |
|---|---|
| track_resource | Existing authoritative TrackDefinition JSON, relative Resources ID without extension |
| visual_resource | World knots bound to the exact track ID and loop distances |
| lanes_resource | PhysicalRangeLaneDefinition; equal-length branch contract preserved |
| junctions_resource / connections_resource | Shared collision/admission ports and directed seams; existing snapshot checks preserved |
| range_layout | Explicit side_entry requirement, entry/exit anchors, firing origin, unit horizontal forward, lane pitch and rear apron depth |
| cameras | Stable ID, track anchor, world-axis offset/look_offset, FOV; generated Camera starts disabled |
| ambience | Stable ID, anchor, future audio event_id and radius; hook only, no sound installation in WP167 |
| assets | Stable ID, prefab Resources ID, anchor, world-axis offset and yaw |

Every anchor uses existing loop ID and distance in metres, validated by the millimetre coordinate contract. Semantic zones are generated from authoritative track segments; do not duplicate grades, widths or AI zone labels in the venue manifest. Offsets are world axes, not a local Frenet frame; they do not alter track distances.

Collections reject duplicate stable IDs across cameras/ambience/assets. Paths reject traversal, drive roots, backslashes, extensions and empty components. Missing text resources, track/visual mismatches, unknown anchors and invalid dimensions fail before output generation. Unity resolves every prefab dependency before creating output.

## Tool

Unity menu `BIATHLON/Venues/Generate Pipeline Fixture`, or Unity MCP script-execute:

```csharp
Biathlon.Unity.ProductionVenueAuthoring.Generate("Biathlon/Venues/venue_pipeline_fixture_v1");
```

Output: `Assets/Generated/Venues/<id>/Authoring.prefab` and normalized manifest snapshot. Generation uses an isolated temporary scene, preserves the previous active scene, and replaces only that venue's generated artifacts. Generated child names are for browsing; bindings use `VenueAuthoringMarker.StableId/Kind/Reference`.

The prefab contains semantic markers, disabled cameras, ambience hooks, resolved prefab placements, range entry/exit and all lane stop markers projected from the physical range alternatives. It contains no race host. This is authoring output, not a production venue admission certificate.

## Verification boundary

`VenueAuthoringPlan` can compile in ordinary .NET without Unity. WP167 validates schema/reference contracts and generated bindings. WP168 adds geometric diagnostics: world distance, loop/seam continuity, grade/width limits, range/penalty placement and assets. WP169–174 additionally require complete dense races with side-entry flow; WP177/178 cover balance/performance.

`venue_pipeline_fixture_v1` deliberately binds the old synthetic stadium. Its geometry is not certified for production. Do not interpret successful authoring generation as fixing the known common entrance queue or approving a real venue. Production expectations remain in `PRODUCTION_RANGE_FLOW_ACCEPTANCE.md`.
