# BIATHLON production state

Updated 2026-10-05. Operational resume; detailed evidence remains in reports/artifacts.

Authority: AGENTS.md -> [Master Plan](BIATHLON_MASTER_PLAN_FINAL_v2.0.md) -> GDD/TDD -> ADRs. [Owner M13 closure directive](owner/OWNER_M13_VELOCITY_WP169_CLOSURE_2026-10-03.md) controls delivery/testing frequency; [manual steering/presentation directive](owner/OWNER_M13_MANUAL_STEERING_AND_PRESENTATION_2026-10-04.md) authorizes full manual direction; [one-venue directive](owner/OWNER_SINGLE_VENUE_SCOPE_2026-10-05.md) reduces current production content to one excellent venue. Master Plan unchanged.

```yaml
project_phase: M13 Production Venue Pipeline and Launch Content
owner_gate_g1: APPROVED 2026-09-13
owner_gate_g2: APPROVED 2026-09-30
m12_status: PASS / owner-approved
active_work_package: M13 owner shortened/varied Woodland revision; manual steering; WP-175 OPEN
active_task: owner stationary-player avoidance; moving range/penalty merges; mat/fork arrows and bounded range assistance; downhill tuck player/AI
owner_motion_task: TASK_M13_OWNER_MOTION_01 PASS 2026-10-05
owner_course_dressing_task: TASK_M13_WOODLAND_DRESSING_01 bounded checks PASS; owner requests substantial course/layout/dressing revision
owner_manual_task: TASK_M13_OWNER_MANUAL_01 IN_PROGRESS; foundation focused/native PASS; integration pending
open_regression_defect: full core1025 PASS /1 FAIL; legacy world-query allocation2600bytes; cause unresolved
production_venue_count: 1
production_venue: woodland_basin_v1
deferred_work_packages: WP-170/171/172/173/174 DEFERRED_OWNER_SCOPE_REDUCTION
last_completed_work_package: WP-169 PASS / DONE 2026-10-04
production_fields: Sprint40 / Pursuit40 / MassStart30; includes player
last_verified_commit: da31ee9; pushed; shortened varied Woodland and event dressing; delivered build owner played
next_action: native stationary-player bypass acceptance; moving range/penalty merge fixes; actual mat/fork guidance and bounded assistance; shared downhill tuck
wp175_preparation: five visual rows and four canonical physical snow bindings; execution/acceptance OPEN
wp175_physical_component_checks: four canonical snow bindings; dotnet10/native10 PASS; full condition races OPEN
review_tooling_task: explicit bounded build/smoke paths; native lease10 and generic authoring11 PASS
current_known_blockers: []
open_acceptance: [manual steering integration; Woodland quality; WP-175/176/177/178]
owner_decisions_needed: []
last_delivered_windows_build: Builds/WoodlandShortCourseReview20261005/Biathlon.exe; Development/High; new short course/roadside/stadium; build/hidden3formats PASS; owner played and approves improvement, further traffic/guidance/tuck requests open
```

[Owner playable course revision](owner/OWNER_WOODLAND_PLAYABLE_COURSE_REVISION_2026-10-05.md): latest hands-on feedback approves athlete motion, but the current course remains too long and monotonous. Actual skiing distance must reduce by at least half, with early turns and multiple real rises/descents. Owner means course-edge ribbon tape and standing spectators, billboards/flag steles, not only remote stands. Confirmed old stadium grandstands08/09 and event screen intrude on the penalty corridor. Geometry/layout/dressing revision delivered and owner reports substantial improvement; the new traffic/guidance/tuck directive remains IN_PROGRESS. Each new build must be explicitly delivered for owner testing. Earlier WP169 race acceptance is historical and does not qualify the new course until rerun.

[Large replay context tooling](reports/TASK_M13_LARGE_REPLAY_CONTEXT_REPORT.md) PASS: complete CLI test assembly28/28; scoped chunk encoding removes the actual oversized JSON-token failure while retaining exact legacy strings and outer capacity limits.

Shortened-course checkpoint: actual Main1200m, Sprint4200.001m, Pursuit5000m, MassStart6000m; first turn60m, real rises/descent,30mats at3m pitch, paid penalty150m preserved. Roadside1372 ribbon segments,26 standing crowd groups/416 anchors,18 billboards,18 flag steles,6 banners/1hut,8280 trees. Native full stadium meshes clear all36 paid penalty loops by at least3m; exact world seams and terrain/crowd grounding checked. Three normal native High GameViews inspected. Sprint40 completes tick43417 and Mass30 tick37106; their persisted current-source replays PASS with exact per-tick states/events/results. Actual-Sprint-qualified Pursuit40 completes tick51989 with exact persisted replay; temporary stopped13290ticks remains an explicit merge-feel defect. Venue-scoped AI coasting and positive entry aisle address dense range traffic; exact route interval reachability ([ADR0030](adr/ADR-0030-exact-route-interval-junction-reachability.md)) removes a false Main crossing inside a skipped Pursuit interval without shrinking bodies/guards. Focused .NET50+30/native junction8/directed24 tests PASS; historical WP169 numerical witnesses now retain their exact19-file authority outside Resources. Final frozen full core1044/1044 PASS; earlier allocation defect did not recur, cause remains unresolved and strict test retained. Windows short-course review delivered with0errors/warnings and hidden3format checks PASS; owner subsequently played it and requested traffic/guidance/tuck changes. Those changes have separate pending acceptance. Failed prefixes and rejected geometry/control experiments are retained under artifacts/owner-short-course-20261005.

