# CODEX DIRECTIVE — Post-WP166 G2 Visual Remediation

**Язык ответа/документов:** русский (допустимы технические названия файлов/папок на английском).
**Проект:** BIATHLON
**Текущая точка:** `WP-166` почти завершён.
**Следующий шаг:** после полного завершения `WP-166` перейти не в `M13`, а в фазу **G2 Visual Remediation**.

---

## 1. Контекст и принципиальное решение

После `WP-166` **НЕ** переходить автоматически к `M13` и **НЕ** считать `G2` пройденным.

Текущее owner-решение:

- `G2` пока считается **NOT APPROVED / FAIL PENDING VISUAL REMEDIATION**;
- `M12` по техническим системам почти закрыт, но визуальное качество benchmark-сцены недостаточно;
- приоритет — не расширение контента на 6 production venues, а **сильная визуальная переработка benchmark-версии**;
- цель — довести benchmark athlete + benchmark venue до уровня, достаточного для реального `G2 APPROVED`;
- только после этого можно возвращаться к плановому переходу в `M13`.

**Master Plan остаётся авторитетным документом.**
Его не нужно переписывать заново. Данное поручение — это **owner-level visual remediation directive**, которая выполняется **между завершением `WP-166` и owner gate `G2`**.

Если нужны изменения в документации, оформить их как:

- addendum,
- owner directive,
- remediation plan,
- review package,

но **не ломать структуру и авторитет исходного `BIATHLON_MASTER_PLAN_FINAL_v2.0.md`**.

---

## 2. Что нужно сохранить без деградации

Во время этой визуальной переработки нужно сохранить и не разрушить:

- весь authoritative simulation kernel;
- уже пройденные milestone M1–M11;
- все существующие тестовые гарантии;
- deterministic behavior там, где он уже зафиксирован;
- текущую игровую логику спринта / преследования / масс-старта;
- существующую архитектуру presentation-state;
- существующую athlete humanoid architecture из `M11`;
- existing ski / rifle / pole / shooting integration, если она уже работает корректно;
- существующий third-person gameplay direction.

**Важно:** игра строится как **third-person biathlon game**.
Игрок должен видеть своего спортсмена, соперников, трассу, стадион и анимации — в духе классических биатлонных игр, но современнее и качественнее.

---

## 3. Отдельная owner-заметка по стрельбе

Owner ранее отметил, что стрельба субъективно ощущалась слишком простой.

На данном этапе это **не главный фокус** — текущая задача именно визуальная переработка.

Требование:

1. **зафиксировать это как открытое owner observation**;
2. не проводить сейчас большой rebalance core shooting mechanics, если в этом нет прямой необходимости;
3. при этом разрешается улучшать:
   - presentation стрельбы,
   - ощущение оружия,
   - позы,
   - IK,
   - recoil/readability,
   - визуальную сложность восприятия,
   - feedback на рубеже.

Нужен отдельный note/report, где это будет отмечено как будущий tuning item.

---

## 4. Стартовое условие

### Сначала обязательно:

1. полностью завершить `WP-166`;
2. оформить и сохранить его profiling report;
3. обновить `PROJECT_STATE.md`;
4. убедиться, что текущее состояние проекта стабильно;
5. только потом начать visual remediation.

### После завершения `WP-166`:

- **не стартовать `M13`;**
- **не размножать текущий визуал на все venues;**
- **не давать себе `G2 PASS`;**
- перейти в режим **G2 Visual Remediation Hold**.

---

## 5. Где лежат новые внешние материалы

Использовать уже подготовленный внешний набор ассетов.

### Базовый путь

```text
C:\game\Biathlon\Biathlon\external_downloads\g2_visual_remediation\
```

### Важные сопутствующие документы

Использовать:

```text
C:\game\Biathlon\Biathlon\docs\licensing\g2_visual\G2_ASSET_DOWNLOAD_REPORT.md
C:\game\Biathlon\Biathlon\docs\licensing\g2_visual\THIRD_PARTY_ATTRIBUTION_DRAFT.md
```

Если в проекте эти файлы лежат в другом месте — сначала аккуратно привести ссылки/пути в порядок, ничего не теряя.

---

## 6. Состав staged assets, который нужно учитывать

### Уже staged / downloaded

Основное, на что ориентироваться:

- `Blender Human Base Meshes v1.4.1`
- `MPFB`
- `Blender CC0 Skin Stamps`
- `Mixamo` (20 FBX)
- `Rokoko 263`
- `Rokoko Sports 12`
- `Rokoko Walk/Run 16`
- `Renderpeople` crowd pack (8 people)
- `Realistic Fir Trees Pack`
- `Pine Trees Pack`
- `Poly Haven` trees / shrubs / snow / rocks / HDRI / fabrics / architecture materials
- `3dassets` stadium/event packs
- `Sketchfab` stadium modules
- `Sketchfab` ski goggles

### Отдельно проверить manual gap

- `Anderson Rohr Crowd Reactions` пока отмечен как manual action required.

Если этот ассет по факту уже добавлен владельцем — принять его в аудит.
Если его всё ещё нет — зафиксировать как missing optional crowd pack и не блокировать весь visual remediation.

### Unity Asset Store пакеты

Есть metadata-only кандидаты:

- `URP Tree Models`
- `European Forests – Realistic Trees`
- `FREE Cinematic Weather VFX`
- `FREE Skybox Extended Shader`

Их **не импортировать слепо**.
Использовать только после отдельной оценки совместимости и целесообразности.

---

## 7. Общая стратегия работ

Работать нужно не как «хаотический апгрейд графики», а как **управляемую remediation phase**.

### Порядок приоритетов

1. **Hero athlete** — самый высокий приоритет.
2. **Athlete animation / presentation polish**.
3. **Forest / track / snow / terrain composition**.
4. **Stadium / event dressing / props / range environment**.
5. **Crowd visual uplift**.
6. **Lighting / atmosphere / HDRI / weather presentation**.
7. **Broadcast/composition polish**.
8. **Новый benchmark review package**.

**Запрещено:** начинать массовое production-scaling, пока benchmark не пройдёт новую owner-проверку.

---

## 8. Обязательная первая фаза — asset audit

Перед controlled import нужно выполнить полноценный аудит.

### Нужно сделать:

1. Прочитать `G2_ASSET_DOWNLOAD_REPORT.md`.
2. Прочитать `THIRD_PARTY_ATTRIBUTION_DRAFT.md`.
3. Пройтись по staged folders в `external_downloads\g2_visual_remediation`.
4. Для каждого ассета определить статус:
   - approved for use,
   - approved with modifications,
   - approved for source-only/reference-only,
   - rejected,
   - deferred.
5. Проверить:
   - формат файлов,
   - целесообразность для Unity/URP,
   - возможные performance risks,
   - лицензионные ограничения,
   - need for attribution,
   - need for de-branding,
   - raw redistribution risks.
6. Убедиться, что CC BY ассеты отражены в attribution draft.
7. Для suspicious branding подготовить safe de-branding plan.

### Отдельное требование по лицензиям

- не использовать ассеты с неясным лицензионным статусом;
- не импортировать raw third-party content в public-facing repo без необходимости;
- не пушить в Git большие исходники и сырые архивы;
- не оставлять реальные логотипы, брендовые маркировки и спорные товарные знаки на финальных игровых объектах.

### Документ результата

Создать новый документ, например:

```text
docs/reports/G2_VISUAL_ASSET_AUDIT.md
```

с таблицей:

- asset,
- source,
- local path,
- license,
- decision,
- intended use,
- import scope,
- required modifications,
- attribution requirement,
- final status.

---

## 9. Главная цель №1 — полностью переработать спортсмена

Текущий mannequin / technical athlete недостаточен для `G2`.

### Требование

Нужно создать **реалистичного benchmark biathlete**, опираясь на существующую `M11` architecture.

### Использовать как основу

- `Blender Human Base Meshes` — как основную базу тела;
- `MPFB` — как инструмент вариативности/вспомогательной генерации, но не как runtime dependency;
- `Blender CC0 Skin Stamps` — для улучшения лица / кожи / микродетали;
- `Bi Stretch` + `Terlenka` — как материальную основу race suit / bib;
- `Ski Goggles` — как исходник для очков (с обязательным de-branding);
- существующую pipeline-архитектуру из `M11` — как presentation foundation.

### Что должно получиться

Нужен **один сильный benchmark hero-athlete**, а не пока что полный ростер.

### У benchmark athlete должны быть:

- реалистичные человеческие пропорции;
- нормальная голова/лицо;
- корректные плечи/локти/таз/колени;
- заметно более качественный skin deformation;
- руки и пальцы, достаточные для believable weapon/pole handling;
- современный облегающий биатлонный костюм;
- bib / harness / визуально правдоподобная экипировка;
- очки;
- ботинки / лыжи / палки / винтовка, согласованные по масштабу и стилю;
- совместимость с existing third-person camera;
- хороший silhouette readability на дистанции;
- LOD strategy.

