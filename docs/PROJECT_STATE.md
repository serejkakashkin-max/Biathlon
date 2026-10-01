# BIATHLON production state

Authority: AGENTS.md → BIATHLON_MASTER_PLAN_FINAL_v2.0.md → GDD/TDD → ADRs.

```yaml
project_phase: M13 Production Venue Pipeline and Launch Content; G2 APPROVED 2026-09-30
active_work_package: WP-169 IN_PROGRESS; Venue01 production range prerequisites
active_task: TASK-169H2 shared pack actual-path projection, drafting, corridor/admission and joint commit; production geometry follows
last_completed_task: TASK169H1 complete cold field/layout binding and fixed-path field replay; .NET193/193 regression+final14/14 layout, Unity14/14 focused, PlayMode168/168 PASS
last_verified_commit: d549943; TASK169H1 implementation/report/state pushed to origin 2026-10-01; G2 target018 remains owner-approved
foundation_status: DONE; standalone baseline reverified 2026-09-06, 14/14
editmode_tests: TASK169H1 final layout14/14 PASS (730 discovered, focused); TASK169G receipt28/28, order14/14, session1/1 PASS (716 discovered, focused); TASK169F staging28/28 and choice5/5 PASS (673 discovered, focused); TASK169DE traversal9/9, athlete6/6, motion3/3, capacity3/3, projection6/6 PASS (640 discovered, focused); TASK169ABC contract13/13, map7/7, motion3/3 PASS (616 discovered, focused); WP167 authoring15/15 PASS; WP168 validator17/17 and intersections8/8 PASS (593 discovered, focused); target018 geometry5/5 PASS (553 discovered, focused); G2-VR013 device11/11 PASS (553 discovered); prior observations and M9 parity retained
playmode_tests: TASK169H1 full168/168 PASS on unchanged runtime code 2026-10-01; TASK169G full168/168 PASS on final tree 2026-10-01; TASK169F full168/168 PASS on final tree 2026-10-01; TASK169DE full168/168 PASS on final tree 2026-10-01; WP167/168 full168/168 PASS; G2-target018 full166/166 PASS,0failed/0skipped; G2-motion017 full164/164 PASS,0failed/0skipped; G2-VR016 full163/163 PASS,0failed/0skipped; G2-VR015 full164/164 PASS; G2-VR014 full161/161 PASS; G2-VR013 full158/158 PASS; current-design Windows delivery full155/155 PASS including actual interactive camera rendering; G2-VR009D full155/155,hero6/6 PASS; G2-VR011 full154/154,target2/2 PASS; G2-VR010 targeted4/4 PASS; G2-VR009C full151/151,target2/2 PASS; G2-VR009B full149/149, targeted3/3,preview3/3 PASS; G2-VR009A full146/146, appearance2/2,hero5/5,profiler3/3 PASS; G2-VR008A focused1/1 PASS (144 discovered, not full144); G2-VR007A full143/143, focused3/3 PASS; G2-VR006A full140/140, focused3/3 PASS; G2-VR005B full137/137, focused3/3 PASS; G2-VR005A full134/134 twice (before/after shader), focused3/3 PASS; G2-VR004G full131/131, focused3/3 PASS; G2-VR004F full128/128, focused3/3 PASS; G2-VR004E full125/125, shadow4/4, probe3/3 PASS; G2-VR004D full118/118 and focused4/4 PASS; G2-VR004C full114/114 and focused3/3 PASS; G2-VR004B full111/111 and final4/4 PASS; G2-VR004A full107/107 PASS; G2-VR003A full104/104 and focused3/3 PASS; G2-VR002B full101/101 and focused5/5 PASS; G2-VR002A full99/99 and final3/3 PASS; WP166 full96/96 and final focused3/3 PASS; WP164 full93/93 PASS; WP163 full90/90 PASS; WP162 full86/86 PASS; WP161 full83/83 PASS; WP160 full78/78 PASS and final focused4/4; WP159 full74/74 PASS and final focused4/4; WP158 full70/70 PASS; WP157 full66/66 PASS; WP156 full63/63 PASS and final focused3/3; WP155 full60/60 PASS; WP154 full57/57 PASS; WP153 full53/53 PASS; WP152 full51/51 PASS; WP151 full48/48 PASS; WP150 full46/46 PASS; WP149 full44/44 PASS; WP148 full42/42 PASS; WP147 full39/39 PASS; WP146 full38/38 PASS; WP145 full37/37 PASS; WP144 full36/36 PASS; WP143 full33/33 PASS; WP142 full30/30 PASS; WP141 full28/28 PASS; WP140 full25/25 PASS; prior WP139 evidence1/1 (24 discovered); prior full30 host1/1; prior Sprint-Pursuit chain1/1; prior full host1/1; prior HUD1/1; prior penalty1/1; prior shooting input3/3; prior range1/1; prior HUD1/1; prior device4/4, camera1/1; prior proxy1/1, scene1/1, mesh1/1 and full7/7
standalone_tests: TASK169H1 regression193/193 and final layout14/14 PASS; TASK169G focused171/171 PASS; TASK169F focused118/118 PASS; TASK169DE focused85/85 PASS; TASK169ABC focused39/39 PASS including six pinned scenario trace/event hashes; WP167/168 focused40/40 PASS; interrupted optional broad run UNVERIFIED; G2-VR013 full553/553 PASS,focused18/18; WP159 observations4/4 PASS; WP151 observations/snapshots5/5 PASS; WP141 observations/regression19/19; WP138 grid/results7/7; WP137 qualification/field6/6; WP136 player-pack/snapshot3/3; WP135 HUD/snapshot2/2; WP134 penalty10/10; WP133 aim/session10/10; WP130 input/replay11/11; prior full540/540, corrected career30/30, final lock3/3, 15-season soak PASS
unity_console_errors: 0 at TASK169H1 final snapshot 2026-10-01; compilation_failed=false; full PlayMode168/168 PASS
unity_console_warnings: TASK169H1 final snapshot4 existing warnings; cached deprecation/diagnostic entries inspected; no new warning suppressed; historical target018 BuildReport0
current_known_blockers: [] #G2 approved; production range flow remains acceptance work, not owner blocker
owner_gate_g1: APPROVED 2026-09-13; owner hands-on Windows greybox Sprint and Pursuit
owner_gate_g2: APPROVED 2026-09-30; explicit owner confirmation after target018; M12 PASS owner-approved
owner_decisions_needed: []
free_asset_authorization: APPROVED 2026-09-23; may search/download/use better free assets after source/license/de-branding audit; docs/owner/FREE_ASSET_AUTHORIZATION_2026-09-23.md
owner_gameplay_feel_issues: [OWNER-FEEL-001 player shooting feels too easy; OPEN; required WP229/WP230/WP232 review]
next_unlocked_work_packages: [WP169] #M13 gate after WP178
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
- WP-125 DONE. Detached state, guarded interpolation and Unity feed; .NET1/1, Unity1/1, PlayMode7/7. See TASK-125-REPORT.md.
- WP-126 DONE. Validated visual geometry and repeatable33-loop Unity mesh generation; .NET1/1, Unity1/1, mesh PlayMode1/1. See TASK-126-REPORT.md.
- WP-127 DONE. Generated saved venue,30 target banks,32 displayed loops; PlayMode1/1 and Game View inspected. See TASK-127-REPORT.md.
- WP-128 DONE.30 stable-ID snapshot-driven capsule proxies; PlayMode1/1, Game View inspected. See TASK-128-REPORT.md.
- WP-129 DONE. Chase camera, obstacle clearance and Game View verified.
- WP-130 DONE. KBM/gamepad command stream, focus release and replay verified.
- WP-131 DONE. Snapshot ski telemetry, reset handling and Game View verified.
- WP-132 DONE. Authored apertures, authoritative aim camera/closures and Game View verified.
- WP-133 DONE. Mouse/right-stick aim, trigger/breath and deterministic replay verified.
- WP-134 DONE. Physical penalty guide, loop obligations, tick clock and Game View verified.
- WP-135 DONE. Lap/range/bout results, split rank and final Sprint result HUD verified.
- WP-136 DONE. Playable Sprint scene, player/shared AI physics, full host race and keyboard Game View verified; G1 owner hands-on direction approved 2026-09-13 with shooting-feel issue retained.
- WP-137 DONE. Sprint-result transition, reordered player identity and full Pursuit completion verified.
- WP-138 DONE. Playable30-athlete Mass Start scene, full host finish and early pack Game View verified.
- WP-139 DONE. Frozen MCP screenshots, versioned state and integrity evidence verified; ribbon overlap logged.
- WP-140 DONE. Windows x64 development build and two-scene executable smoke PASS; shader stripping fixed; full PlayMode25/25. See TASK-140-REPORT.md.
- OWNER GATE G1 APPROVED 2026-09-13. Owner personally tested the Windows greybox Sprint and Pursuit; M10 PASS / owner-approved for core controls/gameplay direction. This does not constitute final visual, animation, atmosphere or game-feel acceptance. See [owner decision](owner/G1_DECISION_2026-09-13.md).
- WP-141 DONE. Detached animation observations, replaceable typed rig/socket binding, original humanoid prefab and animator/root-motion separation. .NET19/19; Unity new4/4 (550 discovered), full PlayMode28/28; Console0 errors/1 warning; Game View captures inspected. See reports/TASK-141-humanoid-architecture-REPORT.md.
- WP-142 DONE. Seven locomotion states, smooth transitions, original procedural motion and repeatable validation scene. Full PlayMode30/30; Console0 errors. See TASK-142-locomotion-REPORT.md.
- WP-143 DONE. Signed visible-route curvature, capped body lean and smooth reversal; full PlayMode33/33, Console0 errors; fixed/chase captures inspected. See TASK-143-cornering-REPORT.md.
- WP-144 DONE. Typed rifle/pole bindings, controlled carry/ready/return under real phase deadlines. Full PlayMode36/36; Console0 errors; fixed and actual range captures. See TASK-144-rifle-carry-REPORT.md.
- WP-145 DONE. Timed crouch/kneel/prone and return, stable rifle direction; full PlayMode37/37, Console0 errors, sequence/side captures. See TASK-145-prone-REPORT.md.
- WP-146 DONE. Planted standing stance, shoulder-ready transition and actual second-bout timing; full PlayMode38/38, Console0 errors. See TASK-146-standing-REPORT.md.
- WP-147 DONE. Sight axis1:1, hands/head/stance alignment; full PlayMode39/39, Console0 errors. OWNER-FEEL-001 presentation review completed, tuning issue remains OPEN. See TASK-147-shooting-ik-REPORT.md.
- WP-148 DONE. Rendered slope, ski normals and pole contact/recovery; full PlayMode42/42, Console0 errors. See TASK-148-contact-REPORT.md.
- WP-149 DONE. Observed exertion blend, tired skiing and finish fixture; full PlayMode44/44, Console0 errors. Finish-area visibility still requires WP151 review. See TASK-149-exertion-REPORT.md.
- WP-150 DONE. Reversible distant mesh/shadows,20 Hz pose cadence;30-body cost and0 B allocation verified; PlayMode46/46, Console0 errors. See TASK-150-crowd-performance-REPORT.md.
- WP-151 DONE. Four state sets, integrated transitions and actual chase/range evidence; PlayMode48/48, EditMode4/4, .NET5/5, Windows build/smoke PASS. M11 PASS WITH ACTIONS; see TASK-151-animation-validation-REPORT.md and MILESTONE-M11-REPORT.md.
- WP-152 DONE. Versioned low/balanced/high URP profiles, safe post policy, reversible camera/pipeline leases and explicit shader build inclusion. Full PlayMode51/51, frozen race snapshot parity, Windows six-profile smoke PASS, Console0/0. See TASK-152-render-quality-REPORT.md.
- WP-153 DONE. Original URP PBR snow, world-space 3D grain/slope/roughness variation and cold/wet presets; generated fixture, full PlayMode53/53 and four final captures, Console0/0. See TASK-153-snow-material-REPORT.md.
- WP-154 DONE. Bounded ski/pole marks with contact gaps, expiry, rewind and distance fade; full PlayMode57/57, actual-contact captures and4096-mark1.27345ms/0 B mesh benchmark. Console0 errors/1 MCP disposal warning. See TASK-154-ski-trails-REPORT.md.
- WP-155 DONE. Bounded ski/pole snow spray, contact/distance/tuck gating, rewind and expiry; full PlayMode60/60, 512-particle0.160467ms/0 B CPU mesh benchmark, Windows six-profile smoke PASS, Console0/0. See TASK-155-snow-vfx-REPORT.md.
- WP-156 DONE. Original winter forest, nested density, bounded instanced LOD/culling and rewindable wind; full63/63 plus final3/3, pixel-exact wind rewind, 598-tree runtime, CPU0 B and captures verified. Close foliage and LOD transitions remain G2 review actions. See TASK-156-vegetation-REPORT.md.
- WP-157 DONE. Versioned normalized four-layer PBR terrain, shared surface with explicit ice mask, material compatibility and geometry tests; full66/66, four captures, Windows six-profile smoke, Console0/0. See TASK-157-terrain-blending-REPORT.md.
- WP-158 DONE. Explicit visual weather presets, bounded snowfall, fog/clouds/light and reversible scene ownership; full70/70, seven captures, pixel-exact wind/weather rewind, CPU0 B verified. Integrated camera/readability review remains WP164–165. See TASK-158-weather-presentation-REPORT.md.
- WP-159 DONE. Explicit cold/load vapor, observed hold/phase, bounded allocation-free mesh, complete-race observation parity; .NET4/4, Unity EditMode4/4, full PlayMode74/74 and final focused4/4, close/chase captures and Windows smoke PASS. See TASK-159-breath-vapor-REPORT.md.
- WP-160 DONE. Original modular stadium/range prefabs, exact geometric target faces, nine fixed views; full78/78 and final focused4/4, no gameplay colliders or balance changes. Target cascade acne corrected; G2 art approval pending. See TASK-160-stadium-range-art-REPORT.md.
- WP-161 DONE. Original instanced spectators,156 authored seats, nested density tiers, near/far LOD, explicit-time gestures; full83/83, six fixed views and pixel-exact rewind, warmed0 B. Corrected venue text showing through roofs. See TASK-161-crowd-benchmark-REPORT.md.
- WP-162 DONE. Four explicit lighting profiles;12 fixed captures, focused3/3 and full86/86 PASS, camera/state restoration and warmed0 B. Late-day contrast and coarse snow grain require WP164/165 review. See TASK-162-lighting-benchmark-REPORT.md.
- WP-163 DONE. Six explicit-time broadcast shots, dedicated camera lease, opt-in zoom LOD; focused4/4 and full90/90 PASS,8 captures and pixel-exact rail rewind. No new package or gameplay camera changes. See TASK-163-broadcast-camera-tooling-REPORT.md.
- WP-164 TECHNICAL DONE / VISUAL ACTIONS OPEN. Integrated winter sector with10 detached pose fixtures, authored actual target bank and visible slope-aligned finish recovery. Full93/93 PASS; Windows3-scene/9-quality smoke PASS; Console0/0. Final visual quality not accepted. See TASK-164-integrated-visual-benchmark-REPORT.md.
- WP-165 DONE.27 fixed camera/weather/quality captures plus3 rail rewind captures; pixel-exact rewind, manifest/metadata verification PASS; Console0/0. Comparative review recommends Visual Gate FAIL; G2 not approved. See TASK-165-visual-screenshot-matrix-REPORT.md.
- WP-166 DONE (profiling/report); PERFORMANCE ACTION_REQUIRED. Windows hidden offscreen10×300 samples, CPU/draw/SetPass/triangles evidence, budgets and unavailable GPU/render-thread/batches explicitly recorded. Full96/96, focused3/3, Python3/3; build0 errors/0 warnings;9/9 real render smoke; Console0/0. Three triangle-budget failures recorded at WP166 were later resolved in004E; latest007A all10 observable budgets PASS, unavailable GPU limitation remains. See reports/WP-166-PROFILING-REPORT.md.
- WP-167 DONE 2026-10-01. Authoring manifest, generated prefab, semantic/camera/ambience/range bindings; Unity EditMode15/15, prior full PlayMode168/168 and .NET authoring15/15 PASS; generator rerun PASS. Optional full .NET session interrupted, no PASS claim. Report: reports/TASK-167-venue-authoring-REPORT.md.
- WP-168 DONE 2026-10-01. Pure geometry validator + intersections, Unity/CLI tools; .NET40/40 focused, Unity17/17+8/8 and full PlayMode168/168 PASS; CLI positive/invalid/legacy exits verified; canonical154 legacy diagnostics match Unity. Report: reports/TASK-168-venue-validator-REPORT.md.
- WP-169 IN_PROGRESS. TASK169A/B/C DONE: immutable shared-aisle physical-path contract, exact map/samples and optional ski-physics preview; .NET39/39, Unity13/13+7/7+3/3, full PlayMode168/168 PASS, Console0errors/5existing warnings. Report: reports/TASK-169ABC-range-path-prerequisites-REPORT.md. No live race selects the new map yet; shooting integration is deliberately rejected until personal stops exist. Next TASK169D prepared athlete movement/individual mat stop, then pack occupancy/admission, dense Mass Start and production geometry/visual acceptance. Venue01 is not DONE.
  - TASK169D/E DONE: optional fixed-path athlete with individual mat stops, accepted shooting/penalty movement, actual terrain in physiology/posture/physics, source context and safe receipt projection; repeated-slice capacity fixed. .NET85/85, Unity27 focused tests, full PlayMode168/168 PASS; Console0errors/5existing warnings. Report: reports/TASK-169DE-personal-mat-motion-REPORT.md. No field/pack factory selects the map yet; fixed solo integration does not certify dynamic assignment/dense flow. Next TASK169F prepared lane/path/traversal publication, then shared-pack flow and production geometry. The earlier169ABC shooting rejection describes that historical checkpoint.
  - TASK169F DONE: revision-bound prepared lane/path/personal-stop choices, preallocated traversal staging and joint accepted publication; cancellation/pre-entry travel emits no assignment, per-bout locks preserve previous exits. .NET118/118, Unity staging28/28+choice5/5, full PlayMode168/168 PASS; Console0errors/5existing warnings. Report: reports/TASK-169F-prepared-range-path-choice-REPORT.md. No field/pack factory selects the path; arbitrary external-choice replay reconstruction and fractional pack admission remain pending. Next TASK169G common-line arrival ordering, then pack flow/production geometry.
  - TASK169G DONE: accepted common-line receipts with minimum retimed crossing fraction, stable first-bib/later-arrival order, independent bout counters and revision-bound prepare/commit; rejected prefixes consume no ordinal. .NET171/171, Unity receipt28/28+order14/14+session1/1, full PlayMode168/168 PASS; Console0errors/5existing warnings. Report: reports/TASK-169G-fractional-range-arrival-REPORT.md. Real session integration proves cancelled/accepted path-order publication; no physical body admission or dense30 claim. Next TASK169H1 field/path construction, then pack flow/production geometry.

  - TASK169H1 DONE: cold exact-reference authored layout builds all paths once; independent field binds own initial lanes and complete cloned choices, retains legacy null context and full source topology context. Complete two-athlete five-lap/four-bout fixture, personal mat stops, repeat frames/events/results and persisted fixed-path replay on a rebuilt layout PASS. .NET193/193 regression plus final14/14 layout, Unity14/14, full PlayMode168/168 on unchanged runtime code; final Console0errors/4existing warnings. Report: reports/TASK-169H1-field-range-layout-REPORT.md. Independent field only; Pack rejects this binding before construction until trace/draft/corridor/admission are integrated. No dense30/production/approved-build change. Next TASK169H2 shared-pack flow.

Historical G2 remediation observations below describe their recorded checkpoints. Current G2 is APPROVED and M13 is active; old pending/blocker wording does not reopen that gate.

- G2-VR-001 DONE: all37 candidates audited; actual32 downloaded +1 manual +4 metadata,235 source hashes,6 Sketchfab CC BY metadata, CC0/limited-redistribution decisions recorded. No Unity imports at audit close. See reports/G2_VISUAL_ASSET_AUDIT.md.
- G2-VR-002 VISUAL TARGET UNMET: controlled hero body/rig/clothing/goggles/LOD checkpoints002A/B complete; integrated008A review confirms remaining hero quality actions. Remediation plan and open issues retained; awaiting G2 owner decision, M13 BLOCKED_G2.
- G2-VR-002A DONE: five audited candidates selectively adapted; continuous four-weight M11 skin, three LODs29224/13064/3482 triangles, CC0 normals and de-branded CC BY goggles; hero factory integrated in races/benchmark. Full99/99, final3/3, Windows build0/0, hidden9/9 render smoke, Console0/0. Source/derivative manifests and shipped credits recorded. Hero visual acceptance remains OPEN; glove/grip/bib/equipment/motion refinement continues in002B/003. See reports/G2_HERO_ATHLETE_IMPLEMENTATION.md.
- G2-VR-002B DONE: original closed gloves and late pole grip fit preserve grounded contacts; per-athlete bib01–60 uses shared-material property blocks. Full101/101, focused5/5, build0/0, hidden player9/9, Console0/0. Current LODs29834/13338/3557 triangles; manifests/evidence updated, prior002A retained. Motion/rifle/material/variation review remains OPEN; next003A.
- G2-VR-003A DONE: original unbranded rifle3580 triangles; hero grip sockets fit rifle without changing sight/pole positions; 400-step stance transfer and four-set motion matrix verified. Focused3/3, full104/104, build0/0, hidden player9/9, Console0/0. Skin joint quality, pole friction illusion and appearance variation remain review actions; G2 not approved. Next004A controlled fir/environment uplift; see reports/G2_ANIMATION_PRESENTATION_REPORT.md.
- G2-VR-004A DONE: G2-010 selected fir adapted to benchmark only; LOD6633/3268/588 triangles, four alpha/normal maps, instanced snow/wind shader, original placement/culling preserved. Full107/107, hidden9/9, build/Console0/0. Profile10x300 real frames: CPU/draw/SetPass within budgets, same3 triangle failures remain; GPU/render/batches unavailable, shutdown RT warning documented. Attribution/manifests shipped. Next004B snow/terrain; see reports/G2_ENVIRONMENT_UPLIFT_REPORT.md.
- G2-VR-004B DONE: selected CC0 Snow02 maps (2K/2K/1K), optional layer-aware material detail, modest off-route relief with mesh interpolation guard, world-space tree ground fit. Full111/111, final4/4, hidden player9/9; build261,997,752 bytes and final Console0 errors/3 MCP lifecycle warnings. Ten profile cases retain three triangle failures and unavailable GPU/render/batch counters; no performance/G2 approval. Source/runtime hashes and credits shipped. Next004C pine variation; reports/G2_ENVIRONMENT_UPLIFT_REPORT.md.
- G2-VR-004C DONE: selected CC BY pine, LOD2355/1253/1001 triangles; five resized source maps to four runtime maps with explicit opacity. Stable35% species selection keeps tree positions/density/LOD counts,0GC. Full114/114, focused3/3, hidden9/9, build270,358,989 bytes and Console0/0. Profile high-athlete5.404M vs5.712M; three triangle failures remain, no GPU/FPS qualification. Actual attribution/hashes shipped. Next004D off-route boulder composition; see environment report.
- G2-VR-004D DONE: selected CC0 Boulder01 LOD1652/494 triangles and2K/1K/1K maps, partial snow shader,16 bounded off-route decorations with stable IDs; route-margin/ground fit/0GC/no-collider checks. Full118/118, focused4/4, hidden9/9; build275,414,149 bytes and Console0/0. Ten profile cases retain three triangle failures and unavailable GPU counters. Manifests/credits shipped. Next004E forest composition/rendering-budget diagnosis; see environment report.
- G2-VR-004E DONE: isolated ablations identified crowd/forest/terrain shadow geometry; simpler shadow LODs and adaptive55,968-triangle surface preserve visible geometry/population and8cm height limit. Full125/125, shadow4/4, probe3/3, analyzer5/5, hidden9/9 PASS. All10 cases within3M triangles (max2,811,809); GPU/render-thread/batches still unavailable. Console0 errors/1 tooling warning. Next004F forest-to-route composition; visual acceptance remains open.
- G2-VR-004F DONE:24 stable-ID trees in trail-edge groves, explicit route/stadium/camera clearance and sampled terrain, no new source import. Obstructed first approach view rejected/corrected with regression. Full128/128, focused3/3, hidden9/9; all10 observable profile budgets PASS, unavailable counters retained. Next004G bounded understory; visual acceptance remains open.
- G2-VR-004G DONE: selected CC0 Shrub02 winter branch skeleton (summer leaves removed),2082/1170/520-triangle LODs, two1K maps and24 guarded placements; source/runtime hashes and credits. Full131/131, focused3/3, hidden9/9, Console0/0; all10 observable budgets PASS, unavailable GPU evidence retained. G2-VR004 technical environment batch complete with integrated visual review still open; next005A stadium/event.
- G2-VR-005A DONE: CC BY stand with advertisements removed, CC0 canopy,168 geometry-derived seating anchors, original roof snow; full134/134 after shader warning fix, hidden9/9 and all10 observable profile budgets PASS. Sources/credits/report recorded; final visual acceptance OPEN. Next005B event infrastructure/materials.
- G2-VR-005B DONE: selected CC0 screen frame, original event signage/four banners,1K concrete/iron maps and geometry-preserving UV derivatives; prefab override regression fixed. Full137/137, focused3/3, hidden9/9; all10 observable profile budgets PASS. Final stadium visual acceptance remains OPEN; next006A crowd.
- G2-VR-006A DONE: local restricted Renderpeople head-only derivatives, own winter clothes,2 variants/2LOD, bounded crowd integration and no public 3D data; full140/140, focused3/3, hidden9/9, all10 observable budgets PASS. Crowd close quality remains OPEN; next007A lighting.
- G2-VR-007A DONE: selected CC0 Passendorf upper sky, weather-aware aligned sun and data-driven snow ambient fill; full143/143, focused3/3, hidden9/9, all10 observable budgets PASS. Final visual acceptance OPEN; next008 combined review and owner G2 STOP.
- G2-VR-008A DONE as review package, not visual acceptance:45PNG2560x1440,30matched WP165 comparisons,48motion frames/2s WebP, production chase and real-bout dedicated cameras, posed close fixtures. Focused1/1 PASS (144discovered, not full144),0Console errors/1MCP warning. Last runtime full143/143 and hidden9/9 retained; no runtime or authoritative changes. Local verified Windows build preserved in Builds/G2OwnerReview-20260922. Reports/G2_VISUAL_BENCHMARK_REVIEW.md recommends G2 FAIL; STOP awaiting owner. Free-asset search/download/use authorization recorded2026-09-23; no new source downloaded for008A.

- G2-VR-009A DONE: fitted goggles, bind-space cloth/skin/lens shader,6 shared fictional kits; SetPass regression rejected and fixed via bounded material cache. Full146/146, hidden9/9, all10 observable budgets PASS. Hidden profiler verifies actual1920x1080 target and16:9 CPU frustum; window dimensions reported separately. Hero/motion acceptance remains OPEN; next009B. See reports/G2_FURTHER_REMEDIATION_REPORT.md.

## Owner gameplay-feel issue — OWNER-FEEL-001

**OPEN: player shooting currently feels too easy.** Reported by the owner during G1 hands-on review on 2026-09-13. This issue is carried forward explicitly; M10/G1 approval does not resolve it. Full review obligations and closure evidence are tracked in [OWNER-FEEL-001](owner/OWNER_FEEL_001_SHOOTING_TOO_EASY.md).

Mandatory review must examine both strength and player readability of:

- breathing sway;
- heartbeat impulses and current HR;
- muscular fatigue;
- stance-specific stability;
- pressure/composure;
- wind and ballistic influence;
- trigger timing;
- breath-hold trade-offs;
- rebound after an overlong breath hold;
- fine aim control;
- athlete shooting stats;
- difficulty/accessibility assists.

Also compare the greybox shooting presentation with authoritative aim/shot telemetry to determine whether the current visuals make shooting easier than the simulation challenge suggests. WP-147 and WP-151 should inspect presentation alignment/readability and retain evidence; they must not silently retune authoritative shooting. A demonstrated presentation defect may be corrected with a documented cause and focused verification.

Required return points: **WP-229 gameplay feel tuning**, **WP-230 difficulty/accessibility tuning**, and **WP-232 race balance finalization**. Each must reference this issue and record its findings, applied changes or explicit remaining follow-up. Player hits/misses remain physically and geometrically explainable; hidden RNG and probabilistic player hit resolution are forbidden. Any later numerical tuning must be explicit, versioned, logged and regression-tested according to the Master Plan.

## M11 execution constraints and gates

Use the [owner handoff](owner/CODEX_M11_HANDOFF.md), [visual addendum](owner/OWNER_M11_VISUAL_ADDENDUM_v1.md), [asset manifest](owner/M11_ASSET_MANIFEST_v1.md), [reference index](owner/M11_REFERENCE_INDEX_v1.md), and `docs/art/reference/m11/` alongside the authoritative Master Plan. Keep third-person chase skiing and the dedicated range camera; preserve G1-approved controls and simulation authority. References guide silhouette, poses, equipment and composition; real or pseudo-real logos/markings in them are not production assets.

Proceed through WP-141–151 with per-WP reports, Unity motion/capture validation, tests, state updates and coherent commits/pushes. Record third-party source, exact license and status before import. The M11 asset gate applies only if external license/account/paid-owner action is actually required; provide an asset shortlist/license report then. There is no automatic new owner gate after WP-151. The next fixed owner gate is **G2 after WP-166**, approving the M12 visual benchmark before producing all venues. No asset blocker has been established at M11 entry.

## Resume
M1–M10 сохранены; G1 APPROVED. M11 PASS WITH ACTIONS. G2 APPROVED владельцем2026-09-30; M12 PASS / owner-approved после remediation018. M13 активен: WP167 и WP168 DONE, WP169 IN_PROGRESS. Approval: owner/OWNER_G2_APPROVAL_2026-09-30.md. Продолжать Venue01 с обязательным боковым подъездом, реальными mat stops и dense range flow; не возвращать G2 в pending. Старые benchmark/greybox fixtures не являются production venues. Исторические GPU/render-thread/FPS limitations и triangle guard failures сохранены; visual approval не является performance PASS. Windows owner-review build018 остаётся последней визуальной поставкой до готовности production venue.
The provisional first-playable tuning version locks127 inputs; see TASK-102-M8-REPORT.md and quality/m8/verification.json.
Full .NET505/505 plus final tuner/lock5/5; Unity new15/15, PlayMode7/7; Console0 errors/1 warning.
WP096 top cohort meets provisional GDD shooting windows. Draft/stat/equipment sensitivity and AI parity are measured.
Known limits (fixed-condition cohorts, non-monotonic shooting timing, synthetic venues) remain explicit in reports.
OWNER-FEEL-001 remains OPEN for the mandatory shooting review at WP229/WP230/WP232; do not introduce hidden RNG or silently retune authoritative shooting during M11. Severe punitive injuries are excluded from WP112 absent owner-approved behavior.
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

Current verified implementation publication: **d549943** — TASK169H1 independent field/range layout binding, report and state pushed2026-10-01. TASK169F f9c7a41 and TASK169G b293bb7 are also pushed. WP167 d6b6806 and WP168 e9cd5a8 also pushed. Owner approvals G1/G2 and G2 target018 remain in effect. M10 technical delivery was completed at3466bf6; M8 closure: **0311edf2e95dc6e33ff4603ad45a39cc997686b8**. Later checkpoint publication accompanies its report/state commit.

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

M9 publication confirmed: b8ee32bac075cbd658a18172aa53fd8d2db42dbe. Every WP103–124 is individually marked DONE above; At that publication WP125/M10 had not started; user subsequently authorized continuation.

WP139 visual finding: coplanar schematic ribbons overlap near the penalty loop; evidence docs/quality/m10/wp139/tick-002500.png. Presentation fix pending; no authoritative movement impact.

- G2-VR-009B DONE: paired skating strokes/hip transfer and bounded world pole plants; full149/149,preview3/3,hidden9/9,10observable profile cases PASS.
- G2-VR-009C DONE as corrective checkpoint:4 bounded joint shapes perLOD, cheek fit/cant; full151/151,target2/2. No authoritative tuning; final visual acceptance/profile pending012.

- G2-VR-010 DONE as corrective checkpoint: winter silhouettes, elbow/shoulder motion, head variation;4/4 target tests, local restricted assets excluded from Git. Visual crowd target remains OPEN for012 review.

- G2-VR-011 DONE as corrective checkpoint: distant basin/forest/service shelters, benchmark-only atmospheric depth;full154/154,target2/2. Profile and final acceptance follow in012.

- G2-VR-009D DONE: final review-found harness edge projection/duplicate-face defect fixed;hero6/6,full155/155. New source imports:none; runtime/derivation hashes updated.

- G2-VR-012 DONE as review package, not visual acceptance:45matched008A/012 camera pairs,48motion frames,rewind pixel parity; full155/155,hero6/6,hidden9/9,10observable budgets PASS(max2791998tri/109SetPass;GPU unavailable). Local build Builds/G2OwnerReview-20260923-012/Windows verified393files. Recommend G2 FAIL; STOP awaiting owner. Master Plan unchanged, M13 blocked, OWNER-FEEL-001 OPEN. See reports/G2_REVIEW_012.md.

- G2 visual review Windows delivery DONE (2026-09-23 owner request): Builds/VisualReviewWindows/START_VISUAL_REVIEW.cmd opens the implemented new environment first. Fixed disabled interactive camera; full155/155, startup camera/controls PASS, hidden9/9, build0errors/0warnings, Console0/0; single-case offscreen diagnostic PASS (not FPS qualification). New venue remains a visual/animation fixture; playable Sprint/Mass Start still use prior tracks. Code/evidence/report: reports/G2_VISUAL_REVIEW_BUILD.md. G2 NOT APPROVED; awaiting hands-on feedback, no M13.

- G2-VR013 DONE as technical checkpoint (24 September, verified25 September): directional live steering holds achieved line; aiming mark115mm in both stances with unchanged45/115scoring; all4 local winter forms/10crowd variants; grounded skate recovery; detailed High forest/crowd shadows. Full.NET553/553,Unity device11/11,PlayMode158/158,Windows startup+9/9 PASS.10hidden profile executions complete; three historical triangle guard FAILs retained (High max16,650,738triangles);GPU unavailable. Review45frames+48motion frames. Range admission and garment/hero/motion quality remain OPEN. G2 NOT APPROVED; owner24September feedback supersedes012 waiting state. See reports/G2_HANDSON_REMEDIATION_013.md.

G2-VR013 publication confirmed25September: input/targets31fd1ba, presentation64ba448, report/state/evidence e26bc6123352dda89102777b9b7291f1b2038b03; origin branch verified. Restricted crowd payload remains untracked.

## G2-VR014 — 2026-09-26

Fabric shader defect fixed, CC0 roughness integrated, benchmark reflections and denser grounded distant forest. Full Unity MCP PlayMode161/161 PASS. [Report](reports/G2_VISUAL_REMEDIATION_014.md). Hero face/motion remediation continues autonomously; no G2 PASS and no M13. Existing Windows player is still013 pending the next completed review build.

## G2-VR015 — 2026-09-27

[Отчёт](reports/G2_VISUAL_REMEDIATION_015.md) · [Review015](quality/g2/owner-review-015/index.html). Лицо, новая прилегающая шапка по замечанию владельца27сентября, шейный бафф и траектория рук проверены;164/164 PlayMode,9/9 Windows render checks,10×300 hidden profile samples. Сборка `Builds/VisualReviewWindows/START_VISUAL_REVIEW.cmd` теперь015, включает014. G2 NOT APPROVED; визуальная цель ещё не заявляется достигнутой. Дальний белый склон/связность освещения леса, костюм/движение/толпа и range admission остаются OPEN. Новая площадка пока демонстрационная; production venues иM13 заблокированы. OWNER-FEEL-001 сохраняется без изменения shooting tuning.



## G2-VR016 — передано владельцу 2026-09-30

[Отчёт](reports/G2_VISUAL_REMEDIATION_016.md) · [Review016](quality/g2/owner-review-016/index.html). Возвращён hero baseline014 без нового баффа, упрощены лица толпы, перенос таза согласован с опорной лыжей. Исправлены коврики/визуальное центрирование всех30 lanes и review-постановка; реальные Sprint/Mass Start prone/standing проверены. Unity163/163, Windows9/9 PASS; 395 файлов сборки сверены по SHA-256. Сборка Builds/VisualReviewWindows/START_VISUAL_REVIEW.cmd — версия016 от27сентября. 10 кадров и2клипа переданы для owner review. Код8fbdb04 уже на origin. G2 НЕ PASS; M13 не начат. После этой поставки STOP до решения владельца согласно новой директиве. Ограничения процедурной анимации/костюма/crowd, старые игровые трассы и очередь перед общим входом перечислены в отчёте. OWNER-FEEL-001 остаётся OPEN для WP229/WP230/WP232; simulation/баланс не менялись.


Публикация016 подтверждена30сентября: код8fbdb04, report/state/review649ab522d0cfb02efbcd2fbd74048f51b5026372; origin совпадает. LFS12/12 (22MB) отправлены, SHA-256 всех25 файлов review manifest совпали. Ожидание owner G2 сохраняется.


## Owner feedback — 2026-09-30 / motion017

Упрощённая толпа APPROVED. Ноги улучшены; руки слишком частые, корпус требует согласования. Motion017 завершён; ожидает hands-on решения владельца. Очередь на greybox принята как ограничение; карту не дорабатываем. Для production venues обязательны требования docs/quality/PRODUCTION_RANGE_FLOW_ACCEPTANCE.md (WP167/168/169–174/177/178). G2 NOT APPROVED; M13 не начат. OWNER-FEEL-001 остаётся OPEN.

## G2 motion017 — передано владельцу 2026-09-30

[Отчёт](reports/G2_MOTION_REFINEMENT_017.md) · [Сравнение016 → 017](quality/g2/owner-review-017/index.html). Один парный толчок на пару шагов; уменьшена компрессия корпуса/таза; траектории лыж сохранены. Поза сильной усталости сохранена через дополнительный1см статического приседания. Full PlayMode164/164 PASS (194,757с), Windows9/9 PASS, Console0errors/0warnings. Кэш MCP содержит прежние ConvGeneric warnings. Все395 файлов сборки совпали с SHA-256. Код0cf1aa8b6abb6c51d7866b2b9882256632af2580. Builds/VisualReviewWindows/START_VISUAL_REVIEW.cmd теперь017. Пакет содержит два новых motion clips рядом с историческими016. Crowd APPROVED отдельно; G2 NOT APPROVED. Greybox queue accepted limitation; production flow requirement OPEN. Simulation/баланс неизменны. STOP до owner G2.

Публикация017 подтверждена30сентября: implementation0cf1aa8, review/state 4254b2433cd1e9d0f8315e960b191495cc9ffb1f; origin совпадает. LFS3/3 (7.2MB) отправлены. Manifest16 файлов review и2исторических reference hash проверен;395 build hashes совпали. G2 ожидает решения владельца.

## G2 target018 — передано владельцу 2026-09-30

[Отчёт](reports/G2_TARGET_CORRECTION_018.md) · [Review018](quality/g2/owner-review-018/index.html). Добавлена видимая граница малого45мм отверстия внутри115мм чёрного диска для prone; standing/closed без внутренней отметки. Размер взят из authoritative geometry; scoring/sight/баланс неизменны. PlayMode166/166, geometry5/5, Windows9/9 PASS;395 файлов сборки сверены. Console0errors/0warnings; cached ConvGeneric warnings сохранены. Builds/VisualReviewWindows/START_VISUAL_REVIEW.cmd теперь018.

Owner положительно оценил motion017 и готов согласовывать G2 после исправления мишени. Исправление018 завершено; окончательное APPROVED не подменяется агентом. M13/WP167 ждёт owner gate. Боковой подъезд внесён в production flow acceptance с проверенными official reference Ленцерхайде/Хохфильцен; тестовая карта не переделывалась. Crowd APPROVED; OWNER-FEEL-001 остаётся OPEN.

Публикация018 подтверждена: implementation28dc3bd, review/state fb3d532cb652b628b4c3ae233858a95f87390d36; origin совпадает. LFS3/3 отправлены;14 review files и395 build files сверены по manifest. Ожидается окончательное решение G2.

## OWNER GATE G2 — APPROVED 2026-09-30

Владелец явно подтвердил завершение G2 и переход далее. [Approval](owner/OWNER_G2_APPROVAL_2026-09-30.md). M12 PASS / owner-approved. M13 начат с WP167; обязательные боковой подъезд и dense range flow входят в pipeline acceptance. OWNER-FEEL-001 и hidden-only measurement limitations сохранены. Исторические G2 pending/rejected записи выше отражают прошлые checkpoints, а не текущее состояние.
