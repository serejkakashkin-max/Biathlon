# TASK M13 — owner shortened Woodland review

2026-10-05. New Windows review DELIVERED; owner subsequently reports substantial
improvement and requests traffic/guidance/tuck work. M13/WP175–178 remain OPEN.

Main is1200m rather than3000m; supported game distances are Sprint4200.001m,
Pursuit5000m and Mass Start6000m. First turn60m; actual XYZ geometry includes
early climbs/descent,20m elevation range and technical downhill bends. Every
paid segment uses that same geometry, not a visual scale or speed multiplier.
Thirty reusable mats at3m pitch, shooting, lap/bout counts,150m penalty and
private range/exit travel remain. ADR0029 records owner authorization for game
distances. Sixty-nine seams have zero world gaps;1500XYZ edge errors ≤5.163e-12m.

Roadside content:1372 tape sections,26 standing crowd groups/416 anchors,
18 billboards,18 flag steles,6 banners/1hut. Original procedural resources use
existing audited materials; no proprietary Biathlon2006 assets copied.
Full native grandstand/event mesh bounds clear all36 paid penalty loops by≥3m.
Terrain foundation variation≤.25m; terrain adaptation preserves the return hill.
Standing anchors retain X/Z and use sampled ground height; boot gap≤3cm.
Canopy conflicts removed:179 trees;8280 actual trees remain in33groups/36tiles.
High graphics is retained. Native early-turn/descent/penalty GameViews inspected.

Core: venue-bound AI entry-aisle/coasting decisions still use ordinary paid
motion/body resolution. Exact route interval junction binding (ADR0030) fixes
a false competing Main port wholly inside the skipped Pursuit interval. Bodies,
guards/tails, travel, shooting and global policies are preserved. Replay context
captures every new policy/interval. Historical WP169 strict numerical witnesses
keep exact19-file authority outside Resources; they do not validate new content.

Current frozen-source full races and persisted exact replay:

| Format | Field | Completed tick | Replay |
|---|---:|---:|---|
| Sprint |40|43417|exact public states/ordered field events/result|
| Pursuit, qualified from this actual Sprint |40|51989|same exact contract|
| Mass Start |30|37106|same exact contract|

All finish; replay executes zero AI steps. Pursuit still has a temporary stopped
interval of13290ticks. Completion is not acceptable merge feel: the new owner
directive explicitly requires fixing stationary-player and range-exit queues.
No diagnosis of a permanently stalled final actor remains; athlete25 finishes.
Retain failed prefixes/traffic/geometry experiments as evidence.

Tests: Python20 PASS; focused .NET50+30+7+6 PASS. Native range AI10,
factory11, exact-junction8, historical directed24 and directed-field7 PASS.
Native authoring/standing/dressing/forest/route/weather component checks passed
separately; no WP175 condition-race acceptance follows. Initial broad core has
1042 PASS/2 FAIL: stale old-distance assertion and an injected skipped-point
test expecting exclusivity instead of rejection. Both updated to explicit new
contracts and pass focused .NET/native. Final full run is pending. Historical
2600-byte world-query allocation defect did not reproduce in that initial full
run; cause remains unresolved, strict test retained. No full-green claim yet.

Windows: `Builds/WoodlandShortCourseReview20261005/Biathlon.exe`, Development,
High startup,0 build errors/warnings. Hidden paid400-tick Sprint40/Mass30 and
actual-qualified Pursuit40 checks PASS. Their rendering is suspended; no FPS,
GPU or standalone visual proof. Shipped credits exact;23 owner file paths,
6 native serialized assets/dirty flags and frame timing unchanged.

Evidence: `artifacts/owner-short-course-20261005/` current `*-exact-record.json`,
`*-exact-replay.json`, tapes/contexts; `short-static.json`, native views,
`test-results/`, `build-review/`, import receipts and rejected variants.
Durable compact receipt: `docs/quality/m13/owner-short-course-20261005/evidence.json`.

Next: execute [owner traffic/guidance/tuck directive](../owner/OWNER_WOODLAND_TRAFFIC_GUIDANCE_TUCK_2026-10-05.md).
Manual heading host integration remains OPEN; this build still uses route-bound
lateral control. Approved minimal torso motion is preserved. Every new build
must be explicitly delivered for hands-on review; no new owner gate is inferred.
