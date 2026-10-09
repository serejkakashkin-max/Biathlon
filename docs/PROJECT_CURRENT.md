# BIATHLON — текущая работа

Обновлено: 2026-10-09. Это указатель, не исторический журнал.
Полная история от Foundation сохранена в [PROJECT_STATE.md](PROJECT_STATE.md);
только последняя датированная запись определяет текущий статус. PROJECT_STATE
остаётся append-only; исторические записи и восстановленные снимки не удаляются.

G1/G2 APPROVED. **M13 OPEN; WP-176 PASS; WP-175/177/178 OPEN.**
Одна production трасса Woodland; WP-170–174 отложены решением владельца.
Поле не более 40 участников; Mass Start 30. Master Plan сохранён.
Следующий Windows build — единый пакет M13, затем owner hands-on проверка.
Промежуточного нового билда нет. Проверки выполняются скрыто.

Текущие игровые Assets используют проверенный короткий спуск: прямое окно
сокращено со 140 до 80 м при общей длине Main 1 200 м. С ним согласованы
terrain, forest, ленточные ограждения, зрители, флаги и roadside dressing.
89 JSON побайтно совпадают с квалифицированным пакетом; .meta сохранены.
Строгий venue validator: 0 diagnostics. Реальный скрытый Game View пакета
снят на Main 696/761/839/887 м; это визуальные кадры, не FPS benchmark.

AI объезда у рубежа повторно проверяет удерживаемую цель каждый тик: ширину
назначенного пути, ближайший боковой шаг и продвижение вперёд. Реактивное
сохранение линии не отменяет проверенный доступный объезд. Размеры тела,
стрельба и способности спортсменов не менялись. Owner одобрил суммарный
предел выхода 193 actor-ticks; отдельный предел непрерывной остановки 75
и private-path total 700 сохранены. Это не общее одобрение очередей или M13.

| Короткий Mass Start 30 | Завершение, тик | Непрерывный простой, максимум | Private paths | Exit |
|---|---:|---:|---:|---:|
| Seed 123 | 20 691 | 36 | 394 | 76 |
| Seed 321 | 20 737 | 70 | 595 | 166 |

Выходы после второго/третьего рубежей с нулевым усилием игрока: 2/2 PASS,
реальный преследователь обогнал, 0 тиков его остановки. Игрок при этом может
катиться: это не доказательство объезда постоянно неподвижного тела.
Полный replay без AI — 20 691 тик, 169 026 серий команд, точное совпадение
состояния/результатов/событий. Профильная .NET регрессия 17/17 и проверка
перенесённых данных/AI/ручных компонентов 69/69 PASS. Unity compile PASS;
финальная скрытая native проверка выхода 1/1 PASS, Error Console пуст.
[Отчёт и доказательства](reports/TASK_M13_RANGE_BYPASS_INVALIDATION_20261007.md).

| Направление | Что осталось |
|---|---|
| Полностью ручное руление | Ядро yaw, силы, AI, world-body/course sweep, odometer и one-section Sprint commit проверены. Playable Pack всё ещё использует lateral v1 и автоматически следует линии трассы. Pack уже публикует forward base-section world ticks и snapshot pose/velocity. Нужны start/range/penalty/seam paths, surface/phase/contact continuation, события, полный replay и host input/rig подключение. |
| WP-175 | Полная headless матрица snow Sprint40→Pursuit40/Mass30 и пять корректных Unity Game View погодных режимов сохранены. После manual интеграции нужны player traffic и rendering/readability проверка итогового пакета. |
| WP-176 | PASS: 16 authoritative Woodland гонок в сезоне, 52 недели и save/restore. Новый/старый календарь сохраняют собственную identity. UI — M14. |
| WP-177 | Короткий рельеф перенесён, два traffic seed/replay/выходы проверены. Финальная калибровка с ручным управлением и owner проверка остаются открытыми. |
| WP-178 | Нужны скрытые sustained frame/GC/memory/LOD замеры итогового Mass Start и broadcast по tier. Editor-after-test и headless throughput не доказывают производительность Windows player. |

