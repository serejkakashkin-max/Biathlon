# BIATHLON — Game Design Document + Technical Design v0.1

**Статус:** рабочая спецификация для разработки через Codex  
**Дата:** 2026-09-06  
**Целевая платформа v0.1:** Windows PC  
**Движок:** Unity 6  
**Главный принцип производства:** код, игровые данные, тесты, сцены и итерации создаются Codex. Ручная работа в Unity Editor должна быть сведена к минимуму и не быть обязательной для функционирования проекта.

---

## 0. Назначение документа

Этот документ является основным контрактом между владельцем проекта и Codex. Любая новая система должна:

1. соответствовать игровому видению;
2. быть детерминированной там, где это возможно;
3. иметь данные вне кода;
4. иметь автоматические тесты;
5. работать без обязательной зависимости от визуального слоя Unity;
6. быть наблюдаемой через telemetry/event log;
7. иметь измеримые критерии качества;
8. не требовать скрытых читов AI;
9. не использовать hidden RNG для определения попадания в мишень;
10. не ломать существующие сохранения без миграции данных.

---

# 1. Видение игры

## 1.1. Краткий pitch

Современный реалистичный симулятор биатлона с глубокой карьерой, где результат определяется сочетанием:

- мастерства игрока;
- физиологии спортсмена;
- лыжной техники;
- тактики распределения сил;
- стрельбы;
- погоды и снега;
- экипировки;
- формы и усталости;
- поведения соперников.

Игра должна быть легко понятна новичку, но оставлять большой запас мастерства.

**Design slogan:** `Easy to understand — difficult to master.`

## 1.2. Целевая планка

Сильные стороны ориентиров:

- Biathlon 2005 — физиология и стрельба;
- Biathlon 2006 — характеристики, тренировки, карьера;
- Wintersport Pro 2006 — drafting, расход сил и тактика;
- RTL Biathlon 2008 — профиль трассы и дисциплины;
- Biathlon Champion 2007 — тренировки, ghost, дополнительные режимы;
- Biathlon Mania — карьерная мета;
- Biathlon Battle VR — физическое ощущение стрельбы;
- NGL Biathlon 2026 — современная целостная структура гонки и карьеры.

Наша цель: объединить лучшие механические идеи и превзойти существующие игры в четырех областях:

1. качество лыжного геймплея;
2. глубина и честность стрельбы;
3. живая карьера и AI-соперники;
4. современная графика, анимация и телевизионная подача.

## 1.3. Не-цели v0.1

В первую версию НЕ входят:

- Individual;
- Relay;
- Mixed Relay;
- multiplayer;
- лицензированные спортсмены;
- лицензированные трассы;
- официальный IBU branding;
- женский и мужской чемпионаты как отдельные контентные кампании;
- полноценный редактор трасс;
- VR;
- консольные версии.

Архитектура должна позволять добавить это позднее.

---

# 2. Вертикальный срез v0.1

## 2.1. Контент

- 1 качественная трасса;
- 30 AI-спортсменов;
- 1 создаваемый спортсмен игрока;
- 3 дисциплины:
  - Sprint;
  - Pursuit;
  - Mass Start;
- стрельба:
  - prone;
  - standing;
- один карьерный мини-сезон;
- тренировки;
- форма;
- усталость;
- простая экипировка;
- результаты и standings;
- replay/event log;
- баланс-лаборатория.

## 2.2. Реальные правила как профили данных

Правила не должны быть hardcoded.

Пример `CompetitionProfile`:

```yaml
id: sprint_standard_men
format: sprint
distance_m: 10000
laps: 3
shooting_sequence: [prone, standing]
shots_per_bout: 5
penalty_loop_m: 150
start_type: interval
start_interval_s: 30
```

Пример Pursuit:

```yaml
id: pursuit_standard_men
format: pursuit
distance_m: 12500
laps: 5
shooting_sequence: [prone, prone, standing, standing]
shots_per_bout: 5
penalty_loop_m: 150
start_type: gaps_from_previous_race
field_size: 60
```

Пример Mass Start:

```yaml
id: mass_start_standard_men
format: mass_start
distance_m: 15000
laps: 5
shooting_sequence: [prone, prone, standing, standing]
shots_per_bout: 5
penalty_loop_m: 150
start_type: simultaneous
field_size: 30
```

Для v0.1 разрешено использовать уменьшенный тестовый профиль трассы во время разработки, но production race profile должен соответствовать данным соревнования.

---

# 3. Основной gameplay loop

```text
PRE-RACE
  ↓
настройка лыж / просмотр погоды / состояние спортсмена
  ↓
START
  ↓
лыжный круг
  ↓
управление effort + позиция + draft + траектория
  ↓
подготовка к рубежу
  ↓
снижение нагрузки / восстановление HR
  ↓
SHOOTING
  ↓
попадания / промахи
  ↓
при необходимости penalty loop
  ↓
следующий круг
  ↓
FINISH
  ↓
результаты / очки / форма / усталость
  ↓
CAREER
  ↓
тренировка / восстановление / следующий старт
```

