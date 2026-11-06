# М16.7 — спортсмен: анатомия, движения и контакт с экипировкой

**М16.7 открыт. Новый билд отложен по указанию владельца.** Предыдущая шея и
самостоятельный студийный цикл отклонены. Технические тесты не означают
художественную приёмку или достижение качества платного референса.

## Текущий кандидат

Пути относительно `Assets/Biathlon/Characters/Biathlete_16_7/`:

- `Models/Source~/Biathlete_16_7_Natural.blend` — редактируемая модель с отдельной
  геометрией тела/одежды/экипировки и упакованными картами.
- `Models/Biathlete_16_7_Natural.fbx` — производный экспорт, 24 кости и три LOD.
- `Prefabs/Biathlete_16_7_Natural.prefab` — действующий кандидат студии.
- `Preview/Biathlete_16_7_Preview.unity` — шесть ракурсов, четыре технические
  позы, движения основного спортсмена, пауза, скорость, точная фаза и выбор
  равнины/подъёма/спринта/стойки спуска. ПКМ вращает камеру, колесо приближает.
- `Documentation/natural-asset-manifest.json` — экспортные числа и SHA256 FBX.
  Unity удаляет вырожденные треугольники, поэтому фактические импортные числа
  отдельно измеряются тестами.
- `Models/Source~/MakeHuman/Natural/` — исходная анатомия и официальная кожа CC0.
- `Models/Source~/PolyHavenRifle/` — скачанная винтовка CC0, карты и provenance.
- `Documentation/contact-adaptation-v1.json` — настройки визуального контакта.

Старые файлы без суффикса `Natural` сохранены как прежний технический checkpoint.
Корневая `MakeHuman/athletic_fitted.blend` — отклонённая подгонка к старым костям,
сохранённая для воспроизводимого отрицательного теста. Она не используется
генератором нового кандидата. Старые Windows-билды не показывают текущую модель.

## Анатомия и материалы

Голова, шея, тело и кисти взяты из единой CC0-анатомии MakeHuman hm08. Пропорции
сохранены после равномерного масштаба до 1,80 м и перемещения. Позиции костей
следуют анатомии. Отрезанная/зажатая старая челюсть и внутренняя трубка шеи не
используются. Ворот костюма — продолжение той же поверхности, с узкой кромкой.
Кожа — официальный diffuse 2K; ткань — карты 4K. Это не скан и не mocap.

Нагрудник вырезан из гладкой анатомической поверхности. Веса швов/ремней/номера
интерполируются по ближайшему треугольнику ткани; молния следует передней
поверхности. В студии фиксируется LOD0 для осмотра, переходы LOD требуют
отдельной проверки перед установкой в гонку.

## Движения и экипировка

`Biathlete167MainMotion` вызывает действующие `AthleteHybridLocomotionDriver`,
`AthleteRiflePresentation`, `AthleteGroundContact` и `AthleteGripPresentation`
основного G2. Невидимый экземпляр G2 служит источником позы. Базовый перенос
сохраняет направления конечностей, длины костей и неподвижный корень; новый
самостоятельный лыжный цикл для кандидата не создаётся.

Поверх переноса применяются явные поправки для новых пропорций: ограниченный
изгиб кисти, пронация предплечья, разнос рукояток и носовой шарнир ботинка.
Лыжа сохраняет положение, ботинок может поднять пятку вокруг носка; манжета
следует голени. Рукоятка подогнана к четырём согнутым пальцевым цепочкам,
контактные точки явно сериализованы. Пальцы пока сформированы офлайн; полноценного
runtime finger rig нет. Остаточные художественные дефекты оцениваются по кадрам,
а не только по численным ограничениям.

Винтовка использует скачанную CC0-модель Poly Haven (Mateusz Sadek), сохранённые
UV/PBR, переделанную внешнюю спортивную компоновку, диоптр, защитную крышку,
держатели и крепления. Это игровой декоративный реквизит; он не заявляется
точной копией Anschütz. Платная CGTrader-модель служит только ориентиром качества.

## Воспроизведение

Официальный Blender 4.5.15 LTS. Из корня проекта:

```powershell
& $blender --background --python-exit-code 2 --python scripts/art/m16_7/anatomy_prepare.py -- --source Assets/Biathlon/Characters/Biathlete_16_7/Models/Source~/MakeHuman --audit docs/quality/m16-7/source-audit.json --output artifacts/m16-7-refinement/natural-anatomy --natural
& $blender --background --python-exit-code 2 --python scripts/art/m16_7/build_biathlete.py -- --source C:/Users/snkashkin/Downloads/Biathlete_Downloader_Windows/BiathlonSourceAssets --anatomy Assets/Biathlon/Characters/Biathlete_16_7/Models/Source~/MakeHuman/Natural/athletic_fitted.blend --output artifacts/m16-7-refinement/natural-asset --review artifacts/m16-7-refinement/equipment-review
```

