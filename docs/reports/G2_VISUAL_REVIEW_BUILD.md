# Windows-сборка текущего дизайна для владельца

23 сентября 2026. По запросу владельца собрана отдельная версия, которая сразу открывает **VisualBenchmark** с текущим окружением и спортсменами. Запуск: `Builds/VisualReviewWindows/START_VISUAL_REVIEW.cmd`; исполняемый файл: `Builds/VisualReviewWindows/Biathlon.exe`. В папке есть русская инструкция и attribution.

Новая площадка пока служит для осмотра дизайна и демонстрации анимаций. Управляемой гонки на ней нет. Кнопки Sprint / Mass Start открывают игровые тесты на прежних трассах; вернуться к площадке можно перезапуском приложения. Это явное ограничение реализации, а не завершённая интеграция нового окружения в гонку. Визуальная оценка из [review012](G2_REVIEW_012.md) сохраняется: target unmet, G2 NOT APPROVED, M13/WP167 blocked.

## Изменения

- `PlayerBuildPipeline.BuildWindowsVisualReview()` собирает VisualBenchmark первой сценой, сохраняет обычный порядок сцен в `BuildWindows()`, добавляет launcher 1920×1080 и русскую инструкцию. Глобальный список build scenes не перенастраивается.
- Исправлен обычный запуск камеры benchmark: в авторской сцене она выключена для Edit Mode captures, теперь `Start()` включает вывод в окно. Автоматический offscreen profiler и benchmark smoke явно выключают её перед своими измерениями, чтобы не рисовать сцену дважды.
- PlayMode integration test проверяет активную камеру без target texture и фактический вызов URP `endCameraRendering`. Player smoke проверяет первую сцену, активную камеру и доступность controls в собранном приложении. PowerShell entry поддерживает `-VisualReview`.

## Проверки

- [Вся PlayMode assembly](../quality/g2/visual-review-build/playmode.json): **155/155 PASS**, 142.755 секунды. В том числе обновлённая проверка настоящего рендеринга камеры.
- Windows Development build: **Succeeded, 0 errors / 0 warnings** по BuildReport, Unity 6000.6.0f1. Размер файлов поставки 296103085 bytes, 395 файлов; [SHA-256 manifest](../quality/g2/visual-review-build/build-files.json).
- [Скрытый Windows smoke](../quality/g2/visual-review-build/player.json): startup VisualBenchmark / CAMERA_ACTIVE / CONTROLS_VISIBLE; Sprint 6 и Mass Start 30 спортсменов, 400 ticks каждый; **9/9 scene×tier render checks PASS**, exit0.
- [Console после сборки](../quality/g2/visual-review-build/console.json): 0 errors / 0 warnings, compilation_failed=false. В [MCP warning cache](../quality/g2/visual-review-build/build-warning-cache.json) сохранены предупреждения package compute shader `ConvGeneric` о неподдерживаемых сочетаниях TRANSPOSE_OUTPUT; нулевой BuildReport не означает их отсутствия в историческом cache. Перед сборкой Console содержала 1 warning / 0 errors.
- Дополнительная скрытая [диагностика high-athlete](../quality/g2/visual-review-build/profile-diagnostic.json): 300 samples, max2714890 triangles /822 draws, triangle budget PASS; CPU main p95 1.2941 ms. [Raw samples](../quality/g2/visual-review-build/profile-high-athlete.json) и [execution](../quality/g2/visual-review-build/profile-execution.json) сохранены. Это один offscreen case, **не qualification обычного оконного FPS/GPU**. Первый запуск анализатора потребовал недостающий execution.json; после записи exit/log/hash evidence анализатор завершился успешно.
- [Снимок из собранной игры](../quality/g2/visual-review-build/player-balanced.png) просмотрен: новое окружение и спортсмены присутствуют. Снимок получен offscreen без панели controls; активность оконной камеры проверена отдельно.

Сборка выполнена из рабочей копии поверх f5bc534 с перечисленными изменениями; исходный dirty-флаг сохранён в evidence, [хэши изменённых source-файлов](../quality/g2/visual-review-build/changed-source-hashes.json) приложены. Бинарная сборка с локальными licensed assets остаётся локальной; в GitHub публикуются код, инструкция, отчёт и evidence. Посторонние изменения рабочего дерева не включаются. Созданные сборщиком временные изменения URP global settings восстановлены до исходного состояния.

Authoritative simulation, управление, баланс и tuning стрельбы не менялись. OWNER-FEEL-001 остаётся OPEN. Следующий шаг — hands-on просмотр владельцем и конкретные замечания; сборка не утверждает G2 и не запускает M13.
