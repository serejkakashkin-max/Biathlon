OWNER DIRECTIVE — M13 VELOCITY / WP169 CLOSURE

Прими это как owner-level execution directive для текущего M13.

Главная цель этой директивы:
ПЕРЕСТАТЬ БЕСКОНЕЧНО РАСШИРЯТЬ WP169 МИКРО-ЧЕКПОИНТАМИ И ДОВЕСТИ VENUE01 ДО PRODUCTION ACCEPTANCE, ПОСЛЕ ЧЕГО ПЕРЕЙТИ К WP170.

Master Plan остаётся авторитетным.
Не переписывай Master Plan.
Эта директива регулирует способ выполнения M13, а не меняет его содержание.

==================================================
1. CURRENT OWNER ASSESSMENT
==================================================

G2 APPROVED.
M12 PASS.
M13 начат правильно.
WP167 и WP168 завершены.
WP169 сейчас является активным production work package.

Архитектурная и тестовая глубина проекта уже доказана.

Однако WP169 становится чрезмерно фрагментированным:
слишком много микрочекпоинтов, forensic diagnostics, промежуточных полных regressions и доказательств для локальных presentation/content изменений.

Это необходимо остановить.

Теперь приоритет:
DELIVERY > дальнейшее исследование уже доказанной инфраструктуры.

Не снижать качество.
Не ослаблять инварианты.
Не удалять тесты.

Но прекратить over-engineering там, где корректность уже достаточно доказана.

==================================================
2. WP169 ДОЛЖЕН ПЕРЕЙТИ В CLOSURE MODE
==================================================

Не создавать новые длинные цепочки вида:

TASK169AB / AC / AD / AE / ...
или новые исследовательские ветки,

если не обнаружен настоящий blocking defect.

Текущая цель WP169 — не дальнейшее совершенствование tooling architecture.

Цель:

VENUE01 = PRODUCTION-QUALITY PLAYABLE VENUE.

Закрыть только реально оставшиеся acceptance items и перейти дальше.

==================================================
3. FREEZE ALREADY-PROVEN WORK
==================================================

Следующие области считать доказанными и НЕ исследовать повторно без нового regression evidence:

- WP167 venue authoring schema/tools;
- WP168 validator;
- deterministic core;
- historical 60-athlete engine/replay capacity;
- TASK169Y Pursuit60 historical replay proof;
- TASK169T/T2 renderer-frame/route seam work;
- TASK169Z High forest visibility;
- accepted terrain tile renderer-frame solution;
- existing Mass30 simulation capability;
- existing G2 visual benchmark direction.

Не выполнять дополнительные исторические Pursuit60 mega-runs только ради ещё одного подтверждения.

Production scope сейчас определяется owner directive:

- Sprint: max 40 production participants;
- Pursuit: max 40 production participants;
- Mass Start: 30;
- historical generic 60 capacity сохраняется как regression capability, но НЕ является текущей production acceptance target.

Historical 60 proof не должен блокировать WP169.

==================================================
4. ОСТАВШИЕСЯ ОБЯЗАТЕЛЬНЫЕ WP169 ITEMS
==================================================

С этого момента WP169 должен быть сфокусирован ТОЛЬКО на следующем.

A. PRODUCTION RANGE

Закончить корректный компактный range:

- physical lane spacing: примерно 2.75–3.0 m;
- left-side athlete entry;
- right-side exit;
- корректная физическая circulation;
- корректные mats;
- prone спортсмен лежит НА своём mat;
- standing спортсмен находится НА своей lane position;
- никаких presentation-only mat shifts;
- никаких teleport hacks;
- никаких уменьшений athlete/body clearance ради прохождения;
- никаких global queues как маскировки геометрической проблемы.

Range должен выглядеть и работать как настоящий биатлонный рубеж.

B. WHITE RECTANGLES

Диагностировать и убрать белые/светлые прямоугольные patches в stadium/range area.

Не продолжать бесконечную диагностику после установления причины.

Как только root cause доказан:
исправить,
сделать focused regression,
проверить Game View,
закрыть.

C. SPRINT EXTENSION CURTAIN

Убрать/исправить видимую production-некорректную curtain/wall геометрию Sprint extension.

Не создавать новый универсальный terrain framework ради одного визуального дефекта, если проблема может быть исправлена bounded content/presentation решением без нарушения route geometry.

D. ACTUAL PRODUCTION FIELD VALIDATION

После range correction выполнить реальные production flows:

Sprint40
→ Pursuit40