После просмотра явно скопировать Natural FBX, `.blend`, manifest и новые карты
из результата в соответствующие каталоги Assets. Дать Unity импортировать и
скомпилировать код, затем вызвать `Biathlete167Authoring.GenerateNatural()`.
Не экспортировать прямо поверх открытых Unity/Git файлов Windows.

## Проверки и оставшаяся работа

Актуальные результаты и настоящие кадры Unity/Blender:
`docs/quality/m16-7-natural/`. Отдельно проверяются исходная форма шеи,
соответствие FBX/manifest, базовый перенос 12 состояний, контактные поправки,
длины/корень, повторяемость фаз, нулевые allocations после прогрева и студийный UI.
Все новые изображения — реальные рендеры геометрии, без image generation.

Остаются художественная доводка одежды/пальцев/винтовки, полный стрелковый цикл
стоя/лёжа, снятие/надевание винтовки, рельеф и поле из 30 спортсменов. Основная
гонка пока использует G2. Симуляция, баланс, выстрелы, карьера и сохранения не
изменены. Новый билд отложен: сначала требуется довести и визуально проверить сам ассет.


## Хват всей ладонью — 2026-11-06

По последнему замечанию владельца хват на спуске переделан: верх рукоятки лежит
диагонально в ладони, все четыре пальца замкнуты вокруг неё, большой палец
охватывает её с другой стороны. Это полный ладонный хват; прежний щипковый хват
отклонён. Верхний колпачок находится рядом с указательным пальцем. Геометрия
рукоятки, кисти и их контактные системы экспортируются вместе.

`power_grip.py` решает три анатомических сустава каждого пальца офлайн.
Критерий приёмки проверяет фактическую поверхность сохранённой перчатки:
касание ладони и всех пяти пальцев, верхние 30 мм рукоятки для большого и
указательного пальцев, 73 луча внутри рукоятки, 32 направления закрытого темляка.
Результат текущего исходника: ладонь в пределах 0,60 мм; пять пальцев в пределах
2,51 мм; пересечений внутренних лучей нет; темляк охватывает запястье полностью.
Старая щипковая поза сохранена в `Models/Source~/Regression/rejected_pinching_end.blend`:
этот же тест на ней закономерно не проходит (ладонь отстоит на 33 мм).

Направление ладони контролируется отдельными точками на поверхности. Это
ловит прежнюю ошибку знака оси при конверсии Blender → FBX → Unity. В стойке
спуска проверяются изгиб запястья, отсутствие взаимного пересечения кистей,
положение локтей, зазор палок относительно предплечий и настоящей skinned-сетки.
Во всех 12 состояниях проверяются постоянство длин костей, неподвижность корня,
контакт носка с креплением и отсутствие аллокаций после прогрева.

Профильный прогон: 57 PASS. Общий PlayMode-прогон и итоговый журнал находятся
в `docs/quality/m16-7-natural/qualification.json`. Это техническая проверка;
М16.7 остаётся открытым, качество уровня референса не объявлено достигнутым.
Настоящие кадры: `comparison/hands-Tuck-0.png`,
`comparison/hands-dorsal-left-Tuck-0.png`, `comparison/Tuck-0.png`.

Воспроизводимая проверка (из корня проекта, Blender 4.5.15 LTS):

```powershell
& $blender --background --python-exit-code 2 --python scripts/art/m16_7/verify_hand_contacts.py -- --asset Assets/Biathlon/Characters/Biathlete_16_7/Models/Source~/Biathlete_16_7_Natural.blend --anatomy Assets/Biathlon/Characters/Biathlete_16_7/Models/Source~/MakeHuman/Natural/athletic_fitted.blend --output artifacts/grip-green.json
# Отрицательная проверка: заменить --asset на Models/Source~/Regression/rejected_pinching_end.blend (полный путь). Ожидается exit 2.
```


2026-11-06 final verification: 69 native PlayMode PASS (57 asset/rig cases plus affected regression checks). Expanded 686-case run was cancelled after a saved 161-case snapshot (158 PASS/3 FAIL); all three found failures are fixed and included in the 69-case retest. Full-assembly PASS is not claimed. Details: docs/quality/m16-7-natural/regression-followup.json. BUILD HOLD; M16.7 remains open.

## Актуальное дополнение — кисти и дистальные участки, 6 ноября

Прежняя ориентация кисти отклонена владельцем. Теперь осевое вращение
распределено на предплечье: в спуске скручивание кисти <=0,087°, изгиб <=21,98°.
Новые независимые точки ладони/костяшек и проверки 12 состояний × 96 фаз
ловят ошибку, которую пропускало прежнее ограничение одного изгиба.

