# M16.2 WP-J — unused penalty-side bypass

2026-10-31. COMPLETE / OWNER TEST PENDING. M16.2 remains open for owner review.

The extra lobe is Main's logical range-clock bypass. Required shooting laps use
the physical lane paths instead. The old display union also drew the bypass's
sub-metre hairpin as an 8m-wide ribbon, producing self-intersections and dark
shards beside the penalty loop. Matched native High captures reproduce the
owner's symptom; hiding Main removes it, while hiding the start approach does not.

`ProductionCoursePresentation.RouteUsage` now includes the actual physical lane
union for required shooting occurrences. A non-shooting occurrence still draws
its Main bypass; unfiltered authoring views retain every original source surface.
All original buffers, physics/data, lane approaches, exits, start and penalty
paths remain intact. Existing natural banks close the selected ends. No graphics
quality reduction, new renderer/material/collider, hard-coded venue exclusion or
per-frame work. `PlayerBuildM16_2` adds the fix to the build's review notes.

Native qualification, **23 unique passing cases**:

| Receipt | Cases | Coverage |
| --- | ---: | --- |
| `visibility-native.json` | 9 | Three real formats omit the bypass; a non-shooting final lap keeps it; complete paid path coverage, original buffers, null-route compatibility, strict terrain contact and moved-frame rejection |
| `format-contact-native.json` | 7 | Sprint/Pursuit original and yaw45 frames, both finish-runout variants, MassStart; full contact/topology and original packed-buffer invariants |
| `apron-captures-native.json` | 7 | Six real-format legacy/natural apron cases plus native High camera review |

`red-native.json`: all three new bypass regressions fail on the original source,
then pass in the final visibility suite. Existing coverage tests now distinguish
the logical range clock from the physically substituted lane paths. MassStart's
cut assertion now checks the new real range cut; no failing test, terrain/contact
bound or topology tolerance was removed or relaxed.

Before/after `stadium.png` and `overview.png` use identical native cameras and
High settings. `before/no-main.png` and `before/no-start.png` isolate ownership;
`after/close.png` and `after/range-exit.png` inspect the corrected area. The capture
fixture intentionally isolates environment/course geometry and excludes athletes,
course furniture and HUD; it is not a whole-game performance benchmark.

Windows delivery: `Builds/M16_2_Live_Shooting_Review/START_TEST.cmd`, borderless
desktop fullscreen. Build Succeeded, **0 errors / 24 warnings**, 17.11s,
385330883 bytes. Every warning is an unchanged CS0618 API-deprecation call site
(`build-messages.tsv`, `build-warning-audit.json`). Shader warnings from the prior
cold build were not re-emitted by this incremental build. The shared builder's
legacy receipt ID remains `m15_5_visual_review`; `milestone.json` identifies M16.2.
Source/binary SHA256 values are recorded in `fingerprints.json`.

Compiled ordinary-player startup PASS / exit0 / zero captured runtime errors at
2560x1440 FullScreenWindow matching the desktop. Random Sprint bib21, wait skipped,
3–2–1, scheduled tick12001, accepted tick12005. Creator/appearance, Continue and
backup recovery, settings/bindings/checkpoint/export pass; owner profiles unchanged.
`player/review.json`, countdown and race PNGs retain the evidence. Optional TAB
history/cursor probe was not rerun; its default navigation fields are not new test
claims. No new full-race, FPS or minimum-hardware claim.

Actual Editor Console before build: zero errors, six unchanged Editor-tool
deprecation/analyzer warnings; incidental Unity AI subscription messages are logs.
After build: zero errors, 24 known warnings. Receipts retained before clearing;
final actual Console zero errors/warnings, compilation and Play Mode idle.

All **669** protected core CS and resource JSON files remain byte-exact to the
starting workspace. The complete PROJECT_STATE prefix at20d1cc58 is preserved.
No deterministic race, balance, scoring, replay or save change; no new race-hash
run is needed for this draw-only selection change. Pre-existing owner/Unity edits
are preserved and excluded. The previous qualified snow-junction build remains
in `Builds/M16_2_Snow_Junctions_Backup_20261031` (binary hash checked).

Next: owner checks the penalty-side area and normal start/range/penalty travel in
the new build. M16.1 remains approved; M16.2 awaits acceptance; M17 not started.
