# M13 — однозначное начальное состояние ручной гонки

Дата: 2026-10-07. Изменены `ManualWorldRosterLedger` и его тесты.

Контекст ledger фиксировал геометрию, правила, ID спортсменов и optional
odometer, но пропускал исходные позиции, heading/yaw, planar velocity и
route cursors. В варианте без odometer пропускался также начальный тик.
Шесть реальных constructor cases получили одинаковый контекст при разных
начальных состояниях: [6/6 RED](../quality/m13/manual-world-context-20261007/manual-world-initial-context-red-bounded.trx).

Добавлена неизменная initial-state запись с отдельным version marker. Она
хранит точные integer nanometres/nanoradians, round-trip double значения,
clock, стабильные ID и упорядоченные наблюдаемые/зачтённые станции. Запись
отделена от изменяемого accepted state и включена в полный контекст ledger.
Ни позиции, ни стартовое направление не выводятся повторно из маршрута.

.NET ledger/base-commit/projection/joint regression [50/50 PASS](../quality/m13/manual-world-context-20261007/manual-world-initial-context-final.trx).
Скрытая Unity EditMode ledger 15/15 PASS. Дополнительный тест восстановил
точно pose, velocity, cursor, odometer и clock из JSON, проверил ru-RU и
неизменность initial receipt после accepted tick. Unity compile/Console PASS.
Движение и баланс не изменились; legacy Pack replay остаётся прежним.

Это устраняет неоднозначность подготовки будущего manual replay. Playable
Pack/все race phases и полный manual race tape ещё не подключены; M13 OPEN.
Следующий шаг — публикация принятого world tick через Pack.