### Важное ограничение

Не разрушать текущую gameplay integration.
Нужно **подменить и улучшить presentation layer**, а не ломать simulation.

### Что можно и нужно сделать

- построить новый athlete mesh/presentation prefab;
- retarget на существующую humanoid structure;
- сохранить/адаптировать sockets и binding points;
- обеспечить корректное крепление rifle, skis, poles;
- адаптировать existing animations;
- при необходимости использовать Rokoko/Mixamo как source/reference/retarget input;
- переработать материалы и shading;
- сделать `LOD0/LOD1/LOD2/LOD3` либо эквивалентный scalable set.

### Что нельзя делать

- нельзя просто заменить всё на случайную красивую модель без интеграции;
- нельзя превращать проект в tech demo без совместимости с gameplay;
- нельзя подменять core architecture ad-hoc решениями;
- нельзя использовать чужой брендинг / реальные marks.

---

## 10. Главная цель №2 — animation / movement polish спортсмена

После появления нового benchmark athlete нужно улучшить визуальную подачу движения.

### Приоритеты

1. locomotion readability;
2. ski posture;
3. pole timing / planting feel;
4. cornering lean feel;
5. rifle carry transitions;
6. prone / standing transitions;
7. shooting pose quality;
8. fatigue/exhaustion readability.

### Что допускается использовать

- `Mixamo` — только как вспомогательный материал / rig / retarget reference;
- `Rokoko` — как источник более живой motion reference;
- существующие animations — если они уже связаны с logic/state machine;
- procedural / IK improvements — где это действительно повышает качество.

### Важно

Не жертвовать контролируемостью и устойчивостью ради красивой, но нестабильной анимации.

### Итог

Игрок в third-person должен видеть **правдоподобного биатлониста**, а не просто работающую технику.

---

## 11. Главная цель №3 — переработать benchmark environment

Сейчас benchmark scene должна перейти от «технической среды» к **убеждающему современному зимнему биатлонному окружению**.

### Использовать:

- `Fir Trees Pack`
- `Pine Trees Pack`
- `Poly Haven Fir Tree 01` (как source/high-quality donor, не как direct runtime mass placement)
- `Fir Sapling`
- `Shrub 02`
- `Snow 02`
- `Snow 01`
- `Snow Floor`
- `Boulder 01`
- `Rock Face 01`
- winter HDRI sources

### Цели

Нужно улучшить:

- композицию леса;
- вариативность деревьев;
- плотность и силуэт леса;
- foreground/midground/background separation;
- края трассы;
- микрорельеф;
- читаемость трассы;
- визуальное качество снега;
- ощущение зимнего объёма и натуральности.

### Требования

- не использовать тяжёлые исходники без LOD/адаптации;
- сохранять performance budget для benchmark сцены;
- не делать лес однотипным;
- не оставлять пустой «серый полигональный мир»;
- сохранить gameplay readability трассы.

---

## 12. Главная цель №4 — stadium / range / event dressing

Стадион и рубеж должны перестать выглядеть как пустая техсцена.

### Использовать:

- `3dassets Racecourse & Parade Ring`
- `3dassets Live Music Venue & Festival Stage`
- `3dassets Velodrome & Track Cycling`
- `Sketchfab Stadium Viewing Stand`
- `Sketchfab Stadium Seats`
- существующие range/stadium systems

### Что нужно сделать

Улучшить:

- трибуны;
- seating density;
- barriers;
- railings;
- event infrastructure;
- scoreboard/screen presence;
- visual layering;
- service / technical structures;
- stadium dressing around range;
- ощущение настоящего спортивного события.

### Важно

Не нужно механически копировать внешний вид исходного пакета.
Нужно использовать его как **kitbash source** и собрать **собственный биатлонный stadium look**.

---

## 13. Главная цель №5 — crowd visual uplift

Текущая crowd presentation должна стать заметно убедительнее.

### Использовать

- `Renderpeople` free people;
- при наличии — `Anderson Rohr Crowd Reactions`;
- существующие crowd systems.

### Нужно

- повысить визуальную вариативность людей;
- улучшить silhouettes и одежду;
- сделать crowd placement более believable;
- добавить простые reaction loops / cheering behavior, если это не ломает performance;
- сохранить crowd scalability.