Главная дилемма гонки:

> выиграть секунды сейчас или сохранить физиологические ресурсы для будущей части гонки и стрельбы.

---

# 4. Управление

## 4.1. Gamepad — основной target

Левый stick:
- направление спортсмена;
- мелкая коррекция траектории.

RT:
- effort / power request, аналоговый 0..1.

LT:
- controlled recovery / снижение интенсивности либо альтернативная функция контекста после playtest.

A:
- tuck на спуске / contextual ski action.

X:
- смена tactical intent / optional quick action.

Right stick на стрельбе:
- управление винтовкой.

RT на стрельбе:
- trigger.

LT на стрельбе:
- breath control.

## 4.2. Keyboard + mouse

W/S:
- effort up/down или аналоговый virtual effort.

A/D:
- steering.

Shift:
- attack effort preset.

Ctrl:
- recovery effort preset.

Mouse:
- aim.

LMB:
- shot.

RMB:
- breath control.

## 4.3. Accessibility

Предусмотреть:

- steering assist;
- automatic optimal ski technique selection;
- simplified effort presets;
- aim sensitivity;
- optional aim damping;
- visual HR rhythm indicator;
- colorblind-compatible targets/UI;
- difficulty presets изменяют assists и AI decision quality, а не физические бонусы AI.

---

# 5. Модель спортсмена

Все игровые stats хранятся 0..100, но simulation layer преобразует их в физические параметры.

## 5.1. Видимые характеристики

### Физиология

- Endurance
- Anaerobic Capacity
- Recovery
- Strength

### Лыжи

- Ski Technique
- Climbing
- Flat Efficiency
- Downhill
- Cornering

### Стрельба

- Prone Stability
- Standing Stability
- Breath Control
- Shooting Speed

### Психология

- Composure
- Pressure Resistance
- Fighting Spirit

## 5.2. Скрытые/внутренние параметры

- resting_hr;
- max_hr;
- aerobic_power;
- anaerobic_capacity_j;
- recovery_tau;
- muscular_fatigue_resistance;
- motor_noise;
- breathing_amplitude;
- heartbeat_impulse;
- skill_growth_rates;
- potential;
- age_curve;
- injury_risk;
- consistency.

Скрытые параметры должны быть доступны Balance Lab и debug UI, но не обязательно игроку.

## 5.3. Текущее состояние

```text
Form          0..1.10
Fatigue       0..1
Morale        0..1
Confidence    0..1
Health        0..1
Anaerobic     0..1
MuscleFresh   0..1
HeartRate     bpm
```

---

# 6. Физиология v0.1

Все формулы ниже — исходные tuning hypotheses, а не окончательные научные модели. Их задача — создать правдоподобное и управляемое поведение.

## 6.1. Relative effort

```text
relative_effort = requested_power / sustainable_power
```

Рекомендуемый рабочий диапазон:

- 0.50–0.70 recovery;
- 0.70–0.88 sustainable;
- 0.88–1.00 race;
- 1.00–1.12 attack;
- >1.12 short sprint only.

## 6.2. Heart rate target

```text
load = clamp(relative_effort * fatigue_hr_modifier, 0, 1.20)

hr_target = resting_hr
          + (max_hr - resting_hr)
          * pow(clamp(load, 0, 1), 0.72)
```

Если load > 1:

```text
hr_target += (max_hr - resting_hr) * 0.15 * (load - 1)
```

Clamp итогового HR до индивидуального safety ceiling.

## 6.3. Инерция HR

```text
if hr_target > hr:
    tau = hr_rise_tau
else:
    tau = hr_recovery_tau

hr += (hr_target - hr) * (1 - exp(-dt / tau))
```

Стартовые tuning values:

```text
hr_rise_tau      = 18–28 s
hr_recovery_tau  = 35–55 s
```

Recovery stat уменьшает recovery tau.

## 6.4. Cardiac drift

При длительной работе и высокой fatigue:

```text
hr_drift_bpm = fatigue * drift_strength * elapsed_high_load_factor
```

Цель: одинаковая мощность в конце гонки требует немного большего HR.

## 6.5. Anaerobic reserve

Нормализованный W' ресурс:

```text
anaerobic = 0..1
```

Если effort > aerobic_threshold:

```text
drain_rate = pow(effort - aerobic_threshold, 1.6)
           * anaerobic_drain_scale
```

Если effort ниже threshold:

```text
recovery_rate = (aerobic_threshold - effort)
              * recovery_stat_modifier
              * anaerobic_recovery_scale
```

Draft уменьшает power requirement и тем самым косвенно ускоряет восстановление.

## 6.6. Muscular fatigue

```text
fatigue_gain = dt
             * pow(relative_effort, 2.1)
             * terrain_muscle_factor
             * athlete_fatigue_susceptibility
```

Fatigue влияет на:

- power efficiency;
- ski technique;
- arm stability;
- HR drift;
- recovery speed.

---

# 7. Лыжная симуляция

## 7.1. Основной принцип

