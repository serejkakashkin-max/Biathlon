# G2 — дополнительные доработки после008A

2026-09-23. Выполняется [поручение владельца](../owner/G2_FURTHER_REMEDIATION_2026-09-23.md) по исправлению недостатков008A. G2 NOT APPROVED, M13/WP167 заблокированы. Отчёт дополняется по завершённым checkpoints; завершение одной части не означает выполнения всей задачи.

## 009A — внешность, экипировка и вариативность

Очки уменьшены по ширине/высоте, глубина посадки скорректирована. Сохранена только ранее проверенная CC BY geometry, без исходных texture/logo atlas. При первом осмотре выявлена пропавшая линза: новый shader не учитывал двусторонность source geometry. Исправлен Cull material state, повторный close capture показывает целую линзу. Harness отнесён от bib surface на12mm, чтобы снизить пересечения; сложная форма ремней ещё требует работы009C.

Новый AthleteSurface разделяет ткань, кожу, composite и angular lens tint. Использует имеющиеся CC0 micro normals; локальные оттенки щёк/губ и швы привязаны к bind-space coordinates, поэтому не скользят в мире при деформации. Никаких реальных маркировок или новых сторонних источников. Цвет кожи остаётся упрощённым; фотореалистичный face asset этим не заявляется.

Шесть вымышленных kit palettes выбираются по уже существующему визуальному roster ordinal. Они не меняют athlete IDs, параметры или simulation RNG. Bib остаётся независимо пронумерованным. Первая реализация с MaterialPropertyBlock нарушила SetPass budget200 в6из10 cases (до348). Она отвергнута: теперь ограниченный общий cache материалов используется всеми спортсменами одной палитры, bib material не клонируется. Замер повторён с теми же бюджетами.

Дополнительно исправлен дефект скрытого profiler: проверялась величина Screen вместо фактического offscreen RenderTexture. При desktop/window ограничениях это останавливало run несмотря на правильно выделенный target1920×1080. Теперь width/height берутся из target; window_width/window_height сохраняются отдельно, PNG dimension проверен тестом с прямоугольным96×64 target. Aspect камеры явно16:9 и записан в evidence: CPU culling должен учитывать весь offscreen frustum, даже при604×1080 скрытом окне. Финальный player/profile подтверждает исправление. Это не изменение workload/budgets или попытка объявить GPU PASS. Исходный rejected run сохранён.

Проверки: полный PlayMode146/146 в139.930s, целевые appearance2/2, hero5/5, profiler3/3; Windows hidden9/9;10observable profile cases без FAIL. GPU/render-thread/batches по-прежнему UNAVAILABLE. [Evidence](../quality/g2/appearance-a/). Полный финальный прогон включает новую внешность, размер offscreen target и existing race/animation integration; последующее явное16:9 подтверждено финальным player/profile. Console0errors/1MCP lifecycle warning, shader has_error=False. Authoritative gameplay и shooting tuning не менялись. OWNER-FEEL-001 OPEN.

Далее009B: симметричное отталкивание палками, перенос веса и проверяемый контакт при движении;009C: суставы и rifle-body fit. Hero visual target остаётся OPEN до новой интегрированной проверки.
