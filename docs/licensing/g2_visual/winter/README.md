# Зимняя одежда владельца — audit 2026-09-24

Staging: `external_downloads/g2_visual_remediation/winter`. Путь с `external/_downloads` в сообщении был опечаткой; использована существующая папка. Owner разрешил использовать все варианты.

Официальный публичный Sketchfab API подтвердил **CC BY 4.0**, commercial use allowed / author must be credited. [Сохранённые metadata](sources.json), [хэши всех staged файлов](source-files.json), [derivation manifest](derivation.json).

| Staged комплект | Автор / источник | Использование |
|---|---|---|
| black-leather-coat-3d-model-free | [iGauravRajput](https://sketchfab.com/3d-models/black-leather-coat-3d-model-free-d12f555e66a14cc287baec58f5ed5c70) | Пальто; количество треугольников 18161 совпадает с API, GLB generator Tripo. Исходные textures/materials и текст внутренней этикетки исключены. |
| puffer-jacket | [Kaze / ChillKrucz](https://sketchfab.com/3d-models/puffer-jacket-8703efe71fd140539787dabe494cc415) | Обе FBX-модели Matte/Shiny: отдельные варианты с разной гладкостью нейтрального материала. |
| warm-jacket | [grs / sgrosjean](https://sketchfab.com/3d-models/warm-jacket-703dbac11fe44edebd4c895860d2b602) | Куртка из model.zip/Collada. Несмотря на текст описания автора «cc0», соблюдается явно выставленная API-лицензия CC BY 4.0. |
| Нет соответствующего staged файла | [Arctic Explorer Snow Coat — ChloeRobynSmith](https://sketchfab.com/3d-models/arctic-explorer-snow-coat-3b757b5833b84bc699a1ea87fd069406) | Ссылка владельца проверена, CC BY 4.0. METADATA_ONLY / NOT IMPORTED. Не выдавать её за один из трёх локальных комплектов. |

Публичный API использован после HTTP403 у текстового web reader страницы; авторизация не обходилась, использованы открытые model metadata и search. Модели проверены локальным Blender, source-файлы не изменялись. Два варианта куртки и два других фасона дают 4 формы одежды × 2 лица = 8 новых вариантов, плюс прежние 2 = 10. Исходная связная геометрия одежды и рукавов сохранена; рукава курток преобразованы в сидячую позу с непрерывными весами двух сегментов. Пальто сохраняет опущенные рукава; положение исходных перчаток согласовано с манжетами. Near LOD сохраняет геометрию одежды, far LOD сокращён. Для этих новых вариантов движение рук отключено до полноценного skinning, повороты головы сохранены. Исходные texture/label/materials не используются, материалы нейтральные и без брендов. Складки и детали source geometry сохраняются, текстурный PBR исходников в этой интеграции не перенесён.

Одежда CC BY 4.0; лица Renderpeople имеют отдельные ограничения. **Комбинированные meshes и face textures остаются в ignored RestrictedG2Crowd**, не публикуются в Git. В репозитории — recipe, hashes, attribution и rendered review evidence. При дальнейшей передаче адаптированной одежды сохранять CC BY attribution и notice изменений, не смешивать лицензии одежды и лиц.

Техническая интеграция не означает visual acceptance: посадка рукавов, материалы и повторяемость лиц остаются предметом G2 review.
