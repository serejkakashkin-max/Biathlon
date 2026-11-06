# M16.7 — проверка визуального эталона и первичных референсов

Дата исследования: 2026-11-05. Статус: художественная проверка и критерии следующей итерации; **достижение эталона не подтверждено**.

## Что действительно просмотрено

- Задание владельца: `docs/M16_7_OWNER_TASK.md`.
- Исходный фактический рендер: `docs/quality/m16-7/blender/side.png`.
- Промежуточная итерация с новым телом: `artifacts/m16-7-refinement/final-blender/front.png` и `side.png`. Несмотря на имя каталога `final-blender`, это не свидетельство художественной приёмки. Во время исследования продолжалась очистка одежды; замечания ниже относятся к просмотренным кадрам, а не автоматически к последующему экспорту.
- Открыты страница и публичное рекламное изображение [целевого CGTrader-персонажа](https://www.cgtrader.com/3d-models/character/man/biathlete-fully-equipped-rigged). [Просмотренный передний/задний ракурс](https://img-new.cgtrader.com/items/7466622/c7992ef451/biathlete-fully-equipped-rigged-3d-model-c7992ef451.webp). Геометрия и текстуры платного ассета не скачивались и не использовались.
- Непосредственно просмотрено официальное [фото IBU — Anaïs Bescond, Ruhpolding 2022](https://assets.biathlonworld.com/image/upload/v1642002611/Photo%20Database/2021-2022/World%20Cup/WC%206%20Ruhpolding/Women%20Sprint/Bescond120122jw037.jpg), опубликованное в [материале IBU](https://www.biathlonworld.com/news/elvira-oeberg-wins-ruhpolding-sprint/VEYF1WBN3GIAWcJdxe2Oq). Хорошо различимы кисти на рукоятках, мягкий воротник, складки подмышками/в паху, прилегание нагрудника, очки и плечевые ремни. Это источник конструкции экипировки; женскую анатомию не следует механически переносить на мужского персонажа.

## Первичные источники и их применение

| Источник | Что подтверждает | Как применять в модели |
|---|---|---|
| [Sportful, Custom Snow FW24, APEX Biathlon Jersey / APEX Suit](https://www.sportful.com/medias/Sportful-Custom-Snow-FW24.pdf?context=bWFzdGVyfGNvbnRlbnQtYXR0YWNobWVudHN8Mzg5ODgwMHxhcHBsaWNhdGlvbi9wZGZ8YUdSaUwyZ3lNeTg1TkRFMU9URXdOamcyTnpVd0wxTndiM0owWm5Wc1gwTjFjM1J2YlY5VGJtOTNYMFpYTWpRdWNHUm18MTc1ZjA5OGEwMzgxZDAwNjNhYmUzNzgxZDZmMTA5MzFhNzAwZTUyZDkxMGU3MmMxNzJkMTcyOWZiYjY5ZmNjMg) | Облегающий гоночный крой; плоские швы; отдельная вентиляционная зона спины; передняя молния; специальная вставка локтя у биатлонного варианта. | Осмысленные сшитые панели, тонкие швы и разные свойства тканей. Не рисовать одинаковую крупную клетчатую фактуру на всём костюме. PDF прочитан, страницы не удалось отрендерить через web-инструмент; не выдавать его за просмотренную фотосессию. |
| [LEKI HRC Race Shark](https://www.leki.com/int/en/HRC-Race-Shark/652901301105) | Плотная спортивная посадка перчатки и система непосредственного соединения кисти с рукояткой; эластичная манжета. | Тонкий, анатомический силуэт пальцев; локальный контакт ладони/большого пальца с рукояткой; отдельный ремешок. Это один допустимый тип системы, а не обязательная экипировка любого биатлониста. |
| [Fischer Carbonlite Skate](https://www.fischersports.com/us_en/carbonlite) | В коньковом ботинке различаются жёсткая манжета, шасси и внутренняя конструкция со шнуровкой. | Читать ботинок как лёгкую спортивную обувь с отдельной опорой голеностопа: тонкий носок, внешняя манжета, шарнир и перекрывающая застёжка. Не цельный резиновый сапог. |
| [ANSCHÜTZ / Deuter — системы переноски](https://shop.anschuetz-sport.com/en/Zubehoer/Tragegestelle/) | Анатомические плечевые ремни, мягкая подкладка и сетчатая контактная поверхность. | Два объёмных, но тонких плечевых ремня поверх одежды, непрерывный путь к переносимому предмету; отдельно показать подкладку и стропу. Только внешняя игровая геометрия, без воспроизведения внутренних механизмов. |
| [IBU, официальный фотоисточник выше](https://www.biathlonworld.com/news/elvira-oeberg-wins-ruhpolding-sprint/VEYF1WBN3GIAWcJdxe2Oq) | Реальные отношения размеров и контактов в движении. | Проверять не только симметричную стойку: одежду под поднятым локтем, сжатую ладонь, складки над бедром, положение ремней при наклоне. |
| [Nordiq Canada — Technique Development, Skating](https://nordiqcanada.ca/wp-content/uploads/L2T-OS-RM-step-4-Technique-development-skating-Nov-132c-2017.pdf) | Различает Offset, One Skate, Two Skate и Free Skate; акцентирует перенос веса, баланс и ритм. | Задать разные фазовые схемы, не заменять лыжный ход циклом ходьбы/бега. Документ содержит разборы полного цикла и контрольные списки техники. |
| [Nordiq Canada — библиотека технических видео](https://nordiqcanada.ca/fr/entraineurs-et-techniciens/entraineur/ressources-pour-entraineurs-pour-le-developpement-des-athletes/matrice-de-developpement-de-lathlete-videos-techniques/) | Прямой доступ к примерам One Skate / Two Skate и упражнениям на равновесие. | Использовать для покадровой сверки фаз. Наличие страницы проверено; полный просмотр всех роликов в этой проверке не выполнен. |
| [Team Aker Dæhlie — Technique](https://www.teamakerdahlie.com/technique/technique/) | Отдельные разделы V1, V2, V2 alternate, поворотов и спуска; техника зависит от скорости и условий. | Разделить демонстрацию skating, пологого хода, подъёма и tuck. Проверять направление переноса веса и опору, а не добавлять произвольное покачивание плеч. |

Фотографии, рекламные изображения и каталоги используются для наблюдения. Они не становятся текстурами игры; отсутствие запрета на просмотр не означает разрешения на включение чужих изображений/логотипов в распространяемый ассет.

## Что отличает эталон от текущей модели

В просмотренном CGTrader preview убедительность обеспечивают цельный силуэт, аккуратная посадка одежды, контролируемые складки в суставах, чистые границы панелей, читаемая промышленная конструкция экипировки и согласованные материалы. Увеличение числа треугольников без исправления этих признаков проблему не решает.

Важное уточнение: белый номерной блок эталона действительно занимает только часть торса; верх нагрудника красный и визуально сливается с формой. Поэтому небольшой белый блок сам по себе не является дефектом. Дефект старой модели — ощущение отдельной жёсткой коробки, отсутствие цельной ткани и плохое прилегание. Публичная A-поза эталона показывает раскрытые пальцы; она не служит доказательством правильного хвата палок или готовой лыжной анимации.

| Приоритет | Наблюдение в фактических кадрах | Конкретная правка | Критерий проверки |
|---|---|---|---|
| P0 | Старый side: перчатки вытянуты «когтями». Новый side: пальцы короче, но хват ещё неоднозначен. | Строить ладонь и пальцы относительно оси рукоятки; отдельно сгибать пальцы и противопоставлять большой. Не деформировать всю кисть одной общей дугой. Центр хвата должен определять socket палки. | Передний, боковой и нижний ракурсы: пальцы огибают рукоятку, она не проходит сквозь ладонь; при poling/recovery контакт не скачет. |
| P0 | Новый bib имеет плечевую часть, но края возле шеи/плеч зубчатые и пересекают костюм. | Чистая замкнутая топология пройм/выреза; единая поверхность одежды с тонкой окантовкой. Проецировать на тело с сохранением расстояния, а не оставлять рваный контур исходного remesh. | В шести ракурсах нет выступающих белых треугольников, дыр и пересечений. При поднятых руках край остаётся гладким. |
| P0 | Воротник остаётся вертикальной отдельной трубой; сзади кожа шеи обрезана ступенью. | Плавно соединить воротник с верхом костюма; подогнать по шее, опустить передний край под подбородок. Скрыть исходный срез кожи внутри одежды и сохранить непрерывную видимую шею. | На боковом/заднем крупном плане нет открытого среза кожи, щели и контакта воротника с челюстью при наклоне головы. |
| P1 | Новые бёдра и голени стали плавнее. На старом теле видны неровные переходы плеч/паха. | Сохранить улучшенную анатомию; поправлять одежду и weights локально. Контролируемые складки — на локтях, коленях, паху, а не волнистый контур всей конечности. | Нейтральная стойка + сгибание сустава: объём не схлопывается, силуэт не ломается в одном кольце. |
| P1 | Под bib на новом side видны свободно висящие серые линии; ремни плохо отделяются от декоративных швов. | Разделить тканевый шов, окантовку и плечевую стропу. Шов следует поверхности и её skin weights; ремень имеет ясные точки крепления. | Нет свободных концов у швов и «проволоки» через подмышку; плечевые ремни остаются на плечах при движении. |
| P1 | Слишком однородная блестящая ткань, жёсткие прямоугольные цветовые зоны на боках/паху. | Согласовать карту раскроя с анатомией; антиалиасинг границ в UV; отдельно низкоконтрастная микрофактура и крупные складки. Стропы, ткань, кожа, подошва, металлические детали имеют различимые roughness. | В игровом масштабе нет муара; при приближении видны ткань/шов, а не гигантская клетка. |
| P1 | Ботинки и очки читаются как крупные простые блоки; шапка гладкая как шлем. | Ботинок: отдельная манжета и тонкая оболочка, привязка носка к креплению. Очки: согласованный изгиб по лицу и дужки за ушами. Шапка: тонкая мягкая ткань, небольшой шов, без избыточной высоты. | Проверять профиль 1:1 и игровой ракурс; нет зазоров у висков, торчащих волос/полигонов, плавающей подошвы. |

## Измеримые инженерные ориентиры

Это предлагаемые художественные допуски для данного игрового ассета, **не размеры из регламента или антропометрические нормы**. Координаты генератора: метры, Z вверх, лицевая сторона по −Y.

- Старый bib занимал примерно Z=1.14–1.325. Не расширять белый цвет до плеч только ради «исправления»: важны цельность и посадка ткани.
- Верх старого воротника Z=1.56 пересекал челюсть; в просмотренном коде снижен до Z=1.522. Одна эта координата не устраняет цилиндрический силуэт и открытый срез шеи. Контроль — фактический профиль головы и шеи после новой анатомии.
- Для прилегающих слоёв начать с 2–5 мм геометрического зазора в нейтральной позе; проверять итог при деформации. Зазор нельзя применять как универсальную замену исправлению топологии.
- Для жёсткого socket рукоятки держать визуальное расхождение контакта кисть–ручка в пределах 3 мм в студийном крупном плане. При отпускании пальцев в recovery ремешок должен сохранять читаемое соединение.
- Носок ботинка/шарнир крепления должен быть общим опорным местом, когда пятка поднимается. Не вращать всю лыжу вокруг голеностопа вместе с ботинком.

## Приёмка движения и следующая проверка

1. Снять фактические front/side/back и крупные face/hands/boots в Unity, при одинаковом освещении сравнить с Blender. Отдельно проверить neutral, максимальное сгибание колена/локтя, перенос веса и разворот головы.
2. Для каждой демонстрируемой техники показать хотя бы полный цикл на обычной скорости и 0.25×. Для One Skate/V2 сверять каждое отталкивание ногой с парным poling; для Two Skate/V2 alternate — другой ритм, без случайного сдвига всех синусоид.
3. У опорной лыжи нет бокового скольжения относительно поставленной стопы; колено направлено согласованно с лыжей, корпус переносится над опорной ногой. Корпус не изгибается боковой дугой от одного позвоночного сустава.
4. Палка сначала устанавливается, затем работает и снимается с опоры; кончик не перемещается по земле как приклеенный к кисти маятник. При восстановлении палка свободна от снега.
5. Переносимая винтовка остаётся у спины; снятие/надевание, стоя и лёжа требуют отдельных контактных поз. Простая перепривязка предмета к руке не является доказательством реалистичной стрельбы.
6. Оценивать также игровой размер персонажа, LOD-переходы и стоимость skinned meshes. Студийная красота не заменяет runtime-проверку; технический PASS не заменяет художественную приёмку владельцем.

Следующая итерация должна сначала закрыть P0: непрерывная шея/воротник, чистые края одежды и настоящий хват. После этого — материалы и детализация оборудования. M16.7 остаётся открытым до фактической визуальной проверки последнего экспорта и решения владельца.


## Уточнение хвата владельцем — 2026-11-06

Последняя директива владельца: **полный ладонный охват верхнего конца рукоятки**.
Ранее рассмотренное фото расслабленного хвата на спуске не отменяет этого
требования. Щипковая поза признана ошибкой, даже когда большой и указательный
пальцы касаются рукоятки. Новый контроль требует контакта ладони и всех пяти
пальцев с настоящей поверхностью верхней рукоятки.

Дополнительные первичные источники, просмотренные при этой итерации:

- [Kanata Nordic, Downhill Skills](https://www.kanatanordic.ca/wp-content/uploads/Downhill-Skills.pdf): палки под руками, близко к корпусу; в низкой стойке руки перед подбородком, локти впереди коленей.
- [Swix — straps and handles](https://swixsport.com/us/article/poles/how-to-find-the-right-straps-and-handles) и [размеры темляков](https://www.swixsport.ca/pages/pole-strap-size-guide): соединение кисти с рукояткой и посадка темляка.
- [LEKI — Innovations](https://lekiusa.com/pages/innovations): конструкция соединения перчатки/темляка с рукояткой. Это конструктивный ориентир, а не копирование маркировки производителя.
- [Fischer — Sturla Holm Lægreid](https://www.fischersports.com/athlete/sturla-holm-laegreid): спортивные позы и экипировка. Фотографии не распространяются с ассетом.
- [Fischer Carbonlite](https://www.fischersports.com/us_en/carbonlite) и [Rottefella Xcelerator](https://rottefella.com/media/sparsh/product_attachment/Mounting_Instructions_Rottefella_Xcelerator_Pro_Classic_Skate.pdf): жёсткая спортивная обувь, высокая манжета, носовая точка крепления. Пятка освобождена, сама лыжа не сгибается вместе со стопой.
- [Poly Haven Bolt Action Rifle 7.62](https://polyhaven.com/a/bolt_action_rifle_7_62), Mateusz Sadek, [CC0](https://polyhaven.com/license): разрешённая исходная геометрия/карты для переработки игрового спортивного реквизита. Оригиналы и hashes сохранены.

Проверены настоящие Unity-кадры обеих кистей с ладонной и тыльной стороны,
общий профиль спуска и обычный хват. Результаты и текущие ограничения фиксируются
в `quality/m16-7-natural/REVIEW_RU.md`; тесты не объявляют ассет эталонным.

## Дополнение: скручивание кисти, 6 ноября

По [тренерским материалам Kanata Nordic](https://www.kanatanordic.ca/wp-content/uploads/Downhill-Skills.pdf)
и [снимкам Fischer](https://www.fischersports.com/athlete/sturla-holm-laegreid)
сверены стойка спуска, положение рук и палок под предплечьями. Фотографии
не включаются в распространяемый ассет. Положение кисти теперь оценивается
вместе с предплечьем: прежний тест изгиба не замечал осевое скручивание.
Фотографии помогают визуальному сравнению, но не являются источником
выбранных инженерных допусков в градусах/миллиметрах.

Текущие крупные планы, полный цикл четырёх техник в обычном и замедленном
воспроизведении, результаты 73 профильных native-тестов и ограничения:
`quality/m16-7-wrist/REVIEW_RU.md`. Это следующий промежуточный результат;
форма перчаток и кромки ткани ещё требуют художественной доводки.

Рабочий хват отдельно исправлен после замечания владельца: рукоятка должна
лежать внутри обхвата ладони, пальцы обнимают её, большой палец остаётся снаружи
у верхнего конца. Конструкцию соединения снова сверили с
[официальным материалом Swix о ручках и темляках](https://swixsport.com/us/article/poles/how-to-find-the-right-straps-and-handles).
Брендовая геометрия не копируется. Обе позы теперь проверяются на настоящих
поверхностях; полный цикл и 85 профильных native-тестов не заменяют визуальной
приёмки. Последний результат — quality/m16-7-wrist/REVIEW_RU.md.


2026-11-06 M16.7 equipment/owner eyewear choice checkpoint: original sports glasses retained as default, saved source6parts have0vertex displacement. Optional mask derives selected lens/frame/padding from Skiing Lady by lancehuang526(CC-BY-4.0), welded/refined/fitted, with project strap. Studio dropdown switches actual imported renderers exclusively through all poses/4motion techniques. Donor alpine boots/rig/animation excluded; Nordic toe hinge and verified full-palmar grip retained. New PULSE4K ski surface graphics, carbon poles, ferrules and correctly oriented3spoke baskets. Independently authored broad sport rifle furniture/cheek rest/butt/sights/4magazine props follows owner animod visual reference; no paid source mesh copied, no exact likeness/art acceptance claimed. Natural wood material isolated from legacy.
Windows loaded-meta truncation reproduced Win321224; byte-identical atomic replacement immediately before Unity settings write preservesGUID/settings, same repeated-authoring regression now PASS and checks current remap persisted to disk. Mapping holder unconfirmed; no error suppression. Final89PlayMode+3EditMode PASS, Console after427296 has0newError/Exception/Assert. Source grip6positive+2expected-negativePASS; actual equipment bitmap/geometryPASS, clipped-print negative rejects; BakeMesh16poses0strict interior cloth crossings. Current source SHA256/evidence in docs/quality/m16-7-equipment/qualification.json. Full assembly/continuous collision/field FPS not claimed. Previous clothing checkpoint621964599a7765f412799ed2b39002306e975e61 confirmed on origin;17LFS objects66MB uploaded. Historical STATE/QA preserved. M16.6 CLOSED; M16.7 OPEN; BUILD HOLD/no new Windows player; raceG2/authoritative gameplay/balance/AI/shooting geometry/career/saves unchanged. Next: further artistic equipment/cloth polish, shooting transitions and separate terrain/LOD/field qualification. Equipment scoped GitHub publication pending.


2026-11-06 M16.7 solid mask webbing and walnut polish checkpoint: optional mask now has1.6mm closed solid cap-fitted webbing, woven PULSE print, temple clips and curved adjustment buckle. Source/actual imported volume checks reject the former flat strip; retained small rejected_flat_mask_webbing.blend has322boundary edges,14.18mm rear gap/4.4mm penetration. Current0boundary edges,20.16cm3volume,500rear vertices with0.267–2.395mm outside-cap clearance. Original glasses6parts remain vertex-exact and selectable. Rifle furniture uses CC0 Natural Walnut Veneer by Jenelle van Heerden/Poly Haven with preserved2Ksource/hash/license, longitudinal UV, warm stain, satin finish and softened aperture rims. Independently found floating magazine/cheek/brace fittings corrected: all10actual decorative mounts now overlap real stock; old8failures and intermediate upper-mount failure retained, no tolerance relaxed.
Final91PlayMode+3EditMode PASS/0FAIL/0SKIP; Console after427369 has0newError/Exception/Assert. Source grip6positive+2expected-negativePASS; actual equipment geometry/printPASS; BakeMesh16poses0strict interior cloth crossings. Current source/evidence SHA256 in docs/quality/m16-7-polish/qualification.json; actual Unity close views retained. Full assembly, continuous collision, field FPS and final art/reference equivalence not claimed. Lens/padding, rifle harness/cloth folds and full shooting/terrain/LOD/field qualification remain open. Historical STATE/QA preserved. M16.6 CLOSED; M16.7 OPEN; BUILD HOLD/no new Windows player. RaceG2/authoritative simulation/balance/AI/shot geometry/career/saves unchanged. Scoped GitHub publication pending.


2026-11-06 rifle-scale correction: compared the owner's CGTrader full-body athlete, the owner's animod rifle exterior and public real-competition photography from the Polish Biathlon Federation. Previous broad profile was rejected; width31.79→16.47cm and stock thickness45→32.4mm. Removed old massive upper exterior; authored thin round capped display barrel, retained licensed receiver detail and walnut source. Carry moved beside the head after independently detecting an interim clipping defect. [Actual before/after and current Unity frames](quality/m16-7-rifle-scale/REVIEW_RU.md), with red/green proportion and actual-head/neck tests. References are view-only and not redistributed.96focusednativePASS qualifies these technical checks, not final artistic equivalence; M16.7 staysOPEN/BUILD HOLD.


2026-11-06 owner revision: previous side carry is rejected. Rifle centred over spine, body depth corrected, HumanoidSpine attachment replaces upper-chest binding. Added4solid shoulder-to-stock bridges; actual rear/side/tuck inspection and red/green geometry evidence in [centred-carry review](quality/m16-7-centred-carry/REVIEW_RU.md). Existing slimmer silhouette retained.99focusednativePASS and source gates qualify these technical checks; M16.7 remainsOPEN/BUILD HOLD.
