# BIATHLON production state

Authority: AGENTS.md → BIATHLON_MASTER_PLAN_FINAL_v2.0.md → GDD/TDD → ADRs.

```yaml
project_phase: M6 AI and pack racing; M1–M5 PASS
active_work_package: WP-079
active_task: TASK-079B range throughput remediation; next autonomous task
last_completed_task: TASK-079 AI benchmark; WP-079 DONE (2026-09-09), M6 gate not passed
last_verified_commit: 9697564949ee883ec839dc2c5ecafb846475bc9d; source checkpoint verified and pushed
foundation_status: DONE; standalone baseline reverified 2026-09-06, 14/14
editmode_tests: 443 current cases covered by WP078 baseline and WP079 focused MCP checks
playmode_tests: 7/7 via Unity MCP, including complete shared Sprint
standalone_tests: 443/443 full stable-JIT run; 16/16 project-config follow-up
unity_console_errors: 0 after WP079 compilation and MCP checks
unity_console_warnings: 4 MCP lifecycle/reconnect warnings retained; no project compile errors
current_known_blockers: [M6 gate requires believable range throughput and final-sprint behavior]
owner_decisions_needed: []
next_unlocked_work_packages: [WP-079 M6 technical remediation; M7 not yet unlocked]
```

## Work packages
- WP-001 DONE. Preserve integer kernel and canonical SHA-256 from Foundation report.
- WP-002 DONE. TASK-002 contracts, serialization, validation and numeric policy verified.
- WP-003 DONE. Effort/power mapping and allocation/monotonicity checks verified.
- WP-004 DONE. HR target/inertia, analytical curves and allocation checks verified.
- WP-005 DONE. Bounded cardiac exposure/drift and recovery verified.
- WP-006 DONE. Anaerobic drain/recovery, boundaries and capability integration verified.
- WP-007 DONE. Muscular fatigue/recovery and power consequences verified.
- WP-008 DONE. Integrated tick/recovery, atomic input and resource event replay verified.
- WP-009 DONE. Versioned monotone stat curves and physical comparisons verified.
- WP-010 DONE. Eight CLI scenarios/CSV/JSON; exact .NET/Unity final state/event fingerprints verified.
- WP-011 DONE. Full .NET/Unity trace snapshots, 9 long runs and zero-allocation benchmark verified.
- WP-012 DONE. M1 PASS; plots, machine summary and MILESTONE-M1-REPORT.md retained.
- WP-013 DONE. Track schema, anchors/variants and exact millimetre coordinates verified.
- WP-014 DONE. Deterministic distance sampling and boundary/allocation checks verified.
- WP-015 DONE. Five generated fixtures, repeat generation and invalid-recipe rejection verified.
- WP-016 DONE. Profile-bound finite route composition and lap/section sampling verified.
- WP-017 DONE. Ski forces, bounded integration, sub-millimetre movement and exact runtime probe verified.
- WP-018 DONE. Terrain/stat/fatigue efficiency, deterministic selection and movement integration verified.
- WP-019 DONE. Constrained tuck, aerodynamic/workload/steering trade-off and recovery verified.
- WP-020 DONE. Curvature/skill safe speed, bounded deterministic braking and trajectory tests verified.
- WP-021 DONE. Versioned conditions, surface binding, resistance/grip and zero-allocation integration verified.
- WP-022 DONE. Bounded gear effects, condition affinities and skill-dominance comparison verified.
- WP-023 DONE. Bounded nonstacking field, wind geometry, power saving and dense allocation checks verified.
- WP-024 DONE. Shared placement, width/body clearance, continuous queries and large-coordinate precision verified.
- WP-025 DONE. Space-aware phases, lateral cost, safe return, replay events and active allocation checks verified.
- WP-026 DONE. Simultaneous safe trajectories, contact episodes, atomicity and dense allocation checks verified.
- WP-027 DONE. Shared-resource final effort and exact canonical finish ordering verified.
- WP-028 DONE. Full solo courses, lap/finish traces, atomic ticks, exact .NET/Unity CSV regression verified.
- WP-029 DONE. M2 PASS; comparisons, plots, deterministic/GC benchmark and PlayMode verified.
- WP-030 DONE. Versioned fixed-tick intent, serialization/ranges and allocation checks verified.
- WP-031 DONE. Analog/preset/assisted response and fixed-frequency checks verified.
- WP-032 DONE. Width/occupancy-constrained steering, posture response and GC verified.
- WP-033 DONE. Explicit assist/manual policy, losses and shared physical behavior verified.
- WP-034 DONE. Presentation-only draft status and physical savings consistency verified.
- WP-035 DONE. Exact-context command tape and complete controlled solo replay verified.
- WP-036 DONE. Device/Unity controller harness, complete replay and M3 PASS verified.
- WP-037 DONE. Fixed-tick shooting phases, stable lane assignment and discharge events verified.
- WP-038 DONE. Five-target apertures, exact boundaries and independent closures verified.
- WP-039 DONE. Fixed-tick bounded aim, fine control and geometry composition verified.
- WP-040 DONE. HR/fatigue/skill breathing oscillator and deterministic cycles verified.
- WP-041 DONE. Exact HR cadence, attack/decay windows and replay verified.
- WP-042 DONE. Seeded finite spectrum, bounded smooth sway and no per-shot RNG verified.
- WP-043 DONE. Stance sway, transition/aim trade-offs and recovery contract verified.
- WP-044 DONE. Costly hold, rebound, forced release/rearm and events verified.
- WP-045 DONE. World/lane wind, exposure, projectile drift and coarse indicator verified.
- WP-046 DONE. Accepted shots, wind geometry, closure accounting and replay events verified.
- WP-047 DONE. Skill/gear timing, post-shot recoil and deterministic settling verified.
- WP-048 DONE. Bounded contextual pressure, composure, recovery and sway composition verified.
- WP-049 DONE. Delayed seeded controller drives shared aim/geometry with no hidden hit roll.
- WP-050 DONE. Full session, exact-context tape, CLI and .NET/Unity trace fingerprints verified.
- WP-051 DONE. 1800 complete bouts, repeated outcomes and exact .NET/Unity statistical hash verified.
- WP-052 DONE. M4 PASS, complete shooting replay/PlayMode/throughput verified.
- WP-053 DONE. Versioned senior/test profiles, audited rule data and route projection verified.
- WP-054 DONE. Ordered race obligations, precise elapsed timing and terminal states verified.
- WP-055 DONE. Exact roster slots, individual clocks and 60-athlete start replay verified.
- WP-056 DONE. Authored range stops, geometric misses and exact penalty traversal verified.
- WP-057 DONE. Stable occurrences, precise splits and updated interval-start gaps verified.
- WP-058 DONE. Registered elapsed ranking, ties, terminal results and validated snapshots verified.
- WP-059 DONE. Complete scripted/AI athlete and interval field through final results verified.
- WP-060 DONE. Complete exact-context command runs, serialization and race replay verified.
- WP-061 DONE. Six complete scenarios, persisted artifacts and .NET/Unity trace hashes verified.
- WP-062 DONE. 60-athlete capacity, exact runtime hashes, complete replay and M5 PASS verified.
- WP-063 DONE. Race and physical perception, shared drafting, replay and allocation checks verified.

