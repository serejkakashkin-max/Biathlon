# BIATHLON — FINAL MASTER PRODUCTION PLAN v2.0

**Status:** definitive production roadmap for autonomous Codex development  
**Date:** 2026-09-06  
**Engine:** Unity 6 / URP  
**Primary platform:** Windows PC  
**Execution model:** Codex is the main implementation agent; the owner/product director approves product, visual, licensing and scope gates.

---

## 0. Authority and supersession

This document is the **single authoritative master plan** for the BIATHLON project and supersedes the earlier planning-only documents `BIATHLON_RELEASE_SCOPE_v1.0.md`, `BIATHLON_MASTER_PLAN_v1.0.md`, `BIATHLON_TASK_INDEX_v1.0.md`, `BIATHLON_AUTONOMOUS_CODEX_PROTOCOL_v1.0.md`, and `CODEX_START_MASTER_PLAN.md` wherever they conflict.

Codex must still read and obey, in this order:

1. `AGENTS.md`;
2. this file;
3. `docs/BIATHLON_GDD_TDD_v0.1.md`;
4. approved ADRs;
5. the current atomic task;
6. tests and verified reports;
7. existing implementation.

`docs/TASK_001_FOUNDATION_REPORT.md` is the verified baseline report. TASK 001 must not be reimplemented without evidence that its protected invariants are broken.

---

## 1. Product north star

BIATHLON must become the most complete modern dedicated biathlon game in its category: a deep single-player sports simulation with a living career world, modern presentation and graphics, and a race model that rewards skill, pacing, positioning and shooting under physiological pressure.

The project does **not** aim to be a cosmetic remake of an older biathlon title. It combines the strongest historical ideas of the genre, then deliberately exceeds them in systemic depth, AI, career simulation, animation, visuals, atmosphere and production quality.

### 1.1 Competitive benchmark rule

The dedicated biathlon titles reviewed during preproduction — including **Biathlon 2005/2006**, **Wintersport Pro 2006**, **RTL Biathlon 2008**, **Biathlon Champion 2007**, **Biathlon Mania**, **Biathlon Battle VR**, and especially **NGL Biathlon 2026** — are references and minimum competitive benchmarks, **not targets to merely match**.

For Release 1.0, the intended quality bar is:

- gameplay depth: clearly more systemic and tactically expressive than the reviewed dedicated biathlon games;
- shooting: more physically understandable, skill-based and responsive, with no hidden RNG determining player hits;
- career: deeper, more persistent and more alive across seasons;
- AI: tactically differentiated and physically fair, without hidden speed/power cheats;
- animation: visibly more natural and contemporary;
- graphics: a clear generational step forward over NGL Biathlon 2026 and the older titles;
- snow/lighting/weather/vegetation/venues: modern premium-indie / AA sports presentation;
- TV presentation and atmosphere: strong enough that a race feels like a sporting event, not a technical simulation demo.

### 1.2 Mandatory visual superiority gate

**NGL Biathlon 2026 is the minimum visual competitor benchmark.** The project does not pass its final Visual Quality Gate if the representative benchmark scene fails to show a clear improvement in overall perceived quality, especially in:

1. athlete models/materials;
2. ski movement and body mechanics;
3. prone/standing rifle animation and transitions;
4. snow material and ski/snow interaction;
5. lighting, shadows and atmospheric depth;
6. winter vegetation and environment density;
7. stadium/range detail and crowd presentation;
8. weather and breath/snow VFX;
9. camera composition and broadcast presentation;
10. visual coherence in motion, not just still screenshots.

Before mass-producing venues, Codex must create a fixed **Visual Benchmark Scene**, fixed screenshot/video capture matrix, and a competitor/reference comparison report. If the benchmark is not clearly ahead, the gate is `FAIL` and production content does not scale.

A subjective final sign-off remains an owner gate; Codex must provide evidence, captures, profiling and specific recommendations rather than silently lowering the bar.

---

## 2. Verified current baseline

TASK 001 Foundation is complete. Treat these as protected facts unless a regression proves otherwise:

- local Unity MCP works;
- Domain / Simulation / AI / Career / Replay core boundaries exist independently of UnityEngine;
- standalone .NET, Unity EditMode and PlayMode test paths work;
- deterministic Foundation fixture matches across .NET and Unity for 10,000 ticks;
- warmed authoritative `Step` has zero managed allocation in the verified fixture;
- Unity presentation/bootstrap and diagnostics exist;
- 77 generated Unity/Codex skills exist;
- the known MCP disposal warning is upstream/non-blocking.

The next unlocked production area is physiology. Do not start authoritative ski movement before physiology acceptance.

---


# PART A — RELEASE SCOPE AND PRODUCT REQUIREMENTS

**Статус:** целевая спецификация коммерческой версии 1.0  
**Дата:** 2026-09-06  
**Движок:** Unity 6 / URP  
**Платформа 1.0:** Windows PC  
**Production model:** основной исполнитель — Codex; человек принимает продуктовые и дизайнерские решения, которые нельзя вывести из спецификации или объективных тестов.

---

## 1. Product promise

BIATHLON — современный single-player симулятор биатлона с глубокой карьерой, в котором результат гонки определяется не QTE и не скрытыми бонусами, а сочетанием:

- навыка игрока;
- физиологии спортсмена;
- темпа и распределения сил;
- лыжной техники и профиля трассы;
- drafting / position / overtaking;
- стрельбы без hidden RNG для игрока;
- ветра, снега и условий;
- формы, усталости и развития;
- честного AI, использующего ту же simulation model;
- экипировки с умеренным влиянием;
- тактических решений в Sprint, Pursuit и Mass Start.

**Core principle:** `Easy to understand — difficult to master.`

---

## 2. Launch scope — MUST HAVE

### 2.1. Дисциплины

Версия 1.0 обязана содержать:

1. Sprint;
2. Pursuit;
3. Mass Start.

Правила задаются data-driven `CompetitionProfile`; система не должна быть архитектурно привязана только к этим трем форматам.

### 2.2. Карьера

Полноценная multi-season career:

- создание спортсмена;
- возраст и развитие;
- скрытый potential;
- форма;
- накопленная fatigue;
- recovery;
- training planning;
- календарь сезона;
- standings;
- prize/economy layer;
- equipment progression без loot-grind;
- AI athletes, которые развиваются, стареют и завершают карьеру;
- генерация нового поколения AI;
- история результатов;
- rivalry data;
- сохранения и миграции схем.

Карьера не должна иметь жесткий короткий предел. Система должна поддерживать 15+ сезонов без деградации данных и без статичного AI-пелотона.

### 2.3. Спортсмены

Минимальная системная емкость:

- Mass Start: 30 участников;
- Pursuit: до 60 участников;
- карьерная база: минимум 120 уникальных AI-профилей одновременно, с возможностью генерации новых спортсменов.

Персонажи и названия — оригинальные/fictional, если отсутствует отдельная лицензия.

### 2.4. Трассы / venues

**Рекомендуемый launch minimum:** 6 оригинальных качественных venues.

Каждый venue должен иметь:

- визуально узнаваемый характер;
- собственный профиль высот;
- набор semantic zones;
- стадион/стрельбище;
- штрафной круг;
- несколько lap configurations, если это нужно дисциплинам;
- минимум одну тактическую особенность: длинный подъем, технический спуск, открытый ветровой участок, узкий проход, сильный drafting sector и т.п.;
- вариативные snow/weather presets.

Если production cost финальных 6 venues окажется выше разумного, уменьшение количества — только через owner decision. Нельзя молча заменить качество количеством или наоборот.

### 2.5. Season structure

Целевая первая полноценная календарная модель:

- 6 venues;
- 14–18 гонок за сезон;
- разнообразные weekend configurations из трех доступных дисциплин;
- Sprint может формировать старт Pursuit;
- Mass Start qualification/ranking правила задаются профилями данных;
- standings и career progression работают между сезонами.

### 2.6. Стрельба

Обязательные элементы:

- prone;
- standing;
- 5 выстрелов на рубеж;
- физически/геометрически определяемый hit/miss для игрока;
- breathing sway;
- heartbeat impulse;
- muscular fatigue sway;
- stance-specific stability;
- breath control;
- wind;
- trigger timing;
- configurable aim assists;
- shooting range training mode;
- AI shooting с той же геометрией/ограничениями, но собственным controller model.

Запрещено определять попадание игрока скрытым `Random()`.

### 2.7. Лыжный gameplay

Обязательные системы:

- requested effort/power;
- aerobic capability;
- anaerobic reserve;
- muscular fatigue;
- heart rate;
- recovery;
- terrain slope;
- technique efficiency;
- tuck;
- drafting;
- overtaking;
- positional occupancy/collision policy;
- cornering/technical descent;
- snow condition influence;
- equipment influence;
- final sprint;
- pre-range recovery decision.

### 2.8. AI

AI обязан:

- использовать ту же authoritative physics/simulation;
- не иметь скрытого speed/power multiplier;
- принимать решения по состоянию гонки;
- уметь recover / hold pace / follow / draft / overtake / attack / final sprint / prepare for range;
- иметь personalities;
- уметь различать Sprint/Pursuit/Mass Start tactics;
- учитывать текущий HR, fatigue, anaerobic reserve, gaps, opponents и terrain ahead;
- демонстрировать разные shooting pace/risk profiles;
- создавать pack behavior без прохождения спортсменов друг сквозь друга.

### 2.9. Presentation

Launch minimum:

- chase gameplay camera;
- shooting camera;
- broadcast/replay cameras;
- live timing;
- splits/gaps;
- rank and position;
- shooting board;
- penalty state;
- finish/results presentation;
- replay/highlight foundation;
- crowd and stadium ambience;
- dynamic weather presentation;
- high-quality snow/lighting target;
- athlete breath vapor;
- ski tracks / snow contact;
- coherent animation transitions.

### 2.10. Controls

- gamepad first-class;
- keyboard + mouse first-class;
- remapping;
- sensitivity options;
- steering assist;
- optional technique assist;
- optional aim damping;
- colorblind-safe UI;
- difficulty presets change assists/AI cognition, not AI physics.

### 2.11. Audio

- ski snow loop;
- poles;
- wind;
- breathing;
- contextual heartbeat;
- rifle shot;
- target hit mechanism;
- crowd intensity;
- UI feedback;
- ambient venue audio;
- mix states for skiing/range/final sprint.

### 2.12. Save/settings