Поза tuck, разведение палок, естественные боковые шаги без рывков корпуса,
ранние cyan arrows вдали от ковриков и камера изготовки уже имеют native
проверки. Физическая связь направления и объезда будет проверена вместе с
manual host. Owner issue «стрельба слишком лёгкая» сохранён для последующего
feel/difficulty/balance tuning без hidden RNG и без скрытой перенастройки.

Нельзя считать подготовленные примитивы ручного движения, отдельные
прогоны или отклонённые эксперименты завершением M13. Следующий шаг —
все race phases, geometry intervals и физические contacts/seams.

Manual replay prerequisite: ledger initial pose/velocity/cursor/clock now have
an immutable exact receipt. .NET50/50 and native15/15 PASS; movement unchanged.
[Report](reports/TASK_M13_MANUAL_WORLD_INITIAL_CONTEXT_20261007.md).

Pack base-world bridge: .NET50/50 + legacy38/38; native8/8 +9/9 PASS.
Stale second-row rejection is atomic; encoded endpoint velocity continuity fixed.
This is forward same-section publication, not a live manual runner.
[Report](reports/TASK_M13_MANUAL_PACK_PUBLICATION_20261007.md).

Accepted world velocity now continues through changing directional grade.
Real Woodland Pack:21 ticks/2 athletes using AI yaw; all-manual .NET177/177,
native14/14 +8/8 +5/5 +9/9 PASS. Sporadic allocation risk remains OPEN.
[Report](reports/TASK_M13_MANUAL_ACCEPTED_VELOCITY_20261007.md).

Actual-world posture and backward base publication: focused.NET39/39,
native14/14 PASS; five reverse ticks retain credit and grow odometer.
Two broad183-test runs each have one intermittent allocation failure;
allocation risk remains OPEN. No live manual/build closure.
[Report](reports/TASK_M13_MANUAL_ACTUAL_WORLD_20261007.md).

2026-10-08 resume: backward downhill/tuck snapshot.NET2/2 and native2/2 PASS.
Allocation isolation narrows test invocation versus core loop but root cause
remains OPEN. Explicit tiered-off183/183 PASS does not erase prior failures.
[Report](reports/TASK_M13_MANUAL_ALLOCATION_ISOLATION_20261008.md).

2026-10-08 physical path catalog: exact start/range/mat/exit and explicit penalty
ordinals; queries never grant admission or route credit. Scoped geometry cannot
bind the old base-world ledger. Final.NET69/69 and native16/16 PASS; exact endpoint
encoding defect repaired. Intermittent allocation failures retained/risk OPEN.
Live manual/phase/host and final M13 build remain pending.
[Report](reports/TASK_M13_MANUAL_PHYSICAL_PATHS_20261008.md).

2026-10-08 scoped progress/ordinary handoff: backward/return credit is protected,
start/penalty arc never adds base distance, exact same-pose ordinary seams verified.
Firing/penalty/finish gates remain closed pending race-owner integration.
.NET26/26 and native10/10 PASS. No live manual or final-build closure.
[Report](reports/TASK_M13_MANUAL_PHASE_PROGRESS_20261008.md).

2026-10-08 heterogeneous world roster: stationary start plus moving base actor
with different mat paths share a complete owned world tick. Contact/course/cap
rejections cannot mint standing ticks; ordinary handoff is atomic. .NET34/34,
native8/8 PASS. Contact continuation, session gates/replay/host and final build OPEN.
[Report](reports/TASK_M13_MANUAL_SCOPED_WORLD_20261008.md).

2026-10-08 scope-gate correction: full intervals cannot hide end/start or phase-cap
excursions in clamped observations. .NET87/87 and native16/16 PASS, Console empty.
Live manual/session continuation and final M13 build remain OPEN.
[Report](reports/TASK_M13_MANUAL_SCOPE_GATES_20261008.md).

