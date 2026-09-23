# G2 Visual Remediation Plan

2026-09-20. Owner-directed phase **между WP-166 и G2**, не изменение Master Plan и не начало M13. Основание: [директива](../owner/CODEX_POST_WP166_G2_VISUAL_REMEDIATION_DIRECTIVE_RU.md), [аудит 37 кандидатов](G2_VISUAL_ASSET_AUDIT.md), [WP-166](WP-166-PROFILING-REPORT.md).

## Порядок работ

| Task | Результат | Проверка / evidence | Статус |
|---|---|---|---|
| G2-VR-001 | Источники, лицензии, форматы, хеши, решения по всем 37 кандидатам | Inventory, 235 source hashes, первичные license declarations, 6 Sketchfab metadata | DONE; Unity imports: 0 |
| G2-VR-002 | Hero athlete: непрерывное анатомическое тело, костюм/лицо/головной убор, de-branded goggles, собственные материалы, skin weights и LOD | Typed M11 bones/sockets, сохранение equipment, skin deformation в ski/prone/standing, front/back/side/close captures, bounded geometry, source-to-output manifest | 002A/B technical DONE;008A confirms visual target UNMET |
| G2-VR-003 | Animation/presentation: контакты лыж/палок, перенос веса, tuck, переходы и позы стрельбы на новом теле | Последовательности кадров, существующие motion/contact/shooting tests, snapshot/zero-allocation checks; authoritative state неизменён | 003A technical checkpoint DONE; visual actions OPEN |
| G2-VR-004 | Лес/снег/terrain: реальные выбранные fir/pine LOD, подлесок/камни, snow material и читаемая трасса | Малые import batches, camera composition, alpha/shadow/LOD проверка, повторение WP166 counters с теми же budgets | 004A–G technical DONE;008A reviewed, composition actions OPEN |
| G2-VR-005 | Stadium/event dressing: выборочный CC0 kitbash, трибуна CC BY, оригинальные event panels | Масштаб, материалы, отсутствие реальных/похожих брендов, читаемость рубежа, screenshot comparison | 005A/B technical DONE;008A reviewed, visual actions OPEN |
| G2-VR-006 | Crowd: ограниченное число адаптированных людей, LOD/вариации, интеграция без раскрытия restricted mesh data | Локальный reproducible conversion recipe, credits, runtime/render checks; restricted sources/derivatives исключены из public Git | 006A technical checkpoint DONE; close quality OPEN |
| G2-VR-007 | Свет/атмосфера и целостность benchmark | Snow/skin/forest exposure, silhouette, haze, weather, chase/shooting cameras; offscreen captures | 007A technical DONE;008A reviewed, visual actions OPEN |
| G2-VR-008 | Новый benchmark owner review package и честная self-review | Итоговые build/captures/tests/profiling, attribution, G2_VISUAL_BENCHMARK_REVIEW.md, G2_OPEN_ITEMS.md, PROJECT_STATE | 008A DONE2026-09-23; recommend FAIL; STOP / WAITING_OWNER_G2 |

## Правила исполнения