Игрок не управляет каждым движением палок. Он управляет:

- effort;
- направлением;
- выбором линии;
- атакой/восстановлением;
- tuck;
- позиционной борьбой.

Animation layer визуализирует подходящую лыжную технику.

## 7.2. Course Segment

Трасса представляется как spline + метаданные по сегментам.

Каждая точка/segment имеет:

```text
distance
slope
curvature
width
surface_condition
snow_type
wind_exposure
draft_allowed
risk_level
recommended_technique
```

## 7.3. Скорость

В v0.1 использовать устойчивую псевдофизическую модель, а не полную механику контакта лыж со снегом.

```text
propulsive_power = athlete_available_power
                 * technique_efficiency
                 * fatigue_efficiency
                 * form_modifier

resistance = rolling_resistance
           + aerodynamic_drag
           + slope_resistance
           + turning_loss

acceleration = (propulsive_force - resistance_force) / mass
```

В реализации допускается вычислять силы из power через текущую скорость с защитой от деления на ноль.

## 7.4. Technique efficiency

```text
technique_efficiency = base_technique
                     * terrain_affinity
                     * fatigue_modifier
```

Climbing stat усиливает эффективность на положительном slope.

Downhill и Cornering влияют на безопасную скорость прохождения поворотов и потери скорости.

## 7.5. Tuck

На отрицательном slope:

```text
TUCK:
  aerodynamic_drag ↓
  active_power ↓
  HR target ↓
  anaerobic recovery ↑
```

Но steering responsiveness немного уменьшается.

## 7.6. Drafting

Drafting рассчитывается, если спортсмен находится позади другого спортсмена в допустимом конусе.

```text
distance_factor = smoothstep(maxDraftDistance, minDraftDistance, distance)
alignment_factor = dot(forwardLeader, directionLeaderToFollower)
wind_factor = function(windDirection, relativeDirection)
speed_factor = clamp(speed / referenceSpeed, 0, 1.2)

draft_strength = distance_factor
               * alignment_factor
               * wind_factor
               * speed_factor
```

Draft уменьшает aerodynamic drag до configurable maximum.

Стартовая цель:

- полезный, но не доминирующий эффект;
- заметная экономия на равнине/спуске;
- меньший эффект на медленном крутом подъеме.

## 7.7. Collisions / positional space

Спортсмены не могут проходить друг сквозь друга.

Не использовать тяжелую rigidbody-физику для каждого спортсмена как основу race logic.

Simulation layer хранит:

- lane offset;
- longitudinal position;
- personal space radius.

Presentation layer решает визуальные контакты/avoidance.

AI обязан учитывать доступное пространство перед обгоном.

---

# 8. Стрельба

## 8.1. Главный принцип

**Нет hidden RNG, который после нажатия trigger решает hit/miss.**

Результат определяется ориентацией оружия и геометрией мишени в момент выстрела.

Характеристики спортсмена усложняют или облегчают удержание оружия, но не отменяют мастерство игрока.

## 8.2. Состав sway

```text
aim_offset = player_input
           + breathing_component
           + heartbeat_component
           + muscular_component
           + stance_component
           + wind_visual_response(optional)
```

### Breathing component

Низкочастотная вертикальная волна:

```text
breath_y = amplitude * sin(phase)
```

Amplitude зависит от:

- HR;
- fatigue;
- Breath Control;
- stance.

### Heartbeat component

Короткий импульс на каждом heartbeat.

```text
heartbeat_interval = 60 / HR
```

Импульс должен иметь быстрое начало и плавное затухание.

Идея gameplay: опытный игрок учится стрелять между импульсами.

### Muscular component

Низкоамплитудный band-limited procedural noise.

Амплитуда зависит от:

- arm fatigue;
- standing/prone skill;
- total fatigue;
- pressure.

Noise должен быть deterministic при фиксированном simulation seed.

## 8.3. Prone vs Standing

Prone:

- sway ниже;
- recovery быстрее;
- меньше sensitivity к muscular fatigue.

Standing:

- больше sway;
- сильнее heartbeat influence;
- выше pressure sensitivity.

## 8.4. Breath control

При удержании breath:

```text
breathing_amplitude *= breath_hold_reduction
oxygen_debt += dt * debt_rate
```

После отпускания:

```text
rebound_sway = function(oxygen_debt)
```

Длительное удержание не должно быть оптимальной стратегией.

## 8.5. Wind

В v0.1 wind влияет на ballistic horizontal offset.

```text
bullet_offset_x = wind_cross_component
                * wind_ballistic_scale
                * flight_time
```

Игрок получает визуальный wind indicator, но не идеальное цифровое решение.

## 8.6. Shooting speed

Игрок может стрелять сразу после каждого bolt cycle.

Shooting Speed stat влияет на:

- скорость стабилизации после предыдущего shot;
- минимальную длительность оружейной анимации;
- не на auto-hit.

## 8.7. Target geometry

Использовать реальные относительные размеры target profiles в конфиге.

Hit определяется пересечением ray/ballistic path с hit collider target.