Подгонка пальцев использует фактическую Catmull-Clark-поверхность. Ладонные
дистальные участки всех пяти пальцев касаются верхней рукоятки. Раздельные
участки хвата, противопоставление большого пальца, замкнутые темляки и
взаимные пересечения проверяются на сохранённой геометрии. Исходная анатомия
тела не изменена; тыльная оболочка перчатки не двигает ладонные участки.
Добавлены материалы Race_Glove_Softshell / Race_Glove_Palm_Grip с явной URP-
привязкой. Повторная генерация открытой чистой студии работает; несохранённая
сцена защищена проверкой.

Текущий результат: 71 PlayMode + 2 EditMode PASS. Действующие изображения,
обычное/замедленное видео и SHA256 находятся в `docs/quality/m16-7-wrist/`.
Предыдущая папка m16-7-natural сохранена как история отклонённого кандидата.
Это не художественная приёмка. Перчатки/кромки ткани, стрелковые позы,
переходы, рельеф/LOD/поле остаются открытыми. М16.7 OPEN, BUILD HOLD.

## Последнее исправление рабочего хвата — 6 ноября

Ручка перед пальцами на рабочих кадрах была настоящим дефектом: прежняя
подгонка по костям оставляла подушечки на расстоянии 7–20 мм, а строгий тест
проверял только спуск. Теперь рабочая поза и спуск используют одну подгонку
по поверхности и проходят одинаковую проверку ладони/пяти пальцев/подушечек,
рукоятки, темляков и пересечений. Большой палец перенесён к верхнему краю;
прежний тест его положения сохранён и проходит. Старый рабочий хват сохранён
как rejected_working_grip.blend и закономерно не проходит новый тест.

Итог: 82 PlayMode + 3 Editor PASS, 6 положительных и 2 отрицательных проверки
исходника PASS. Генератор сохраняет служебные данные HumanDescription при
повторном импорте; регрессия ошибки meta воспроизведена и исправлена.
Актуальные 51 PNG, один отклонённый кадр и полные циклы нормальной/0,25× скорости:
docs/quality/m16-7-wrist. Полный прогон сборок тестов не объявляется пройденным.
М16.7 OPEN, BUILD HOLD; художественная доводка и интеграция в гонку остаются.

```powershell
& scripts/art/m16_7/verify_grips.ps1 -BlenderPath $blender
```


2026-11-06 M16.7 clothing checkpoint: UV-printed bib replaces floating glyphs; GDI winding preserves overlapping contours. Shared suit/bib partition and normalized weights remove independent-layer cuts; covered runtime triangles excluded with full editable underlay retained. Surface-following harness, matte textile and continuous panels refined. Tuck pelvis presentation lift6cm with both foot bindings restored by IK removes torso/thigh crossings. Actual imported BakeMesh16poses:0strict interior garment crossings, negative former poses reject; source layer/bitmap negative fixtures retained.86PlayMode+3EditMode PASS, source grip6positive+2expected-negativePASS. ActiveLOD0149094tri vs197883; allstored229616, no field FPS claim. Evidence/actual videos and source hashes: docs/quality/m16-7-cloth/qualification.json. Full assembly and final art acceptance not claimed. Race G2/authoritative gameplay/balance/shot geometry/career/saves unchanged. BUILD HOLD, no new Windows player; M16.6 CLOSED, M16.7 OPEN, M17 not started. Equipment follow-up with owner downloaded CC-BY Skiing Lady and independently authored reference rifle remains active. Scoped publication pending; historical STATE bytes preserved.


2026-11-06 M16.7 equipment/owner eyewear choice checkpoint: original sports glasses retained as default, saved source6parts have0vertex displacement. Optional mask derives selected lens/frame/padding from Skiing Lady by lancehuang526(CC-BY-4.0), welded/refined/fitted, with project strap. Studio dropdown switches actual imported renderers exclusively through all poses/4motion techniques. Donor alpine boots/rig/animation excluded; Nordic toe hinge and verified full-palmar grip retained. New PULSE4K ski surface graphics, carbon poles, ferrules and correctly oriented3spoke baskets. Independently authored broad sport rifle furniture/cheek rest/butt/sights/4magazine props follows owner animod visual reference; no paid source mesh copied, no exact likeness/art acceptance claimed. Natural wood material isolated from legacy.
Windows loaded-meta truncation reproduced Win321224; byte-identical atomic replacement immediately before Unity settings write preservesGUID/settings, same repeated-authoring regression now PASS and checks current remap persisted to disk. Mapping holder unconfirmed; no error suppression. Final89PlayMode+3EditMode PASS, Console after427296 has0newError/Exception/Assert. Source grip6positive+2expected-negativePASS; actual equipment bitmap/geometryPASS, clipped-print negative rejects; BakeMesh16poses0strict interior cloth crossings. Current source SHA256/evidence in docs/quality/m16-7-equipment/qualification.json. Full assembly/continuous collision/field FPS not claimed. Previous clothing checkpoint621964599a7765f412799ed2b39002306e975e61 confirmed on origin;17LFS objects66MB uploaded. Historical STATE/QA preserved. M16.6 CLOSED; M16.7 OPEN; BUILD HOLD/no new Windows player; raceG2/authoritative gameplay/balance/AI/shooting geometry/career/saves unchanged. Next: further artistic equipment/cloth polish, shooting transitions and separate terrain/LOD/field qualification. Equipment scoped GitHub publication pending.


