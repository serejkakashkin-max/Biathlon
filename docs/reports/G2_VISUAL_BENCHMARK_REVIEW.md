# G2 — исторический benchmark review008A

> Актуальный пакет после дальнейших доработок: [review012](G2_REVIEW_012.md). Этот документ и008Acaptures сохранены как исходное сравнение.

23 сентября 2026. **Рекомендация: G2 FAIL / more remediation needed.** Пакет проверки G2-VR-008A готов; визуальная цель remediation не достигнута. G2 остаётся NOT APPROVED. Остановка перед решением владельца, согласно шагу9 [директивы](../owner/CODEX_POST_WP166_G2_VISUAL_REMEDIATION_DIRECTIVE_RU.md). M13/WP167 не начаты; Master Plan не изменён.

## Материалы для владельца

- [Галерея: 45 кадров, before/after, движение](../quality/g2/owner-review/index.html).
- [Манифест и хеши](../quality/g2/owner-review/manifest.json), [48 кадров движения](../quality/g2/owner-review/motion-manifest.json).
- [Свет: clear/dawn/overcast/late day и close crowd](../quality/g2/lighting-a/).
- [Открытые проблемы](G2_OPEN_ITEMS.md), [state](../PROJECT_STATE.md), [план remediation](G2_VISUAL_REMEDIATION_PLAN.md).
- Проверенная локальная Windows Development сборка: `C:\game\Biathlon\Biathlon\Builds\G2OwnerReview-20260922\Windows\Biathlon.exe`. Рядом сохранён `build-files.json`:393 файла,296001096 байт, SHA-256 каждого файла проверен при копировании. Unity BuildReport считает296630856 байт; это другая метрика. Сборка не загружена в публичный Git.

Для самостоятельного просмотра сборка начинает с PlayableSprint. Кнопка **M12 visual benchmark** справа сверху открывает benchmark. В нём доступны approach/sector/athlete/range/targets/finish/rail/crowd_zoom, Animate/Freeze, low/balanced/high, четыре lighting presets и snow. Кнопки Sprint/Mass Start возвращают игровые сцены. Агент использовал только скрытые проверки и не запускал видимое окно.

## Что завершено и что это означает

WP-166 технически завершён; [profiling report](WP-166-PROFILING-REPORT.md) сохранён. Недоступные GPU counters остаются ограничением, а не подтверждением производительности. Checkpoints001–007 реализованы, протестированы и опубликованы отдельно;008A завершает evidence/review. Их технический DONE не означает выполнения художественных критериев002/003/004–007 или G2 PASS.

| Область | Реализовано | Оценка текущего изображения |
|---|---|---|
| Hero | CC0 human base, непрерывный skinned mesh, M11 typed rig,3LOD; материалы ткани/кожи, de-branded CC BY goggles, собственные перчатки и bib01–60 | Лучше прежних отдельных примитивов, но лицо/костюм ещё воспринимаются как mannequin; очки непропорционально крупные, материалы недостаточно убедительны |
| Animation/equipment | Закрытые grips палок, собственная винтовка, typed rifle contacts и переходы prone/standing, сохранение sight | Угловатые локти/колени, механический шаг и перенос веса; pole contact корректируется в локальной плоскости, без убедительного world-locked контакта. Shoulder/cheek fit требует дальнейшей работы |
| Forest/snow/terrain | Выбранные fir/pine с LOD, CC0 Snow02, off-route relief, rocks,24 edge trees и24 winter shrubs | Трасса читаемее, зимние поверхности разнообразнее. Пустой дальний план, редкие группы и плоский горизонт всё ещё выглядят как тестовая площадка |
| Stadium/event | CC BY stand без advertisements, CC0 canopy/screen frame, оригинальные panels/banners, concrete/iron maps | Event space стал понятнее, но состав объектов и масштаб пустот пока не дают целостного стадиона |
| Crowd | Два локальных Renderpeople head derivatives; собственная зимняя одежда,2LOD,5цветов | Лица лучше издали; близко видны простые коробчатые пальто, цилиндрические конечности и повторяемость. Реакции механические |
| Lighting | CC0 Passendorf upper sky, согласованное солнце, weather/cloud time, отдельные data-driven snow ambient presets | Улучшена читаемость снега и лиц в тени; небо/горизонт и свет ещё простые. Это sky appearance, не HDR image-based lighting |