Все пять targets имеют независимое состояние.

---

# 9. Shooting pressure

Pressure — не random miss.

Он влияет на:

- sway amplitude;
- breath instability;
- heartbeat impulse;
- AI decision speed;
- UI/audio intensity.

Примеры pressure events:

- последний standing bout;
- борьба за 1 место;
- Mass Start с несколькими спортсменами рядом;
- первый podium в карьере;
- низкая Confidence.

```text
pressure = race_importance
         * position_importance
         * proximity_of_rivals
         * athlete_pressure_sensitivity
```

---

# 10. AI

## 10.1. AI не получает физических бонусов

Запрещено:

```text
hard_ai_speed_multiplier = 1.10
```

Difficulty влияет на:

- качество оценки ситуации;
- planning horizon;
- reaction delay;
- ошибки выбора effort;
- shooting aim controller quality.

## 10.2. AI state

AI наблюдает:

```text
distance_remaining
course_next_segments
current_speed
heart_rate
fatigue
anaerobic
nearby_athletes
draft_opportunity
gap_to_targets
distance_to_range
shooting_state
race_format
current_position
```

## 10.3. Tactical intents

```text
RECOVER
HOLD_PACE
FOLLOW
DRAFT
OVERTAKE
ATTACK
FINAL_SPRINT
PRE_RANGE_RECOVERY
```

## 10.4. Utility decision

Каждое действие получает score.

Пример ATTACK:

```text
score_attack = aggression
             * opportunity
             * anaerobic_available
             * race_importance
             - distance_to_range_penalty
             - fatigue_penalty
```

Пример PRE_RANGE_RECOVERY:

```text
score_recover = shooting_importance
              * high_hr_factor
              * shooting_sensitivity
              * proximity_to_range
```

## 10.5. AI personalities

Каждый спортсмен имеет постоянные traits:

- Aggression;
- Patience;
- Draft Preference;
- Shooting Risk;
- Shooting Tempo;
- Final Lap Aggression;
- Pressure Resistance;
- Overtake Confidence.

Так соперники становятся узнаваемыми.

## 10.6. AI shooting

AI использует ту же sway/target simulation.

AI Aim Controller создает input в aim system.

Difficulty/skill влияет на:

- tracking error;
- reaction time;
- desired stability threshold before firing;
- cadence choice.

Hit/miss по-прежнему определяется геометрией в момент выстрела.

---

# 11. Sprint

## 11.1. Race flow

```text
interval start
lap 1
prone
penalty loop(s)
lap 2
standing
penalty loop(s)
lap 3
finish
```

## 11.2. Tactical identity

Sprint должен награждать:

- высокий общий темп;
- быстрые рубежи;
- способность контролировать HR без чрезмерной потери времени.

## 11.3. Target balance

Начальные targets для elite-like field:

- время гонки должно иметь реалистичный порядок величины относительно дистанции и трассы;
- один penalty loop должен ощутимо менять позицию;
- два промаха обычно существенно вредят podium chances, но не всегда исключают хороший результат;
- чрезмерное снижение effort перед рубежом не должно быть бесплатно.

---

# 12. Pursuit

## 12.1. Qualification/data dependency

Pursuit получает результаты предыдущего Sprint.

```text
start_gap = sprint_finish_time - sprint_winner_time
```

## 12.2. Flow

```text
start by gap
lap 1
prone
lap 2
prone
lap 3
standing
lap 4
standing
lap 5
finish
```

## 12.3. Основная эмоция

Игрок постоянно видит:

- кого догоняет;
- кто догоняет его;
- реальную позицию на трассе.

## 12.4. AI tactics

AI должен менять стратегию на основе gaps.

Например:

- спортсмен +40 s не обязан вести себя как лидер;
- группа может временно сотрудничать через drafting;
- финальный standing dramatically changes tactical state.

---

# 13. Mass Start

## 13.1. Flow

```text
30 athletes simultaneous
lap 1
prone
lap 2
prone
lap 3
standing
lap 4
standing
lap 5
finish
```

## 13.2. Ключевые системы

Mass Start — главный stress test для:

- AI;
- collision avoidance;
- drafting;
- overtaking;
- shooting range lane assignment;
- performance;
- presentation.

## 13.3. Старт

Первые 30–60 секунд должны иметь специальную traffic logic:

- AI avoids impossible overtakes;
- athlete spacing;
- no clipping;
- no sudden artificial braking.

---

# 14. Трасса

## 14.1. Track data

Трасса состоит из:

- start/finish stadium;
- main course spline;
- loop variants;
- penalty loop;
- shooting range entry/exit;
- timing checkpoints;
- overtaking width metadata;
- terrain semantic zones.

## 14.2. Semantic zones

Примеры:

```text
CLIMB_LONG
CLIMB_STEEP
FLAT
ROLLING
DESCENT_FAST
DESCENT_TECHNICAL
STADIUM
RANGE_APPROACH
PENALTY_LOOP
```

AI принимает решения на основе semantic zones, а не pixel geometry.

## 14.3. Codex-friendly track authoring