### Важно

Это benchmark-quality uplift, а не full crowd simulation rewrite.

---

## 14. Главная цель №6 — lighting / atmosphere / weather presentation

### Использовать

- `Snowy Forest HDRI`
- `Passendorf Snow HDRI`
- `Snowy Forest Path 01 HDRI`
- existing weather systems
- candidate-only sky/weather assets — только если реально оправдано после audit

### Нужно улучшить

- общий lighting direction;
- sky/exposure balance;
- ощущение зимнего воздуха;
- volumetric feel (если уже есть или можно безопасно усилить);
- color contrast;
- readability of athlete against snow/forest;
- broadcast appeal.

### Требование

Нельзя просто подключить очередной эффект ради эффекта.
Нужно получить **чистый, современный, убедительный winter sports look**.

---

## 15. Controlled import strategy

Ничего не импортировать «одним махом».

### Правильная последовательность

1. asset audit;
2. выбрать только реально нужные ассеты;
3. импортировать малыми порциями;
4. после каждого meaningful шага проверять:
   - compile;
   - editor stability;
   - missing references;
   - material correctness;
   - scale;
   - normals;
   - URP compatibility;
   - runtime behavior;
   - memory/performance impact.

### Рекомендуемая логическая группировка импорта

#### Batch A — athlete source
- human base;
- fabric materials;
- goggles;
- supportive skin detail sources.

#### Batch B — athlete motion/support
- selected Rokoko/Mixamo sources.

#### Batch C — forest + snow
- fir/pine packs;
- shrubs/saplings;
- snow/rocks.

#### Batch D — stadium/event
- selected stadium modules;
- props;
- barriers;
- screens;
- stands.

#### Batch E — crowd
- selected Renderpeople;
- optional crowd animation support.

#### Batch F — optional candidates
- only if justified by audit.

---

## 16. Git and repository safety

### Жёсткие правила

- raw external downloads не пушить автоматически;
- большие third-party исходники не заливать в репозиторий без необходимости;
- работать аккуратно с `.gitignore`;
- не превращать репозиторий в dump of downloaded assets;
- в git должны попадать только действительно нужные проектные производные/интеграционные файлы, если это оправдано;
- соблюдать ограничения сторонних лицензий по raw redistribution.

Если для контроля нужна отдельная папка производных импортированных runtime-ассетов — оформить её разумно и документировать.

---

## 17. Что нужно обновить в документации

После старта remediation phase создать/обновить следующие документы (названия можно сохранить или очень близко к ним):

### Обязательные новые/обновлённые документы

1. `docs/reports/WP-166-PROFILING-REPORT.md`
   Финальный результат `WP-166`.

2. `docs/reports/G2_VISUAL_ASSET_AUDIT.md`
   Аудит staged ассетов и решения по каждому.

3. `docs/reports/G2_VISUAL_REMEDIATION_PLAN.md`
   Конкретный remediation plan по потокам работ.

4. `docs/reports/G2_HERO_ATHLETE_IMPLEMENTATION.md`
   Что именно сделано для нового benchmark athlete.

5. `docs/reports/G2_ENVIRONMENT_UPLIFT_REPORT.md`
   Что сделано по лесу/снегу/стадиону/свету.

6. `docs/reports/G2_VISUAL_BENCHMARK_REVIEW.md`
   Новый итоговый self-review перед owner gate.

7. `docs/reports/G2_OPEN_ITEMS.md`
   Что ещё не дотянуто и почему.

8. `PROJECT_STATE.md`
   Обновить так, чтобы в нём ясно отражалось, что проект находится в `post-WP166 / pre-G2 visual remediation phase`.

### Отдельно зафиксировать

В одном из документов должна быть явная строка о том, что:

- `shooting difficulty felt easy to owner during earlier technical playtest`;
- этот пункт отмечен и отложен как follow-up gameplay tuning concern, а не решается сейчас тотальным rebalance.

---

## 18. Что нужно получить на выходе

Нужен **новый benchmark build / benchmark scene package**, который существенно лучше текущего по восприятию.

### Минимально ожидаемый набор результатов

1. **Новый benchmark athlete**
   - реалистичнее,
   - современнее,
   - с хорошим силуэтом,
   - лучше деформируется,
   - корректно интегрирован в existing systems.

2. **Новая benchmark environment presentation**
   - лес,
   - снег,
   - трасса,
   - стадион,
   - рубеж,
   - crowd,
   - lighting.

