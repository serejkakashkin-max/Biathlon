# Final M15.5 evidence

Use `review.html` for the ten matched images, five winter looks and three motion
videos. `before-verified/` and `after-final/` are the final native1920×1080 capture
and measurement runs. `motion-final/*.mp4` encodes the labelled final comparison:
left original, right new, current materials on both. PNG source frames remain
local; they are not all published to avoid duplicating270 frames in Git.

`playmode-final.xml`:365 passed,0 failed/skipped. `playmode-first.xml`: initial
355 passed,10 failed, retained as history. MCP timeout receipts only describe
transport bounds; they are not Unity test outcomes. The selected core24-pass
receipt is `m15-5-core-replay.trx`; stadium authoring10-pass receipt is
`stadium-authoring-tests.txt`.

`native-game/standalone-review.json`: ordinary-player UI/race/aiming review,
isolated profile store, owner profiles preserved. Its results screen is a fixture;
actual complete race/replay validation is in the core/native tests.
`build-summary.json` / `build-warnings.json` record the successful test build and
existing Sentis shader-variant warnings. `performance-summary.json` has scoped
render statistics, unsupported GPU counters remain null/-1.

Other local candidate directories are development history, including invalid
hidden/stale-skin captures and earlier mislabelled motion framing. They are
excluded from final acceptance and are not the gallery's image sources.
