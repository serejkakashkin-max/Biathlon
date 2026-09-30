# G2 Open Items

2026-09-27. G2 **NOT APPROVED**. Новая [owner directive](../owner/OWNER_G2_CONSTRAINED_CORRECTIVE_DIRECTIVE_2026-09-27.md) ограничивает работу corrective016: rollback → locomotion → mat placement → review build → STOP для решения владельца. Указания015 продолжать окружение/asset search отменены. M13/WP167 BLOCKED_G2.

| ID | Issue / evidence | Required next action | Status |
|---|---|---|---|
| G2-VIS-001 | 016: прежние CC0 лицо/шапка и спокойные руки; бафф015 удалён; перенос веса по опоре | Оценка владельцем по новому build; ограничения костюма/процедурной анимации явно перечислены | OWNER REVIEW — проверено и передано30September; не открывать новую арт-ветку |
| G2-VIS-002 | Дальнее окружение014/015 сохраняет ограничения читаемости | За пределами corrective016 по новой директиве; передать как известное ограничение | DEFERRED / owner decision |
| G2-VIS-003 | 016: четыре зимние формы/10вариантов сохранены, фотографические лица отключены; крупные шарфы/статичные рукава остаются | Оценка зимнего силуэта владельцем; никаких новых facial realism экспериментов | OWNER REVIEW |
| G2-PERF-001 | 015:High max16,742,298triangles,balanced-athlete5,131,308; three historical3M guard FAILs | Preserve owner High-quality priority; separately review balanced cost; no GPU/FPS claim | OPEN diagnostic |
| G2-PERF-002 | Hidden offscreen DX11 exposes no reliable GPU/render-thread/batches counters | Keep unavailable values explicit; report CPU/draw/triangle evidence only. Owner requests hidden checks only; no visible run authorized | LIMITATION |
| G2-LIC-001 | Renderpeople terms restrict public source/modified 3D data distribution | Local ignored derivatives + reproducible recipe; verify compiled distribution and credits, no public raw/derivative mesh | RESOLVED006A: ignored local derivatives, public recipe/hashes only; release audit remains required |
| G2-LIC-002 | Used goggles/fir/pine/stand CC BY notices and modifications shipped; CC0/crowd/sky sources recorded; integrated views reviewed for branding | Preserve notices/modification records on further imports; audit final release distribution | DONE for selected current imports; release audit pending |
| G2-ASSET-001 | 4 Unity Asset Store entries are metadata only; Anderson Rohr pack needs manual action | Optional deferred alternatives; not a blocker while approved staged alternatives suffice | DEFERRED |
| OWNER-FEEL-001 | Owner: player shooting feels too easy | Mandatory gameplay-feel/difficulty/race-balance review at WP229/WP230/WP232; physical/geometric shots, no hidden RNG, no covert M11/remediation tuning | OPEN |
| G2-INPUT-001 | 013 A/D/stick release holds achieved line, replay format unchanged | Retain regression and hands-on validation | FIXED / tests PASS |
| G2-TARGET-001 | 013 aiming mark115mm both stances; scoring45/115unchanged | Preserve separation from owner shooting-feel issue | FIXED / tests PASS |
| G2-RANGE-001 | 30lanes,11concurrent shooters, firstwait797.249m before800m fork | Improve physical admission with dense/replay tests; no ghosting | OPEN |
| G2-RANGE-002 | 016: коврики в физических anchors; видимое боковое смещение плавно убирается во время Setup; review fixture на собственном коврике | Проверить реальные Sprint/Mass Start prone/standing и итоговую сборку | FIXED / реальные Sprint и Mass Start PASS; review016 |
| G2-OWNER-001 | Новая директива27September ограничила corrective pass | Build016 передан30September; STOP до owner decision | NOT APPROVED; no M13 |

WP166/008A/012/015 остаются историческими доказательствами. Пункты OPEN не являются разрешением автоматически расширять corrective016. После передачи016 ожидается решение владельца, включая допустимость оставшихся ограничений. Hidden-only measurement limitations сохранены.
