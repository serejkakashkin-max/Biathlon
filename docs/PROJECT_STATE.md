# BIATHLON production state

Updated 2026-10-03. Operational resume; detailed evidence remains in reports/artifacts.

Authority: AGENTS.md → [Master Plan](BIATHLON_MASTER_PLAN_FINAL_v2.0.md) → GDD/TDD → ADRs. The [owner M13 closure directive](owner/OWNER_M13_VELOCITY_WP169_CLOSURE_2026-10-03.md) controls execution and testing frequency; Master Plan is unchanged.

```yaml
project_phase: M13 Production Venue Pipeline and Launch Content
owner_gate_g1: APPROVED 2026-09-13
owner_gate_g2: APPROVED 2026-09-30
m12_status: PASS / owner-approved
active_work_package: WP-169 IN_PROGRESS / CLOSURE_MODE
active_task: resolve actual compact range crossing deadlock and Sprint snow-bank contact
last_completed_task: TASK169AA production40 import/regression PASS
production_fields: Sprint40 / Pursuit40 / MassStart30; total includes player
last_verified_commit: a2f706b28bb7c9a3a6e1f6c3a17b44ed04e1b9e5; pushed and origin verified
next_action: qualify explicit local crossing admission with real Sprint40/Mass30 before recording final replay; fix native Sprint snow contact
current_known_blockers: [compact crossing mutual stop in actual Sprint40/Mass30, exposed Sprint snow-bank seam]
owner_decisions_needed: []
last_delivered_windows_build: G2 target018; no new production build yet
```

WP-167 authoring and WP-168 validator are DONE. WP-169 Venue01 (Woodland Basin) is active. WP-170 Pine Ridge is an outside-Assets draft, pending qualified Venue01 range; WP-171–178 are not complete. After WP-169 acceptance, proceed directly to WP-170 through the existing pipeline. Stop at the real M13 gate after WP-178; no additional approval between production venues.

Current validation baseline:

- TASK169AA: actual imported .NET Factory10/10; native Factory10/10, Editor3/3, Host3/3; full .NET926/926; full PlayMode225/225, zero failures/skips. Console0errors/5retained warnings, compilation_failed=false.
- Production profiles/default scene/recipes bind40/40/30. Only2 of206 Woodland JSON resources changed;204 others and all metas unchanged. Generic historical60 capacity remains supported. [Cap report](reports/TASK-169AA-production-field-cap-REPORT.md), [proof](quality/m13/wp169aa-production-field-cap.json).
- T2 route/frame correction is DONE for its bounded scope: focused5/11/8/2 and fullPlay224/224; nine actual normal GameViews reviewed. [Report](reports/TASK-169T2-route-scoped-course-presentation-REPORT.md).
- Historical U/Y complete Sprint60/Mass30/Pursuit60 all-state/event/result zero-AI replay is preserved regression evidence, not current production acceptance. No further historical60 mega-runs are needed without new regression evidence.

Remaining WP-169 acceptance, in execution order:

1. Compact physical range: approximately2.75–3m lane spacing, left entry/right exit, correct paid circulation, correct mats and actual prone/standing placement. Preserve full body clearance; no teleport, visual-only mat movement or global queue.
2. Remove stadium white rectangles. Controlled captures and terrain samples identify narrow packed ribbon/terrain contrast; implement a bounded continuous surface remedy, focused regression and GameView closure.
3. Correct the actual paid Sprint extension curtain using bounded content/presentation geometry.
4. Complete real Sprint40 → qualified Pursuit40, and separate Mass30: range/occupancy/penalty/finish/results/start gaps, persisted authoritative replay.
5. Final visual inspection, relevant focused regressions and final fullPlay; full core regression only for core/data changes or once at final acceptance.
6. Windows Development owner review build; hidden-only automated smoke of all3formats plus real rendered third-person/range/stadium/forest views. Preserve unavailable GPU/frame-counter limitations.
7. One consolidated [WP169 production report](reports/WP169_VENUE01_PRODUCTION_REPORT.md) when ready, review package, state and Git publication. Mark WP169 DONE only after all owner definition-of-done items pass; then immediately begin WP170.

Compact range closure bundle is a local imported draft, not production acceptance:30lanes/3m pitch, left entry/right exit, unchanged body/guards, actual stop-.88m, physical2m mats and extra measured prone support bounds. Static content and bounded2/3-body completion/replay passed; native apron3format checks passed, stadium GameViews show the white rectangles removed. Final native hero pose and mat raster-depth checks remain pending.

Actual production probes exposed a blocking local crossing mutual stop: Sprint40 remains PREFIX_ONLY at60000ticks, first permanent stop near19587; Mass30 also stalls in its first bout. No completed Sprint40/Pursuit40/Mass30 or new build is claimed. Preserve failed evidence under artifacts/wp169closure; fix the named crossing without narrowing bodies/guards, teleporting or adding a global queue. Optional Sprint banks preserve route geometry but native full contact still fails at one exposed seam; repair before acceptance.

Replay CLI now has an explicit bounded --max-context-mib128..512 option, default128 retained. Actual Mass30 cold context exceeded the old128MiB tool limit; its bytes/schema/core remain unchanged. Relevant native .NET tool assembly20/20PASS, including expansion rejection, exact replay and malformed capacity options. Full core/final PlayMode evidence is still required for the final coherent range bundle.

Already-proven authoring/validator/core, Y historical replay, T/T2 frame/seams, Z forest visibility, accepted terrain placement, Mass30 capability and G2 direction are frozen unless new regression evidence appears. Use focused tests/compile/Console/targeted GameView for local presentation; focused range/integration/replay for changed physical paths. Full regressions run at coherent/final acceptance, not after every visual tweak.

Deferred findings: historical Pursuit60 stopped ski time30.05% and telemetry → WP-177 cause/balance review unless a specific Venue01 geometry defect is demonstrated. Weather matrix → WP-175; performance/LOD → WP-178; further polish → Alpha/Beta. OWNER-FEEL-001 shooting too easy remains OPEN for WP-229/230/232; no hidden RNG or silent shooting rebalance.

Keep approved third-person chase and dedicated shooting cameras, licensing/CC BY attribution/de-branding, deterministic fixed ticks and pure core boundaries. Standing authorization covers coherent commits/pushes and free-asset research/import after license audit. Preserve unrelated owner workspace edits. Stop only for real technical blocker, explicit owner gate, licensing/paid/account action or material scope decision.

Earlier detailed progress, failed diagnostics and publication history are preserved in [historical state](reports/PROJECT_STATE_HISTORY_PRE_CLOSURE_2026-10-03.md), individual reports and Git history. M1–M10/G1 and M11 completion remain recorded there; G2 approval supersedes all historical pending/fail statuses.