- versioned save schema;
- at least 3 manual/automatic career save slots;
- autosave with corruption-safe strategy;
- migration tests;
- graphics settings;
- audio settings;
- controls/settings persistence;
- accessibility settings;
- deterministic gameplay config version stored with save/replay where required.

---

## 3. Explicitly OUT of 1.0 unless owner expands scope

- multiplayer;
- VR;
- official licensed IBU branding;
- licensed real athletes;
- licensed real venues;
- Individual race;
- Relay;
- Mixed Relay;
- console ports;
- user track editor;
- live service / battle pass / microtransactions;
- mobile version.

Architecture may support future addition, but Codex must not spend 1.0 budget implementing these.

---

## 4. Visual quality strategy

Goal: **clearly exceed the perceived visual quality of every dedicated biathlon title reviewed during preproduction, with NGL Biathlon 2026 treated as the minimum modern benchmark rather than a target to merely match.** The desired presentation tier is modern premium-indie / AA sports quality, subject to realistic asset/licensing constraints and owner-approved quality gates.

Codex must treat visual quality as a measurable production pipeline rather than a late cosmetic pass.

### 4.1. Required visual benchmark scene

Before mass-producing venues, create one benchmark sector containing:

- stadium/range;
- forest sector;
- climb;
- descent;
- 8–12 athletes;
- snowfall preset;
- clear weather preset;
- dawn/overcast lighting variants;
- final material/shader direction;
- representative athlete animation;
- broadcast camera shot.

No additional production venue should be authored until this scene passes the visual gate.

### 4.2. Asset sourcing rule

Codex may:

- create procedural/editor-generated geometry;
- create shaders/materials/VFX;
- generate tool pipelines;
- integrate already available project assets;
- propose third-party/free assets.

Codex must NOT:

- purchase assets;
- accept paid licenses;
- use unclear-license models/audio/textures;
- upload project content to external services;
- claim ownership of third-party content.

For every external asset, record:

- source;
- exact license;
- attribution requirement;
- version/date;
- files imported.

If a high-quality character/animation target requires an external asset/account action, Codex must prepare the exact recommendation and stop only for that owner action.

---

## 5. Performance targets

Baseline targets for release validation:

- 60 FPS target at 1080p on the chosen reference PC;
- no simulation-dependent frame-rate behavior;
- 30 visible athletes in Mass Start without simulation bottleneck;
- no recurring managed allocations in authoritative tick loops;
- headless race simulation substantially faster than realtime;
- loading into race without uncontrolled editor/runtime errors;
- no memory growth across repeated race/restart loops;
- performance presets scalable for vegetation/shadows/crowd/VFX.

Exact hardware tiers must be measured and locked during Alpha; Codex must not invent minimum/recommended Steam specs before profiling real builds.

---

## 6. Release quality gates

Release Candidate cannot ship unless:

1. all MUST HAVE scope above exists;
2. all blocking tests pass;
3. no compile errors;
4. no known save corruption bug;
5. no deterministic simulation regression without an approved tuning/version change;
6. no AI physics cheats;
7. player shooting hit geometry has no hidden RNG;
8. all three race formats can complete repeatedly with 30/60 AI as applicable;
9. full career can simulate 15 seasons headlessly without fatal errors;
10. career can be played through at least 3 full seasons in integration soak tests;
11. six production venues (or owner-approved revised count) pass content validation;
12. controller and keyboard/mouse flows are complete;
13. options/save/load work in player build, not only Editor;
14. launch build has crash/log review completed;
15. all external asset licenses are documented;
16. no secrets, developer credentials or local-machine paths are packaged;
17. build/versioning pipeline creates reproducible release artifacts;
18. owner signs off on gameplay, career balance and visual benchmark.

---

## 7. Definition of success

The game succeeds when a player can understand the controls quickly, but after many hours still improves through:

- pacing;
- recovery timing;
- drafting;
- line/position choice;
- reading race situation;
- shooting rhythm;
- pressure management;
- training/form planning;
- learning individual opponents and venues.

A win should feel explainable by the simulation and the player's decisions, not by opaque dice rolls or AI rubber-banding.


# PART B — PRODUCTION STRATEGY AND SYSTEM DESIGN ROADMAP

**Purpose:** единый маршрут от уже завершенного TASK 001 Foundation до feature-complete, tested, polished Windows PC Release Candidate.  
**Execution model:** Codex читает план целиком, сам декомпозирует Work Packages в атомарные TASK, выполняет их по dependencies и продолжает автономно до blocker/owner gate.

---

# 0. Current verified baseline

TASK 001 Foundation считается завершенным и является protected baseline.

Зафиксировано:

- локальный Unity MCP работает;
- чистые Core assembly boundaries существуют;
- Domain/Simulation/AI/Career/Replay отделены от UnityEngine;
- standalone .NET + EditMode + PlayMode test paths работают;
- canonical 10,000-tick deterministic hash совпадает между .NET и Unity для Foundation fixture;
- critical warmed Step allocation = 0;
- Unity presentation/bootstrap и diagnostics существуют;
- 77 agent skills сгенерированы;
- известный upstream MCP console warning не является blocker.

Не переписывать Foundation ради вкусовых предпочтений. Любое изменение protected invariants — только с доказательством и ADR.

---

# 1. Master strategy

Разработка идет в порядке риска:

```text
Simulation correctness
→ Physiology
→ Track/Ski dynamics
→ Shooting
→ Race rules
→ AI
→ 3 complete formats
→ Balance Lab
→ Career
→ Playable greybox
→ Visual benchmark
→ Production content
→ UX/TV/audio
→ Reliability/performance
→ Alpha
→ Beta
→ RC
```

Причина: Codex сначала должен доказать, что гонка интересна и математически стабильна, прежде чем массово производить графический контент.

---

# 2. Global non-negotiable invariants

1. Authoritative gameplay core does not depend on Unity rendering frame rate.
2. Core simulation assemblies remain free of UnityEngine unless an explicit approved architectural revision changes a boundary.
3. Player hit/miss is not hidden RNG.
4. AI has no hidden physical multiplier.
5. Tuning is data-driven and versioned.
6. Race-affecting systems are observable through telemetry.
7. Important race events are replay/event-log visible.
8. New systems have automated tests before completion.
9. Unity Editor manual Inspector setup is not required when code/tooling can author/validate it.
10. Save schemas have stable IDs/versioning/migration strategy.
11. External assets/packages have documented licenses.
12. No remote publish/push/account action without owner authorization.
13. Gameplay choices have trade-offs; avoid dominant always-correct inputs.
14. Accessibility assists may simplify control, but cannot secretly rewrite opponent physics.
15. Codex may tune toward approved target ranges but cannot redefine product identity autonomously.

---

# 3. Milestone map

| Milestone | Outcome | Principal proof |
|---|---|---|
| M0 | Foundation | already PASS |
| M1 | Physiology | workload/recovery traces and deterministic tests |
| M2 | Track/Ski dynamics | headless mathematical time trial |
| M3 | Player controls | replayable deterministic command stream |
| M4 | Shooting | complete headless 5-shot bouts without player RNG |
| M5 | Sprint | complete headless Sprint |
| M6 | AI | 30 believable athletes with no physical cheats |
| M7 | Pursuit + Mass Start | all 3 formats headless |
| M8 | Balance | batch simulation and locked provisional tuning |
| M9 | Career | 15-season living world soak |
| M10 | Playable greybox | all 3 formats playable in Unity |
| M11 | Athlete animation | coherent humanoid/rifle/ski presentation |
| M12 | Visual benchmark | approved representative final-quality sector |
| M13 | Production content | recommended 6 launch venues |
| M14 | UI/TV/Replay | complete player-facing game loop |
| M15 | Audio | production gameplay/ambience mix |
| M16 | Save/settings/reliability | robust persistent product |
| M17 | Alpha | feature complete, profiled, regression automated |
| M18 | Beta | tuned/polished, blocker bugs driven down |
| M19 | RC | release-candidate Windows build and audits |

Detailed work packages are defined in `BIATHLON_TASK_INDEX_v1.0.md`.

---

# 4. M1 — Physiology detailed design implementation plan

M1 converts abstract athlete stats into physical state. It is the first race-affecting layer after Foundation.

## 4.1. Data model

Create versioned physiology config containing at least:

- resting HR;
- max HR;
- sustainable/aerobic power proxy;
- HR rise tau;
- HR recovery tau;
- cardiac drift strength;
- anaerobic capacity;
- anaerobic threshold;
- anaerobic drain curve/scale;
- anaerobic recovery curve/scale;
- muscle fatigue resistance;
- fatigue recovery rates;
- power-efficiency fatigue curve.

Use explicit units in names/docs. Avoid ambiguous generic floats.

## 4.2. Fixed-tick behavior

At each simulation tick:

1. receive requested relative effort/power;
2. derive load relative to current athlete capability;
3. update anaerobic drain/recovery;
4. update muscular fatigue;
5. derive target HR;
6. update HR through rise/recovery inertia;
7. apply bounded cardiac drift;
8. emit telemetry/events only when appropriate;
9. avoid per-tick allocations.

## 4.3. M1 verification scenarios

At minimum:

- 5 min low load;
- 3 min race load;
- 60 sec recovery;
- repeated attack/recovery intervals;
- fresh vs fatigued athlete;
- Recovery 40 vs 80;
- Anaerobic 40 vs 80;
- same workload repeated twice for deterministic hash;
- invalid config rejection;
- long-run stability/no NaN/Inf;
- allocation benchmark.

M1 does not move the athlete along a track.

---

# 5. M2 — Track and skiing detailed strategy

## 5.1. Track as data, not scene magic

Authoritative course represented by data indexed by route distance:

```text
distance
position reference / route coordinate
slope / grade
curvature
width
surface/snow profile
wind exposure
semantic zone
overtake opportunity
checkpoint/range metadata
```

Unity spline/mesh is presentation/authoring adapter.

## 5.2. Ski dynamics model

Use a game-physics model that is explainable and tunable, not an unnecessarily scientific finite-element simulation.

Minimum forces/factors:

- athlete propulsion from physical capability + requested effort;
- gravitational grade term;
- aerodynamic drag;
- rolling/snow resistance;
- technique efficiency;
- skis/equipment condition;
- drafting reduction;
- curvature/technical speed constraint.

The model must naturally produce:

- higher cost uphill;
- speed gain/downhill tuck;
- recovery opportunity downhill;
- meaningful drafting;
- fatigue-driven late-race decline;
- different athlete strengths on different course profiles.

## 5.3. Position and contact

Avoid using Unity Rigidbody collisions as authoritative race positioning.

