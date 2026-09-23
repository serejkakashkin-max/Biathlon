# G2 — повторный owner review012

23 сентября2026. **Recommend G2 FAIL / more remediation needed.** Запрошенные checkpoints009–011 и финальная коррекция009D выполнены и проверены;012 review package готов. Визуальный production target не достигнут. G2 NOT APPROVED, M13/WP167 заблокированы. Остановка перед новым решением владельца по шагу9 [директивы](../owner/CODEX_POST_WP166_G2_VISUAL_REMEDIATION_DIRECTIVE_RU.md).

## Пакет для просмотра

- [Галерея45 пар008A/012 и движение](../quality/g2/owner-review-012/index.html). Все45 камер/ориентаций/FOV совпадают с008A; rewind текущей версии пиксельно идентичен. Свет/материалы/геометрия намеренно отличаются.
- [Манифест](../quality/g2/owner-review-012/manifest.json), [motion48frames](../quality/g2/owner-review-012/motion-manifest.json), [профиль](../quality/g2/owner-review-012/profile/analysis.json), [Windows smoke](../quality/g2/owner-review-012/player.json).
- [Подробный отчёт009A–D/010/011](G2_FURTHER_REMEDIATION_REPORT.md), [открытые вопросы](G2_OPEN_ITEMS.md), [state](../PROJECT_STATE.md).
- Локальная проверенная сборка: `C:\game\Biathlon\Biathlon\Builds\G2OwnerReview-20260923-012\Windows\Biathlon.exe`. Рядом `build-files.json`:393 файла,296101520bytes, каждый SHA-256 проверен при копировании. BuildReport296731280bytes — отдельная метрика. EXE/compiled content не опубликованы в public Git.

Сборка начинает с PlayableSprint. **M12 visual benchmark** открывает benchmark с выбором ракурсов, quality/weather/lighting и Animate/Freeze. Агент использовал исключительно скрытые окна. Игровые трассы сохраняют greybox окружение; герой общий для гонок и benchmark. Benchmark показывает10 detached наблюдённых poses из tick400, не текущую полноценную гонку. Dedicated shooting captures используют реальные замороженные AI bouts. Offscreen изображения не включают HUD и не заменяют hands-on оценку.

## Статус WP-166 и результаты доработок

WP-166 завершён технически, [profiling/report](WP-166-PROFILING-REPORT.md) сохранён. Ограничение недоступных GPU/render-thread/batches counters не скрыто. Master Plan не переписывался, production venues не масштабировались.

| Область | Изменение009–011 | Честная оценка012 |
|---|---|---|
| Hero appearance | Меньшие очки/посадка, отдельные cloth/skin/lens материалы,6 общих вымышленных kit palettes, исправленный harness | Разнообразие и силуэт лучше. Лицо/ткань/линза всё ещё упрощены; cap/bib silhouette и крайние деформации не достигают owner reference |
| Ski animation | Парная работа рук,2pole cycles на равнине/1на подъёме, перенос таза/корпуса, краткий world-space plant с ограниченным отпусканием | Убрана походная противофаза рук, контакт объясним. Процедурный шаг и перенос веса всё ещё механические; V1 асимметрия/полный силовой контакт не реализованы |
| Shooting/body |4bounded volume shapes на3LOD; более низкая посадка головы и боковой наклон у приклада; исправлены складки ремней | Близкие позы аккуратнее, но anatomical плечи/суставы и тканевые складки требуют art work. Authoritative sight не меняется |
| Environment | Дальний снежный basin4096tri,512bounded FirLOD2 placements, fog depth и сохранённый snow/forest foreground | Появился дальний план. Склоны слишком сглажены, лес ещё редок и видна процедурность; уверенного production-quality преимущества не заявляется |
| Stadium/crowd |3service shelters, waist/quilt silhouettes, независимые elbows и head turns | Пространство понятнее; service zone простая. Два лица повторяются, рукава/куртки сегментированы, reactions условны |
| Lighting | Benchmark-only ясная/облачная глубина тумана; прежние4lighting presets и snowfall сохранены | Лучше разделение планов; снег/лицо и общий look требуют дальнейшей художественной настройки. Нет заявления о полноценном HDR image-based lighting |

## Источники и отклонённые варианты

Новых third-party sources в009–012 не скачивалось и не импортировалось. Использованы уже аудированные CC0 body/materials/snow/rocks/understory/sky; CC BY goggles/fir/pine/stand с прежними source/license/modification notices; restricted Renderpeople head derivatives остаются локальными. Новая geometry/service shelters и процедурные corrections — собственные. Возможность искать бесплатные альтернативы сохраняется по [разрешению](../owner/FREE_ASSET_AUTHORIZATION_2026-09-23.md).

[Аудит37кандидатов](G2_VISUAL_ASSET_AUDIT.md) и [shipping attribution](../licensing/g2_visual/THIRD_PARTY_ATTRIBUTION_DRAFT.md) сохранены.4Asset Store metadata-only entries и manual crowd pack не импортировались. Source textures с маркировкой очков и advertisements трибуны не возвращены. Restricted mesh/texture data не попали в Git; в public package только код, recipes/hashes и rendered images.

В этой итерации отклонены: MaterialPropertyBlock kit-вариант из-за SetPass regression(009A);8° head pitch, нарушавший существующий sight test(009C); исходный NaN edge рельефа(011); независимо спроецированные кромки/дублированные обратные faces ремней(009D). Исправления и failing evidence сохранены, тесты не удалялись и не ослаблялись.

## Проверки и ограничения

- Финальный Unity MCP PlayMode **155/155 PASS**,144.852s. До коррекции ремней154/154; целевые hero6/6, joints2/2, motion3/3, crowd4/4, backdrop2/2. Финальный full включает повторную матрицу45кадров и48motionframes с дальним лесом.
- Windows Development: build0errors/0warnings; hidden smoke **9/9** для Sprint6,MassStart30 и benchmark,3quality tiers.
- Профиль:10cases,1920×1080offscreen с16:9frustum; максимум **2791998triangles**, **109SetPass**, **1246draw calls**; максимум case main-thread p95 **1.571ms**. Все доступные бюджеты PASS. Это не GPU/FPS qualification: GPU/render-thread/batches **UNAVAILABLE**, общий profiling disposition ACTION_REQUIRED.
- Console после capture:0errors/3warnings, compilation_failed=false. Отдельно сохранены существующие CS0618/UAC editor diagnostics и MCP disposal/closed-connection warnings; они не объявляются нулём. Состояние после финальной сборки сохранено отдельно.
- Snapshot/race/stance integration прошли в полном PlayMode. Domain/Simulation/AI/Career/Replay и authoritative tuning не менялись; standalone deterministic suite повторно не запускался, поскольку race output не изменялся. Нет hidden RNG или изменения попаданий.
- **OWNER-FEEL-001 OPEN**: слишком лёгкая стрельба переносится вWP229/230/232. Эта работа не является её закрытием.

45PNG2560×1440,48motion samples1280×720, WebP24fps/2секунды — частота сохранения, не измеренный FPS. Сравнение008A→012 показывает исправленные дефекты, но не подтверждает художественную цель. Вывод: **recommend G2 FAIL / more remediation needed**. Следующее действие — owner review этого конкретного пакета; дальнейшее content scaling и G2 PASS без решения владельца запрещены.
