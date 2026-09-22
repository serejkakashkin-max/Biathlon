# G2 Stadium / Event Uplift

## G2-VR-005A — адаптированная трибуна и навес

2026-09-22. Технический checkpoint завершён; итоговая визуальная приёмка G2 остаётся OPEN.

Из G2-030 (William Prosser, CC BY 4.0) выбрана конструкция трибуны: ступени,168 кресел, проходы и ограждения. Два рекламных объекта polySurface15/polySurface24 полностью удалены; исходные материалы и все текстуры отброшены. Равномерный масштаб26 переводит маленькую исходную модель в трибуну шириной17.868м. Собственные нейтральные материалы; исходные нормали сохранены. Каждый spectator anchor получен из горизонтальной поверхности соответствующего кресла, с поправкой30см на существующую seated pose. Нет source colliders или authoritative gameplay identifiers.

G2-027 (3dassets.dev, CC0): один200-треугольный навес, ширина адаптирована до19.52м. Белая исходная фасция остаётся металлической панелью; поверх крыши добавлен отдельный собственный снежный слой. Итоговая геометрия14,292 треугольника, семь mesh groups. Старый grandstand отключается только в VisualBenchmark через typed SpectatorSeating; остальные stadium/crowd fixtures сохранены. [Derivation](../licensing/g2_visual/grandstand-derivation.json), [runtime hashes](../licensing/g2_visual/grandstand-runtime-manifest.json), [seat bindings](../licensing/g2_visual/grandstand-seat-bindings.json). Полный CC BY notice с автором, URL, лицензией и изменениями поставляется с player.

При реализации исправлена Windows-коллизия имён Seats.json/seats.json: привязки теперь seat-bindings.json, импорт проверяет mesh channels. Ошибка не была скрыта как успешный импорт. Первая интерпретация белой фасции как снега отклонена при осмотре конструкции.

Отдельно устранены четыре предупреждения нашего AuditedFir shader о потенциально неинициализированном результате: обе текстуры читаются до выбора материала вместо ветвления вокруг implicit-derivative sampling. Это presentation-only исправление, но дополнительная выборка может иметь GPU-стоимость; скрытый профиль её не измеряет. ShaderUtil: has_error=False, сообщений нет. В предыдущей сборке было502 предупреждения: четыре нашего shader,496 Sentis/Inference и два URP debug-shader. Итоговая инкрементальная сборка0 warnings не означает, что предупреждения сторонних пакетов исправлены; исходный список сохранён.

Проверки: focused3/3, полный PlayMode134/134 до исправления shader и повторный полный134/134 после. Все168 anchors проверены против реальных треугольников кресел; проверены clearance крыши/маршрута, отсутствие colliders, bounded geometry и поддержка shader. Нулевой повторный запуск MCP со статусом Unknown не засчитан; выполнен полноценный повтор после reload. Console и точные результаты сохранены в evidence.

Windows Development280,794,736 bytes,0 errors/0 warnings; скрытый smoke9/9 PASS. Все10 сценариев неизменённой матрицы проходят доступные budgets; максимум2,716,320 треугольников. GPU/render-thread/batches недоступны, итоговый profile ACTION_REQUIRED; FPS/min-spec qualification не заявляется. Известное предупреждение завершения player о RenderTexture.active сохранено в логах.

Осмотрены общие и изолированные кадры конструкции, толпы, рубежа и финиша. Проходы/ограждения и навес улучшают конструктивную правдоподобность. Толпа пока выглядит игрушечной, материалы плоскими, стадион недостаточно наполнен инфраструктурой события. Это не production-quality acceptance. [Captures/tests/player/profile](../quality/g2/stand-a/).

Следующий checkpoint005B: оригинальное оформление события, выбранные structural screen/props и материалы архитектуры. Далее006 crowd,007 lighting и возврат к открытым hero/motion actions. Authoritative simulation, shooting tuning и replay не менялись; OWNER-FEEL-001 OPEN. G2 NOT APPROVED, M13/WP167 BLOCKED_G2.

Console: 0 errors, 3 warnings, compilation_failed=false. Все три предупреждения относятся к Unity MCP после domain reload: disposed BufferedFileLogStorage, disposed SignalR HubConnection и попытка вернуть результат в закрытое соединение. Native Console snapshot сохранён; предупреждения сцены/нашего shader не обнаружены.

## G2-VR-005B — оформление события и материалы архитектуры

2026-09-22. Технический checkpoint DONE; итоговая visual acceptance OPEN.

Из G2-029 CC0 выбран только108-треугольный каркас экрана, без исходных материалов/текстур. Собственная статическая заставка BIATHLON, четыре оригинальных вертикальных указателя RANGE/FINISH; чужих марок и выдуманных live results нет. Первый вариант размещения экрана за трибуной отклонён из-за перекрытия навесом; итоговая позиция слева от рубежа проверена прежними камерами. Панель вписана в реальные границы каркаса. Это пока простая заставка, не live video/scoreboard.

G2-034/035 Poly Haven CC0: семь выбранных1K diffuse/OpenGL normal/roughness/metallic maps. Материалы URP Lit: R=metallic, A=1-roughness в linear mask, подтверждено выборкой исходных/упакованных пикселей. Собственные metre-scale planar UV/tangents для бетонного backstop, фундамента/ступеней timing lodge, опор finish gantry и металлических деталей lodge. Четыре mesh bindings в benchmark; геометрия всех треугольников и bounds совпадает с исходной. Legacy fixture assets не заменены. Текстуры исходных стадий roughness/metallic пока входят в Resources вместе с packed masks; build вырос примерно9.8MB, дальнейшая упаковка возможна.

Исправлена найденная интеграционным тестом потеря prefab overrides: mesh/material/binding modifications явно записываются перед сохранением сцены. Исходный failing result сохранён. Крупный план сначала был чёрным из-за отсутствия прогрева нового offscreen target; исправлен capture loop с тремя кадрами. Принятые кадры осмотрены, включая screen-close/sector/range. Материалы и signage заметнее, но общая сцена всё ещё разреженная и недостаточно реалистичная; crowd/lighting/hero actions не закрываются этим этапом.

Focused3/3 и полный **137/137 PASS,120.298s**. Проверяются route/range intrusion, schema/duplicate IDs, размер геометрии, linear masks, фактические pixel channels, сохранение архитектурной геометрии после повторной загрузки. Console0 errors/1 MCP warning, compilation_failed=false; native snapshot сохранён. Сборка **290,603,964 bytes**,0 errors/0 warnings; скрытый smoke9/9 PASS. Все10 observable profile budgets PASS, максимум **2,717,642 triangles**. GPU/render-thread/batches UNAVAILABLE, итог ACTION_REQUIRED; FPS/min-spec approval не заявляется. Известное shutdown RenderTexture.active warning в player остаётся.

[Evidence](../quality/g2/event-b/), [source derivation](../licensing/g2_visual/event-derivation.json), [runtime hashes](../licensing/g2_visual/event-runtime-manifest.json). Credits поставляются с player. Simulation, стрельба, replay и игровое управление не менялись. Далее G2-VR-006A: выбранные crowd derivatives, зимняя одежда и масштабируемая интеграция; restricted source/derivative data остаются локально. G2 NOT APPROVED, WP167/M13 BLOCKED_G2; OWNER-FEEL-001 OPEN.
