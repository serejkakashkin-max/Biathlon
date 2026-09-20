# WP-166 — profiling benchmark

Дата: 2026-09-20. Статус: **DONE — измерения и отчёт завершены; performance qualification: ACTION_REQUIRED**. G2 не одобрен.

Измерительная задача WP-166 отделена от допуска по производительности и визуального G2. В скрытом Windows Development player выполнены 10 сценариев по 300 измеренных кадров после 128 кадров прогрева, 1920×1080, Direct3D 11. Три качества × approach/athlete/range и отдельный high/snowfall/athlete. Десять detached pose fixtures используют наблюдения Mass Start на tick400; это не нагрузка полноценной гонки на 30 спортсменов.

Вывод камеры в RenderTexture выполняется через URP SingleCameraRequest в PostLateUpdate после обновления skinning, перед FinishFrameRendering. Смена LOD до обновления skinning вызывала реальные ошибки размера/stride меша в раннем эксперименте; корректный порядок устраняет их. Проверка сохраняет PNG фактического результата и отвергает отсутствие draw calls/треугольников. Player-loop lease снимается при Dispose; состояние камеры и чужие loop callbacks сохраняются, что проверяется PlayMode-тестом.

Unity 6.6 не публикует прежние имена `Draw Calls Count`/`Batches Count` в доступном наборе этого runtime. Запуск recorder по отсутствующему имени давал ложные нули. Теперь наличие проверяется до создания recorder; total draws — сумма восьми доступных непересекающихся категорий Standard/SRP Batcher/BRG/Null Geometry, включая instanced/indirect варианты. Отсутствующий общий batches записан как -1/UNAVAILABLE, а не 0. Его потолок 2000 сохранён, но соблюдение не подтверждено. Инвентаризация фактических counters сохранена рядом с evidence; [официальный справочник Unity](https://docs.unity.com/en-us/engine/6000.6/manual/analysis/profiler/counters-reference) описывает отдельные категории.

FrameTimingManager выдаёт CPU frame/main, но GPU/render-thread timing в скрытом offscreen-запуске недоступен. Дополнительная проверка с `-force-gfx-direct` и GPU camera recorder также не дала GPU времени; её результаты не смешаны с основным запуском. Владелец явно выбрал только скрытые проверки. UNAVAILABLE не засчитывается как PASS. Низкое CPU время не является доказательством FPS или свободного GPU budget. Тайминги [FrameTimingManager](https://docs.unity3d.com/6000.0/Documentation/Manual/frame-timing-manager.html) приходят с задержкой; повторные timestamps не выдаются за новые измерения.

Потолки до замеров: frame/main/render/GPU p95 ≤16.667 ms; draw calls и batches ≤2000, SetPass ≤200, triangles ≤3 млн. Это инженерные бюджеты benchmark, а не подтверждённые minimum/recommended specs. RTX5080/Ryzen9950X3D — высокопроизводительная машина; release build, экранный Present/HUD, минимальная конфигурация и полная гонка этим прогоном не квалифицируются. Профилировщик включён, deep profiling выключен; его overhead входит в результат.

## Исправление прежних утверждений

WP164 Windows smoke подтверждал загрузку трёх сцен, состояние симуляции и применение качества, но не реальную отрисовку. Его прежний PASS не доказывает render/shader coverage. WP166 заменяет это девятью реальными offscreen render checks с PNG. Ранние zero-draw профили не используются как performance evidence. Информационные URP записи о stripped Gaussian/Bokeh/Panini относятся к отключённым эффектам и не являются compile/runtime errors.

## Влияние и дальнейшая работа

Authoritative simulation, геометрия попаданий, физика, AI и баланс не изменены; отдельная deterministic regression не требуется для opt-in диагностического пути. OWNER-FEEL-001 остаётся OPEN: **shooting difficulty felt easy to owner during earlier technical playtest**. Обязательный возврат в WP229/WP230/WP232, без hidden RNG или скрытого rebalance во время visual remediation.

После фиксации WP166 действует [owner directive](../owner/CODEX_POST_WP166_G2_VISUAL_REMEDIATION_DIRECTIVE_RU.md): post-WP166 / pre-G2 visual remediation, сначала аудит staged assets, затем hero athlete → animation → forest/snow/terrain → stadium/event → crowd → lighting → новый benchmark review. G2 — NOT APPROVED / FAIL PENDING VISUAL REMEDIATION. M13/WP167 не начинаются. Master Plan не изменён.

## Итоговые измерения

| Сценарий | CPU main p95, ms | Draw calls max | SetPass max | Треугольники max |
|---|---:|---:|---:|---:|
| low-approach | 0.917 | 562 | 17 | 852996 |
| low-athlete | 0.928 | 551 | 26 | 1505274 |
| low-range | 0.950 | 266 | 22 | 943162 |
| balanced-approach | 1.068 | 947 | 30 | 1487727 |
| balanced-athlete | 1.060 | 685 | 39 | 3549647 |
| balanced-range | 1.140 | 462 | 36 | 1610515 |
| high-approach | 1.088 | 1103 | 30 | 1743309 |
| high-athlete | 1.084 | 724 | 41 | 5608699 |
| high-range | 1.060 | 497 | 37 | 1727603 |
| high-snowfall-athlete | 1.248 | 756 | 42 | 5641569 |

Во всех 10 сценариях GPU time, GPU camera scope, CPU render-thread time и общий batches: **UNAVAILABLE**. Три athlete-сценария balanced/high/high-snowfall превышают установленный предел 3 млн треугольников. Бюджеты не повышались ради PASS. Draw calls и SetPass укладываются в свои потолки. Raw samples содержат память/GC и задержанные timestamps; суммарные allocation/GC всего player не равнозначны allocation чистого simulation kernel.

## Проверки и воспроизведение

- PlayMode **96/96 PASS**, 89.50 s; после замены устаревшего singleton lookup финальный focused **3/3 PASS**.
- Python statistics **3/3 PASS**: unavailable values, nearest-rank quantiles, invalid samples.
- Windows Development build **235942132 bytes**, 0 errors / 0 warnings. Smoke: Sprint6 и MassStart30, tick400, benchmark10; **9/9 реальных render checks PASS**, exit0, девять PNG.
- Основной профиль **10/10 сценариев, 3000 samples**, exit0. Console **0 errors / 0 warnings**, compilation_failed=false.
- Путь evidence: `docs/quality/m12/wp166/`: full/focused JSON/XML, console, profile raw JSON/PNG/log.gz/build manifest, player smoke и отдельный direct-diagnostic. Rejected-initial явно невалиден для performance.
- Воспроизведение: `./scripts/Invoke-VisualProfile.ps1`; затем `python scripts/Analyze-VisualProfile.py <output-directory>`. Окно остаётся скрытым.

Изменённые системы: opt-in `VisualBenchmarkProfiler`, `BenchmarkFrameCounters`, `BenchmarkRenderTarget`, versioned profile definition; Windows build включает frame timing stats временно и восстанавливает настройки; `PlayerBuildSmoke` действительно рисует сцены; PowerShell runner, Python analyzer и lifecycle/statistics tests. Source build: 03c68af + проверенные изменения WP166; manifest SHA256 `57ff64567c6faf81cb46f25c02ae8b338e8097f56524ce0915ff02cfe3081adf`.