Maintain track-relative position/lateral occupancy. Unity bodies interpolate/present.

Mass Start requirements:

- no overlapping athletes;
- no infinite blocking deadlocks;
- finite overtaking opportunity;
- wide/narrow sectors matter;
- AI cannot teleport through packs.

---

# 6. M3 — Player control philosophy

Player controls decisions, not every pole stroke.

Primary inputs:

- steering/line;
- effort/power;
- attack/recovery presets;
- tuck;
- shooting aim/trigger/breath.

Advanced depth comes from timing and race context.

Controls must support:

```text
5 minutes → understand
5 hours   → understand strategy
50 hours  → still improve execution
```

Record authoritative commands so a race can be replayed/debugged independently from real-time input polling.

---

# 7. M4 — Shooting product target

Shooting is a skill challenge generated by athlete state, not an accuracy dice roll.

## 7.1. Aim composition

Authoritative aim state combines:

- player controller input;
- breathing waveform;
- heartbeat impulses derived from current HR;
- muscular instability;
- stance;
- weapon handling;
- pressure/composure effect;
- wind/ballistic effect.

## 7.2. Athlete stats

Stats influence difficulty of maintaining aim and recovering between shots. They do not invalidate a geometrically correct player shot.

## 7.3. Breath control

Trade-off:

```text
less breathing sway now
↔ oxygen/discomfort cost
↔ rebound/instability after excessive hold
```

## 7.4. AI shooting

AI receives aim control noise/decision model based on athlete skills, context and pressure, then fires through the same target geometry.

Balance Lab validates hit-rate windows; production gameplay does not call a hidden hit-percent function.

---

# 8. M5–M7 — Race formats

## Sprint identity

- individual/interval start;
- two bouts P/S;
- race against clock;
- pacing vs shooting preparation;
- source result for Pursuit.

## Pursuit identity

- result gaps become start gaps;
- direct position battle;
- four bouts PPSS;
- group dynamics and gap decisions are central.

## Mass Start identity

- 30-athlete simultaneous start;
- densest pack/drafting/contact challenge;
- four bouts PPSS;
- position before range and final-lap tactics matter strongly.

All rules from data profiles; no one-off scene code.

---

# 9. M6 — AI product architecture

AI pipeline:

```text
Perception snapshot
→ Tactical utility evaluation
→ Intent
→ Low-level pace/line controller
→ Same simulation inputs as player
```

Personality parameters include:

- aggression;
- patience;
- risk tolerance;
- draft preference;
- overtake preference;
- pre-range caution;
- final-lap aggression;
- shooting pace preference;
- pressure resistance/consistency.

AI debug must answer:

> Why did athlete X attack here?

with state/utility values, not a black box.

---

# 10. M8 — Balance Lab as first-class product tool

Balance Lab is not optional dev tooling. It enables an autonomous agent to improve game balance without guessing.

Required commands/scenarios should support:

- one athlete vs course;
- one strategy comparison;
- 30-athlete race;
- 10,000+ seeded race batch;
- career-season batch;
- marginal stat analysis;
- parameter sweep;
- regression against target windows.

Outputs:

- JSON machine-readable metrics;
- CSV data;
- concise Markdown summary;
- regression result PASS/WARN/FAIL.

Codex can automatically tune only declared tuning parameters and must log each change.

---

# 11. M9 — Career product design implementation

Career is a world simulation, not a menu around races.

## 11.1. Athlete layers

Permanent/long-term:

- age;
- potential;
- growth characteristics;
- baseline physiological traits;
- mental traits.

Trainable:

- endurance;
- anaerobic capacity;
- recovery;
- strength;
- ski technique/climbing/flat/downhill/cornering;
- prone/standing/breath/shooting speed;
- composure/pressure/fighting spirit where design uses them.

Current state:

- form;
- fatigue;
- morale;
- confidence;
- health;
- race readiness.

## 11.2. Training

Each session has:

- target adaptations;
- fatigue cost;
- recovery need;
- injury/health risk foundation;
- time cost;
- diminishing returns.

The player should make meaningful weekly planning decisions, not click every available upgrade.

## 11.3. AI careers

AI uses the same career state concepts. It may use simplified optimization, but no static hand-authored stats forever.

World requirements:

- prospects appear;
- stars peak;
- veterans decline;
- retirements occur;
- new athletes replace them;
- records/history persist.

## 11.4. Economy

Economy supports career choices but must not become grind.

Money affects:

- training camps/services;
- recovery services;
- modest equipment acquisition.

No randomized loot rarity system.

---

# 12. M10 — Greybox vertical slice acceptance

Before production art, a human player must be able to:

1. open game;
2. start Sprint;
3. control athlete on greybox course;
4. draft/overtake AI;
5. see HR/effort response;
6. enter range;
7. shoot prone/standing;
8. complete penalty loop if needed;
9. finish and receive result;
10. run Pursuit from prior Sprint result;
11. run Mass Start with 30 athletes.

All three formats must work in a Windows development build, not only Editor.

---

# 13. M11–M13 — Graphics/content production strategy

## 13.1. Order

Do not build six mediocre venues in parallel.

```text
one benchmark athlete
+ one benchmark venue sector
+ final snow/forest/lighting/animation direction
→ approve/validate
→ production pipeline
→ remaining venues
```

## 13.2. Animation quality priorities

Most visible credibility issues in a sports game come from motion, not polygon count.

Required state family:

- flat skating;
- climbing at multiple intensities;
- acceleration;
- sprint;
- tuck;
- technical descent;
- cornering;
- fatigue finish;
- rifle on/off back;
- prone transitions;
- standing transitions;
- firing/bolt/recovery.

Use IK/procedural layers to keep poles/skis/rifle aligned.

## 13.3. Venue differentiation

Each venue must differ in both gameplay and visual identity.

Examples of tactical identities (names intentionally generic/original):

- steep alpine climb / fast descent;
- rolling forest / drafting-heavy flats;
- windy open plateau;
- technical narrow course;
- stadium-centric fast circuit;
- long endurance climbs with recovery descents.

Codex chooses final fictional names after ensuring no accidental licensed branding dependence.

---

# 14. M14–M15 — Sports broadcast presentation

A biathlon game should make the race understandable even when 30 athletes compete.

Required UI/TV information hierarchy:

- current position/rank;
- gap to leader / nearby athletes;
- lap;
- split/checkpoint;
- next shooting stage;
- HR/effort/resources only as much as gameplay needs;
- shooting target board;
- misses/penalty loops;
- result changes after range;
- final finish standings.

Replay/highlight director listens to race events rather than guessing from camera position.

Important highlight candidates:

- lead change;
- close overtake;
- final shooting;
- key miss by leader;
- perfect 5/5 under pressure;
- last-lap attack;
- photo/close finish.

---

# 15. M16 — Reliability design

Career games live or die by save trust.

Rules:

- never write directly over only good save without safe replacement strategy;
- include schema version;
- validate after serialization;
- keep migration fixtures;
- preserve stable IDs;
- avoid scene references in save state;
- test interrupted/invalid save paths;
- export useful diagnostics without secrets.

---

# 16. M17–M19 — Production quality process

## Alpha

Definition:

- all launch features exist;
- all recommended venues/content integrated or explicit scope exception approved;
- Windows builds automated;
- performance baselines exist;
- known bugs triaged;
- no architecture placeholders blocking player loop.

## Beta

Definition:

- feature freeze except fixes/polish;
- balance/playfeel tuned;
- career economy/development stabilized;
- content/UI/audio/animation polish;
- soak tests repeated;
- performance regression tightly controlled.

## RC

Definition:

- only release-blocker fixes;
- license/security/clean-machine/save/build audits;
- full regression green;
- final known-issues list;
- owner approval before external publication.

---

# 17. Quality metrics and automated acceptance

Codex must maintain a quality dashboard with at least:

### Code/test health

- standalone test pass count;
- EditMode pass count;
- PlayMode pass count;
- build smoke status;
- Unity Console errors/warnings.

### Simulation

- deterministic regression status;
- per-tick allocation benchmark;
- simulation throughput;
- invalid-state/NaN guard status.

### Race balance

- mean/variance race times by field tier;
- prone/standing hit distributions;
- penalty count distribution;
- pacing strategy differences;
- draft benefit distribution;
- marginal stat effects.

### AI

- finish completion rate;
- overlap/deadlock count;
- tactical intent distribution;
- unexplained/invalid decision count;
- cheat-parity audit.

### Career

- 15-season soak success;
- population stability;
- age/skill distribution;
- economy balance metrics;
- save migration tests.

### Rendering

- reference scene frame time;
- Mass Start frame time;
- memory peak;
- repeated race memory delta.

---

# 18. Owner interaction policy

Codex should NOT repeatedly ask owner:

- which class name to use;
- whether to run tests;
- whether to fix a compiler error;
- whether to create a helper tool;
- whether to refactor a local implementation when tests prove it necessary;
- whether to open Unity/read Console/take screenshot.

Codex SHOULD ask when:

- a permanent game-feel target is unspecified and alternatives materially differ;
- art direction requires a subjective choice after benchmark options exist;
- external licensed asset selection requires approval;
- release scope must shrink/grow;
- publishing/account/paid action is required.

When asking, include recommendation and evidence.

---

# 19. Recommended repository documents Codex maintains

```text
AGENTS.md

docs/
  BIATHLON_GDD_TDD_v0.1.md
  BIATHLON_RELEASE_SCOPE_v1.0.md
  BIATHLON_MASTER_PLAN_v1.0.md
  BIATHLON_TASK_INDEX_v1.0.md
  BIATHLON_AUTONOMOUS_CODEX_PROTOCOL_v1.0.md
  PROJECT_STATE.md

  tasks/
  reports/
  adr/
  tuning/
  quality/
  licensing/
```

Generated artifacts/balance captures/screenshots may live under ignored `artifacts/` but milestone reports belong in version control.

---

# 20. Immediate next action after installing this plan

Codex must NOT create all 255 work-package files up front.

It should:

1. ingest this Master Plan + Release Scope + Protocol + Task Index;
2. reconcile with current TASK 001 report;
3. create/update `docs/PROJECT_STATE.md`;
4. mark WP-001 DONE;
5. inspect git/test baseline;
6. choose first unlocked WP (`WP-002`);
7. decompose WP-002 into one or more atomic tasks;
8. execute them;
9. continue through unlocked WPs automatically;
10. stop only on defined blocker/owner gate.

This prevents thousands of stale task files while still preserving a complete plan to release.


