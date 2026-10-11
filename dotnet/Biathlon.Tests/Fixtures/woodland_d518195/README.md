Historical simulation venue for captured geometry regressions

Source: commit `d518195` (`fix(sim): bound repeated snow cover queries at course edges`),
`Assets/Resources/Biathlon/Venues/woodland_basin_v1/*.json`.
The 20 top-level simulation definition files are copied with whitespace outside
JSON strings removed; numeric tokens and all authored fields are preserved.
Environment and terrain/vegetation files are deliberately excluded: these tests
exercise course geometry, not rendered scenery.

Only `ShortStartSeamWindowIncludesTheWholeDiskCover` and
`AuthoredEndpointSurvivesCancellationWhenTheCurveCrossesZeroX` select this fixture.
Their original captured poses, stations, body/edge assertions and proof duration
are unchanged. The production course later became shorter and its start became
a separate spur, so current venue data no longer contains those historical poses.
This fixture stays outside `Assets` and is never a runtime/player resource.
