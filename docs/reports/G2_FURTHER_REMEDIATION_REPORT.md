# G2 — дополнительные доработки после008A

2026-09-23. Выполняется [поручение владельца](../owner/G2_FURTHER_REMEDIATION_2026-09-23.md) по исправлению недостатков008A. G2 NOT APPROVED, M13/WP167 заблокированы. Отчёт дополняется по завершённым checkpoints; завершение одной части не означает выполнения всей задачи.

## 009A — внешность, экипировка и вариативность

Очки уменьшены по ширине/высоте, глубина посадки скорректирована. Сохранена только ранее проверенная CC BY geometry, без исходных texture/logo atlas. При первом осмотре выявлена пропавшая линза: новый shader не учитывал двусторонность source geometry. Исправлен Cull material state, повторный close capture показывает целую линзу. Harness отнесён от bib surface на12mm, чтобы снизить пересечения; сложная форма ремней ещё требует работы009C.

Новый AthleteSurface разделяет ткань, кожу, composite и angular lens tint. Использует имеющиеся CC0 micro normals; локальные оттенки щёк/губ и швы привязаны к bind-space coordinates, поэтому не скользят в мире при деформации. Никаких реальных маркировок или новых сторонних источников. Цвет кожи остаётся упрощённым; фотореалистичный face asset этим не заявляется.

Шесть вымышленных kit palettes выбираются по уже существующему визуальному roster ordinal. Они не меняют athlete IDs, параметры или simulation RNG. Bib остаётся независимо пронумерованным. Первая реализация с MaterialPropertyBlock нарушила SetPass budget200 в6из10 cases (до348). Она отвергнута: теперь ограниченный общий cache материалов используется всеми спортсменами одной палитры, bib material не клонируется. Замер повторён с теми же бюджетами.

Дополнительно исправлен дефект скрытого profiler: проверялась величина Screen вместо фактического offscreen RenderTexture. При desktop/window ограничениях это останавливало run несмотря на правильно выделенный target1920×1080. Теперь width/height берутся из target; window_width/window_height сохраняются отдельно, PNG dimension проверен тестом с прямоугольным96×64 target. Aspect камеры явно16:9 и записан в evidence: CPU culling должен учитывать весь offscreen frustum, даже при604×1080 скрытом окне. Финальный player/profile подтверждает исправление. Это не изменение workload/budgets или попытка объявить GPU PASS. Исходный rejected run сохранён.

Проверки: полный PlayMode146/146 в139.930s, целевые appearance2/2, hero5/5, profiler3/3; Windows hidden9/9;10observable profile cases без FAIL. GPU/render-thread/batches по-прежнему UNAVAILABLE. [Evidence](../quality/g2/appearance-a/). Полный финальный прогон включает новую внешность, размер offscreen target и existing race/animation integration; последующее явное16:9 подтверждено финальным player/profile. Console0errors/1MCP lifecycle warning, shader has_error=False. Authoritative gameplay и shooting tuning не менялись. OWNER-FEEL-001 OPEN.

Далее009B: симметричное отталкивание палками, перенос веса и проверяемый контакт при движении;009C: суставы и rifle-body fit. Hero visual target остаётся OPEN до новой интегрированной проверки.

## 009B — лыжный перенос веса и контакт палок