Checkpoint G2-VR-002A завершён: controlled import пяти источников, native rig/LOD интеграция, full99/99 + final3/3, Windows hidden9/9. [Hero report](G2_HERO_ATHLETE_IMPLEMENTATION.md). Checkpoint002B завершён: closed gloves/pole fit, numbered bib, full101/101 + focused5/5, hidden player9/9. Checkpoint003A завершён: собственная винтовка, rifle grip fit, full104/104, hidden player9/9; [motion report](G2_ANIMATION_PRESENTATION_REPORT.md). Checkpoint004A завершён: audited fir, full107/107, hidden9/9, profile10 cases ACTION_REQUIRED; [environment report](G2_ENVIRONMENT_UPLIFT_REPORT.md). Checkpoint004B завершён: CC0 snow, terrain guard и tree ground fit, full111/111 + final4/4, hidden9/9; profile ACTION_REQUIRED. Checkpoint004C завершён: mixed pine/fir, full114/114, focused3/3, hidden9/9; три triangle failures остаются. Checkpoint004D завершён: CC0 boulder/dressing, full118/118, focused4/4, hidden9/9. Checkpoint004E завершён: shadow correction, full125/125, hidden9/9; все10 triangle budgets PASS, GPU unavailable. Checkpoint004F завершён:24-tree groves, full128/128, focused3/3, hidden9/9; все observable profile budgets PASS. Checkpoint004G завершён: CC0 winter understory, full131/131, hidden9/9; 005A stadium/canopy завершён: full134/134, hidden9/9; 005B event/materials завершён: full137/137, hidden9/9; 006A crowd завершён: full140/140, hidden9/9; 007A lighting завершён: full143/143, hidden9/9; далее008A integrated review; visual actions002/003 возвращаются в integrated review. Это не завершение визуальной приёмки002/003.

Каждый этап разбивается на небольшие проверяемые изменения. После каждой завершённой задачи обновляются этот план, PROJECT_STATE и соответствующий implementation/report; coherent commit и push. Локальные checkpoints не считаются owner acceptance.

Импорт только выбранных runtime derivatives после аудита. Исходные архивы, Blender distribution и промежуточные экспорты остаются в ignored staging/artifacts. CC BY attribution содержит автора, URL источника, ссылку на лицензию и фактические изменения; credits поставляются вместе с build. Нельзя публиковать restricted 3D data, включая decimated/retargeted derivatives, под видом собственных ассетов. Для Renderpeople используются локальные derivatives и воспроизводимый recipe; доступность этих данных в другом checkout явно проверяется, без неявных missing-GUID dependencies.

Hero сохраняет архитектуру ADR-0020: presentation-only rig, typed bindings, +Y up/+Z forward/metres, root motion выключен, sockets оборудования. Новые визуальные данные не меняют race identifiers, collision/route physics, shot geometry, shooting tuning или replay. OWNER-FEEL-001 остаётся OPEN для WP229/WP230/WP232.

Для системных изменений: unit/integration tests по AGENTS.md, Unity compilation/Console, соответствующие EditMode/PlayMode проверки и визуальное изучение кадров. Полный regression запускается при интеграции нескольких подсистем либо изменении race output. Статические отчёты не требуют искусственных unit tests.

**Только скрытые проверки**, согласно ответу owner. Используется проверенный WP166 offscreen render path; видимые окна player/Blender не запускаются. Нельзя объявлять GPU timing/FPS/min-spec qualification при unavailable counters. Triangle/draw/CPU budgets не повышаются ради PASS. Три прежних triangle-budget failures устранены в004E без повышения budgets; последующие изменения повторно проверяются по той же матрице.

## Условия завершения remediation

2026-09-23: [008A integrated review](G2_VISUAL_BENCHMARK_REVIEW.md) выполнен:45stills,30before/after,48motion frames,focused1/1 PASS; prior runtime143/143/hidden9/9. **Review package завершён, visual remediation не объявляется успешной.** Рекомендация FAIL; owner gate STOP. Прежние «далее008» выше — последовательность технических checkpoints, уже выполненная. Hero/animation/crowd/composition actions остаются открытыми. Разрешение владельца на дополнительные бесплатные ассеты записано отдельно в owner/FREE_ASSET_AUTHORIZATION_2026-09-23.md.

Hero/animation/environment/event/crowd/lighting представлены совместно в benchmark, а не только isolated assets. Review показывает chase, dedicated shooting camera и близкие планы с одинаково честной подачей, список использованных источников/изменений, technical evidence и remaining issues. Сравнение с owner references/NGL2026 должно оценивать фактическое изображение, а не объём импорта. Если результат остаётся слабым, рекомендация — FAIL/ещё доработка. **G2 утверждает только owner; WP167/M13 остаётся BLOCKED_G2.**