2026-10-08 complete-clock interval staging: real ordinary seam pays full0.05 s
without changing yaw/velocity or publishing partial rows. New.NET10/10, native
10/10+16/16 PASS. Broad default256/259 has three existing allocation failures;
explicit tiering-off259/259 is diagnostic only. Qualification and M13 remain OPEN.
[Report](reports/TASK_M13_MANUAL_WORLD_TICK_20261008.md).

2026-10-08 frozen trajectory slices: reuse original force/snow interval across
clock cuts. Negative contact-slice quadrature subtraction regression repaired;
whole query behavior retained. .NET84/84, native17+11+9+8 PASS, Console empty.
Earlier broad allocation qualification, full manual/host and M13 build OPEN.
[Report](reports/TASK_M13_MANUAL_TRAJECTORY_SLICES_20261008.md).

2026-10-08 reached contacts: actual terminal admission, immutable mass-bound
group impulse and paid suffix in one owned tick. .NET94/94; native9+13+8+11 PASS.
Compile/Console clear. General scheduler, session/replay/host, allocation
qualification and M13/build OPEN.
[Report](reports/TASK_M13_MANUAL_CONTACT_ARRIVAL_20261008.md).

2026-10-08 scoped sessions: actual physical input and same-occurrence full-roster
publication across start/range/penalty, no legacy resumption. .NET92/92 and native
14+4 PASS. New reproducible boxing fixed; old allocation qualification OPEN.
Cross-occurrence/lifecycle/contact scheduler/Pack/replay/host and M13/build OPEN.
[Report](reports/TASK_M13_MANUAL_SCOPED_SESSION_20261008.md).

2026-10-08 owned interval/session seams: start→base and physical pre-entry
approach pay complete same-clock motion, exact start exit time. .NET71/71;
native4+14 PASS. Phase gates, contact scheduler/events, Pack/replay/host and
allocation qualification/M13/build OPEN.
[Report](reports/TASK_M13_MANUAL_SESSION_INTERVALS_20261008.md).

2026-10-08 accepted phase gates: actual five-shot departure moves on next shared
force tick; real penalty debt/ordinal endpoints preserve world motion/time.
.NET77/77, native6/6 PASS. In-tick future gates/contacts/Pack/replay/host and
allocation qualification/M13/build remain OPEN.
[Report](reports/TASK_M13_MANUAL_ACCEPTED_PHASES_20261008.md).


2026-10-08 paid in-tick penalty phases: moving entry/repeat/return preserve speed
and pay the suffix; provisional lifecycle preview publishes only with full clock.
.NET82/82; native5+6 PASS, Console empty. Contact/field/replay/host, broad allocation
qualification and M13/build remain OPEN.
[Report](reports/TASK_M13_MANUAL_WORKING_PHASES_20261008.md).


2026-10-08 actual first-contact scheduling: shared reached endpoint group and
continuous all-pair/edge prefix; world v9 pays contact/response/suffix on one clock.
.NET103/103; native9+9 PASS, Console empty. Full runner/slip/events/field/replay/
host, prior broad allocation qualification and M13/build remain OPEN.
[Report](reports/TASK_M13_MANUAL_FIRST_CONTACT_20261008.md).


2026-10-08 scoped field/Pack publication: focused.NET94/94, native14+4 PASS,
compile/Console clear. Strict all-plane straight-cell alternative retains full
body/phase boundaries; original outer start seam still unqualified. Broad default
327/328 fails existing allocation test at440 B; qualification OPEN. Driven slip,
all-phase runner/resources/replay/host and M13/final build remain OPEN.
[Report](reports/TASK_M13_MANUAL_SCOPED_FIELD_20261008.md).


2026-10-08 shared longitudinal contact drive: no velocity/heading snap; physical
backwards braking and paid forward restart. .NET14/14 and focused84/84 PASS; native
14+17+13 PASS/Console empty. Broad manual/replay364/365 fails existing allocation
assertion5496 B; qualification OPEN. Controlled slip yaw/persistent contacts,
resources/replay/host and M13/final owner build remain OPEN.
[Report](reports/TASK_M13_MANUAL_CONTACT_DRIVE_20261008.md).