2026-11-06 M16.7 solid mask webbing and walnut polish checkpoint: optional mask now has1.6mm closed solid cap-fitted webbing, woven PULSE print, temple clips and curved adjustment buckle. Source/actual imported volume checks reject the former flat strip; retained small rejected_flat_mask_webbing.blend has322boundary edges,14.18mm rear gap/4.4mm penetration. Current0boundary edges,20.16cm3volume,500rear vertices with0.267–2.395mm outside-cap clearance. Original glasses6parts remain vertex-exact and selectable. Rifle furniture uses CC0 Natural Walnut Veneer by Jenelle van Heerden/Poly Haven with preserved2Ksource/hash/license, longitudinal UV, warm stain, satin finish and softened aperture rims. Independently found floating magazine/cheek/brace fittings corrected: all10actual decorative mounts now overlap real stock; old8failures and intermediate upper-mount failure retained, no tolerance relaxed.
Final91PlayMode+3EditMode PASS/0FAIL/0SKIP; Console after427369 has0newError/Exception/Assert. Source grip6positive+2expected-negativePASS; actual equipment geometry/printPASS; BakeMesh16poses0strict interior cloth crossings. Current source/evidence SHA256 in docs/quality/m16-7-polish/qualification.json; actual Unity close views retained. Full assembly, continuous collision, field FPS and final art/reference equivalence not claimed. Lens/padding, rifle harness/cloth folds and full shooting/terrain/LOD/field qualification remain open. Historical STATE/QA preserved. M16.6 CLOSED; M16.7 OPEN; BUILD HOLD/no new Windows player. RaceG2/authoritative simulation/balance/AI/shot geometry/career/saves unchanged. Scoped GitHub publication pending.


2026-11-06 — исправлены отклонённые владельцем пропорции винтовки. Общая ширина31,79→16,47см, толщина ложи45→32,4мм, длина относительно спортсмена60,6→56,4%. Вместо массивной верхней оболочки — собственная тонкая круглая внешняя сетка ствола. Посадка сбоку от головы проверена по фактическому BakeMesh в12состояниях/4фазах; промежуточное пересечение головы воспроизведено новым тестом и устранено.93PlayMode+3EditModePASS,10крепленийPASS; хват/очки/маска/печать/одежда регрессий не показали. Свежие реальные кадры и проверки: docs/quality/m16-7-rifle-scale/REVIEW_RU.md. М16.7 OPEN, BUILD HOLD; следующий шаг — ремни винтовки и оставшаяся художественная/стрелковая/полевая доводка.


2026-11-06 — по замечанию владельца винтовка возвращена по центру спины, устранено погружение ложи в таз. Уменьшенные размеры сохранены; источник и back socket следуют HumanoidSpine. Добавлены4объёмные перемычки плечевого подвеса.49поз,177135лучевых попаданий: минимальный зазор10,08мм; проверка головы/шеиPASS.96PlayMode+3EditModePASS,4подвеса и10крепленийPASS, хват/очки/маска/одежда без обнаруженных регрессий. Свежие реальные кадры: docs/quality/m16-7-centred-carry/REVIEW_RU.md. М16.7OPEN/BUILD HOLD, нового Windows-билда нет; остаётся художественная доводка/полноценная стрельба/полевая интеграция.


2026-11-06 — актуальная версия: отдельный объёмный биб поверх полного комбинезона, собственные карты ткани, семь комплектов NOR/SWE/FIN/GER/FRA/ITA/RUS из палитры игры. В студии доступен выбор сборной; очки/маска остаются отдельным выбором. Лямки винтовки непрерывно возвращаются под руками к заднему подвесу; пряжки закреплены на фактической ткани.104профильных Unity-теста PASS,16импортированных поз без строгих пересечений биб/комбинезон. Актуальные реальные кадры и ограничения: docs/quality/m16-7-textile-kits/REVIEW_RU.md. М16.7 OPEN, BUILD HOLD, гонка продолжает использовать G2.