WP-167/168/169 are DONE. [WP169 consolidated report](reports/WP169_VENUE01_PRODUCTION_REPORT.md) and [acceptance](quality/m13/wp169-production/acceptance.json) contain the final evidence. Woodland is now the sole production venue. WP170–174 are deferred by explicit owner scope reduction, not completed. Pine's corrected Sprint40 finished tick53,594 with exact persisted replay; Mass30/range and Pursuit40/shortcut acceptance remained OPEN. [Pine diagnostic record](quality/m13/wp170-pine-profile-repair.md), failed artifacts and [recoverable source checkpoint](deferred/wp170/README.md) are retained for later work. All412 Pine asset/meta files were removed from active Assets after byte/hash verification; the469-entry archive is outside Unity Resources. WP175–178 remain OPEN for Woodland; continue to the actual M13 gate.

Owner minimal torso refinement is PASS: [TASK M13 motion01 report](reports/TASK_M13_OWNER_MOTION_01_REPORT.md). Actual native lateral ranges Flat1.877cm/Sprint2.920cm/descent0.426cm/Tuck0; nineteen focused PlayMode tests PASS and normal GameView inspected. Legs/poles/root and authoritative gameplay are preserved. Manual steering is separately IN_PROGRESS and is not yet connected to the playable host.

[Woodland course dressing report](reports/TASK_M13_WOODLAND_DRESSING_01_REPORT.md): six banner groups/two fences/two crowd pockets/one hut imported and inspected;336 new actual seats,26,554 retained trees. Eleven presentation JSON changed;205 venue JSON and all meta GUIDs exact. Nine authoring tests plus one actual native footprint/ground/crowd test PASS. Current Windows review at `Builds/WoodlandReview20261005` contains minimized torso motion and course dressing;0build errors/warnings, hidden Mass30/Sprint40/actual-qualified Pursuit40 each400ticks PASS, shipped credits exact. Owner20files/3absent paths and6native assets/dirty flags preserved. No hidden rendering FPS/GPU claim and no playable manual-heading claim. This closes the bounded dressing TASK, not M13 or a new owner gate.

[Manual heading foundation report](reports/TASK_M13_OWNER_MANUAL_01_REPORT.md): separate v2 signed steering/world yaw has no automatic centre or course-follow target. Focused .NET21/native11 PASS with the same actual10,000-step final pose. Full core1,026 retains one legacy allocation failure (2,600bytes), despite isolated/context/40-repeat zero-allocation passes; its cause is unresolved and tests remain strict. Manual input/AI/shared-body/replay integration is OPEN; no complete-task or full-regression PASS claim.

WP169 final acceptance (historical baseline):

- Compact30-lane range at3m pitch; left entry/right exit; flush1.5x2m mats. Full bodies/guards, paid travel, local ownership/private exit tails preserved. No teleport/body shrink/global queue.
- Complete Sprint40 tick62,247, exact-qualified Pursuit40 tick57,248, Mass30 tick58,974; all finish. Three persisted replays exact per-tick public state/ordered field events/results and zero AI replay steps. Current source/resource/assembly hashes verified.
- Continuous apron removes white rectangles; bounded Sprint snow shoulders remove fork curtains. Ordinary native GameViews inspected; all six actual paid prone/standing support poses PASS.
- .NET core1015/1015; native full PlayMode234/234; later focused review3/3; build lease6/6 PASS. Final Console0errors/0warnings, compilation_failed=false. Failed diagnostics retained; no tests disabled or strict guards weakened.
- Final Windows build0errors/0warnings; actual shipped credits exact; hidden Sprint40/real-qualified Pursuit40/Mass30 content checks PASS. Build file-manifest/managed hashes are in acceptance. All19 unrelated owner files/native dirty values preserved exactly.

Hidden-only standalone checks remain the owner's instruction. Windows suspended normal-camera rendering (`HIDDEN_WINDOW_SUSPENDED`); GPU/frame counters, Windows visual/FPS proof unavailable. Ordinary Unity GameViews supply visual review separately. No sustained performance claim follows from headless timing or hidden content checks; WP-178 owns final performance/LOD.

Frozen historical60/Y/T/T2/Z/tooling proofs require no broad rerun without new evidence. Historical Pursuit60 stopped ski time30.05% -> WP-177 cause/balance review where relevant to the retained course. Woodland weather/snow matrix -> WP-175. OWNER-FEEL-001 shooting too easy remains OPEN for WP-229/230/232; no hidden RNG or silent rebalance.

WP175 preparation: [single-Woodland conditions/readability plan](../scripts/content/m13/WP175_WOODLAND_WEATHER_PLAN.md) defines five fresh visual environments (High clear/overcast/snowfall, Balanced/Low snowfall) and four canonical physical snow bindings. Eleven planner/validation tests PASS; current candidate inputs are outside Assets under `artifacts/wp175-woodland-weather-plan-20261005-v2`. These are planned inputs, not native views, completed condition races, performance evidence or WP175 acceptance. Preserve the hidden-only standalone limit.

[WP175 physical component report](reports/TASK_WP175_WOODLAND_CONDITIONS_01_REPORT.md): .NET10/native10 PASS, all36loops/1,291segments and5,164samples per canonical profile; shared force/cornering trade-offs and short exact repeats verified. No completed condition race/replay or weather view claim. Main descent_technical labels currently have zero grade; real downhill views/profile review must use actual geometry/grade, not labels. WP175/177 remain OPEN.

Preserve approved chase/shooting cameras, licenses/CC BY notices/de-branding, pure deterministic core and unrelated owner edits. Standing authorization covers coherent commits/pushes and audited free assets. Stop only at real owner gate, technical blocker, licensing/paid/account action or material scope decision.

[Historical state](reports/PROJECT_STATE_HISTORY_PRE_CLOSURE_2026-10-03.md) and prior reports/Git retain earlier milestones and failed diagnostics; G2 approval supersedes historical pending/fail statuses.