и отдельно:

Mass Start30.

Проверить:

- complete races;
- range entry/exit;
- correct lane occupancy;
- penalty flow;
- finish;
- results;
- Pursuit qualification/start gaps;
- replay correctness.

Это текущая authoritative production acceptance.

E. WINDOWS REVIEW BUILD

После исправления A–D:

- собрать новый Windows Development review build;
- smoke Sprint40;
- smoke Pursuit40;
- smoke Mass30;
- проверить нормальный third-person gameplay;
- проверить range визуально;
- проверить stadium;
- проверить forest;
- проверить obvious clipping/walls/patches.

Подготовить owner review package.

==================================================
5. 30.05% STOPPED SKI TIME
==================================================

Существующий показатель Pursuit60:
30.05% stopped ski time

НЕ превращать сейчас в новый WP169 research branch.

По текущему плану race-balance/course calibration относится к WP177.

Следовательно:

- сохранить finding;
- сохранить telemetry;
- перенести полноценный cause/balance review в WP177.

Исключение:

если есть конкретное доказательство, что существенная часть остановок вызвана дефектом геометрии / range / junction именно Venue01, то исправить этот конкретный venue defect сейчас.

Не делать глобальный AI/balance rewrite внутри WP169.

==================================================
6. TESTING POLICY — ПЕРЕЙТИ НА RISK-BASED TESTING
==================================================

Автоматические тесты обязательны.
Но не нужно выполнять максимальный full regression после каждого визуального или локального content tweak.

Использовать следующую стратегию.

Для локального presentation/content изменения:
- focused tests;
- relevant validator tests;
- Unity compile;
- Console;
- targeted Game View.

Для изменения range/path/race-affecting content:
- focused route/range tests;
- relevant race integration tests;
- deterministic/replay test соответствующей области.

Полный PlayMode:
- после завершения значимого coherent checkpoint;
- перед WP169 acceptance;
- перед Windows owner build.

Полный standalone .NET regression:
- если менялся authoritative/core/data-contract код;
- либо один раз перед финальным WP169 acceptance.

Не запускать огромный full .NET suite повторно после чистых material/shader/presentation changes, если authoritative core не менялся.

Не снижать test coverage.
Снизить только ненужную ЧАСТОТУ полного повторения уже доказанных тестов.

==================================================
7. DOCUMENTATION POLICY
==================================================

PROJECT_STATE должен оставаться кратким operational resume, а не становиться журналом каждого неудачного экспериментального кадра.

Сохранять evidence и failed diagnostics в artifacts/reports там, где это полезно.

Но в PROJECT_STATE держать:

- current phase;
- active WP;
- last completed coherent task;
- current open acceptance items;
- relevant test baseline;
- exact next action.

Не добавлять огромные forensic narratives для каждого временного исправления.

После завершения WP169 создать ОДИН консолидированный production report, например:

docs/reports/WP169_VENUE01_PRODUCTION_REPORT.md

В нём:

1. final venue identity;
2. supported formats;
3. range configuration;
4. route variants;
5. visual/content summary;
6. external assets/licenses;
7. actual Sprint40/Pursuit40/Mass30 results;
8. replay validation;
9. performance snapshot;
10. known deferred issues;
11. Windows build path;
12. WP169 PASS/FAIL.

==================================================
8. VISUAL QUALITY POLICY
==================================================

G2 уже APPROVED.

НЕ открывать заново G2.

Venue01 должен соответствовать утверждённому benchmark direction, но WP169 не должен превращаться во второй многонедельный Visual Benchmark.

Исправлять:

- obvious visual defects;
- broken geometry;
- visible curtains;
- white patches;
- clipping;
- incorrect range layout;
- glaring environment failures.

Не пытаться сделать Venue01 идеально финальным до Beta.

Позднее остаются:

WP175 multi-venue weather matrix;
WP177 venue race-balance calibration;
WP178 venue performance/LOD;
M17 Alpha;
M18 Beta/polish.

Не выполнять работу этих будущих WP заранее без необходимости.

==================================================
9. WP169 HARD DEFINITION OF DONE
==================================================

WP169 может быть закрыт, когда:

1. Venue01 существует как цельный production venue.
2. Sprint40 полностью проходит.
3. Pursuit40 полностью проходит от Sprint qualification.
4. Mass Start30 полностью проходит.
5. Replay/authoritative state для production configurations проходит.
6. Range имеет нормальную compact geometry и flow.
7. Prone/standing визуально находятся на корректных mats/lane positions.
8. White rectangles устранены.
9. Sprint curtain устранена либо заменена production-correct geometry.
10. Route/terrain не имеют blocking gaps/walls.
11. Visual inspection не показывает очевидного production blocker.
12. Relevant focused tests PASS.
13. Final full PlayMode PASS.
14. Final core regression PASS, если core/data changes это требуют.
15. Windows Development build запускается и все 3 production formats smoke-tested.
16. Console не содержит новых критических errors.
17. Licenses/attribution остаются корректными.
18. Консолидированный WP169 report создан.
19. PROJECT_STATE обновлён.

После выполнения этих пунктов:

WP169 = DONE.

Не продолжать полировку WP169 ради второстепенных улучшений.

==================================================
10. СРАЗУ ПОСЛЕ WP169
==================================================

Если нет реального owner blocker:

автономно переходить к WP170.

Не ждать дополнительного owner approval между каждым production venue.

Master Plan уже определяет:

WP169 — Venue01
WP170 — Venue02
WP171 — Venue03
WP172 — Venue04
WP173 — Venue05
WP174 — Venue06
WP175 — multi-venue weather matrix
WP176 — career calendar content
WP177 — venue race-balance calibration
WP178 — venue performance/LOD pass
→ M13 Gate

==================================================
11. КРИТИЧЕСКОЕ ПРАВИЛО ДЛЯ WP170–174
==================================================

WP169 является первым production venue и поэтому естественно потребовал некоторого pipeline hardening.

НО:

WP170–174 НЕ должны повторять масштаб инженерной разработки WP169.

Venue authoring schema и validator уже существуют.

Следующие venues должны преимущественно быть:

CONTENT AUTHORING THROUGH THE EXISTING PIPELINE.

Для каждого следующего venue:

- distinct visual identity;
- distinct tactical identity;
- terrain/course;
- semantic zones;
- stadium/range composition;
- weather/lighting setup;
- validation;
- three-format compatibility where applicable;
- concise report.

Не создавать новый subsystem/tool для каждого venue.

Новый shared subsystem допускается только если:

1. существующий pipeline объективно не может выразить обязательный content requirement;
или
2. один и тот же deficiency обнаружен минимум в нескольких venues;
или
3. без него невозможно пройти acceptance.

Предпочитать data/content over new architecture.

==================================================
12. COMMIT / CHECKPOINT DISCIPLINE
==================================================

Не нужен отдельный commit/checkpoint на каждую микроскопическую диагностическую попытку.

Предпочитать coherent deliverables:

- compact range correction;
- stadium visual cleanup;
- production field integration;
- final acceptance/build.

Failed evidence можно сохранять без превращения каждого experiment в отдельную production milestone.

Цель — прозрачная история, но не task explosion.

==================================================
13. OWNER-FEEL-001
==================================================

OWNER-FEEL-001:
player shooting feels too easy

остаётся OPEN.

Не решать сейчас внутри WP169, если это не correction obvious presentation/placement defect.

Главный tuning review остаётся:

WP229
WP230
WP232.

No hidden RNG.
No silent shooting rebalance.

==================================================
14. CURRENT PRIORITY ORDER
==================================================

С этого момента порядок строго такой:

1. Finish TASK169AA production40 import/regression.
2. Build/fix compact physical range.
3. Fix white rectangles.
4. Fix Sprint extension curtain.
5. Complete actual Sprint40 → Pursuit40.
6. Complete Mass30.
7. Verify replay.
8. Final visual Game View inspection.
9. Final relevant regression.
10. Windows owner review build.
11. Consolidated WP169 report.
12. Mark WP169 DONE.
13. Immediately begin WP170.

Не добавлять новые side quests между этими пунктами без реального blocker evidence.

==================================================
15. FINAL OWNER INTENT
==================================================

Качество проекта остаётся приоритетом.

Но качество ≠ бесконечное доказательство каждого микроизменения.

На текущем этапе главный риск уже не недостаток инженерной строгости.
Главный риск — потеря production velocity.

Нужно сохранить сильную архитектуру и тестовую дисциплину, но начать быстро превращать её в законченный контент.

WP169 должен быть закрыт как первый production venue.
После этого pipeline должен показать свою ценность за счёт более быстрого WP170–174.

Proceed autonomously.

Stop only for:
- real blocking technical defect;
- explicit owner gate;
- licensing/paid/account action;
- material scope decision.

Otherwise continue according to the Master Plan.