2026-10-08 owned current contacts: stable actual proximity pairs, initial response
without pose/time correction, separated-pair removal and full-group atomicity.
.NET140/140/native11+16 PASS; compile/Console clear. Unequal-force continuing
contacts remain unproved, earlier allocation qualification OPEN. Full manual
runner/resources/replay/host and final M13 build remain pending.
[Report](reports/TASK_M13_MANUAL_OWNED_CONTACTS_20261008.md).


2026-10-08 continuing contact forces: actual two-endpoint support reactions share
unequal traction without pose/velocity correction or a paid stop. New .NET19/19;
native19+8+11 PASS, compile/Console clear. Actual Woodland pair moves on all100
complete clocks; 40-body algorithm witness needs2507/4096 passes and is not a
production performance approval. Broad210/211 then211/213 FAILED on existing
allocation assertions6256/8088/2576 B; qualification OPEN. Signed slip steering,
full runner/resources/replay/host and M13/final build remain OPEN.
[Report](reports/TASK_M13_MANUAL_CONTACT_FORCES_20261008.md).


2026-10-08 common contact/boundary clock: world v13 selects actual common event
prefixes and preserves velocity/time across paid ordinary and penalty seams.
Focused.NET21/21/native21+5+19 PASS; compile/Console clear. Final broader.NET258/260
FAILED on existing allocation assertions7440/3144 B. Windows8.0.31 empty-counter
BGC diagnostic7/256 versus0/256 for identical DLL without BGC; test-host validation
is the next bounded step. Full manual runner/slip/resources/replay/host and
WP-175/177/178/M13/final build remain OPEN; WP-176 PASS. Historical failures remain.
[Report](reports/TASK_M13_MANUAL_COMMON_EVENTS_20261008.md).


2026-10-08 allocation measurement correction: package-free Windows/CoreCLR8.0.31
empty-loop A/B reproduces BGC counter positives twice (7/256,10/256), identical DLL
without BGC0/256 twice, exact test-runtime0/256; all real64 B controls32/32.
Test project alone disables concurrent GC; exact assertions and game/Unity/
BalanceLab settings unchanged. Original subset260/260 and all-manual/replay416/416
PASS. Earlier failed receipts/statuses preserved; selected .NET allocation
qualification now PASS in this test profile, production performance still OPEN.
Common-event code b5e595f pushed. M13/host/manual runner and WP-175/177/178/final
owner build OPEN; WP-176 PASS. Next controlled slip yaw + complete common runner.
[Report](reports/TASK_M13_TEST_COUNTER_BGC_20261008.md).


2026-10-08 controlled slip steering: signed A/D deflects a frozen front snow
constraint; one front/rear grip box changes independent velocity/spin with
non-increasing kinetic energy and the existing angular-response limits. Shared
longitudinal physics and actual-yaw cornering retained, distinct policy context.
New15 tests, 1620-case energy/impulse matrix, actual reached-pair order parity,
100-step model turn/release, one fully paid signed prepared session/world tick;
all-manual/replay.NET431/431 and native15+11+14 PASS; compile/Console clear.
Production host still legacyv1; full runner/seams/resources/shooting/finish/AI/
replay/host/WP-175/177/178/M13/final owner build OPEN. WP-176 PASS. Next complete
common-clock runner using controlled drives; no intermediate Windows build.
[Report](reports/TASK_M13_MANUAL_CONTROLLED_SLIP_20261008.md).


2026-10-08 stationary firing common clock: world v14 explicitly admits already
owned FiringLine holds only at exact stationary full trajectories/endpoints.
Complete body/contact/course/edge/phase/session proof remains required; movers,
spin, acceleration and start/ordinary/exit/penalty/finish holds reject. Mixed
shooting + moving actors pay the same clock; no shooting/departure permission.
.NET442/442 plus final focused11/11, native11+21 PASS; initial NUnit AnyOf compile
error preserved/fixed; final refresh success, recent Console clear. Controlled
steering c16794d pushed. M13/WP-175/177/178/full runner/AI/seams/resources/replay/
host/final owner build OPEN; WP-176 PASS. No intermediate Windows build.
[Report](reports/TASK_M13_MANUAL_SHOOTING_HOLD_20261008.md).

