# M13 — общий manual field, контакты и наклонные стыки

2026-10-09. **Integration checkpoint; M13 OPEN.** Следующая Windows сборка
разрешена только после полного M13. Старый callback промежуточной сборки отменён
до запуска; готового нового build нет.

## Реализовано

- ManualScopedFieldStepper объединяет подготовленные усилия/позу, signed yaw,
  shared ski/snow forces, контакты, обычные и штрафные переходы и полную
  публикацию Pack на одном fixed clock. Отказ отменяет provisional tick;
  запасного lateral-v1 движения или оплаченной неподвижной очереди нет.
- Вращающиеся контакты учитывают кориолисово ускорение поверхности и конечный
  интервал опоры. Открывающийся контакт освобождается, когда его перемещение
  разрешимо относительно существующего spatial skin. Force residual использует
  настроенный closing-speed cap; impact solver сохраняет более строгую численную
  сходимость. Full body sweep остаётся обязателен.
- Обычные физические стыки имеют общую биссекторную плоскость и согласованную
  off-centre station chart. Фаза, дистанция, pose, velocity и время не выдаются
  геометрическим запросом. Конец ребра сохраняется буквально, без повторной
  сборки a+(b-a), разрывавшей соединение при переходе через ноль.
- Проверка snow ribbon покрывает всё ориентированное тело перекрывающимися
  дисками. Для non-simple polygon используется покрытие внутри существующих
  консервативных edge tubes с прежними ширинами/turn reserve. Далёкие и разорванные
  пути не соединяются convex hull. Skin не разрешает нулевой/отрицательный gap.
- AI выбирает объезд неподвижного тела через signed yaw, держит линию до
  фактического обгона и учитывает фактическое скольжение на повороте. Запрос
  инвертирует тот же скоростной отклик, что общий физический solver. Player yaw
  проходит без исправления по центру/касательной. Намерение не является боковым
  перемещением или разрешением пройти через соперника.
- Запрошенный резкий поворот имеет общую стоимость торможения даже до набора
  ограниченного сцеплением yaw. Константы физики/стрельбы и размеры тела не перенастроены.

## Проверки и предел доказательств

Receipts: docs/quality/m13/field-stepper-20261008/. Дата каталога — начало работы;
сегодняшние результаты и прошлые неудачи сохранены вместе.

| Проверка | Результат |
|---|---|
| Последняя завершённая широкая baseline manual/replay | 449: 444 PASS / 5 FAIL, manual-replay-final.trx |
| Четыре baseline phase failures | Ошибка synthetic fixture: legacy scalar hint не совпадал с off-centre chart. Restore теперь наблюдает настоящий arc и сохраняет уже заработанный high-water. Фазы/границы не ослаблены. |
| Исправленная phase/session/event регрессия | .NET40/40 PASS, phase-chart-correction.trx |
| Финальная профильная физика/AI/geometry/phase регрессия | .NET111/111 PASS, physical-policy-final.trx |
| Physical velocity pursuit: первый круг + stationary player Mass30 | 1 PASS / 1 FAIL; Mass30 проходит400ticks, первый круг отклонён на tick3021 |
| Unity: portal/progression, baseline cover, contact forces | 12/12, 7/7, 21/21 PASS |
| Unity: исправленные рабочие штрафные фазы, controlled drive, spatial contacts | 5/5, 16/16, 15/15 PASS |
| Unity: baseline stationary-player Mass30 | 1/1 PASS,400ticks; baseline до physical-velocity AI поправки |
| Unity: инверсия общего yaw response и фактическое скольжение AI | 3/3 +1/1 PASS |
| Unity: окончательный ribbon и тесный mat hairpin | 9/9 PASS |

Native Summary.TotalTests показывает discovery assembly1553/1559, а не число
выполненных проверок. Здесь посчитаны только actual Results. Refresh успешен,
свежий Error Console пуст. Проверки скрытые и не дают FPS/GPU approval.

Повтор broad после fixture correction отменён после более350CPU s, final TRX
не оставил и отмечен ABORTED. Попытка одновременной пересборки была отклонена
MSBuild из-за занятой DLL; после остановки проверки выполнялись последовательно.
Это не PASS и не изменение test assertions/runtime policy. Новый широкий
диагностический запуск с per-test hang timeout начат; результат дописывается отдельно.
Первоначальные compile errors диагностики (неверный NUnit API ранее, обращение
к отсутствующим Forward fields у knot сегодня) исправлены.

## Открытый дефект и следующий шаг

AiRosterReachesItsFirstShootingBoutOnTheManualClock остаётся активным failing test.
Physical-pursuit политика переносит отказ с tick584/base arc161.38m на tick3021,
occurrence3, mat_01 arc2.1885m. Athlete входит в тесный разворот со скоростью
около8.3m/s. Stationary body там проходит domain proof, та же быстрая moving
trajectory закономерно не проходит. Нельзя исправлять это расширением snow
domain, отключением теста, мгновенным торможением или ожиданием. Следующий шаг:
anticipatory physical braking, short-bend target, stationary firing docking.

Candidate ещё не поддерживает reusable Sprint40 mats, finished roster, полный
achieved-world drafting, complete command replay и подключение production host.
Production host остаётся lateral-v1. Все фазы/ресурсы, WP175 player/weather
readability, WP177 balance и WP178 sustained профили должны быть закрыты до
итоговой owner Windows сборки.

Повтор одинаковых owned команд проверен; контексты изменённых политик
версионированы. Это изменение будущего manual race output, не доказанная полная
replay/balance совместимость. Hidden shooting RNG не добавлен. Owner issue
«стрельба слишком лёгкая» сохраняется для последующего tuning.

**M13 OPEN; WP176 PASS; WP175/177/178 OPEN.** Одна Woodland трасса, WP170–174
owner-deferred, max40/Mass30, G1/G2 APPROVED. Master Plan не изменён.

## Дополнение: расширенная регрессия

manual-replay-physical-final.trx: 533 PASS,1 FAIL (известный manual mat arrival),
затем ABORTED по диагностическому60s timeout. Остановлен дополнительный дорогой
WoodlandFullFieldCommandReplayTests.ThirtyAthleteWoodlandMassStartPersistsAndReplaysExactFieldState,
у которого собственный объявленный Timeout900000ms. Этот watchdog был слишком
коротким для него; итог не PASS и не доказанный replay defect. Sequence XML и
локальный dump сохранены. Этот отдельный тест запущен повторно без короткого
watchdog и без параллельной пересборки занятой DLL. Финал фиксируется отдельно.
Новый native initial manual Mass30 после physical-pursuit поправки:1/1 PASS.