Для первой трассы Codex должен иметь возможность:

1. создать spline программно;
2. сгенерировать checkpoints;
3. создать terrain profile из JSON/ScriptableObject;
4. автоматически проверить:
   - длину;
   - slope limits;
   - пересечения;
   - ширину;
   - связь loops;
   - доступность range.

Не требовать ручного размещения сотен waypoint объектов.

---

# 15. Погода и снег

v0.1:

```text
air_temperature
snow_temperature
snow_condition
wind_speed
wind_direction
snowfall_intensity
```

Snow condition влияет на ski friction.

Wind влияет на:

- shooting;
- aerodynamic drag/drafting в дальнейшем.

Для v0.1 погода фиксируется на гонку, но architecture должна поддерживать динамические изменения.

---

# 16. Экипировка

Не делать RPG loot с огромными бонусами.

## 16.1. Skis

Параметры:

- glide;
- grip;
- stability;
- condition affinity.

## 16.2. Rifle

Параметры:

- stability;
- handling;
- ergonomics.

Разница между хорошим и плохим equipment должна быть заметна, но не превосходить skill спортсмена.

Career equipment progression — lateral + modest vertical progression.

---

# 17. Career v0.1

## 17.1. Цель

Игрок должен чувствовать развитие спортсмена и историю соперников.

## 17.2. Карьерный цикл

```text
calendar
↓
training / recovery
↓
race weekend
↓
results
↓
ranking / money / confidence
↓
fatigue / form update
↓
next week
```

## 17.3. Training

Типы v0.1:

- Endurance;
- Intensity;
- Ski Technique;
- Shooting Prone;
- Shooting Standing;
- Recovery.

Каждая тренировка имеет:

```text
adaptation_gain
fatigue_cost
injury_risk
time_cost
```

Нельзя бесконечно прокачиваться без восстановления.

## 17.4. Development

```text
training_gain = training_quality
              * potential_modifier
              * age_modifier
              * recovery_modifier
              * diminishing_returns
```

Высокие stats растут медленнее.

## 17.5. Form

Form — краткосрочная готовность, а не постоянный stat.

Форма растет при хорошем тренировочном цикле и падает при:

- чрезмерной fatigue;
- болезни/травме;
- плохом планировании.

## 17.6. AI careers

Все AI используют ту же модель:

- age;
- growth;
- training abstraction;
- form;
- fatigue;
- retirement.

AI не остаются статичными.

## 17.7. Rivalries

Rivalry score строится из:

- количества близких финишей;
- борьбы за standings;
- взаимных обгонов;
- повторяющихся podium battles.

В v0.1 достаточно собрать данные; presentation может появиться позже.

---

# 18. Экономика v0.1

Минимальная.

Доход:

- prize money;
- season objectives;
- простые sponsor rewards позднее.

Расход:

- training camps;
- equipment;
- recovery services.

Запрещено делать обязательный grind ради увеличения длительности игры.

---

# 19. Balance targets

Это не финальные цифры, а acceptance windows для автоматического поиска проблем.

## 19.1. Общие

В поле спортсменов близкого уровня:

- результат должен определяться несколькими факторами, а не одним stat;
- стратегия effort должна быть измеримо значимой;
- идеальная стрельба не должна автоматически гарантировать победу;
- очень сильный лыжник может компенсировать 1 промах;
- слабый лыжник может выиграть за счет идеальной стрельбы при подходящем раскладе.

## 19.2. Shooting

Первоначальные simulation targets для top-level AI field:

```text
Prone hit rate:     82–94%
Standing hit rate:  72–89%
```

Точные значения будут выведены из желаемого уровня турнира и playtests.

## 19.3. Pacing

Неправильный all-out effort с первого круга должен приводить к:

- повышенной fatigue;
- худшему HR на рубеже;
- снижению финального темпа;

но не превращаться в необратимое поражение после одной небольшой ошибки.

## 19.4. Drafting

Draft должен:

- давать стратегическую пользу;
- не превращать лидирование в заведомо плохую стратегию;
- не создавать бесконечные «паровозы» без атак.

## 19.5. Attribute impact

Разница 10 points в одном stat не должна автоматически перекрывать несколько других stats.

Balance Lab обязан измерять marginal impact каждого stat.

---

# 20. Balance Lab

## 20.1. Назначение

Codex должен иметь command-line способ прогнать тысячи гонок без рендера.

Пример:

```bash
Biathlon.BalanceLab simulate \
  --profile sprint_standard_men \
  --track test_track_01 \
  --athletes elite_field.json \
  --iterations 10000 \
  --seed 12345
```

## 20.2. Output

CSV/JSON:

```text
finish_time
ski_time
range_time
penalty_time
hits_prone
hits_standing
avg_hr
max_hr
anaerobic_min
fatigue_finish
draft_time
overtakes
position
```

## 20.3. Automated reports

Codex должен уметь автоматически вычислять:

- mean;
- median;
- standard deviation;
- percentile ranges;
- correlations stats → result;
- shot hit rates;
- penalty distribution;
- average position changes;
- pacing strategy comparison.

