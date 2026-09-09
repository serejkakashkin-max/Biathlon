# TASK-015 / WP-015 — PASS

Added .NET generate-tracks authoring command, versioned recipe data, generator script and five generated
JSON fixtures. Profiles: flat, climb, rolling, technical descent and range approach. Shared flat range tail
and penalty/checkpoint anchors relocate by exact millimetres. Outputs validate before any file write.

.NET Release and Unity MCP EditMode: 97/97 each. Generated profiles/anchors/signs/metadata and template
preservation pass; regeneration of all five files byte-identical. Invalid final recipe profile exits 1
without creating output directory, after earlier profiles were evaluated (validation-before-write verified).
Foundation/M1 snapshots unchanged. Unity Console 0 errors / 4 upstream reload warnings, compile successful.
Evidence artifacts/physiology/wp015-* and artifacts/track-regeneration. git diff --check passed.

Generator uses .NET built-in JSON nodes, adds no package or Unity dependency. Monotone climb/descent
fixtures are marked open; scalar closed flags on other fixtures do not prove 3D geometric closure.
No skiing/production venue or remote action. Prior uncommitted baseline preserved. Next WP-016 routes.
