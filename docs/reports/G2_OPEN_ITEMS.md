# G2 Open Items

2026-09-30. G2 **OWNER READY TO APPROVE AFTER TARGET018**, окончательное APPROVED ещё не записано. Новый отзыв владельца разрешает ограниченный motion017 (руки/корпус), согласовывает упрощённую толпу и оставляет очередь greybox как известное ограничение. См. [feedback](../owner/OWNER_G2_MOTION_FEEDBACK_2026-09-30.md). Предыдущая [owner directive](../owner/OWNER_G2_CONSTRAINED_CORRECTIVE_DIRECTIVE_2026-09-27.md) ограничивает работу corrective016: rollback → locomotion → mat placement → review build → STOP для решения владельца. Указания015 продолжать окружение/asset search отменены. M13/WP167 BLOCKED_G2.

| ID | Issue / evidence | Required next action | Status |
|---|---|---|---|
| G2-VIS-001 | Движение017 владелец оценил как «сейчас гораздо лучше» | Сохранить017, без новой арт-ветки | OWNER POSITIVE REVIEW 2026-09-30 |
| G2-VIS-002 | Дальнее окружение014/015 сохраняет ограничения читаемости | За пределами corrective016 по новой директиве; передать как известное ограничение | DEFERRED / owner decision |
| G2-VIS-003 | Упрощённая толпа016, четыре зимние формы/10вариантов | Сохранить согласованный вариант | OWNER APPROVED 2026-09-30 |
| G2-PERF-001 | 015:High max16,742,298triangles,balanced-athlete5,131,308; three historical3M guard FAILs | Preserve owner High-quality priority; separately review balanced cost; no GPU/FPS claim | OPEN diagnostic |
| G2-PERF-002 | Hidden offscreen DX11 exposes no reliable GPU/render-thread/batches counters | Keep unavailable values explicit; report CPU/draw/triangle evidence only. Owner requests hidden checks only; no visible run authorized | LIMITATION |
| G2-LIC-001 | Renderpeople terms restrict public source/modified 3D data distribution | Local ignored derivatives + reproducible recipe; verify compiled distribution and credits, no public raw/derivative mesh | RESOLVED006A: ignored local derivatives, public recipe/hashes only; release audit remains required |
| G2-LIC-002 | Used goggles/fir/pine/stand CC BY notices and modifications shipped; CC0/crowd/sky sources recorded; integrated views reviewed for branding | Preserve notices/modification records on further imports; audit final release distribution | DONE for selected current imports; release audit pending |
| G2-ASSET-001 | 4 Unity Asset Store entries are metadata only; Anderson Rohr pack needs manual action | Optional deferred alternatives; not a blocker while approved staged alternatives suffice | DEFERRED |
| OWNER-FEEL-001 | Owner: player shooting feels too easy | Mandatory gameplay-feel/difficulty/race-balance review at WP229/WP230/WP232; physical/geometric shots, no hidden RNG, no covert M11/remediation tuning | OPEN |
| G2-INPUT-001 | 013 A/D/stick release holds achieved line, replay format unchanged | Retain regression and hands-on validation | FIXED / tests PASS |
| G2-TARGET-001 | 018: обозначена внутренняя граница45мм для prone при внешнем115мм; standing/closed без отметки | Передать исправленную сборку; scoring неизменен | FIXED; PlayMode166/166, geometry5/5 PASS; Windows9/9; review018 DELIVERED |
| G2-RANGE-001 | Очередь перед общим входом greybox; 30lanes,11concurrent shooters | Тестовую карту не дорабатывать; production venues обязаны пройти [flow acceptance](../quality/PRODUCTION_RANGE_FLOW_ACCEPTANCE.md) при WP167/168/169–174/177/178 | ACCEPTED TEST LIMITATION; PRODUCTION REQUIREMENT OPEN |
| G2-RANGE-002 | 016: коврики в физических anchors; видимое боковое смещение плавно убирается во время Setup; review fixture на собственном коврике | Проверить реальные Sprint/Mass Start prone/standing и итоговую сборку | FIXED / реальные Sprint и Mass Start PASS; review016 |
| G2-OWNER-001 | После017 owner готов согласовывать G2, последнее условие — обозначить prone aperture | Target018 и итоговая сборка; [feedback](../owner/OWNER_G2_TARGET_FEEDBACK_2026-09-30.md) | READY TO APPROVE; final decision pending; no M13 |

WP166/008A/012/015 остаются историческими доказательствами. Пункты OPEN не являются разрешением автоматически расширять corrective016. По последнему отзыву30September выполняется только target018; затем завершение owner gate. Hidden-only measurement limitations сохранены.

Motion017: один парный толчок на пару шагов, меньшая компрессия корпуса; физика/ноги сохранены. Full164/164, Windows9/9 PASS; [review017](../quality/g2/owner-review-017/index.html) передан владельцу. STOP до G2.

Для production-трасс добавлен обязательный боковой подъезд по официальным reference Ленцерхайде/Хохфильцен. Greybox не переделывался.