## 20.4. Regression snapshots

Для фиксированных seeds хранить benchmark snapshots.

Если изменение кода изменяет результаты сверх tolerance, тест должен показать diff.

---

# 21. Determinism

Simulation Core должна быть deterministic при одинаковых:

- input stream;
- seed;
- athlete data;
- course data;
- weather data;
- tick rate.

Не использовать Unity `Random` внутри Core.

Создать собственный `IRandomSource` с seed.

Для player shooting итог geometry hit deterministic относительно input и состояния.

---

# 22. Tick model

Simulation Core работает с fixed tick.

Предложение v0.1:

```text
simulation_hz = 20 or 30 Hz
```

Presentation работает независимо на frame rate и интерполирует состояние.

Причины:

- repeatability;
- replay;
- AI consistency;
- Balance Lab speed;
- multiplayer-ready architecture в будущем.

---

# 23. Event log / Replay

Каждое важное событие:

```text
RaceStarted
CheckpointPassed
EffortChanged
DraftEntered
DraftExited
OvertakeStarted
OvertakeCompleted
RangeEntered
ShotFired
TargetHit
TargetMissed
PenaltyLoopEntered
PenaltyLoopCompleted
RangeExited
RaceFinished
```

Event содержит:

```text
tick
simulation_time
athlete_id
event_type
payload
```

Replay v0.1 может сохранять:

- initial state;
- seed;
- player inputs;
- race events.

Не сохранять полный Transform каждого объекта каждый frame.

---

# 24. Архитектура кода

## 24.1. Assembly boundaries

```text
Biathlon.Domain
  └─ immutable/shared domain definitions

Biathlon.Simulation
  ├─ physiology
  ├─ skiing
  ├─ shooting
  ├─ race rules
  └─ timing

Biathlon.AI
  ├─ tactical decision
  ├─ pacing
  └─ shooting controller

Biathlon.Career
  ├─ calendar
  ├─ training
  ├─ progression
  └─ standings

Biathlon.Replay
  └─ event/input log

Biathlon.Unity.Presentation
  ├─ GameObjects
  ├─ animation
  ├─ camera
  ├─ audio
  ├─ VFX
  └─ UI

Biathlon.Tools.BalanceLab
  └─ headless simulation / reports

Biathlon.Tests
```

## 24.2. Dependency rule

```text
Unity.Presentation
        ↓
Simulation / Domain / AI / Career

Core assemblies MUST NOT reference UnityEngine.
```

Идеально: core компилируется как обычный .NET project и как Unity assembly.

## 24.3. MonoBehaviour rule

MonoBehaviour — adapter/presentation only.

Запрещено помещать основную race logic в:

- `Update()`;
- `FixedUpdate()`;
- Inspector state;
- scene object names.

---

# 25. Data architecture

Game data хранится в versioned definitions.

Рекомендуется:

- JSON/YAML для Core/Balance Lab;
- ScriptableObject wrappers для удобства Unity Presentation при необходимости.

Все definitions имеют:

```text
schema_version
id
```

Не хранить критические tuning constants как magic numbers в C#.

Пример:

```yaml
schema_version: 1
id: physiology_default
hr_rise_tau_s: 22
hr_recovery_tau_s: 45
anaerobic_drain_scale: 0.08
anaerobic_recovery_scale: 0.025
fatigue_power_exponent: 2.1
```

---

# 26. Save game

Save содержит ID и state, а не сериализованные Unity object references.

```text
save_version
career_id
current_date
player_athlete
ai_athletes
standings
calendar
inventory
career_rng_state
```

Обязателен migration layer между save versions.

---

# 27. Automated tests

## 27.1. Unit tests

Примеры:

### HR rises under load

```text
Given athlete at 60 bpm
When 180 s at high effort
Then HR > baseline
```

### HR recovers

```text
Given HR 180
When effort reduced for 60 s
Then HR decreases
```

### Draft saves power

```text
Given same speed and terrain
When athlete follows leader in valid draft zone
Then required power < no-draft case
```

### Pursuit gaps

```text
Sprint A: 1500.0
Sprint B: 1512.7
Then pursuit B starts +12.7 s
```

### Penalty loop

```text
Given 2 misses
Then required penalty loops = 2
```

### Shooting geometry

```text
Given muzzle ray intersects open target
Then hit = true
```

```text
Given muzzle ray misses target
Then hit = false
```

## 27.2. Simulation tests

- 30 AI Sprint completes without deadlock;
- 30 AI Mass Start completes without overlap corruption;
- no athlete leaves course topology;
- all shooting bouts have correct number of shots;
- correct number of penalty loops;
- deterministic replay reproduces result.

## 27.3. Statistical tests

Statistical tests use wide tolerance and fixed large samples.

Например:

```text
Elite AI prone hit rate 10k bouts ∈ [0.82, 0.94]
```

Не использовать слишком узкие ranges, создающие flaky tests.

---

# 28. Telemetry / Debug