Hero factory используется в гонках и benchmark. **Окружение/crowd/lighting uplift применяется к VisualBenchmark; игровые трассы остаются greybox.** Benchmark содержит десять detached pose fixtures, наблюдённых из tick400, а не полноценную текущую гонку. Поэтому он не доказывает качество всей игровой сессии.

## Источники, исключения и права

Полный [аудит37 кандидатов](G2_VISUAL_ASSET_AUDIT.md), [download report](../licensing/g2_visual/G2_ASSET_DOWNLOAD_REPORT.md), [attribution draft](../licensing/g2_visual/THIRD_PARTY_ATTRIBUTION_DRAFT.md) и derivation manifests сохранены. Фактические notices включены в `Assets/Resources/Biathlon/G2ThirdPartyCredits.txt` и проверенную сборку. Детали импорта: [hero](G2_HERO_ATHLETE_IMPLEMENTATION.md), [animation](G2_ANIMATION_PRESENTATION_REPORT.md), [environment](G2_ENVIRONMENT_UPLIFT_REPORT.md), [crowd](G2_CROWD_IMPLEMENTATION.md), [lighting](G2_LIGHTING_IMPLEMENTATION.md).

Выбраны отдельные runtime derivatives вместо полного импорта архивов: base body/skin/cloth/goggles, fir/pine/snow/rocks/shrub, stand/canopy/screen/architecture maps, два head-only crowd варианта и один HDRI. Source branding/materials goggles заменены; у stand удалены advertising objects. Реальные бренды с owner reference images не воспроизводились.

Отклонены/отложены: generic walk/run mocap как замена лыжной технике; офисная/летняя одежда и аксессуары crowd; избыточные исходные mesh/texture resolutions; Snowy Forest/Forest Path HDRI с нависающими кронами для открытого стадиона. Четыре Unity Asset Store позиции остаются metadata-only; optional Anderson Rohr pack требует manual action. Его отсутствие не было техническим blocker. Restricted Renderpeople mesh/textures, включая производные, исключены из public Git; там только recipe, hashes и rendered evidence. Другому checkout нужно воспроизвести локальную сборку crowd из лицензированного staging. Release licensing audit остаётся обязательным.

Дополнительный поиск/скачивание подходящих бесплатных ассетов теперь [разрешён владельцем](../owner/FREE_ASSET_AUTHORIZATION_2026-09-23.md). В008A новых внешних ассетов не скачивали.

## Проверки и пределы доказательств

| Проверка | Результат |
|---|---|
| Последний полный PlayMode на runtime007A |143/143 PASS,127.601s; focused lighting3/3 PASS |
| Новый capture harness008A |1/1 PASS,12.768s;144 discovered — **не** полный запуск144 тестов |
| Compilation/Console после008A |compilation_failed=false;0errors/1MCP disposal warning. При перекомпиляции также видны2существующих CS0618 в PlaytestEvidenceTests/VisualBenchmarkTests; сохранены отдельно |
| Windows runtime007A |Build Succeeded,0errors/0warnings; hidden9/9 реально отрисованных scene/tier cases PASS |
| Profiling007A |10/10 observable budgets PASS при прежних порогах; max2715548triangles, max draw calls1242; GPU/render-thread/batches UNAVAILABLE, итог ACTION_REQUIRED |
| Capture008A |45PNG2560×1440;30совпадающих WP165 камер/условий; max разница компонентов камеры<0.000002; текущие rewind-a/b пиксельно идентичны |
| Motion008A |48реальных PNG1280×720 при24samples/s →2s WebP quality90; все кадры отрисованы; это частота записи, **не** measured FPS |