2026-10-09 common manual field/contact/ordinary portals: focused.NET111/111;
phase correction40/40; native portals12, forces21, phases5, controlled16,
spatial15, physical AI4 and final ribbon9 PASS. Baseline stationary-player
Mass30 native400ticks PASS. New physical-velocity AI .NET Mass30PASS, but
first-lap test remains FAIL at tick3021/mat approach (previous tick584/base).
Anticipatory braking and physical stationary docking are next; no domain widening,
unpaid velocity reset, paid queue or legacy fallback. Original449/444/5 and aborted
rerun retained; new broader diagnostic pending. Production host still lateral-v1;
resources/finish/draft/replay/WP175/177/178/M13/build OPEN. Next Windows review
build only after all M13. [Report](reports/TASK_M13_MANUAL_FIELD_CONTACTS_AND_PORTALS_20261009.md).

Broad diagnostic:533PASS/1FAIL then ABORTED on a60s watchdog in the additional
full-field persisted replay (its own timeout900s). Standalone replay is running
with its declared limit; no PASS claim. New native initial physical AI Mass30:1/1PASS.

2026-10-09 latest: shared controlled braking + ordered AI bend approach. Final
all-manual.NET466PASS/1FAIL; focused67/67, native21+1+3+5+10 PASS; compilation/
Console clear. Tight mat bend now passed, known active failure moves to tick3214
MovingFiringArrival (lane03, residual physical motion). Next stationary docking;
no paid wait/snap. Legacy full persisted replay standalone1/1PASS,20691ticks,
hash15908983522868725793; manual replay still OPEN. Production host lateral-v1;
M13/WP175/177/178/final build OPEN, WP176 PASS. No intermediate Windows build.
[Report](reports/TASK_M13_MANUAL_BRAKING_AND_BEND_APPROACH_20261009.md).

2026-10-09 latest: analytical exact neutral physical rest removes floating point
impulse-cancellation residue only when the full free constraint is attainable.
Low-grip residuals remain. .NET470PASS/1FAIL/471; native25+8+14 PASS; compile/
Console clear. Known tick3214 real MovingFiringArrival still OPEN, next inverse
stopping/docking. Host lateral-v1/M13/WP175/177/178/build OPEN; WP176 PASS.
[Report](reports/TASK_M13_MANUAL_EXACT_PHYSICAL_REST_20261009.md).

2026-10-09 latest: separated stop/cap free ramps keep their complete physical law;
reached contacts and ordinary constant-force representation retained. Refined
bounded full-rectangle cover and inverse shared-force braking queries qualified
as components. Final.NET482PASS/1FAIL/483; native30+4+13+21+1 PASS; compile/Console
clear. Mass30 stationary-player400ticks PASS again after rejected controller
trials were removed. Known first-bout moving arrival tick3214 still OPEN; terminal
controller trials/evidence preserved outside Unity. Host lateral-v1, resources/
finish/draft/manual replay/WP175/177/178/M13/final build OPEN; WP176 PASS. Next
qualified last2–3m mat control. PROJECT_STATE appended without historical edits.
[Report](reports/TASK_M13_MANUAL_STOP_PIECES_AND_BODY_COVER_20261009.md).

2026-10-09 latest: the known first-bout moving arrival is resolved in the scoped
candidate. Optional data-driven terminal braking, exact paid neutral stop and
continuous firing-plane admission bring all three Woodland athletes to their
first bouts with actual zero velocity/spin. Final manual.NET493/493 plus exact
two-run physiology/world/events1/1 PASS; native9+1+30+1 PASS; compile/Console clear.
Earlier failed trials remain. This is the final0.1m braking strip, not the full
2–3m player capture, complete formats, manual replay or host selection. Next:
shooting/departure/penalty/finish and resource integration. Host lateral-v1,
M13/WP175/177/178/final build remain OPEN; WP176 PASS. State history preserved.
[Report](reports/TASK_M13_MANUAL_TERMINAL_MAT_BRAKING_20261009.md).