## B.21 Visual benchmark acceptance supplement — mandatory

The earlier Visual Benchmark milestone is strengthened by the following mandatory checks.

### Benchmark composition

The benchmark must include at minimum:

- one final-direction athlete;
- 8–12 visible athletes in representative pack/range situations;
- stadium/range sector;
- forest sector;
- climb;
- technical or fast descent;
- packed snow and track-edge snow;
- snowfall and clear presets;
- dawn/overcast plus one high-contrast daylight preset;
- close gameplay camera;
- shooting camera;
- broadcast camera;
- representative crowd and venue dressing.

### Required evidence

Codex must maintain under `docs/quality/visual/` and/or ignored `artifacts/visual/`:

- fixed-camera screenshots;
- short fixed-path capture sequences when automation permits;
- performance measurements for each quality preset;
- a defect list for animation, materials, lighting, VFX and composition;
- a competitor/reference matrix, using owner-provided or otherwise legally usable reference captures;
- a written recommendation: `PASS`, `PASS WITH ACTIONS`, or `FAIL`.

### Fail conditions

The benchmark fails if any of these remain visibly below the intended modern standard:

- wooden/robotic ski motion;
- obvious foot/ski/pole/rifle sliding or contact errors in primary cameras;
- athletes intersecting one another in common race situations;
- flat or plastic-looking snow;
- weak separation between athlete, track and background under standard lighting;
- empty/cheap-looking stadium or forest presentation;
- weather that looks like a screen overlay rather than a scene condition;
- poor LOD/pop-in that dominates normal gameplay;
- broadcast cameras that expose unfinished geometry or animation;
- frame-time instability that makes the visual target impractical.

### Asset quality gate

If final-quality humanoid models, clothing, vegetation, animation libraries, audio or other art cannot reasonably be produced to the required level by project tooling alone, Codex must prepare an **Asset Decision Report** with 2–3 legal options, costs/licenses, integration risk, visual upside and a recommendation. It must not quietly reduce the quality target to avoid an owner decision.

---


# PART C — COMPLETE WORK PACKAGE INDEX

This index defines the complete production path from verified Foundation to Release Candidate. Work packages are intentionally larger than atomic Codex tasks. Codex decomposes each WP into task files and may refine ordering inside a phase while respecting dependencies and milestone gates.

Legend:

- `DONE` — already verified.
- `LOCKED` — dependency not yet complete.
- `UNLOCKED` — may be started.
- `OWNER GATE` — requires explicit product/art/scope approval before downstream mass production.

---

# M0 — Agent-ready Foundation — DONE

### WP-001 Foundation — DONE
Verified deterministic core, assembly boundaries, MCP, tests, diagnostics, local transport and Unity bootstrap.

---

# M1 — Physiology Core

### WP-002 Physiology contracts and numeric determinism policy
Define athlete physical parameter types, units, clamping, config schemas, telemetry state and deterministic numeric strategy for new non-integer systems.

Acceptance: invalid data rejected; units documented; serialization/versioning tests; determinism ADR updated.

### WP-003 Relative effort and sustainable power model
Requested effort → requested power; individual sustainable power and modifiers.

Acceptance: monotonic response, boundaries tested, no Unity dependency.

### WP-004 HeartRateSystem
HR target, rise/recovery inertia, individual rest/max HR and recovery stat mapping.

Acceptance: fixed workload curves, recovery curves, telemetry, deterministic regression.

### WP-005 Cardiac drift
Fatigue/high-load duration influences HR requirement without runaway instability.

### WP-006 Anaerobic reserve
W'-like resource drain above threshold and recovery below threshold.

### WP-007 Muscular fatigue
Accumulates with load/terrain demand; affects efficiency and later shooting stability inputs.

### WP-008 Recovery integration
Interactions between effort, HR, anaerobic recovery and muscle freshness.

### WP-009 Athlete stat → physical parameter mapping
Map 0..100 career attributes into physical parameters using data-driven curves.

### WP-010 Physiology telemetry and debug scenarios
Machine-readable traces, canned workload profiles and Balance Lab-ready output.

### WP-011 Physiology benchmark/regression suite
Long duration, attack/recovery, exhausted athlete, high/low recovery comparisons.

### WP-012 Physiology milestone report
Document targets, plots/data, deterministic/performance results.

**M1 Gate:** headless athlete responds plausibly to workload and recovers; no race movement yet.

---

# M2 — Track Model and Ski Dynamics

### WP-013 Track schema/versioning
Stable track IDs, loop variants, checkpoints, range/penalty loop, semantic zones.

### WP-014 Deterministic track sampling
Distance → slope/grade/curvature/width/surface/wind exposure.

### WP-015 Synthetic test track generator
Flat, climb, rolling, technical descent and range approach fixtures generated from code.

### WP-016 Venue/lap/race-route composition
Compose race route from loops according to CompetitionProfile.

### WP-017 Ski physical force model
Gravity, grade, drag, snow resistance and athlete output → acceleration/speed.

### WP-018 Technique efficiency
Technique selection/efficiency by terrain and athlete skills.

### WP-019 Tuck/downhill model
Aerodynamic benefit, control/technical constraints and recovery influence.

### WP-020 Cornering/technical descent
Curvature-limited speed, skill/line efficiency and non-random mistake policy.

### WP-021 Snow condition model
Glide/grip/drag modifiers from race condition data.

### WP-022 Ski equipment model
Small but meaningful glide/grip/stability/condition-affinity effects.

### WP-023 Drafting field
Distance/speed/wind/relative placement → power saving.

### WP-024 Positional occupancy model
Lanes/track width/body occupancy without relying on Unity collisions for authoritative position.

### WP-025 Overtake mechanics
Initiation, lateral space, cost, blocking prevention, resolution.

### WP-026 Collision/contact presentation contract
Authoritative safe separation + later presentation collision hooks.

### WP-027 Final sprint model
High effort/anaerobic output, diminishing capability and finish timing.

### WP-028 Headless time-trial runner
Run athlete over test tracks with inputs and produce splits/resource traces.

### WP-029 Ski dynamics milestone regression
Compare athlete stats, pace strategies, slopes, snow and drafting scenarios.

**M2 Gate:** headless athlete can complete a mathematical course with credible pace/resource behavior.

---

# M3 — Player Control Model

### WP-030 Player input command schema
Frame-independent authoritative input stream: effort, steering/line intent, tuck, tactical presets.

### WP-031 Effort controls
Analog and preset handling with accessibility assist contracts.

### WP-032 Steering/line intent
Convert input into desired route position without presentation coupling.

### WP-033 Technique assist
Optional automatic technique choice; manual/advanced mode extension point.

### WP-034 Draft feedback contract
Expose draft strength and context to presentation/UI.

### WP-035 Input recording/replay
Recorded player commands reproduce authoritative race state.

### WP-036 Controller tuning harness
Synthetic and Unity input tests; deadzones/curves data-driven.

**M3 Gate:** controllable headless skier input stream is deterministic and replayable.

---

# M4 — Shooting Simulation

### WP-037 Shooting state machine
Range entry, lane assignment contract, setup, aiming, shots, exit.

### WP-038 Target geometry
Five targets, prone/standing apertures, deterministic ray/trajectory intersection.

### WP-039 Weapon aim state
Authoritative aim vector and stable input integration.

### WP-040 Breathing sway
Rhythmic vertical component influenced by HR/fatigue/breath control.

### WP-041 Heartbeat impulse
Shot timing windows tied to HR cadence without hidden hit RNG.

### WP-042 Muscular sway
Horizontal/noise component from fatigue/stability attributes; deterministic controller noise policy.

### WP-043 Prone vs standing parameters
Distinct stability, transition time and movement characteristics.

### WP-044 Breath control
Temporary sway reduction, oxygen/discomfort cost and rebound.

### WP-045 Wind model
Wind vector, exposure, projectile influence and readable feedback.

### WP-046 Ballistic/shot resolution
Trigger → shot ray/trajectory → target hit/miss; event log integration.

### WP-047 Shooting speed/handling
Athlete/equipment affects acquire/settle/bolt cycle/control difficulty, not hidden outcome.

### WP-048 Pressure/composure model
Race context influences controllability within documented bounds.

### WP-049 Shooting AI controller primitive
Aim/settle/trigger behavior obeying same geometry.

### WP-050 Shooting training harness
Range-only scenario, scripted inputs, deterministic telemetry.

### WP-051 Shooting statistical Balance Lab
Hit-rate distributions for AI profiles, stance, HR, fatigue, wind.

### WP-052 Shooting milestone tests
Geometry, zero-wind perfect aim, HR comparison, fatigue comparison, breath-hold cost.

**M4 Gate:** complete headless shooting bout with no hidden player RNG.

---

# M5 — Race Rules and Complete Headless Sprint

### WP-053 CompetitionProfile schema
Race format, distance, laps, start type, shooting sequence, field size, penalty loop.

### WP-054 Race lifecycle/timing
Prestart, start, laps, checkpoints, range, penalty, finish, classification.

### WP-055 Interval start scheduler
Sprint start order/interval and race clock.

### WP-056 Range/penalty loop integration
Misses create required penalty-loop traversal and timing.

### WP-057 Split/checkpoint system
Stable splits/gaps/events.

### WP-058 Classification/results
Finish order, ties policy, DNF/DNS/invalid states foundation.

### WP-059 Sprint headless orchestrator
Full race from start to finish with scripted/AI athletes.

### WP-060 Sprint race replay log
Enough events/inputs to reproduce and diagnose complete race.

### WP-061 Sprint regression fixtures
Perfect, one-miss, aggressive pacing, conservative pacing, equipment/snow comparisons.

### WP-062 Headless 60-athlete capacity benchmark
Even if Sprint display field differs, validate scalable simulation.

**M5 Gate:** deterministic complete Sprint exists without Unity presentation.

---

# M6 — AI and Pack Racing

### WP-063 AI perception snapshot
Race state, route ahead, gaps, physiology, draft and range distance.

### WP-064 Tactical intent model
Recover/Hold/Follow/Draft/Overtake/Attack/FinalSprint/PreRangeRecovery.

### WP-065 Utility evaluator
Data-driven weighted decision utility with personality modifiers.

### WP-066 Decision cadence/planning horizon
2–5 Hz tactical planning separate from simulation tick.

### WP-067 Pace controller
Intent → requested effort/power with control error model.

### WP-068 Terrain anticipation
AI uses future grade/curvature to plan pace.

### WP-069 Draft selection
Choose wheel/group while avoiding oscillation.