Обе руки теперь отталкиваются парно. На равнине две пары отталкиваний на полный цикл ног, на подъёме одна; частота плавно переходит при смене состояния. Усилены боковой перенос таза и небольшая вертикальная/корпусная фаза. Это presentation approximation skating techniques, не сертифицированная спортивная анимация: асимметрия V1 и полный силовой контакт ещё упрощены. Описание uphill G2/V1 сверено с [первичным исследованием](https://journals.plos.org/plosone/article?id=10.1371%2Fjournal.pone.0197592).

Краткий контакт наконечника удерживается в world space, пока коррекция кисти укладывается в10cm. При недостижимом хвате палка освобождается до следующего recovery; перемотка, телепорт, большой временной разрыв и стрельба сбрасывают контакт. Preview использует тот же путь. Никаких gameplay forces/root-motion. Все новые параметры вынесены в versioned ski_technique_visual_v1.

Проверено через Unity MCP:149/149 PlayMode(138.217s), целевые3/3, затем3/3 motion/preview после подключения preview. Контакт, геометрическая длина палки, перенос веса, сбросы, отсутствие warm allocations покрыты тестами. Hidden Windows9/9, build0errors/0warnings;10profile cases без observable budget FAIL, main-thread p95 максимум1.443ms. GPU/render-thread/batches UNAVAILABLE. Console после build0errors/0warnings. [Кадры, клип24fps и результаты](../quality/g2/ski-technique-b/) сохранены; частота клипа не является FPS измерением. Движение стало согласованнее, но остаётся процедурным; G2-VIS-001 не закрыт. Authoritative shooting/race не менялись.

## 009C — объём суставов и посадка головы у приклада

Четыре локальные blendshape corrections в каждом из3LOD компенсируют часть LBS-сужения локтей/колен. Дельты до14mm для рук и18mm для ног затухают за пределами зоны сустава; затронута только ткань, без лица/оборудования. Runtime читает фактический угол сгиба после окончательного IK, сохраняет кости, root и sockets. В rest pose веса нулевые. Это ограниченная компенсация объёма, не полноценная anatomical deformation: сложные складки и плечевой пояс остаются упрощены. Recipe G2JointVolumeAuthoring воспроизводит изменения из прежних аудированных CC0 derivatives.

Голова посажена на50mm ниже относительно cheek rest и получает12° бокового наклона. Пробный8° pitch был отклонён существующим тестом направления взгляда; pitch удалён, исходный failing result сохранён. Rifle forward, butt socket, физический sight и hit resolver не меняются. Близкие standing/prone кадры повторно осмотрены: разрыв у приклада уменьшен; финальное качество ещё оценивается в012.

Unity MCP: целевые2/2, полный151/151 в139.068s после исправления pitch. Тесты проверяют всеLOD, bounded deltas, rest, фактическую baked deformation, сохранение sockets и нулевые warm allocations; существующие stance/sight/transition проверки сохранены. [Evidence](../quality/g2/joints-c/). Новый Windows/profile будет измерен вместе с010–011 в012;009B остаётся последним player profile, не выдаётся за проверку009C. Authoritative race/shooting и баланс не менялись.

## 010 — силуэты и реакции зрителей

Собственные зимние куртки получили талию/объём простёжки и отличающийся профиль двух вариантов. Предплечья/перчатки имеют отдельный elbow pivot с затухающим влиянием у шва; после сгиба локтя применяется движение плеча. Голова и шапка поворачиваются вместе, ширина силуэта слегка варьируется по позиции. Плотность168/quality tiers и прежние culling/LOD сохраняются. Near/far бюджеты3500/1400tri на персонажа соблюдены.

Unity MCP4/4 целевых tests, включая новый контроль shoulder/elbow channels, existing bounds/face-only channels/density/zero-GC/captures. [Кадры и проверки](../quality/g2/crowd-b/). В close views остаются повтор двух лиц, простая одежда и отдельные сегменты рукавов: G2-VIS-003 улучшен, но не объявлен финальным production crowd. Новых внешних sources нет. Restricted derivatives по-прежнему ignored; обновлены только публичные recipes/source-output hashes/render images. Общий player/profile следует в012.

## 011 — дальний рельеф, лес и сервисная зона

Benchmark получил собственный замкнутый снежный basin mesh4096tri за пределами маршрута,512 размещений существующего аудированного FirLOD2 с camera-frustum culling и без shadow pass, три оригинальных service shelters. Геометрия не содержит коллайдеров; трасса/контактная плоскость и production venues не меняются. Высота деревьев интерполируется по ячейкам рельефа с небольшим заглублением. Счётчик VisibleTrees включает дальний лес.

Benchmark-only fog distance в ясную погоду расширен в2.7раза, при облачности в1.5; снегопад сохранён. Weather owner восстанавливает базовые значения перед каждой коррекцией и исходные RenderSettings после выхода. Это осознанное изменение видимости дальнего плана, не изменение физической погоды. Первый пустой склон дополнен лесом; его регулярность уменьшена смещением групп.

Целевые2/2 и полный154/154 PlayMode(141.053s) PASS. Покрыты finite geometry, seam, outward winding, route clearance, collider absence, стабильный fog и0warm allocations при tree submission. Тест поймал NaN на внешнем крае из-за отрицательной float-погрешности sin(pi) перед дробной степенью; аргумент ограничен снизу нулём, failing evidence сохранён. [Кадры/результаты](../quality/g2/composition-b/). Рельеф пока слишком сглажен, лес разрежен, event composition требует художественного развития; G2-VIS-002 не закрыт. Следующий012: новый Windows/profile, combined evidence и owner gate.

## 009D — исправление ремней при финальном review

В012 close capture выявлены складки-«плавники» у плеч. Recipe независимо проецировал два края ленты на соседние поверхности, а противоположные совпадающие грани ухудшали нормали. Лента теперь строится от сглаженной центральной линии с общими весами на сечение, без дублированной обратной грани. ТриLOD пересозданы; updated hero derivation/runtime hashes сохранены. [Тот же close ракурс до/после, изолированный вид, тесты](../quality/g2/harness-d/).

Hero6/6 и полный155/155(144.852s) PASS. Новый тест запрещает совпадающие обратные треугольники ремней. Изображение стало ровнее; тонкая подгонка bib/harness на всех крайних позах остаётся художественным ограничением. Это найденный review defect, не скрытый tuning. Финальный player/profile012 повторяется после этой геометрии; прежние замеры не выдаются за окончательные.

## 012 — повторный integrated owner package

Финальные155/155 PlayMode, Windows hidden9/9,10cases без observable budget FAIL (max2791998tri,109SetPass,1246draw calls,main p95max1.571ms), build0errors/0warnings; Console после build0/0. GPU/render-thread/batches UNAVAILABLE. Галерея45matched008A/012pairs,48motionframes/2s,rewind equality;393buildfiles скопированы с проверкой SHA-256. [Полный review012](G2_REVIEW_012.md). Новых third-party sources нет; restricted payload не tracked.

Запрошенные checkpoints завершены, художественный target не объявлен достигнутым. **Recommend G2 FAIL / more remediation needed. STOP / WAITING_OWNER_G2.** M13/WP167 не начаты; OWNER-FEEL-001 OPEN.
