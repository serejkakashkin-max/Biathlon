# M13 — публикация ручного мирового тика через Pack

Дата: 2026-10-07. Статус: PASS для ограниченного base-route bridge;
полностью ручная playable гонка и M13 остаются OPEN.

`ManualBaseRosterSessionCommit.CommitPackField` связывает полное поле с одним
актуальным мировым доказательством. До публикации проверяются точные route,
roster, body definition, clock, odometer, физическая позиция, lateral и все
race previews. Через существующий scheduler публикуются часы, checkpoint
timing и lifecycle; одновременно Pack получает принятые pose/velocity,
физические точки и draft observations. После перехода legacy `Step` запрещён.
Контекст получает отдельный marker и точную initial-world запись однократно.
Detached `RacePresentationSnapshot` хранит принятые world pose/velocity.

Двухтиковая проверка выявила реальный отказ второго force tick: endpoint
velocity использовала неокруглённые heading/speed, а следующий тик начинался
с закодированного состояния. Endpoint теперь использует сохранённые значения;
внутренние continuous samples и shared force solver не менялись. Дополнительный
тест проверяет 200 последовательных ускорений/поворотов на плоской поверхности.
Это не квалификация переходов между разными directional grades или side-slip.

Проверена настоящая production Woodland Sprint composition с двумя sessions
в разных route sections. Устаревшая вторая строка не меняет ни одного accepted
тика, motion, physical point или event count. После повторной подготовки
поле и ledger принимают обе строки вместе. Неверный размер тела также
отклоняется. Предыдущий presentation snapshot остаётся независимым.

Проверки:

- .NET manual bridge/force/world/joint/presentation/command tape: 50/50 PASS.
- .NET legacy Pack/player/Sprint force/replay/field: 38/38 PASS, включая
  фиксированные result hashes и exact replay.
- Unity EditMode base bridge: 8/8 PASS; force trajectories: 9/9 PASS.
- Unity compile PASS; свежий Error Console пуст.

Первый расширенный .NET run был RED из-за velocity continuity и наблюдения
3584 B в allocation assertion; следующий run с новым двухспортсменным fixture
обнаружил неверно выбранный section и отдельное наблюдение 5072 B. Fixture
теперь выбирает фактический section каждого спортсмена. Allocation assertions
не изменялись и проходят в итоговом 50-test run; промежуточные receipts
сохранены. Это не sustained performance qualification WP-178. Первый Unity
regex class filter исполнил 0 тестов и не учитывается; затем использованы
точные имена двух классов. Evidence: `docs/quality/m13/manual-pack-publication-20261007/`.

Ограничения: все строки активны, движение вперёд в пределах текущего base
section и до range entry; draft observations пока подаёт caller. Нет
start/range/penalty/finish phase orchestration, seam/contact continuation,
полного manual race replay и подключения input/rig в ProductionVenueHost.
Legacy playable host остаётся на lateral v1. Тюнинг способностей и стрельбы
не менялся; новый Windows build пока не создавался.

Следующий шаг: непрерывное world movement между geometry intervals, phase
paths и транзакция полного manual field, затем host/replay и итоговый build.
