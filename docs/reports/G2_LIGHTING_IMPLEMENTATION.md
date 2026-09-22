# G2 Lighting implementation

2026-09-22. G2-VR-007A technical checkpoint DONE. Final visual acceptance OPEN.

Осмотрены все три staged HDRI. Snowy Forest и Snowy Forest Path отклонены как skybox открытого стадиона: кроны/стволы над камерой не соответствуют этой площадке. Выбран CC0 G2-023 Passendorf Snow,2K HDR. Исходное солнце/halo заменены интерполяцией соседних направлений той же высоты; runtime-диск следует направлению key light. Фотографические ground/horizon исключены маской верхнего неба. Первый кадр показывал чужой далёкий лес, маска исправлена до финальной проверки. Это sky appearance, **не HDR image-based lighting**.

Собственный BenchmarkSky сохраняет explicit weather/cloud/wind time и дымку. Benchmark использует отдельные четыре lighting presets. Дневной direct gain уменьшен, ambient gain поднят, hemisphere fractions изменены с legacy0.65/0.35 на0.9/0.75 для отражения света снегом. JSON data-driven; ограничения проверяются. Ambient-probe readback подтвердил обновление, shader/material не получил искусственный emissive fill. Лица под навесом стали читаемее, а солнце и тени сохраняются. Исходные игровые lighting presets, позиции/FOV камер и authoritative shooting не менялись.

WeatherPresentation принимает необязательный sky template, создаёт собственный material instance и восстанавливает исходное состояние сцены при release. Legacy defaults проверены. Тесты: focused3/3, **полный143/143 PASS,127.601s**;0GC warmed weather application, template immutability, sun-direction alignment, bounds и lighting lease restoration. ShaderUtil has_error=False без сообщений. Post-test Console0 errors/3 warnings — MCP disposed log/SignalR shutdown, excerpt сохранён; compilation_failed=false.

Windows development **296,630,856 bytes**,0 errors/0 warnings; hidden9/9 rendered cases PASS. Все10 observable budgets PASS; максимум **2,715,548 triangles**. GPU/render-thread/batches UNAVAILABLE, profile ACTION_REQUIRED; это не FPS/min-spec qualification. Известное RenderTexture.active предупреждение при shutdown остаётся. [Evidence](../quality/g2/lighting-a/), [derivation](../licensing/g2_visual/lighting-derivation.json). Rebuild: scripts/Build-G2Lighting.ps1.

Осмотрены clear/dawn/overcast/crowd close и остальные альтернативные ракурсы. Контраст улучшен, но фон/горизонт и облака ещё простые; свет не устраняет angular athlete joints, механические движения, повторяемость толпы и пустоты композиции. Далее008 combined owner review с возвращением к002/003, честной рекомендацией и G2 STOP. M13/WP167 BLOCKED_G2; OWNER-FEEL-001 OPEN, без скрытого tuning.