3. **Новый benchmark capture package**
   - повторить и/или расширить capture matrix уровня `WP-165`;
   - сделать набор кадров/ракурсов достаточный для честного owner review;
   - включить athlete close/medium/far;
   - forest;
   - stadium;
   - range;
   - during-skiing;
   - shooting;
   - crowd/event atmosphere;
   - bad-weather / alternative lighting, если применимо.

4. **Side-by-side comparison**
   - before vs after;
   - старый benchmark vs новый benchmark.

5. **Performance reality check**
   - убедиться, что uplift не делает benchmark непригодным по производительности;
   - где нужно — применить LOD/material/shader discipline.

---

## 19. Жёсткие критерии успеха

Считать remediation phase успешной только если выполнено следующее:

### Технически
- проект компилируется;
- нет новых критических console errors;
- core tests, которые должны быть пройдены, проходят;
- нет разрушения gameplay state/logic;
- third-person playability сохранена.

### Визуально
- спортсмен выглядит как реальный игровой биатлонист, а не как технический mannequin;
- environment выглядит как современная зимняя спортивная сцена, а не как greybox с ассетами;
- стадион и crowd воспринимаются как event space;
- lighting/snow/forest materially improve visual appeal;
- owner review package явно сильнее текущего состояния.

### Процессно
- G2 **не** помечается PASS без честного self-review;
- M13 **не** запускается автоматически;
- asset licensing задокументирован;
- CC BY attribution не потеряна.

---

## 20. Что НЕ делать

### Запрещено

- не переходить в `M13`, пока owner не одобрит новый benchmark;
- не разносить текущий слабый визуал на все venues;
- не делать полный rewrite core systems;
- не ломать determinism без крайней причины;
- не втаскивать всё скачанное в проект без фильтрации;
- не импортировать every optional asset «на всякий случай»;
- не игнорировать лицензии;
- не оставлять брендовые детали в goggles или других imported assets;
- не скрывать open issues;
- не объявлять успех раньше времени.

---

## 21. Как трактовать Master Plan

### Важно

Master Plan **не отменяется**.

Правильная трактовка:

- `WP-166` нужно завершить по плану;
- затем вместо немедленного перехода в `M13` выполнить **visual remediation before G2 approval**;
- считать это owner-governed corrective phase внутри существующей логики плана;
- после успешной remediation и нового owner approval — вернуться к нормальному движению по Master Plan.

Если нужна явная фиксация этого в документации — оформить **addendum / gate note / remediation note**, а не переписывать весь план.

---

## 22. Ожидаемый порядок действий от тебя, Codex

### Шаг 1
Завершить `WP-166` и оформить profiling report.

### Шаг 2
Обновить `PROJECT_STATE.md` и перевести проект в состояние:

```text
post-WP166 / pre-G2 visual remediation
```

### Шаг 3
Провести `G2 visual asset audit` по staged assets.

### Шаг 4
Сформировать remediation plan с приоритетом:

```text
athlete → animation → forest/snow/terrain → stadium/event → crowd → lighting → benchmark review
```

### Шаг 5
Реализовать новый benchmark hero-athlete.

### Шаг 6
Выполнить environment uplift benchmark-scene.

### Шаг 7
Сделать новый benchmark capture package.

### Шаг 8
Провести честный self-review и подготовить owner gate package.

### Шаг 9
Остановиться и ждать owner decision по `G2`.

---

## 23. Финальный формат ответа после выполнения этой директивы

Когда закончишь remediation phase и подготовишь новый benchmark package, дай owner структурированный отчёт:

1. Статус `WP-166`.
2. Статус `G2 Visual Remediation`.
3. Что импортировано и почему.
4. Какие ассеты отклонены и почему.
5. Что сделано по athlete.
6. Что сделано по environment.
7. Что сделано по stadium/crowd/lighting.
8. Что осталось open.
9. Где лежат новые benchmark captures.
10. Явное решение:
    - `recommend G2 PASS`, либо
    - `recommend G2 FAIL / more remediation needed`.

Не нужно сглаживать оценку. Нужен честный production-quality вывод.

---

## 24. Итоговая суть поручения в одной фразе

**После завершения `WP-166` не идти дальше по контентному масштабированию, а провести сильную, управляемую, документированную визуальную переработку benchmark athlete и benchmark venue на основе уже скачанных бесплатных ассетов, не ломая core architecture, и только потом снова выйти на owner review for `G2`.**
