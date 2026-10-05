# BIATHLON production state

Updated 2026-10-05. Operational resume; detailed evidence remains in reports/artifacts.

Authority: AGENTS.md -> [Master Plan](BIATHLON_MASTER_PLAN_FINAL_v2.0.md) -> GDD/TDD -> ADRs. [Owner M13 closure directive](owner/OWNER_M13_VELOCITY_WP169_CLOSURE_2026-10-03.md) controls delivery/testing frequency; [manual steering/presentation directive](owner/OWNER_M13_MANUAL_STEERING_AND_PRESENTATION_2026-10-04.md) authorizes full manual direction; [one-venue directive](owner/OWNER_SINGLE_VENUE_SCOPE_2026-10-05.md) reduces current production content to one excellent venue. Master Plan unchanged.

```yaml
project_phase: M13 Production Venue Pipeline and Launch Content
owner_gate_g1: APPROVED 2026-09-13
owner_gate_g2: APPROVED 2026-09-30
m12_status: PASS / owner-approved
active_work_package: M13 owner presentation/manual-steering work; WP-175 preparation
active_task: integrate owner-approved manual ski direction; Woodland course dressing and quality
owner_motion_task: TASK_M13_OWNER_MOTION_01 PASS 2026-10-05
owner_manual_task: TASK_M13_OWNER_MANUAL_01 IN_PROGRESS; foundation focused/native PASS; integration pending
open_regression_defect: full core1025 PASS /1 FAIL; legacy world-query allocation2600bytes; cause unresolved
production_venue_count: 1
production_venue: woodland_basin_v1
deferred_work_packages: WP-170/171/172/173/174 DEFERRED_OWNER_SCOPE_REDUCTION
last_completed_work_package: WP-169 PASS / DONE 2026-10-04
production_fields: Sprint40 / Pursuit40 / MassStart30; includes player
last_verified_commit: 04ea339606de703e722acbb5bbe315f3c3994b18; pushed; WP169 DONE
next_action: verify and connect versioned manual steering; import/review bounded Woodland course dressing; WP-175 matrix
wp175_preparation: five visual rows and four canonical physical snow bindings; execution/acceptance OPEN
current_known_blockers: []
open_acceptance: [manual steering integration; Woodland quality; WP-175/176/177/178]
owner_decisions_needed: []
last_delivered_windows_build: Builds/WoodlandReviewWindows/Biathlon.exe; Development; final build/smokes PASS
```

WP-167/168/169 are DONE. [WP169 consolidated report](reports/WP169_VENUE01_PRODUCTION_REPORT.md) and [acceptance](quality/m13/wp169-production/acceptance.json) contain the final evidence. Woodland is now the sole production venue. WP170–174 are deferred by explicit owner scope reduction, not completed. Pine's corrected Sprint40 finished tick53,594 with exact persisted replay; Mass30/range and Pursuit40/shortcut acceptance remained OPEN. [Pine diagnostic record](quality/m13/wp170-pine-profile-repair.md), failed artifacts and [recoverable source checkpoint](deferred/wp170/README.md) are retained for later work. All412 Pine asset/meta files were removed from active Assets after byte/hash verification; the469-entry archive is outside Unity Resources. WP175–178 remain OPEN for Woodland; continue to the actual M13 gate.

Owner minimal torso refinement is PASS: [TASK M13 motion01 report](reports/TASK_M13_OWNER_MOTION_01_REPORT.md). Actual native lateral ranges Flat1.877cm/Sprint2.920cm/descent0.426cm/Tuck0; nineteen focused PlayMode tests PASS and normal GameView inspected. Legs/poles/root and authoritative gameplay are preserved. Manual steering is separately IN_PROGRESS and is not yet connected to the playable host.

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

Preserve approved chase/shooting cameras, licenses/CC BY notices/de-branding, pure deterministic core and unrelated owner edits. Standing authorization covers coherent commits/pushes and audited free assets. Stop only at real owner gate, technical blocker, licensing/paid/account action or material scope decision.

[Historical state](reports/PROJECT_STATE_HISTORY_PRE_CLOSURE_2026-10-03.md) and prior reports/Git retain earlier milestones and failed diagnostics; G2 approval supersedes historical pending/fail statuses.