- WP-064 DONE. Eight validated, expiring, serializable tactical intents verified.

- WP-065 DONE. Data-driven utility, personality contributions, eligibility and stable decisions verified.

- WP-066 DONE. Exact 2–5 Hz cadence, horizon, shifted clocks and allocation checks verified.

- WP-067 DONE. Data-driven effort control, seeded bounded error and complete Sprint replay verified.

- WP-068 DONE. Terrain preview, resource-limited effort planning and full hilly Sprint replay verified.

- WP-069 DONE. Shared-physics draft target selection and anti-oscillation checks verified.

- WP-070 DONE. Shared space queries and energy/value/terrain overtake decisions verified.

- WP-071 DONE. Complete shared-physics Sprint with periodic bodies, bounded lanes, costly steering, tick-start draft, continuous prefix/follower replanning, retained penalty seams, exact results/replay and AI command integration. Dense 30-athlete race and cross-runtime result hashes pass; see TASK-071C report.

- WP-072 DONE. Cadenced distance/HR recovery request with configured effort reduction, lower arrival HR at a measured approach-time cost, complete shared-physics replay and 423/423 cross-runtime tests.

- WP-073 DONE. Data-driven risk/stability/time thresholds drive the shared shooting controller; complete geometric bouts, persisted replay and legacy regression verified.

