# TASK-013 — WP-013 track schema/versioning

Dependency M1 PASS. Required immutable versioned track JSON: stable loops/segments/checkpoints,
race-loop variants, semantic zones, range anchors and penalty entry/rejoin references.
Geometry/authoring bounds belong to track data. Route distances use exact integer millimetres;
grade is rise/run, signed curvature is inverse metres, width metres and exposure normalized.
Validate identifiers, references, distances/order, complete fields and numerical domains; round-trip.
No Unity spline/physics authority, no sampling or movement yet. ADR records topology/numeric limits.
Tests: invalid schemas/duplicates/broken anchors/bounds, exact distances, serialization and pure-core regression.
Run .NET/Unity, inspect Console, report and update state. Rollback new track-only files.