### WP-070 Overtake decision
Space, energy, race value and terrain constraints.

### WP-071 Pack/lane behavior
Stable multi-athlete flow, no overlap/teleportation/pass-through artifacts in authoritative model.

### WP-072 Pre-range recovery tactics
AI decides when/amount to reduce load before shooting.

### WP-073 AI shooting strategy
Speed vs stability/risk decisions within same physics.

### WP-074 AI personalities
Aggression, patience, risk, draft preference, final-lap behavior, pressure resistance.

### WP-075 Difficulty model
Decision quality/reaction/planning/aim precision only; never hidden physical multipliers.

### WP-076 AI telemetry/explainability
Every material decision can be inspected in debug trace.

### WP-077 AI scenario tests
Duel, pack, climb attack, draft train, final sprint, pre-range recovery.

### WP-078 30-athlete pack soak
Long repeated race tests for deadlocks/oscillation/overlaps.

### WP-079 AI benchmark report
Decision and performance distributions.

**M6 Gate:** 30 believable competitors complete races without physics cheating.

---

# M7 — Pursuit and Mass Start

### WP-080 Pursuit dependency/results contract
Consume qualifying Sprint result data safely/versioned.

### WP-081 Pursuit gap start
Start offsets reproduce source gaps exactly within race timing representation.

### WP-082 Pursuit PPSS route/range orchestration
Four bouts and five-lap lifecycle.

### WP-083 Pursuit tactical AI adjustments
Gap chasing, group formation, leader defense, last-range decisions.

### WP-084 Pursuit regression suite
Start gaps, overtakes, penalty effects, close finishes.

### WP-085 Mass Start simultaneous start/grid
30 athlete start positions/occupancy and launch behavior.

### WP-086 Mass Start PPSS orchestration
Four bouts/five laps/penalties.

### WP-087 Mass Start range lane/order policy
Deterministic lane assignment and queue/occupancy behavior as required by chosen rule profile.

### WP-088 Mass Start pack behavior stress
First-lap density, narrow zones, range entry/exit.

### WP-089 Mass Start regression suite
No overlap/deadlock, stable ranking, repeated seeds.

### WP-090 Three-format championship harness
Sprint → Pursuit dependency plus independent Mass Start.

**M7 Gate:** all three launch race formats complete headlessly with AI.

---

# M8 — Balance Lab and Gameplay Calibration

### WP-091 Batch simulation CLI/tool
Run thousands of races/config sets without Unity graphics.

### WP-092 Metrics schema
Times, hit rates, penalties, HR, fatigue, attack value, draft benefit, stat marginal impact.

### WP-093 Distribution reports
CSV/JSON/Markdown outputs suitable for Codex analysis.

### WP-094 Regression snapshots
Versioned target windows and canonical seed suites.

### WP-095 Pacing calibration
Aggressive vs optimal vs conservative strategy impact.

### WP-096 Shooting calibration
Top/mid/developing athlete distributions by stance and conditions.

### WP-097 Draft calibration
Meaningful but non-dominant group benefit.

### WP-098 Stat sensitivity analysis
+10 stat point marginal effect; prevent one-stat dominance.

### WP-099 Equipment sensitivity
Modest progression vs athlete skill.

### WP-100 AI parity audit
No hidden speed/power advantages; same inputs produce same physical capability.

### WP-101 Automated bounded tuner
Optional optimizer may tune declared coefficients only toward explicit targets.

### WP-102 Balance milestone report
Lock first playable tuning version.

**OWNER REVIEW RECOMMENDED:** approve broad game-feel target before visual polish, but work may continue with provisional GDD targets unless an actual design ambiguity exists.

---

# M9 — Career Simulation

### WP-103 Career save/domain schema
Versioned athlete/world/season IDs and serialization contracts.

### WP-104 Player athlete creation
Name/appearance hook/national representation and initial attribute generation policy.

### WP-105 AI roster generation
Stable fictional identities, attributes, personality, age, potential.

### WP-106 Calendar model
Weeks, venues, race events, training/rest windows.

### WP-107 Season standings
Points/rank/history for supported formats.

### WP-108 Training session model
Endurance, intensity, technique, prone, standing, recovery.

### WP-109 Training adaptation
Potential, age, diminishing returns, quality and recovery modifiers.

### WP-110 Career fatigue carryover
Training/races/rest affect race readiness across weeks.

### WP-111 Form model
Short-term readiness/peaking separate from permanent skill.

### WP-112 Health/injury-risk foundation
Data and risk calculation exist; severe injury gameplay requires explicit owner-approved behavior before punitive implementation.

### WP-113 Morale/confidence
Race/training outcomes influence bounded state and pressure/control modifiers.

### WP-114 Athlete ageing curve
Growth, peak and decline; data-driven by athlete/potential profile.

### WP-115 AI abstract training
AI progresses by same career rules using computationally cheaper planning where appropriate.

### WP-116 Retirement/new generation
Prevent static world; keep field population stable across decades.

### WP-117 Economy foundation
Prize money, training camps, recovery services, equipment purchases.

### WP-118 Equipment inventory/progression
No loot rarity grind; clear comparisons and modest vertical progression.

### WP-119 Rivalry/history tracking
Close finishes, standings battles, recurring opponents.

### WP-120 Career qualification/progression layer
Define tiers/eligibility using fictional competition structure unless owner later requests a different structure.

### WP-121 Career season orchestrator
Training → event → results → development → next week/season.

### WP-122 15-season headless soak
No fatal data growth, ID collisions, broken standings or retirement population collapse.

### WP-123 Career balance reports
Development speed, peak age distributions, earnings/spending, competitive progression.

### WP-124 Career milestone report
Lock initial career tuning/version.

**M9 Gate:** living multi-season career exists headlessly.

---

# M10 — Unity Playable Greybox Vertical Slice

### WP-125 Presentation state adapters
Read simulation state without becoming authoritative.

### WP-126 Track spline/geometry authoring tool
Generate/validate Unity track representation from track data.

### WP-127 Greybox venue
Stadium, range, loops, penalty loop, semantic zone visualization.

### WP-128 Athlete proxy presentation
30 capsule/mannequin athletes driven by authoritative route state.

### WP-129 Camera chase
Stable gameplay camera, look-ahead, terrain handling.

### WP-130 Unity Input System integration
Gamepad + KBM actions mapped to deterministic command stream.

### WP-131 Ski HUD debug
Effort, speed, HR, anaerobic, fatigue, draft, position.

### WP-132 Range presentation
Targets, lane, aim camera and shot feedback.

### WP-133 Shooting input integration
Mouse/right-stick aim, trigger, breath control.

### WP-134 Penalty loop presentation
Visual route integration and timing.

### WP-135 Basic race HUD
Lap, rank, gaps, next range, shooting results.

### WP-136 Playable Sprint greybox
Human can finish complete race against AI.

### WP-137 Playable Pursuit greybox
Use prior result/start gaps.

### WP-138 Playable Mass Start greybox
30 athlete simultaneous start and pack race.

### WP-139 Automated screenshot/playtest harness
MCP screenshots at defined checkpoints; machine-readable state alongside image.

### WP-140 Player-build smoke pipeline
Create Windows development build and automated launch/log capture.

**OWNER GATE G1:** approve core controls/gameplay direction after hands-on greybox. If owner does not review immediately, Codex may continue non-destructive technical/content tooling but must not radically redesign controls.

---

# M11 — Animation and Athlete Presentation

### WP-141 Humanoid presentation architecture
Rig-independent state mapping, LOD and animator separation.

### WP-142 Locomotion state model
Flat/climb/acceleration/sprint/tuck/technical descent transitions.

### WP-143 Cornering presentation
Lean/turn states tied to route/curvature.

### WP-144 Rifle carry transitions
On-back/off-back/range transitions.

### WP-145 Prone enter/exit
Animation timing synchronized to simulation shooting state.

### WP-146 Standing shooting enter/exit
Stable camera/pose transitions.

### WP-147 Shooting procedural/IK layer
Aim alignment, hands/rifle consistency.

### WP-148 Pole/ski IK/contact
Ground alignment and avoid obvious skating artifacts.

### WP-149 Fatigue/exhaustion presentation
Blend intensity without changing authoritative physics.

### WP-150 Athlete LOD/crowd performance
30 racers close-up and distance behavior.

### WP-151 Animation validation scenes
Automated clips/state screenshots and missing-transition checks.

**Asset gate:** if final-quality humanoid/animation assets require external license/account action, produce shortlist/license report for owner.

---

# M12 — Visual Benchmark and Rendering

### WP-152 URP production settings
Quality tiers, render scale, shadows, post-processing policy.

### WP-153 Snow material system
PBR snow, slope/roughness variation, wet/cold presets.

### WP-154 Ski track/decal system
Efficient trail/contact marks; distance/LOD management.

### WP-155 Snow interaction VFX
Pole/ski spray, subtle accumulation effects where feasible.

### WP-156 Vegetation system
Forest density/LOD/wind/season look and GPU budget.

### WP-157 Terrain material blending
Snow/ice/packed trail/edge transitions.

### WP-158 Weather system presentation
Snowfall, wind, fog/visibility, cloud/lighting presets.

### WP-159 Breath vapor
Temperature/load contextual VFX.

### WP-160 Stadium/range benchmark art
Representative final-quality structures/targets/barriers/signage without unlicensed branding.

### WP-161 Crowd benchmark
Performance-scalable animated crowd and density tiers.

### WP-162 Lighting benchmark
Overcast, clear cold daylight, dawn/late-day variants.

### WP-163 Cinemachine/broadcast camera tooling
Only install/configure extensions when actually needed and compatible.

### WP-164 Visual benchmark scene
Final representative sector with 8–12 athletes.

### WP-165 Visual screenshot matrix
Fixed camera/weather/quality captures for regression.

### WP-166 GPU/CPU profiling benchmark
Record frame timings and draw-call/batch budgets.

**OWNER GATE G2:** approve visual direction before producing all venues.

---

# M13 — Production Venue Pipeline and Launch Content

### WP-167 Venue authoring schema/tools
Data + generated validation, semantic zones, camera markers, ambience hooks.

### WP-168 Venue validator
Distance, loop continuity, range/penalty placement, widths, grade limits, missing assets.

### WP-169 Venue 01 production
Benchmark venue upgraded to full production quality.

### WP-170 Venue 02 production
Distinct tactical/visual identity.

### WP-171 Venue 03 production
Distinct tactical/visual identity.

### WP-172 Venue 04 production
Distinct tactical/visual identity.