Debug overlay должен показывать для выбранного спортсмена:

```text
speed
effort
power
HR
anaerobic
fatigue
draft strength
AI intent
position
gap
sway components
pressure
```

Codex должен иметь возможность получить те же данные в machine-readable log.

---

# 29. Unity Presentation

## 29.1. Первоначальный visual target

Сначала:

- greybox terrain;
- capsule athletes;
- basic range;
- simple UI.

После доказательства gameplay:

- snow material;
- vegetation;
- stadium;
- weather;
- crowd;
- detailed athletes;
- rifle;
- ski tracks;
- breath vapor;
- snow interaction;
- high quality lighting.

## 29.2. Animation target

Не использовать одну универсальную лыжную animation.

Минимальный будущий набор:

- flat skating;
- climb low/medium/high effort;
- acceleration;
- sprint;
- tuck;
- technical descent;
- left/right corner;
- prone enter/exit;
- standing enter/exit;
- shooting prone;
- shooting standing;
- fatigue finish.

Animation state выбирается из simulation state.

## 29.3. Camera

Режимы:

- gameplay chase;
- shooting camera;
- broadcast camera;
- replay camera.

---

# 30. TV presentation

Минимум:

- live standings;
- split times;
- current rank;
- gaps;
- shooting board;
- penalty indicator;
- finish overlay.

Позже:

- automatic battle detection;
- replay highlights;
- leader camera;
- last shooting drama;
- commentator hooks.

---

# 31. Audio

Audio — часть gameplay feedback.

Обязательно:

- ski-on-snow loop varying with speed/snow;
- poles;
- breathing;
- heartbeat under high HR / shooting focus;
- rifle shot;
- target mechanical hit;
- crowd intensity;
- wind.

Heartbeat audio не должен раздражать; использовать contextual mix.

---

# 32. Performance targets

Для v0.1 Windows:

- target 60 FPS typical gaming PC;
- simulation 30 athletes << render budget;
- simulation headless: тысячи гонок быстрее realtime;
- no per-frame managed allocations in critical simulation loop;
- AI decisions не обязательно каждый tick: tactical decisions можно обновлять 2–5 Hz.

---

# 33. Codex development contract

## 33.1. На каждую задачу Codex получает

1. цель;
2. затрагиваемые systems;
3. constraints;
4. acceptance criteria;
5. tests;
6. commands to run.

## 33.2. Codex обязан

Перед завершением задачи:

1. собрать проект;
2. запустить релевантные tests;
3. проверить Unity Console, если задача касается Unity;
4. не оставлять compile errors;
5. не отключать tests ради прохождения;
6. обновить документацию при изменении public contract;
7. записать tuning changes;
8. показать краткий summary изменений.

## 33.3. Запрещено Codex

- помещать Simulation logic в presentation layer;
- добавлять hidden AI stat multipliers;
- решать hit/miss случайным броском;
- hardcode track objects по scene names;
- использовать magic numbers вместо config;
- массово менять architecture без ADR/обоснования;
- удалять failing tests вместо исправления причины;
- создавать дорогие runtime allocations без необходимости;
- вручную дублировать одно и то же tuning data в нескольких местах.

---

# 34. Git workflow

Рекомендуемый подход:

```text
main
└─ feature/<task-id>-<short-name>
```

Каждая задача:

```text
small coherent change
+ tests
+ docs if needed
```

Commit message:

```text
feat(sim): implement heart-rate dynamics
fix(ai): prevent invalid mass-start overtakes
test(shooting): add deterministic hit geometry cases
```

---

# 35. Architecture Decision Records

Для крупных решений Codex создает:

```text
docs/adr/ADR-0001-deterministic-simulation.md
```

Формат:

- Context;
- Decision;
- Alternatives;
- Consequences.

---

# 36. Definition of Done для system task

Система считается готовой только если:

- реализована;
- data-driven;
- deterministic где требуется;
- имеет unit tests;
- имеет integration test;
- имеет debug visibility;
- не имеет compile warnings/errors уровня project policy;
- documented public behavior;
- Balance Lab может измерить ее влияние, если она влияет на race result.

---

# 37. Roadmap разработки

## Phase 0 — Foundation

- Git repo;
- Unity 6 project;
- assembly definitions;
- Core .NET structure;
- tests;
- AGENTS.md;
- Unity MCP connection;
- CI/local test commands.

**Exit:** Codex может изменить Core, запустить tests и прочитать Unity Console.

## Phase 1 — Deterministic simulation skeleton

- fixed tick;
- athlete state;
- race clock;
- input stream;
- event log;
- deterministic RNG interface.

**Exit:** deterministic empty race replay test passes.

## Phase 2 — Physiology

- HR;
- anaerobic;
- muscular fatigue;
- recovery.

**Exit:** scripted workload produces expected curves.

## Phase 3 — Track + skiing

- spline/segment representation;
- effort;
- speed;
- slope;
- cornering;
- tuck;
- drafting.

**Exit:** athlete completes test course and pacing strategies produce measurable differences.

## Phase 4 — Shooting

