# M13 — продолжение принятой мировой скорости

Дата: 2026-10-07. Промежуточная интеграция; M13 OPEN.

После исправления endpoint encoding оставалась другая проблема: изменение
directional grade при повороте заново переводило scalar speed в горизонтальную
скорость. Траектория начиналась с другого вектора, и строгий world-ledger gate
отклонял следующий тик. Новый тест воспроизвёл расхождение 6.7819463753383866
против 6.769959276485543 m/s до исправления.

`ManualSkiMotionSystem.PreviewAcceptedVelocity` сохраняет принятый planar vector.
Scalar coordinate общего force solver выводится из его нормы и текущего grade,
сохраняя odometer. Для движущегося тела yaw вращает исходный вектор, а общий
force speed ramp задаёт изменение его нормы. Simpson integration использует
тот же векторный путь. Точный start velocity не заменяется вычисленным из
направления трассы. Rest acceleration совпадает с существующим force preview.
Accepted endpoint остаётся согласованным с encoded pose/speed. Отдельный
контекст фиксирует эту политику; legacy preview и force API сохранены.

Незначительное floating roundoff при норме sin/cos вектора допускает только
явный 16-ulp encoding reserve для scale/cap. Исходная velocity не изменяется.
Существенный side-slip, движение против собственной ориентации или настоящий
scalar-cap excess отклоняются: они требуют отдельного slip/cap continuation,
а не скрытого поворота, clamp позиции или оплаченного стояния.

Production Woodland Sprint fixture публикует 21 совместный тик двух спортсменов
в разных sections через Pack. AI route controller выдаёт yaw intent, общий
force solver сохраняет exact accepted velocity, body/course sweep доказывает
полный интервал. Проверяется действительно меняющийся geometric directional
grade, clocks, odometer и detached snapshot. Первоначальный прямой input
достиг границы course tube на 20-м продолжении; этот отказ сохранён. Course
proof не ослаблен — следующие intents поступают от существующего AI controller.
Игрок через этот controller не проходит.

Проверки:

- Итоговая .NET регрессия всех manual-компонентов: 177/177 PASS.
- Скрытые Unity EditMode: motion 14/14, base/Pack bridge 8/8, tube 5/5,
  force trajectories 9/9 PASS.
- Проверены 200 поворотов с changing grade, exact start vectors, acceleration
  from rest, encoded speed cap, отказ unsupported velocity и warm allocation.
- Unity compile PASS; Error Console пуст. Legacy race host не переключён.

Первый широкий run: 172/175 PASS, три FAIL. Два allocation measurements (80 B
в composed preview и 720 B в ledger) не воспроизвелись в итоговом run; причины
не доказаны и существующий общий allocation-risk не закрыт. Assertions не
изменялись. Третий FAIL был устаревшим fixture-count порогом >100 short edges:
после owner-approved сокращения Main их 62. Теперь проверяется **каждый**
internal knot (`accepted == loop.Count - 2`) и наличие repeated short edges.
Полные clear-interval assertions для всех knots сохранены. Это не доказательство
sustained player performance WP-178. RED/TRX/native receipts сохранены в
`docs/quality/m13/manual-velocity-continuation-20261007/`.

Осталось: actual-world posture/physiology и backward/retrace в Pack, surface/phase
splits, start/range/penalty/seams/contact continuation, полный manual race tape,
host input/rig, окончательная погодная/performance проверка и Windows build.
Нельзя считать проверенный base-route preview полной playable ручной гонкой.
