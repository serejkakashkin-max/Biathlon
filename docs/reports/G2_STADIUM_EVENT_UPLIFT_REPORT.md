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