- target geometry;
- prone/standing;
- breathing;
- heartbeat;
- muscular sway;
- breath control;
- wind;
- penalty logic.

**Exit:** player/AI can complete deterministic shooting bout.

## Phase 5 — Sprint

- full rule state machine;
- interval start;
- timing;
- standings.

**Exit:** 30 AI Sprint produces valid result table.

## Phase 6 — AI

- pacing utility;
- drafting;
- overtake;
- pre-range recovery;
- shooting controller.

**Exit:** distinct AI personalities produce measurably different strategies.

## Phase 7 — Pursuit

**Exit:** gaps from Sprint reproduced exactly and winner determined by finish order.

## Phase 8 — Mass Start

**Exit:** 30 athletes finish without topology/collision failures.

## Phase 9 — Balance Lab

- batch simulations;
- CSV/JSON;
- regression reports;
- stat sensitivity.

**Exit:** Codex can run 10k races and generate balance report without Unity rendering.

## Phase 10 — Career

- calendar;
- training;
- form;
- fatigue;
- athlete growth;
- AI development;
- standings;
- save/load.

## Phase 11 — Unity vertical slice

- playable course;
- player controls;
- UI;
- cameras;
- basic animations;
- basic audio.

## Phase 12 — Visual quality

- final snow;
- lighting;
- weather;
- vegetation;
- athletes;
- animation polish;
- crowd;
- TV presentation.

---

# 38. Первые acceptance milestones

## M0 — Agent-ready repository

Codex способен сам:

- прочитать AGENTS.md;
- изменить C#;
- запустить unit tests;
- открыть/проверить Unity;
- прочитать Console;
- исправить ошибку;
- повторно запустить tests.

## M1 — Athlete on mathematical track

Без графики:

```text
athlete + effort script + slope profile
→ speed
→ HR
→ fatigue
→ finish time
```

## M2 — Complete headless Sprint

```text
30 athletes
+ AI pacing
+ shooting
+ penalty loops
→ valid classification
```

Это первый момент, когда мы получаем «игру» на уровне симуляции.

## M3 — Playable greybox Sprint

Игрок может физически пройти гонку в Unity.

## M4 — Pursuit + Mass Start

Три заявленные дисциплины полностью работают.

## M5 — Career loop

Игрок проходит несколько race weekends, тренируется и развивается.

---

# 39. Первый набор параметров для Balance Lab

Codex обязан сделать все параметры configurable:

```text
HR rise tau
HR recovery tau
HR drift
anaerobic capacity
anaerobic drain exponent
anaerobic recovery
fatigue exponent
fatigue recovery
ski drag
snow friction
slope cost
draft max benefit
draft distance
cornering loss
breathing sway
heartbeat sway
muscular sway
breath hold reduction
breath rebound
wind ballistic scale
AI decision interval
AI aggression weights
AI range-recovery weights
```

Ни один из них не считается финальным в v0.1.

---

# 40. Критерий качества игры

Мы не считаем систему хорошей только потому, что она реалистична математически.

Каждая механика должна пройти три фильтра:

```text
REALISM
   ∩
PLAYER AGENCY
   ∩
READABILITY
```

Если система реалистична, но игрок не способен понять, почему проиграл — ее нужно улучшить.

Если система понятна, но не создает решений — ее нужно углубить.

Если система глубока, но AI/Codex невозможно тестировать — ее нужно перепроектировать.

---

# 41. Product principles — неизменяемое ядро

1. **Нет скрытого RNG попадания.**
2. **AI играет по тем же физическим правилам.**
3. **Пульс и fatigue имеют реальные игровые последствия.**
4. **Плохое pacing должно иметь цену.**
5. **Характеристики спортсмена влияют на сложность, а не отменяют skill игрока.**
6. **Каждый сильный action имеет trade-off.**
7. **Карьера живет вокруг игрока: AI растут, стареют и меняют форму.**
8. **Одна качественная трасса лучше десяти пустых.**
9. **Core simulation не зависит от Unity graphics.**
10. **Codex должен иметь тестируемый способ доказать, что его изменение работает.**

---

# 42. Следующий технический шаг

Первой задачей Codex после создания repository должна быть не графика и не player controller.

Первая существенная система:

> `Deterministic Simulation Kernel + AthleteState + Fixed Tick + Event Log`.

После этого:

> `HeartRateSystem + AnaerobicSystem + FatigueSystem`.

И только после прохождения tests начинается ski movement.

---

# 43. Источники правил

При реализации competition profiles проверять актуальные IBU Event and Competition Rules. На момент подготовки v0.1 официальный сайт IBU публикует версию правил ноября 2025 года. Sprint: 10 км мужчины / 7.5 км женщины, три круга, prone + standing, 150 м penalty за промах; Pursuit: 12.5 / 10 км, пять кругов, PPSS, старт по отставанию предыдущей гонки, 150 м penalty; стандартный Mass Start: 15 / 12.5 км, пять кругов, PPSS, 150 м penalty.

---

**Конец GDD/TDD v0.1**