### WP-173 Venue 05 production
Distinct tactical/visual identity.

### WP-174 Venue 06 production
Distinct tactical/visual identity.

### WP-175 Multi-venue weather matrix
Validate conditions/readability/performance.

### WP-176 Career calendar content
14–18 race season using production venues.

### WP-177 Venue race-balance calibration
Course times, climb advantage, draft sectors and difficulty.

### WP-178 Venue performance/LOD pass
Worst-case Mass Start and broadcast shots.

**M13 Gate:** launch content minimum reached or owner approves revised venue count.

---

# M14 — UI / UX / TV / Replay

### WP-179 UI architecture/design tokens
Resolution independence, navigation, controller focus, localization-ready text.

### WP-180 Main menu/profile flow
New career/continue/settings/quit.

### WP-181 Pre-race screen
Venue, weather, athlete state, equipment, event rules.

### WP-182 Gameplay HUD production
Readable race information without obscuring course.

### WP-183 Shooting HUD production
Targets, wind/HR feedback, penalties/accessibility.

### WP-184 Live timing/splits
Rank/gaps/checkpoints.

### WP-185 Results screen
Times, shooting, penalties, splits, standings impact.

### WP-186 Career dashboard
Calendar, athlete state, next event, ranking.

### WP-187 Training UI
Plan sessions/recovery and forecast costs/benefits without hidden exact potential.

### WP-188 Equipment UI
Clear modest stat tradeoffs.

### WP-189 Standings/history UI
Season and career history.

### WP-190 Rivalry presentation
Surface tracked rivalry data without scripted fake outcomes.

### WP-191 Replay serialization
Persist compatible race replay/inputs/events with version metadata.

### WP-192 Replay playback
Deterministic/recorded presentation with camera controls.

### WP-193 Automatic highlight detection
Lead changes, final range, overtakes, close finish, key misses.

### WP-194 Broadcast camera director
Select cameras based on race events/battles.

### WP-195 TV graphics package
Live leaderboards, shooting board, gaps, penalties, finish overlays.

### WP-196 Accessibility UI
Colorblind, text size where practical, assists explanation.

### WP-197 UI automated navigation tests
Controller/KBM essential flows.

**M14 Gate:** complete player-facing loop from menu → career → race → result → next event.

---

# M15 — Audio

### WP-198 Audio event architecture
Simulation/presentation event mapping and mixer groups.

### WP-199 Ski/pole snow audio
Speed/condition/load variation.

### WP-200 Breathing/heartbeat
Contextual, non-fatiguing mix with shooting focus.

### WP-201 Rifle/target audio
Shot, echo, mechanism, target hit/miss physical feedback.

### WP-202 Wind/weather ambience
Venue/weather contextual loops.

### WP-203 Crowd system
Intensity reacts to stadium, shooting and finish context.

### WP-204 UI/menu audio
Consistent feedback.

### WP-205 Audio performance/mix pass
Voice count, spatialization, dynamic range and settings.

---

# M16 — Saves, Settings, Reliability

### WP-206 Save format v1
Versioned career/world/settings metadata.

### WP-207 Atomic/corruption-safe saves
Temp/write/verify/replace or equivalent strategy.

### WP-208 Autosave policy
Before/after race and career transitions without destructive overwrite risk.

### WP-209 Save migration framework
Fixtures for schema changes.

### WP-210 Settings system
Graphics/audio/gameplay/accessibility.

### WP-211 Input rebinding persistence
Gamepad/KBM.

### WP-212 Localization-ready string system
Even if 1.0 ships limited languages, no hardcoded UI text architecture trap.

### WP-213 Error/report bundle
Logs/config/version/save diagnostic export excluding secrets.

### WP-214 Crash/recovery scenarios
Interrupted save, missing content, invalid config, graceful user message.

### WP-215 3-season interactive/save-load soak
Repeated save/reload/race transitions.

---

# M17 — Performance, Build and QA Alpha

### WP-216 Reference hardware profiling setup
Capture CPU/GPU/frame/memory and build version.

### WP-217 Simulation profiling
30/60/120 athletes headless and integrated.

### WP-218 Rendering worst-case profiling
Mass Start, stadium crowd, snowfall, broadcast camera.

### WP-219 CPU optimization pass
Only measured bottlenecks.

### WP-220 GPU optimization pass
LOD, shadows, vegetation, VFX, crowd.

### WP-221 Memory/load optimization
Venue transitions, repeated races, asset unload.

### WP-222 Build pipeline
Versioned Windows dev/release builds from documented command/process.

### WP-223 Build smoke automation
Launch, load menu, run deterministic test race, exit/log capture.

### WP-224 Full regression suite orchestration
One command/report for core + Unity + balance + build smoke.

### WP-225 100-race soak
Randomized supported event/config combinations; no fatal error/leak/deadlock.

### WP-226 15-season career soak
Repeated regression under near-release schemas.

### WP-227 Alpha issue triage
Severity policy and blocker dashboard.

### WP-228 Alpha milestone report
Known bugs, performance, scope completion, test health.

**M17 Gate:** feature complete Alpha.

---

# M18 — Beta, Tuning and Polish

### WP-229 Gameplay feel tuning pass
Use player feedback/telemetry without violating core invariants.

### WP-230 Difficulty/accessibility tuning
Assists and AI cognition quality.

### WP-231 Career progression tuning
Growth, fatigue, training, economy, AI world longevity.

### WP-232 Race balance finalization
Ski vs shooting tradeoffs; Sprint/Pursuit/Mass Start identities.

### WP-233 AI polish
Reduce robotic patterns, oscillation, weird packs and implausible shooting cadence.

### WP-234 Animation polish
Transitions, foot/ski/pole/rifle artifacts.

### WP-235 Visual polish
Lighting/material/VFX/camera issues across six venues.

### WP-236 Audio polish
Mix, repetition, context transitions.

### WP-237 UI polish
Readability, controller navigation, edge cases.

### WP-238 Tutorial/onboarding
Teach effort, HR, drafting, shooting and career planning without long exposition.

### WP-239 Training modes
Ski time trial/ghost and shooting range practice.

### WP-240 Replay/highlight polish
Camera selection and TV pacing.

### WP-241 Beta regression/soak
No blocker bugs and stable performance.

### WP-242 Beta milestone report
Release-risk list and remaining P0/P1 issues.

---

# M19 — Release Candidate

### WP-243 License audit
All external content/packages documented and legally usable.

### WP-244 Privacy/security/package audit
No credentials/local tokens/private paths/debug secrets in build.

### WP-245 Clean-machine build test
Install/run on machine without development environment assumptions.

### WP-246 Fresh-profile test
First launch/new career/controller/settings.

### WP-247 Upgrade/migration test
Older supported save fixture → release schema.

### WP-248 Full 3-season manual/automated hybrid validation
Core loop repeated with saves/results/career progression.

### WP-249 Long career simulation validation
15+ seasons.

### WP-250 Performance certification
Document real tested tiers; only now propose store hardware specs.

### WP-251 Final visual capture matrix
Every venue/weather plus menus/range/race.

### WP-252 Final regression suite
All blocking tests green.

### WP-253 Known-issues review
No release-blocking defect accepted silently.

### WP-254 Version/build metadata
Semantic/product version and deterministic config version.

### WP-255 Release candidate artifact
Generate final Windows build and checksums locally.

### WP-256 RC report
Full scope/test/performance/license/known-issues summary.

**OWNER GATE G3:** final release approval. Publishing/upload is not autonomous unless explicitly authorized.

---

# Post-1.0 backlog — LOCKED until release scope expands

Potential future work:

- Individual;
- Relay/Mixed Relay;
- more venues;
- official licenses;
- multiplayer;
- modding/track editor;
- console builds;
- advanced commentary;
- summer biathlon;
- VR.

Do not consume 1.0 production time unless owner explicitly moves an item into scope.


# PART D — AUTONOMOUS CODEX EXECUTION PROTOCOL

**Purpose:** правила, по которым Codex самостоятельно превращает Master Plan в атомарные задачи и последовательно строит игру до Release Candidate.

---

## 1. Роль Codex

Codex — основной autonomous implementation agent проекта.

Он отвечает за:

- планирование технических подзадач;
- код;
- Unity setup, доступный через MCP;
- automated tests;
- Balance Lab;
- debug tooling;
- data schemas;
- editor tooling;
- documentation;
- performance profiling;
- исправление ошибок;
- локальную интеграцию;
- отчеты о завершенных задачах.

Человек остается owner/product director и требуется только там, где:

- спецификация оставляет существенный game-design выбор;
- нужна OAuth/account confirmation;
- нужен purchase/license acceptance;
- нужно необратимое внешнее действие;
- требуется публикация/remote push/release upload;
- есть конфликт двух равноценных продуктовых направлений.

Не перекладывать на owner обычные terminal/Unity/debug/test операции, если Codex способен выполнить их сам.

---

## 2. Источники истины — priority order

1. `AGENTS.md`
2. `docs/BIATHLON_RELEASE_SCOPE_v1.0.md`
3. `docs/BIATHLON_MASTER_PLAN_v1.0.md`
4. `docs/BIATHLON_GDD_TDD_v0.1.md`
5. approved ADRs
6. current task file
7. automated tests
8. existing implementation

Если более поздняя спецификация осознанно меняет раннюю — оформить ADR/changelog. Не исправлять дизайн молча.

---

## 3. Persistent project state

Codex должен создать и постоянно обновлять:

```text
docs/PROJECT_STATE.md
docs/tasks/
docs/reports/
docs/adr/
docs/tuning/
docs/quality/
docs/licensing/
```

`PROJECT_STATE.md` содержит минимум:

```yaml
project_phase:
active_work_package:
active_task:
last_completed_task:
last_verified_commit:
foundation_status:
editmode_tests:
playmode_tests:
standalone_tests:
unity_console_errors:
unity_console_warnings:
current_known_blockers:
owner_decisions_needed:
next_unlocked_work_packages:
```

После каждой атомарной задачи обновить state в той же change set.

---

## 4. Work-package decomposition

Master Plan содержит work packages, а не обязательно финальный размер task.

Перед началом work package Codex должен:

1. прочитать dependencies;
2. проверить текущее состояние репозитория;
3. создать `docs/tasks/TASK-<id>-<slug>.md` для первой атомарной задачи;
4. указать:
   - objective;
   - dependencies;
   - files/systems likely affected;
   - constraints;
   - tests;
   - acceptance criteria;
   - rollback/risk notes;
