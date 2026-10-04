# BIATHLON production state

Updated 2026-10-04. Operational resume; detailed evidence remains in reports/artifacts.

Authority: AGENTS.md → [Master Plan](BIATHLON_MASTER_PLAN_FINAL_v2.0.md) → GDD/TDD → ADRs. [Owner M13 closure directive](owner/OWNER_M13_VELOCITY_WP169_CLOSURE_2026-10-03.md) controls delivery/testing frequency; Master Plan unchanged.

```yaml
project_phase: M13 Production Venue Pipeline and Launch Content
owner_gate_g1: APPROVED 2026-09-13
owner_gate_g2: APPROVED 2026-09-30
m12_status: PASS / owner-approved
active_work_package: WP-170 IN_PROGRESS / CONTENT_IMPORT
active_task: import Pine Ridge compact candidate, qualify actual races/replays and native presentation
last_completed_work_package: WP-169 PASS / DONE 2026-10-04
production_fields: Sprint40 / Pursuit40 / MassStart30; includes player
last_verified_commit: 73aa50c; pushed; WP169 acceptance bundle publication in this update
next_action: Pine201JSON import; existing factory/native checks; actual Sprint40→Pursuit40 and Mass30 persisted replay
current_known_blockers: []
owner_decisions_needed: []
last_delivered_windows_build: Builds/WoodlandReviewWindows/Biathlon.exe; Development; final build/smokes PASS
```

WP-167/168/169 are DONE. [WP169 consolidated report](reports/WP169_VENUE01_PRODUCTION_REPORT.md) and [acceptance](quality/m13/wp169-production/acceptance.json) contain the final evidence. WP-170 Pine Ridge candidate exists at `artifacts/wp170/production-compact-20261003/`; static data/strict policy PASS, imported native/runtime/rendered acceptance still pending. WP-171–178 are not complete. Continue without inter-venue approval; stop at actual M13 gate after WP-178.

WP169 final acceptance:

- Compact30-lane range at3m pitch; left entry/right exit; flush1.5×2m mats. Full bodies/guards, paid travel, local ownership/private exit tails preserved. No teleport/body shrink/global queue.
- Complete Sprint40 tick62,247, exact-qualified Pursuit40 tick57,248, Mass30 tick58,974; all finish. Three persisted replays exact per-tick public state/ordered field events/results and zero AI replay steps. Current source/resource/assembly hashes verified.
- Continuous apron removes white rectangles; bounded Sprint snow shoulders remove fork curtains. Ordinary native GameViews inspected; all six actual paid prone/standing support poses PASS.
- .NET core1015/1015; native full PlayMode234/234; later focused review3/3; build lease6/6 PASS. Final Console0errors/0warnings, compilation_failed=false. Failed diagnostics retained; no tests disabled or strict guards weakened.
- Final Windows build0errors/0warnings; actual shipped credits exact; hidden Sprint40/real-qualified Pursuit40/Mass30 content checks PASS. Build file-manifest/managed hashes are in acceptance. All19 unrelated owner files/native dirty values preserved exactly.

Hidden-only standalone checks remain the owner's instruction. Windows suspended normal-camera rendering (`HIDDEN_WINDOW_SUSPENDED`); GPU/frame counters, Windows visual/FPS proof unavailable. Ordinary Unity GameViews supply visual review separately. No sustained performance claim follows from headless timing or hidden content checks; WP-178 owns final performance/LOD.

Frozen historical60/Y/T/T2/Z/tooling proofs require no broad rerun without new evidence. Historical Pursuit60 stopped ski time30.05% → WP-177 cause/balance review. Six-venue weather matrix → WP-175. OWNER-FEEL-001 shooting too easy remains OPEN for WP-229/230/232; no hidden RNG or silent rebalance.

Preserve approved chase/shooting cameras, licenses/CC BY notices/de-branding, pure deterministic core and unrelated owner edits. Standing authorization covers coherent commits/pushes and audited free assets. Stop only at real owner gate, technical blocker, licensing/paid/account action or material scope decision.

[Historical state](reports/PROJECT_STATE_HISTORY_PRE_CLOSURE_2026-10-03.md) and prior reports/Git retain earlier milestones and failed diagnostics; G2 approval supersedes historical pending/fail statuses.