Evidence: [capture result](../quality/g2/owner-review/capture-test-final.json), [Console](../quality/g2/owner-review/console-state.json), [compile warnings](../quality/g2/owner-review/console-warnings.json), [post-test excerpt](../quality/g2/owner-review/post-test-console-excerpt.txt), [full143](../quality/g2/lighting-a/playmode-full.json), [smoke](../quality/g2/lighting-a/player/verification.json), [profile](../quality/g2/lighting-a/profile/analysis.json).

Runtime после007A не изменялся:008A добавляет test-only capture и упаковку evidence, поэтому повторный build/full suite не выдаются за новые результаты. Capture test проверяет неизменность benchmark SourceTick400; dedicated aim снимает реальные замороженные AI bouts в prone/standing. Остальные стрелковые ракурсы — явно постановочные pose fixtures. Offscreen вывод не содержит HUD/crosshair и не заменяет hands-on оценку управления. Старые WP165 Game View и новый offscreen output различаются путём вывода; before/after качественный, а не pixel regression между версиями.

Во время подготовки отвергнут capture с broadcast position вместо chase: assertion выявил6.342m отклонение, сохранён [red result](../quality/g2/owner-review/rejected-chase-capture.json). Исправлено только управление камерой в test harness. Также исправлены black first motion frame из-за readback до PostLateUpdate и crop лица, не учитывавший текущую bone pose. Новые кадры изучены после повторного запуска. Известное player shutdown предупреждение RenderTexture.active остаётся в profiling evidence.

Authoritative Domain/Simulation/AI/Career/Replay и tuning стрельбы в этой работе не изменены. Отдельная новая balance qualification не проводилась. **OWNER-FEEL-001 OPEN**: слишком лёгкая стрельба обязательно возвращается на gameplay-feel/difficulty/race-balance WP229/WP230/WP232. Чистая schematic aim presentation может влиять на восприятие сложности, но причинная связь пока не проверена; скрытых численных изменений и RNG нет.

## Сравнение и решение

До/после показывает ощутимый рост детализации деревьев, снега, hero mesh и event props. Однако это не достаточное основание объявлять visual target достигнутым. Owner M11 hero reference задаёт гораздо более убедительную анатомию, экипировку, материалы и силуэт; текущий close hero этому не соответствует.

Для внешнего ориентира просмотрены официальные изображения [NGL Biathlon2026 в Steam](https://store.steampowered.com/app/4059780/NGL_Biathlon_2026/), включая [стадион/спортсменов](https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/4059780/9babd18d2d0ad3e5dcb2b8998d7aa136ee483e5b/ss_9babd18d2d0ad3e5dcb2b8998d7aa136ee483e5b.1920x1080.jpg?t=1773149979) и [chase](https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/4059780/422e47ff7e9214f8657569c6af8666b21ff509d1/ss_422e47ff7e9214f8657569c6af8666b21ff509d1.1920x1080.jpg?t=1773149979). Наш текущий результат не демонстрирует требуемого уверенного визуального преимущества: особенно слабы материалы/разнообразие athlete и заполнение пространства. Это субъективное сравнение изображений, не оценка чужого FPS, motion или gameplay. Изображения не импортировались в игру, бренды не копировались.

Следующий приоритет после решения owner — отдельная доработка hero surface/deformation и ski weight transfer/pole contact; затем shoulder/cheek fit, crowd winter silhouettes, композиция дальнего плана и повторный combined review. Подбор новых бесплатных ассетов должен обслуживать эти задачи, а не количество импортов. Масштабировать текущий результат на production venues преждевременно.

**Recommend G2 FAIL / more remediation needed. Review package завершён; успешная visual remediation не заявляется. Ожидается owner decision G2.**
