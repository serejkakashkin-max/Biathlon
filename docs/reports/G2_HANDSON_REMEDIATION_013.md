# G2-VR013 — доработки после hands-on

24 сентября 2026, результаты запуска проверены25 сентября. [Решение владельца](../owner/G2_HANDSON_FEEDBACK_2026-09-24.md): **G2 NOT APPROVED, дальнейшая remediation разрешена, M13/WP167 BLOCKED_G2**. Это промежуточный checkpoint, не художественная приёмка.

[Сравнение012→013](../quality/g2/owner-review-013/index.html). Обновлённая Windows-сборка: `Builds/VisualReviewWindows/START_VISUAL_REVIEW.cmd`. Она открывает новый VisualBenchmark; игровые Sprint/Mass Start пока используют прежние трассы.

## Изменения

- DeviceCommandBuilder / PlayerDeviceInput / PlayableSprintHost: A/D и stick задают направление бокового смещения. Отпускание удерживает достигнутую линию, заблокированный манёвр не накапливает недостижимую цель. Расчёт на fixed tick; абсолютный PlayerCommand/replay и AI сохраняют смысл. Стоимость смещения и коллизии остаются в simulation. [ADR0021](../adr/ADR-0021-directional-live-steering.md).
- TargetBankArt: видимый чёрный круг115mm одинаков в prone/standing; зачётная геометрия45/115mm не менялась. Это соответствует [IBU Rules2025 AnnexA3.2.1.8](https://www.skidskytte.se/download/18.5c27cdf4199374017e0d764f/1758095397982/Rules_2025_EN_cap4.pdf). Sway/HR/ballistics/difficulty не перенастраивались; OWNER-FEEL-001 OPEN.
- Winter: пальто, матовый и глянцевый пуховики, тёплая куртка × два лица —8 новых вариантов,10 всего. [CC BY4.0 audit, авторы и hashes](../licensing/g2_visual/winter/README.md), credits включены в сборку. Arctic Explorer имеет только metadata: соответствующего локального файла нет. Исходные текстуры/этикетки не используются; материалы нейтральные. Связные рукава сохранены и подогнаны в позу, новые руки пока статичны. Near сохраняет геометрию одежды; far сокращён. Комбинированные Renderpeople derivatives остаются ignored/local-only.
- AthleteLocomotionDriver / ski_technique_visual_v1: grounded glide65% цикла, короткий поднятый возврат, возвратный угол лыжи35% основного, перенос таза к опорной ноге. Две лыжи не уходят в поднятую фазу одновременно; paired poling сохранён. Это presentation-only refinement; реалистичность ещё не принята.
- High forest/crowd: ближние тени используют видимую геометрию вместо следующего дешёвого LOD. Low/balanced сохраняют proxy shadows. Остальные LOD/culling/terrain настройки не объявляются отменёнными. Старые budgets сохранены как диагностика, High не урезан ради их прохождения.
- PlayableMassStartTests: [диагностика](../quality/g2/owner-review-013/mass-range.json) показала11 одновременно стреляющих. Первый junction wait на2118tick, main_long797.249m перед fork800m. Всего703ticks ожидания по всем junctions — это не время исключительно у рубежа и не запись ввода владельца. В симуляции30 lanes;10 декоративных установок benchmark их не ограничивают. Пропускная способность общего въезда/FIFO **пока не исправлена**.

## Проверки

- [Полная .NET assembly](../quality/g2/owner-review-013/full-core.trx): **553/553 PASS**,0failed/timeout/aborted. [Focused input/steering/replay](../quality/g2/owner-review-013/feedback.trx):18/18 PASS.
- [Unity DeviceCommandTests](../quality/g2/owner-review-013/device-unity.json):11/11 PASS.553 — discovered, не выполненные в этом фильтре. Первые попытки с неправильным именем assembly дали Unknown/0 и не засчитывались.
- [Полная PlayMode после финальной геометрии](../quality/g2/owner-review-013/full-playmode-final.json): **158/158 PASS**,147.150s. Включает реальные InputSystem events, scoring/target artwork,10crowd variants/LOD/culling/zero allocations, опору/непрерывность лыж, полный Mass Start и capture matrix.
- [Windows build/smoke](../quality/g2/owner-review-013/player-build-run.json): Succeeded,0errors/0warnings BuildReport,348699560bytes. Startup camera/controls PASS; Sprint6/Mass Start30 по400ticks; **9/9 scene×tier render checks PASS**, exit0. Только скрытые проверки.
- [Console](../quality/g2/owner-review-013/console-final.json):0errors,4warnings, compilation_failed=false. [Warning cache](../quality/g2/owner-review-013/warnings-final.json): package ConvGeneric TRANSPOSE_OUTPUT warnings. Пробный экспорт диагностического Mesh через JsonUtility был отклонён API и заменён экспортом координат; игровой код не затронут.
- [Capture manifest](../quality/g2/owner-review-013/manifest.json):45 кадров2560×1440,48 motion frames1280×720, камеры/FOV совпадают с012, rewind pixel-equal. Просмотрены крупная толпа, обе dedicated aim и кадр compiled player. Изображения не дорисовывались.

## Стоимость графики

[Hidden DX11 matrix](../quality/g2/owner-review-013/profile/analysis.json):10/10 execution cases,300samples/case. High-athlete: max16,650,738 submitted triangles /854draws, CPU main p95 3.187ms. Balanced-athlete5,157,096triangles; high-snowfall13,177,849triangles. Исторический3M guard выдаёт FAIL в этих трёх cases: результат сохранён, пороги не переписаны ради PASS. High следует приоритету качества владельца; balanced overrun остаётся отдельной задачей. Остальные доступные guard metrics проходят. GPU/render-thread/batches недоступны, оконный FPS этим не подтверждён.

## Открытые ограничения

Крупный план одежды требует работы: нейтральные материалы без исходного PBR, посадка манжет/складки после posing, два повторяющихся лица, статичные новые руки. Hero anatomy/cloth/skin, полноценная лыжная анимация, композиция леса/склонов, въезд на30мест и интеграция новой площадки с игровой трассой остаются OPEN. G2 visual target не достигнут.

Следующая задача — исправление range admission с сохранением body clearance, deterministic ordering, replay и ADR0014/0017; затем hero/animation/materials. Разрешение владельца на remediation уже действует. Изменение live input намеренно меняет управление игрока; сохранённые команды, AI physics, shooting geometry и physiology tuning не менялись. Master Plan/WP166 evidence сохранены. Сборка выполнена поверх d640a3c с dirty рабочим деревом; публикуются собственные source/report/evidence, посторонние изменения и restricted payload исключены.
