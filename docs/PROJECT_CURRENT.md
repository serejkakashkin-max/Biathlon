# BIATHLON — текущая работа

Обновлено: 2026-10-08. Это указатель, не исторический журнал.
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
