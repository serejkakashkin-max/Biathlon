# G2 target018 — видимая зачётная область лёжа

30 сентября2026. [Owner feedback](../owner/OWNER_G2_TARGET_FEEDBACK_2026-09-30.md): движение017 стало значительно лучше; последнее указанное исправление касается мишени лёжа.

## Исправление

`TargetBankArt` сохраняет внешний чёрный диск115мм в обоих положениях. Для открытой prone-мишени материал `ProneTargetFace` рисует тёмный центр45мм с тонкой серой границей. Размер берётся из authoritative `TargetGeometryDefinition`, а не из отдельной копии константы. Ободок находится снаружи зачётного радиуса, сглаживается по размеру пикселя; попадание в ободок не расширяет зачётную область. Standing использует прежний большой чёрный диск. Закрытие переключает всю поверхность на прежний светлый материал, без остаточного внутреннего круга.

Новый shader и материал включены через Resources. Authoring-метод `StadiumArtAuthoring.GenerateProneTargetMaterial` восстанавливает material asset; всю сцену/стадион перегенерировать не требуется. Runtime использует повторно один MaterialPropertyBlock; проверка без аллокаций сохранена. Не добавлены gameplay colliders, RNG, изменения sight ray или физики/баланса. Новых third-party assets нет. Анимация017 и согласованная толпа сохранены.

## Проверки и сборка

- Unity MCP: **166/166 PlayMode PASS**,0failed/0skipped,201,729с. Отдельные target tests6/6 PASS; EditMode TargetGeometryTests5/5 PASS (553discovered, не полный553).
- Windows: **9/9 скрытых render-проверок PASS**, три сцены × low/balanced/high; startupVisualBenchmark PASS. BuildReport0errors/0warnings,350152277bytes.
- Финальный Unity Console snapshot:0errors/0warnings, compilation_failed=false. В MCP cache остаются прежние package warnings ConvGeneric/TRANSPOSE_OUTPUT; полное отсутствие предупреждений не заявляется.
- Все395 файлов сборки сверены по SHA-256. Код28dc3bd30a2bfa2f419520f2afdcf09f25356481. В сборку включены существующие локальные Unity/package/rendering настройки; воспроизводимость из одного чистого коммита не заявляется.
- GPU/FPS performance скрытыми offscreen checks не подтверждается; новые profiling claims не делаются.

Новые проверки покрывают все32 closure masks и переключения prone→standing→prone, соответствие shader-параметра физическому радиусу, различимость внутренней границы на1920×1080 через production aim camera при50м, исчезновение границы для standing/closed. Integration test настоящего bout проверяет материал по наблюдаемым stance/closedMask. Положение камеры в render fixture фиксировано в центре; HUD не записывается. Скриншоты — неизменённые Unity captures.

## Подъезд к рубежу и G2

По просьбе владельца просмотрены официальные схемы Ленцерхайде2025 (стр.12 media guide, несколько форматов) и Хохфильцена2025. В [production range flow acceptance](../quality/PRODUCTION_RANGE_FLOW_ACCEPTANCE.md) добавлены ссылки и обязательный боковой подъезд: проезд позади ковриков, распределение к отдельным местам, боковой выход, без транзита через зону стрельбы. Это handoff для WP167/168/169–174/177/178; тестовая карта не переделывалась.

Владелец выразил готовность согласовать G2 после мишени; окончательная формулировка APPROVED пока не получена. Исправление передаётся для завершения owner gate; M13 не начат. Новых художественных веток не открыто. OWNER-FEEL-001 остаётся OPEN для WP229/WP230/WP232.

[Review018](../quality/g2/owner-review-018/index.html). Запуск: `C:\game\Biathlon\Biathlon\Builds\VisualReviewWindows\START_VISUAL_REVIEW.cmd`.