- WP-074 DONE. Persistent eight-trait profiles drive utility and shooting policy; identical-capability deterministic comparisons verified.
- WP-075 DONE. Developing/expert profiles alter cadence, horizon, reaction and control errors only; no physical multipliers.
- WP-076 DONE. Integrated PackAiRaceRunner with exact context, owned utility/target/reason traces, invariant CSV and intent/policy events.
- WP-077 DONE. Six real AI scenarios: duel, pack, climb attack, draft train, final sprint, pre-range recovery.
- WP-078 DONE. Three fixed-seed 30-athlete combinations each repeat completely; no overlaps, persistent stalls or sustained lateral oscillation. FIFO junction admission fixes observed penalty-exit starvation. See ADR-0013 and TASK-078 report.

- WP-079 DONE. Four matched-seed30-athlete decision/performance distributions, bounded observer,
  reproducible queue diagnostics and runtime-controlled allocation tests. M6 is not PASS.

## Resume
Continue autonomously with TASK-079B: range throughput and queue/final-sprint interaction.
WP079 measured63–74% stationary athlete ticks outside shooting/staging in dense fields, with
2655–6899 sprint starts. Flat expert queue probe localizes95.93% of stationary ticks to810–850m
before the firing line. A30-second start interval still leaves428220 stationary on-course ticks.
Solo and small-field controls are retained. No physical or tuning change has yet been made.
Do not unlock M7 or label M6 passed from completion/safety alone. No owner decision needed yet.

Latest standalone full443/443 passes with tiered compilation disabled. The test project now
emits System.Runtime.TieredCompilation=false; follow-up16/16 passes without shell overrides.
Two earlier full runs each failed only the existing Foundation GC assertion (2472/1880bytes);
full-workload warmup did not resolve it and was reverted. Strict zero allocation remains.
This is a stable test-runtime mode, not a proven allocation-stack diagnosis. Game/BalanceLab
runtime settings are unchanged. New observer tests2/2 pass in Unity MCP. Foundation14/14 checked.
WP078's441-case Unity verification plus the two new cases cover current443; no new full Unity
soak is claimed. PlayMode7/7 remains WP078's baseline. Console0 errors,4 MCP lifecycle warnings.

Evidence: docs/reports/TASK-079-ai-benchmark-report.md and docs/quality/wp079/{benchmark,queues,
verification}.json. Local raw artifacts live under artifacts/physiology/wp079-* and
ai-benchmark-wp079 / ai-queues-wp079. Benchmark sources preserve exact WP078 flat-expert SHA256.

Follow ADR0012/0013: shared continuous geometry, physical drafting, costly lateral movement,
FIFO junction tickets, retained seam tails and staging/finish retirement. Preserve the WP078
soak baseline and independent M5 hashes. Range-lane work must not bypass physical collisions.
## Publication
Owner explicitly authorized regular commits/push and state updates on2026-09-09.
Branch: codex/wp-078-ai-pack; origin: https://github.com/serejkakashkin-max/Biathlon.git.
The remote had only the initial template. This checkpoint publishes accumulated game source,
resources, tests, standalone projects and reports through WP-078. Unrelated local package/editor
settings, icon changes, local agent configuration and generated plugin dependencies are outside
this checkpoint. Publication: pushed source checkpoint 9697564949ee883ec839dc2c5ecafb846475bc9d to origin/codex/wp-078-ai-pack on2026-09-09. Isolated Git-snapshot restore/build/test passed441/441 in3m25s. This state update is a follow-up documentation commit.


Future workflow: after each verified coherent change, update this state with results, commit and push the task branch under the owner's standing authorization. Keep unrelated local edits separate.

