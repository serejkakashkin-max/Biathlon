# BIATHLON production state

Authority: AGENTS.md → BIATHLON_MASTER_PLAN_FINAL_v2.0.md → GDD/TDD → ADRs.

```yaml
project_phase: M1–M9 PASS; authorized scope through M9 complete
active_work_package: none; WP-124 DONE
active_task: none; M9 milestone closed
last_completed_task: TASK-124 M9 milestone and initial career tuning lock
last_verified_commit: 4aef1d6; WP123 verified and pushed; WP124 verification accompanies this state
foundation_status: DONE; standalone baseline reverified 2026-09-06, 14/14
editmode_tests: 541 discovered; corrected actual-season1/1 and weekly1/1; WP124 lock1/1; .NET/Unity byte parity PASS
playmode_tests: 7/7 via Unity MCP, including complete shared Sprint
standalone_tests: full540/540; corrected career30/30; final lock3/3; 15-season actual-race soak PASS
unity_console_errors: 0 after WP124 compilation and MCP checks
unity_console_warnings: 4 retained warnings; compilation_failed=false
current_known_blockers: []
owner_decisions_needed: []
next_unlocked_work_packages: [WP-125]; M10 not started
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
- WP-080 DONE. Versioned Sprint dependency, registered tied gaps, context validation and actual AI-result import verified.
- WP-081 DONE. Exact registered-gap starts, simultaneous ties, atomic validation and replay verified.
- WP-082 DONE. Shared Pursuit field, exact starts, four PPSS bouts/five laps, global finish-order snapshots and format-correct perception verified.
- WP-083 DONE. Gap chasing, leader defense, physical draft preferences and last-standing urgency; bounded costs and replay reasons verified.
- WP-084 DONE. Repeated tied/dense/separated Pursuit fields, actual overtakes, penalties, exact starts and close finishes verified in .NET/Unity.
- WP-085 DONE. Thirty-body physiological launch, real grid offsets, common gun and exact main-circuit handoff verified; see TASK085B2/ADR0016.
- WP-086 DONE. Thirty-AI PPSS/five-lap/actual-penalty composition and exact cross-runtime results verified.
- WP-087 DONE. Versioned bib/arrival lanes commit with physical branches and logical shooting, repeated events/results verified; ADR0017.
- WP-088 DONE. Repeated flat/rolling/narrow thirty-AI stress and reproducible fixtures verified.
- WP-089 DONE. Exact cross-runtime matrix, result ranking validation and full484 regression passed.
- WP-090 DONE. Shared Sprint → Pursuit and independent Mass Start harness, repeat artifacts and exact .NET/Unity hashes; M7 PASS.
- WP-091 DONE. Bounded parallel batch CLI;10002 complete solo Sprint races verified.
- WP-092 DONE. Versioned timing/shooting/resource metrics with explicit counterfactual semantics.
- WP-093 DONE. Deterministic CSV/JSONL/JSON/Markdown distributions and worker-order checks.
- WP-094 DONE. Canonical12 records/9 windows, failure diffs, export and recovery-trigger fixes verified.
- WP-095 DONE. Independent256-seed pacing holdout, paired effects and exact1536-row CLI/.NET/Unity qualification.
- WP-096 DONE. Held-out 18,000 bouts; top 87.20% prone / 85.08% standing. .NET7/7, Unity2/2 exact CSV. See TASK-096-REPORT.md; non-monotonic tired cohort accuracy remains an explicit provisional limitation.
- WP-097 DONE. Eight matched shared-race draft pairs; +3.20s mean benefit; Unity1/1 exact JSONL.
- WP-098 DONE. 96 ski +10 marginals and 6,000 shooting bouts; no aggregate single-stat dominance; limitations recorded.
- WP-099 DONE. 32 equipment comparisons; -2.24% to +1.80%; below broad athlete skill progression. Unity stat/equipment3/3.
- WP-100 DONE. AI-command replay without AI: expert/developing, flat/rolling, exact complete frames/results. .NET4/4 and Unity4/4.
- WP-101 DONE. Bounded32-candidate proposal-only tuner; .NET3/3, Unity3/3, repeatable CLI evidence.
- WP-102 DONE. M8 PASS; 127-input first-playable tuning lock. Full505/505 + final5/5, Unity new15/15 and PlayMode7/7.
- WP-103 DONE. Versioned save2, explicit v1 confidence migration, identity/state/RNG validation; exact .NET/Unity save digest.
- WP-104 DONE. Player name, supported national representation, appearance hook and authored initial attribute policy.
- WP-105 DONE. Seeded30-athlete fictional roster, stable ordinal IDs, age/potential/personality; no static shared identity.
- WP-106 DONE. 52-week calendar, six supported-format events, Pursuit source dependencies and bounded training/rest slots; Unity4/4 foundation tests.
- WP-107 DONE. Versioned race history, overall/format standings, atomic invalid-import rejection. .NET career6/6; Unity2/2. See TASK-107-REPORT.md.
- WP-108 DONE. Six data-driven training types, immutable weekly plans, calendar time limits and explicit recovery costs. .NET8/8; Unity2/2. See TASK-108-REPORT.md.
- WP-109 DONE. Potential/age/quality/recovery modifiers, diminishing returns and atomic capped gains. .NET10/10; Unity2/2. See TASK-109-REPORT.md.
- WP-110 DONE. Persisted training/race fatigue, skill-dependent rest and recovery sessions; .NET12/12, Unity2/2. See TASK-110-REPORT.md.
- WP-111 DONE. Data-driven training/taper/detraining form, fatigue suppression, unchanged permanent skills. .NET14/14, Unity2/2. See TASK-111-REPORT.md.
- WP-112 DONE. Bounded observable session/fatigue/age health risk; no punitive injury behavior. .NET15/15, Unity1/1. See TASK-112-REPORT.md.
- WP-113 DONE. Bounded morale/confidence, outcome/training effects and pressure signal; .NET16/16, Unity1/1. See TASK-113-REPORT.md.
- WP-114 DONE. Shared annual age/decline transitions from authored curve; .NET17/17, Unity1/1. See TASK-114-REPORT.md.
- WP-115 DONE. Shared weekly training execution and fatigue-aware AI planning; .NET19/19, Unity2/2. See TASK-115-REPORT.md.
- WP-116 DONE. Stable population and monotone identities over 50 years; .NET21/21, Unity2/2. See TASK-116-REPORT.md.
- WP-117 DONE. Exact prizes, equipment/services purchases and persisted camp entitlement; .NET23/23, Unity2/2. See TASK-117-REPORT.md.
- WP-118 DONE. Owned selection and condition-specific equipment tradeoffs; .NET28/28, Unity1/1. See TASK-118-REPORT.md.
- WP-119 DONE. Bounded season history and result-derived rivalry; .NET26/26, Unity2/2. See TASK-119-REPORT.md.
- WP-120 DONE. Fictional tier progression and idempotent season qualification; .NET28/28, Unity2/2. See TASK-120-REPORT.md.
- WP-121 DONE. Atomic weeks and actual three-format races; full .NET540/540, corrected career30/30, Unity weekly1/1 and actual-season1/1 with byte parity. See TASK-121-REPORT.md.
- WP-122 DONE. 15 seasons/90 actual races/21 replacements, population30, uniqueIDs51, max save167395 bytes; PASS. See TASK-122-REPORT.md.
- WP-123 DONE. Development/age/economy/progression JSON and CSV interpreted with provisional limits. See TASK-123-REPORT.md.
- WP-124 DONE. M9 PASS;142 input tuning lock, .NET3/3 and Unity1/1; milestone evidence complete. See TASK-124-M9-REPORT.md.
- WP-125 NOT STARTED. Next roadmap package: M10 presentation state adapters.

## Resume
M8 and M9 PASS. Requested autonomous scope through WP124 is complete. Next roadmap work is WP125 in M10; not started.
The provisional first-playable tuning version locks127 inputs; see TASK-102-M8-REPORT.md and quality/m8/verification.json.
Full .NET505/505 plus final tuner/lock5/5; Unity new15/15, PlayMode7/7; Console0 errors/1 warning.
WP096 top cohort meets provisional GDD shooting windows. Draft/stat/equipment sensitivity and AI parity are measured.
Known limits (fixed-condition cohorts, non-monotonic shooting timing, synthetic venues) remain explicit in reports.
No owner/blocker gate pending. Severe punitive injuries are excluded from WP112 absent owner-approved behavior.
Regular commit/push authorization persists. Update this state and reports for each coherent completed package.
## Historical M7 evidence (superseded as current status)

M7 PASS: three30-AI formats complete headlessly; Sprint qualification supplies Pursuit roster
and exact gaps; Mass Start uses an independent roster/grid.90 finishers,1500 accepted shots,
zero precision fallbacks. CLI repeat matches10 files; all3 result hashes match .NET and Unity.

Validation: full .NET484/484 before new factory, then its focused2/2; Unity championship2/2,
Mass Start matrix1/1 and result validation2/2.486 discovered, not a new full486 suite run.
Console0 errors,4 retained MCP warnings. PlayMode7/7 remains the earlier baseline.

Reports: TASK-088-089-REPORT.md, TASK-090-REPORT.md, MILESTONE-M7-REPORT.md.
Machine evidence: docs/quality/wp088-089 and wp090. No owner/blocker gate pending.
M8 calibration, production venues/presentation and withdrawal handling are not claimed by M7.
Preserve ADR0012–0017, canonical Foundation hashes and unrelated owner workspace edits.
Continue coherent commits/push and state updates under standing owner authorization.
## Publication history

Current verified publication: **4aef1d6** — WP123. WP124 / M9 closure is committed with this state update. M8 closure: **0311edf2e95dc6e33ff4603ad45a39cc997686b8**.

The entries below are historical checkpoints, not the current development position.
Owner explicitly authorized regular commits/push and state updates on2026-09-09.
Branch: codex/wp-078-ai-pack; origin: https://github.com/serejkakashkin-max/Biathlon.git.
The remote had only the initial template. This checkpoint publishes accumulated game source,
resources, tests, standalone projects and reports through WP-078. Unrelated local package/editor
settings, icon changes, local agent configuration and generated plugin dependencies are outside
this checkpoint. Publication: pushed source checkpoint 9697564949ee883ec839dc2c5ecafb846475bc9d to origin/codex/wp-078-ai-pack on2026-09-09. Isolated Git-snapshot restore/build/test passed441/441 in3m25s. This state update is a follow-up documentation commit.


Future workflow: after each verified coherent change, update this state with results, commit and push the task branch under the owner's standing authorization. Keep unrelated local edits separate.


WP079 publication confirmed: 95249b12c0b5c5a1ecab1e3281455bc6705ee17f on origin/codex/wp-078-ai-pack (2026-09-09).

M6 publication confirmed: 123631efcbc5594f040c3d0b8de9e3a379d09a19 on origin/codex/wp-078-ai-pack (2026-09-09).


WP080 publication confirmed: 4fefea1 on origin/codex/wp-078-ai-pack (2026-09-10).

WP081 publication confirmed: c3dee53 on origin/codex/wp-078-ai-pack (2026-09-10).
TASK082A verified: .NET6/6 and regression14/14; Unity MCP6/6, Console0 errors.
Historical next step at TASK082A: TASK085B; subsequently completed.

WP082B publication confirmed:46bddd0 on origin/codex/wp-078-ai-pack.
WP083 intentional Pursuit baseline14342ticks/result449333e5ccdfc60908353e5471f23d2b5285061e8ec6df011532337189dabff2 (.NET/Unity).

WP083 publication confirmed:1150138 on origin/codex/wp-078-ai-pack.
WP084 three scenario result hashes match .NET/Unity (docs/quality/wp084); max skiing stall9ticks (0.45s).

WP084 publication confirmed:7e52280e0873863ef6a979327ce1c67e1d54ce34 on origin/codex/wp-078-ai-pack (2026-09-10).

WP085A grid/kernel evidence:30 bodies,1200ticks, min travel504840986125nm, trace80c93f9e09952b64 (.NET/Unity).
Fixed350W kernel fixture is not full AI/physiology integration; see docs/reports/TASK-085A-REPORT.md.

TASK085A publication confirmed:3e342206926e6f32053759356012163077e8f79c on origin/codex/wp-078-ai-pack (2026-09-10).

TASK085B1 publication confirmed: a171ec99a4b0959ab28fce00a96cdf2815c4e0bc on origin/codex/wp-078-ai-pack (2026-09-10).

WP088-089 publication confirmed:4f50876; WP090/M7 publication confirmed:fa4cb932f65391328ac18274f72d497a1bef7acc on origin/codex/wp-078-ai-pack (2026-09-11). This follow-up records the verified source publication.

WP091-093 publication:0d19af3; WP094 publication:d83b08e; WP095 publication:ab963b68993824f76674a191e406ae054684f59e confirmed on origin/codex/wp-078-ai-pack (2026-09-11). This follow-up records the verified source publication.

WP096 publication: 92d3888. WP097–100 publication: 11a6592. WP101–102 / M8 PASS publication: 0311edf2e95dc6e33ff4603ad45a39cc997686b8.
WP103–106 publication: d3fcf016f8e33f1c2882588d33fb0c2f4456b0bf; confirmed on origin/codex/wp-078-ai-pack. M9 subsequently completed through WP124; next roadmap package WP125.

WP107 publication confirmed: 3755938. WP108: .NET career8/8, Unity2/2; publication accompanies this state update.

WP108 publication confirmed: db4d15f. WP109: .NET career10/10, Unity2/2; publication accompanies this state update.

WP109 publication confirmed: 820aff7. WP110: .NET career12/12, Unity2/2; publication accompanies this state update.

WP110 publication confirmed: 251bd89. WP111: .NET career14/14, Unity2/2; publication accompanies this state update.

WP111 publication confirmed: 213caab. WP112: .NET career15/15, Unity1/1; publication accompanies this state update.

WP112 publication confirmed: 6d88ff7. WP113: .NET career16/16, Unity1/1; publication accompanies this state update.

WP113 publication confirmed: 3def284. WP114: .NET career17/17, Unity1/1; publication accompanies this state update.

WP121 progress: full .NET540/540 PASS; initial actual-season Unity1/1 and byte-identical .NET/Unity saves. Mental pressure-context correction implemented; corrected career30/30 and Unity season1/1 PASS with byte-identical final saves. WP122 actual 15-season soak PASS. See TASK-121-REPORT.md. M9 closed by TASK124.