5. выполнить task;
6. создать `docs/reports/TASK-<id>-<slug>-REPORT.md`;
7. перейти к следующему task в work package.

### Правило размера

Одна atomic task должна давать один проверяемый результат.

Плохой task:

> Implement skiing, AI, shooting and UI.

Хороший task:

> Implement deterministic HeartRateSystem with config schema, unit tests, telemetry and no per-tick allocations.

---

## 5. Execution loop

Для каждой задачи:

```text
READ
↓
PLAN
↓
IMPLEMENT
↓
COMPILE
↓
UNIT TESTS
↓
INTEGRATION TESTS
↓
UNITY MCP VALIDATION (если применимо)
↓
BALANCE/PERF TESTS (если применимо)
↓
CONSOLE INSPECTION
↓
GIT DIFF REVIEW
↓
DOC/STATE UPDATE
↓
REPORT
↓
NEXT TASK
```

Codex не должен считать задачу законченной, если acceptance criteria не доказаны.

---

## 6. Stop conditions

Codex продолжает автономно, пока существует unlocked work package.

Остановиться и спросить owner только при:

### DESIGN_BLOCKER
Спецификация не определяет существенное ощущение/правило, и выбор materially изменит продукт.

### AUTH_BLOCKER
Требуется браузерная авторизация, пароль, secret или account confirmation.

### LICENSE_BLOCKER
Требуется покупка, acceptance license или выбор внешнего asset, который меняет юридический/финансовый статус.

### DESTRUCTIVE_BLOCKER
Необходимо удаление/переписывание значимых пользовательских данных, history rewrite, remote force push и т.п.

### SCOPE_BLOCKER
Нужное решение расширяет Release Scope.

### QUALITY_BLOCKER
Два или более подхода имеют существенный trade-off, который нельзя выбрать объективным benchmark/test.

При blocker дать owner:

1. точную проблему;
2. 2–3 варианта;
3. плюсы/минусы;
4. рекомендацию;
5. что продолжится после выбора.

Не задавать вопрос, если есть явно лучший обратимый технический вариант.

---

## 7. Git policy for autonomous work

- No remote push without explicit owner permission.
- No force push/rebase published history.
- Inspect `git status` before every task.
- Preserve unrelated user changes.
- Stage only files attributable to the task.
- Local commits are allowed and recommended after a fully passing task when unrelated changes can be excluded safely.
- Use coherent commit messages.
- If clean selective commit is impossible, leave changes uncommitted and report why.

Recommended:

```text
feat(sim): implement anaerobic reserve dynamics
feat(track): add deterministic grade sampling
feat(shooting): implement heartbeat sway
fix(ai): prevent blocked overtake oscillation
test(career): add 15-season soak simulation
```

---

## 8. Test hierarchy

### Tier A — Core unit tests
Run on every relevant change.

### Tier B — Cross-system deterministic tests
Run when authoritative race state changes.

### Tier C — Unity EditMode
Run for Unity-facing contracts/tooling/data import.

### Tier D — Unity PlayMode
Run for presentation/bootstrap/runtime integration.

### Tier E — Statistical/Balance Lab
Run when tuning/AI/race distribution changes.

### Tier F — Player build smoke
Run from playable vertical slice onward.

### Tier G — Soak/regression
Run in Alpha/Beta and career/save work.

No flaky test may be accepted as permanent. Statistical tests use fixed seeds and explicit tolerances.

---

## 9. Determinism/versioning policy

Foundation determinism is a protected invariant.

When new floating-point systems are introduced:

- document determinism expectations;
- add cross-run regression tests;
- keep authoritative decisions independent of rendering frame rate;
- avoid Unity physics as authoritative race outcome source;
- if exact cross-runtime determinism becomes impossible, define and test the supported determinism boundary explicitly via ADR rather than pretending it remains bit-identical.

Any intentional change to canonical race output must:

- update simulation/tuning version;
- document reason;
- update regression snapshots deliberately.

---

## 10. Performance policy

For hot simulation paths:

- profile before optimizing architecture prematurely;
- no recurring GC allocation without justification;
- prefer arrays/struct-like data where beneficial;
- AI tactical planning update rate may be lower than simulation tick;
- expensive telemetry/debug may be conditional;
- build repeatable benchmarks.

Performance regression >10% on an established benchmark must be explained before merge/task completion.

---

## 11. Balance autonomy

Codex MAY automatically tune numeric parameters when:

- target ranges are already specified;
- tuning remains within declared bounds;
- it can prove improvement with Balance Lab;
- it records before/after values and distributions.

Codex MUST NOT autonomously redefine target game feel, e.g. decide that shooting should become arcade or that one miss should cost half as much, unless the existing spec authorizes that range.

If target is missing, use a clearly marked provisional value and queue an owner decision rather than silently treating it as final.

---

## 12. Visual autonomy

Codex may autonomously:

- build greybox;
- generate editor tools;
- create procedural geometry;
- implement shaders/VFX;
- configure URP;
- create cameras/UI;
- create screenshot comparison tooling;
- optimize rendering;
- integrate approved/clearly licensed assets.

Codex must create a `Visual Benchmark` gate before mass content production.

No external paid asset purchase or ambiguous-license download without owner approval.

---

## 13. External dependencies

For each added package/library:

- justify need;
- pin version where practical;
- record license;
- test Unity compatibility;
- avoid redundant packages;
- prefer built-in/BCL/project code when small and stable.

Never reintroduce a known incompatible dependency without an explicit compatibility test/ADR.

---

## 14. Reporting format

Every task report:

```markdown
# TASK ... REPORT

## Outcome
PASS / PARTIAL / BLOCKED

## Implemented
...

## Files changed
...

## Tests
...

## Determinism/balance impact
...

## Unity validation
...

## Performance
...

## Known limitations
...

## Git status/commit
...

## Next unlocked task
...
```

At milestone completion create `docs/reports/MILESTONE-<id>-REPORT.md` with summarized metrics and screenshots/artifacts.

---

## 15. Definition of autonomous success

The agent is behaving correctly when owner interaction is needed primarily for:

- approving feel/art direction;
- choosing scope tradeoffs;
- legal/licensing decisions;
- account actions;
- final product acceptance;

and not for:

- copying shell commands;
- opening scenes;
- running tests;
- fixing compiler errors;
- reading logs;
- creating ordinary files;
- wiring components;
- routine performance/debug work.


## D.16 Required behavior for long autonomous runs

Codex is expected to continue through multiple atomic tasks and work packages without requesting approval for routine reversible technical work. It must stop only at a defined blocker/owner gate.

At the start of every new Codex session:

1. read `AGENTS.md`;
2. read this master plan;
3. read `docs/PROJECT_STATE.md`;
4. inspect git status;
5. inspect the last relevant reports/ADRs;
6. verify the minimum healthy baseline relevant to the next task;
7. resume the first unlocked work package.

At the end of a session or before context exhaustion:

1. finish the current atomic task to a safe verified state if possible;
2. update `PROJECT_STATE.md`;
3. write a task/milestone report;
4. preserve exact resume instructions;
5. do not rely on chat memory as the source of truth.

### Visual development behavior

During M11–M13 and later polish:

- use Unity MCP screenshots frequently;
- build regression capture tooling with fixed cameras;
- compare in-motion issues, not just still image beauty;
- prioritize animation/contact/lighting coherence over raw polygon count;
- do not mass-produce content before visual gate approval;
- do not declare visual completion solely because shaders/assets exist; prove the result in Game View/player builds.

---


# PART E — FINAL RELEASE DEFINITION OF DONE

A Release Candidate is **not** complete merely because all Work Packages have implementation code. It is complete only when the product passes all system, content, quality and owner gates below.

## E.1 Gameplay

- Sprint, Pursuit and Mass Start are complete and robust;
- pacing, drafting, overtaking, pre-range recovery, shooting rhythm and final-lap tactics create meaningful trade-offs;
- no dominant always-correct control strategy remains;
- player hit/miss is geometrically/physically explainable;
- AI physical parity audit passes;
- race results are understandable through telemetry/replay.

## E.2 Career

- multi-season career loop is complete;
- athlete development, training, form, fatigue, potential, ageing, economy and equipment all function;
- AI athletes develop, peak, decline, retire and are replaced by new generations;
- 15-season headless soak succeeds;
- save/load/migrations remain stable.

## E.3 Content

- target venue count is complete unless explicitly changed by owner;
- venues have distinct gameplay and visual identity;
- no accidental dependency on unlicensed real-world branding;
- weather/snow presets function across venues;
- all common race routes and penalty/range flows validate.

## E.4 Visual and animation quality

- Visual Benchmark Gate is approved;
- final representative captures show a clear modern step beyond the dedicated biathlon references, with NGL Biathlon 2026 as minimum competitor benchmark;
- athlete motion, ski/pole contact and shooting transitions are coherent in primary cameras;
- snow, lighting, vegetation, weather, stadium/range and crowd presentation meet the approved benchmark;
- all production venues are checked against the benchmark, not merely against greybox acceptance;
- no known critical visual defect is hidden by camera restrictions rather than fixed.

## E.5 UI / broadcast / audio

- race state is understandable at speed;
- live timing, splits, shooting board, penalties and results are complete;
- replay/highlight logic works from race events;
- gameplay, range, crowd, weather and UI audio have coherent mix states;
- controller and keyboard/mouse paths are both production-ready.

## E.6 Performance / reliability

- no simulation dependence on render frame rate;
- 30-athlete Mass Start meets approved frame-time target on the reference system;
- player builds pass smoke/regression loops;
- no recurring authoritative hot-loop managed allocations without documented exception;
- repeated race/restart loops do not show uncontrolled memory growth;
- clean-machine release build audit passes;
- no packaged credentials/secrets/local absolute paths.

## E.7 Owner gates

Owner approval is mandatory for:

- final core game feel;
- final Visual Benchmark;
- any paid/licensed third-party asset decision;
- any material Release Scope change;
- Beta feature freeze;
- Release Candidate publication.

---

# PART F — ONE-SHOT INSTRUCTION TO CODEX

The text below is the instruction the owner may send after placing this file in the project.

