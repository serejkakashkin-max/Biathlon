# Локальная сборка G2 crowd

Renderpeople разрешает интегрированное использование в играх, но ограничивает распространение исходных и изменённых 3D data. Поэтому `Assets/Resources/Biathlon/RestrictedG2Crowd/` и его `.meta` исключены из Git. LFS для этих данных не используется. Публичный recipe не предоставляет лицензию на исходники.

Нужны полученные законным способом staged free models:

- `person_06/extracted/rp_eric_rigged_001_u3d.fbx` и `tex/rp_eric_rigged_001_dif.jpg`;
- `person_07/extracted/rp_carla_rigged_001_u3d.fbx` и `tex/rp_carla_rigged_001_dif.jpg`.

Базовая папка: `external_downloads/g2_visual_remediation/01_characters/crowd_people/renderpeople/`. Источники: [free models](https://renderpeople.com/free-3d-people/), [terms](https://renderpeople.com/general-terms-and-conditions/). Исходные preview-фотографии и texture atlases в публичный репозиторий не копировать.

При открытой Unity из корня проекта выполнить `./scripts/Build-G2Crowd.ps1`. Нужен Blender4.5.9; можно передать его путь через `-BlenderPath`. Скрипт проверяет ignore/tracked status перед импортом, работает в скрытом background process, создаёт локальные derivatives и native Unity assets. Исходные офисная одежда, обувь и часы удаляются полностью; используется только голова/верх шеи. Зимняя одежда — собственная, но комбинированная модель всё равно остаётся restricted derivative.

В новом checkout до этой операции G2 VisualBenchmark явно завершает инициализацию ошибкой с инструкцией генерации; нет молчаливой замены на старую толпу или missing-GUID fallback. Legacy crowd fixtures по умолчанию используют собственные модели. G2 tests требуют локальные licensed assets.

Перед push: `git ls-files Assets/Resources/Biathlon/RestrictedG2Crowd` должен быть пуст. Перед release повторно проверить упаковку: только integrated compiled content, никакие FBX/mesh/texture source files рядом с player. Credits поставляются в `THIRD_PARTY_CREDITS.txt`. Публичные evidence содержат только отрендеренные кадры, метаданные, hashes, код и результаты тестов.