```text
Ты продолжаешь работу как основной автономный разработчик проекта BIATHLON.

TASK 001 Foundation уже завершён и проверен. Не повторяй его без доказанного regression.

Сначала полностью прочитай:
1. AGENTS.md
2. docs/BIATHLON_MASTER_PLAN_FINAL_v2.0.md
3. docs/BIATHLON_GDD_TDD_v0.1.md
4. docs/TASK_001_FOUNDATION_REPORT.md
5. docs/PROJECT_STATE.md, если он уже существует
6. релевантные ADR/tests/reports.

Этот FINAL MASTER PLAN является главным production roadmap и заменяет предыдущие planning-only версии там, где они расходятся.

После чтения:
- проверь git status и сохрани unrelated пользовательские изменения;
- проверь Unity MCP реальным tool call;
- запусти минимальный здоровый Foundation regression;
- создай/актуализируй docs/PROJECT_STATE.md;
- отметь WP-001 DONE;
- найди первый незавершённый и разблокированный Work Package;
- начиная с WP-002, сам декомпозируй Work Package в атомарные TASK-файлы непосредственно перед реализацией;
- выполняй их последовательно по dependencies;
- после каждой задачи: compile → tests → MCP/runtime validation при необходимости → determinism/performance/balance checks → git diff → report → PROJECT_STATE update;
- не останавливайся после каждой маленькой задачи и не проси разрешения на обычные обратимые технические действия;
- продолжай автономно до blocker/owner gate, существенного milestone либо конца безопасной рабочей сессии.

Критические product invariants:
- Simulation Core != Unity Presentation;
- player shooting hit/miss без hidden RNG;
- AI без hidden speed/power multipliers;
- data-driven tuning;
- replayable/deterministic authoritative inputs там, где это требуется;
- тесты для major systems;
- hot-path allocation discipline;
- save versioning/migration;
- documented asset licenses;
- не масштабировать production art до прохождения Visual Benchmark Gate.

Критическая визуальная цель:
NGL Biathlon 2026 — минимальный современный визуальный benchmark, а не цель для простого совпадения. Финальная игра должна демонстрировать явный современный шаг вперёд по общей картинке, анимации спортсменов, снегу, освещению, окружению, погоде, стадионной атмосфере и broadcast presentation. Если этого нельзя доказать на benchmark-сцене — Visual Gate FAIL, и массовое производство venues не начинается.

Если качественный humanoid/animation/environment/audio asset требует внешней лицензии, покупки или аккаунта — подготовь Asset Decision Report с 2–3 вариантами и рекомендацией и остановись только на этом owner action. Не понижай качество молча.

Останавливайся только при blocker, определённом в Master Plan: существенное неописанное game-design решение, OAuth/secret, license/purchase, destructive external action, изменение scope или объективно неразрешимый quality trade-off. При blocker дай варианты, плюсы/минусы, evidence и рекомендацию.

Не делай remote push/release publication без явного разрешения owner.

Начинай сейчас. Сначала покажи краткий план ближайшего WP-002, затем сразу приступай к его выполнению.
```

---

## Final note to Codex

The objective is not to finish the roadmap as fast as possible by lowering standards. The objective is to produce a coherent, tested, modern biathlon game that is **systemically deeper and visibly more contemporary than the dedicated biathlon games used as references**. When schedule, content count and quality conflict, do not silently choose. Preserve the approved quality bar and surface the trade-off at the appropriate owner gate.


## Owner-authorized milestone update -2026-11-01

M16.5 closed by owner. M16.6 scope: docs/M16_6_OWNER_TASK.md, operational tracker
docs/M16_6_WP_STATUS.md and append-only docs/PROJECT_STATE.md. Appearance/equipment
published as33c51d5e/744fa50c. Dynamic weather implemented with native/standalone
qualification; training UI/final regression audit remain. M16.6 OPEN; M17 not
started. Owner explicitly authorized GitHub publication and a fullscreen build
after each task. Current branch codex/m16-6-equipment-weather-training.


2026-11-01: M16.6.4 six illustrated training tasks implemented; all M16.6 feature stages have focused/native/fullscreen qualification. Latest review Builds/M16_6_4_Training_Review/START_TEST.cmd. Owner authorized continued repair of old broad-regression failures in this same task. Operational tracker docs/M16_6_WP_STATUS.md; M16.6 OPEN, M17 not started.


2026-11-02: M16.6.5 review Builds/M16_6_5_Regression_Review/START_TEST.cmd includes all M16.6 features plus versioned start-world/snow-intent repairs. Current native40Edit/5Play and fullscreen standalone qualification passed; full current.NET assembly remains pending. GitHub source90686970/7b233648, qualification9980660d. Track docs/M16_6_WP_STATUS.md and append-only STATE. M16.6 OPEN, M17 not started.


2026-11-02 11:10 Moscow - M16.6 feature implementation and owner-authorized legacy regression repair AUTOMATED QUALIFICATION COMPLETE. Complete Release2188/2188PASS/0fail +all5Explicit separately PASS =2193unique cases; recent native52Edit/5PlayPASS, Console0errors. Original third-exit600s limit passes128.543s; no .NET test assertion/tick budget/individual timeout relaxed. Runtime90686970, fixture10417a2e, launcher/diagnosticsbf0f4cf5; receipts docs/quality/m16-6/regression/release-acceptance.json. Original numeric balance/event goldens preserved, new AI/start policies versioned for replay. Current fullscreen owner build Builds/M16_6_5_Regression_Review/START_TEST.cmd unchanged and actual startup qualified. Tracker docs/M16_6_WP_STATUS.md; append-only STATE contains full evidence/history. M16.5 owner-closed; M16.6 OPEN FOR OWNER REVIEW ONLY; M17 not started.


2026-11-02 latest owner-directed visual follow-up — M16.6.6:18original generated training/ski/rifle images integrated into photographic cards; responsive layout, selection/focus and top shop paging.19nativeEdit+4nativePlay+18.NETPASS; actual2560x1440fullscreen menu probePASS,18resources/3captures/ownerprofilesunchanged/exit0. BuildSucceeded0errors. Evidence:docs/quality/m16-6/menu-art; design/prompts:docs/ui/m16-6-menu-art. New review:Builds/M16_6_6_Menu_Art_Review/START_TEST.cmd. Gameplay/balance unchanged. M16.6OPEN for ownerreview;M17notstarted.


2026-11-02 owner-directed M16.6.7 menu UX follow-up: grouped direct career navigation/current-page marker, real next-week action, Continue priority, training plan beside forecast, shop tabs/balance above products and visible scrolling.68unique focused testsPASS(final6affectedPlay rerunPASS),fullscreen2560x1440menu probePASS/ownerprofilesunchanged/18resources,build0errors/520warnings. Review:Builds/M16_6_7_Menu_UX_Review/START_TEST.cmd. Evidence:docs/quality/m16-6/menu-ux;research:docs/ui/m16-6-menu-ux. Gameplay/balance unchanged. M16.6OPEN for ownerreview;M17notstarted.


2026-11-02 owner-directed M16.6.8: stadium timing glyphs fit actual header; shared gantry blank at launch/seek0. Official BIATHLON: PULSE / Биатлон: Пульс гонки integrated in localized menu/window/launcher.58unique focused testsPASS,final2affectedmenu rerunPASS,fullscreen2560x1440probePASS,bothlocalizedtitles/ownerprofilesunchanged. Finalbuild0errors/520warnings. Review:Builds/M16_6_8_Pulse_Review/START_TEST.cmd; evidence:docs/quality/m16-6/pulse. Stable persistence identity and balance preserved. M16.6OPEN for ownerreview;M17notstarted.


2026-11-03 owner-directed M16.6.9: Russia available in new roster_v2careers, blue/white/red kit, UI/range flags, thirteenth coach+scope. Existing v1saves/drafts and numeric generation/RNG preserved. Independent production tree/crowd/dressing shadow casters repair offscreen/farLOD disappearances without changing visible geometry or quality shadow distance.81nativePlay+37.NET+3nativeEditPASS(119distinct cases),fullscreen2560x1440RUSprobePASS/ownerprofilesunchanged. Finalbuild0errors/520warnings. Review:Builds/M16_6_9_Russia_Shadow_Review/START_TEST.cmd;evidence:docs/quality/m16-6/russia-shadows;ADR0091. M16.6OPEN for ownerreview;M17notstarted.

2026-11-03 publication confirmed: M16.6.9 commit 5b3b590e29e6636a871b7dd7b7fc1372c85aa99e pushed to GitHub; owner review build ready. M16.6OPEN for owner testing.


2026-11-03 M16.6.10 owner follow-up implemented: exact ski-query reuse, moving-body overtakes with versioned replay policy, read-only physical-impact pose reaction, shooting/standalone qualification. Native79Edit/26Play+2seek+3impactPASS (overlapping groups), focused.NETPASS; complete.NET assembly and5explicit diagnostics still RUNNING. Fullscreen build Builds/M16_6_10_Performance_Review/START_TEST.cmd:0errors. Evidence docs/quality/m16-6/performance and traffic-contact; ADR0092/0093. Overall qualification OPEN; append-only STATE controls latest status. M16.6OPEN;M17notstarted.


2026-11-03 M16.6.10 final qualification complete for owner review: exact ski-query reuse; physical moving passes on permitted free corridors; impulse-driven visible impact; shooting checks. Initial full2204PASS/1FAIL retained; failure fixed with immutablev4 private-mat bend reserve and authored moving-pass zones(ADR0094). Current32-case mass/season+124shooting/steering+203career/parity groupsPASS(overlap359executions); original16-event seasonPASS, both captured reprosPASS. Final native39Edit/16PlayPASS,Console0errors. Full assembly afterv4 not rerun. Finalbuild0errors/522warnings,fullscreen2560x1440/shooting/profilePASS,ownerprofilespreserved,all25settingsbyteexact. Review:Builds/M16_6_10_Performance_Review/START_TEST.cmd. Evidence:docs/quality/m16-6/traffic-contact/acceptance.json. NewAI decisions may change order/times; physics/shooting unchanged,oldreplaysretained. M16.6OPEN FOR OWNER REVIEW;M17notstarted.


2026-11-05 M16.6.11 owner follow-up: natural inward pelvis transfer/distributed16degree turn bank; first range overlay prepared while hidden, conditional resize recomposition.45nativePlayPASS+1final affected range rerunPASS(overlap),40.NETPASS;actual first-reveal layout0.3179→0.1527ms at same tick/route;no whole-frame baseline stall reproduced on this hardware. Finalfullscreen2560x1440PASS/ownerprofilespreserved;build0errors/524warnings;all25settingsbyteexact;Console0errors. Review:Builds/M16_6_11_Turn_Range_Review/START_TEST.cmd. Evidence:docs/quality/m16-6/turn-board/acceptance.json;ADR0095. Authoritative gameplay unchanged;complete simulation suite not rerun. M16.6OPEN FOR OWNER REVIEW;M17notstarted.
