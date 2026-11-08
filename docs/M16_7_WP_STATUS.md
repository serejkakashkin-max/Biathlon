# M16.7 — Hero biathlete

2026-11-05. Owner closed M16.6 and authorized M16.7. Owner specification:
`M16_7_OWNER_TASK.md`. This is a real editable 3D asset and Unity integration
task; beauty images cannot substitute for geometry or validation.

| Stage | State | Evidence / next gate |
|---|---|---|
|16.7.1 Project/source analysis|COMPLETE|Unity6000.6.0f1/URP17.6; G2 typed read-only presentation;11 imported source files audited in Blender4.5.15; licenses/provenance below|
|16.7.2 Body/face/skeleton|IMPLEMENTED; ART OPEN|1.80m CC0 anatomical base, repaired24bones, valid Humanoid;2K skin, further face/neck refinement needed|
|16.7.3 Tailored race suit|IMPLEMENTED; ART OPEN|Separate continuous weighted shell, seams/collar/zip/bib27; manual topology/folds/joint cleanup remains|
|16.7.4 Cap/glasses/gloves/boots|IMPLEMENTED; ART OPEN|Original sports silhouettes; static curled fingers, no articulated finger rig; full grip/pose matrix pending|
|16.7.5 Nordic skis/poles|IMPLEMENTED; RACE OPEN|1.94m skis/44mm waist, bindings/boots, pole grips/straps/baskets; moving ground/contact integration pending|
|16.7.6 Sporting rifle/harness|IMPLEMENTED; RACE OPEN|Independent original prop, straps, back/hand sockets; full transfer/aim cycle pending|
|16.7.7 Rig assembly|TECHNICAL PASS|Humanoid/six sockets/root motion off, four finite repeatable poses, foot-preserving knee flexion; full ski animation not qualified|
|16.7.8 PBR|IMPLEMENTED; ART OPEN|Actual4K textile/2K skin, normal import and URP metallic/smoothness; reference-quality skin/fabric not achieved|
|16.7.9 Actual3D visual review|ITERATED; OPEN|Six Blender renders plus native/player views; geometry/material/parenting/UV/bib defects corrected; professional art gate remains|
|16.7.10 Unity integration|STUDIO READY; RACE OPEN|Editable.blend/FBX/prefab/orbit scene/Windows build; existing G2 race athlete retained pending animation adaptation|
|16.7.11 Qualification|TECHNICAL PASS; STAGE OPEN|Native10PASS, build0errors, Console/standalone/image gates; exact receipts in docs/quality/m16-7; no whole-suite or final art approval claim|

## Analysis

Source folder is already extracted and normalized. No extraction necessary.
The downloader did not obtain valid `.blend` files. No model is missing from the
required GLB/FBX set. Readme warning about an empty archive predates the successful
download report and is not the current state. Nothing in that source folder is
an instruction to change the Unity game.

The source man is262348 triangles before importer triangulation; one packed
2K material,24 bones, walk/run actions. Face has detailed geometry/base color,
but skin lacks scanned microdetail. The model wears a loose T-shirt and shorts:
it cannot be recolored as a race suit. Head/neck supply the anatomical base;
new separate garments/accessories are required. Imported bone heads fit1.80m,
but displayed tails extend tens of metres: correct tail orientation/lengths
before Humanoid qualification. Original walk/run remain source locomotion only.

Alpine skis11954tris, beanie8688tris, safety goggles10560tris, poles184tris,
placeholder rifle4420tris and PolyHaven rifle19985tris have been imported and
measured. None is accepted as final professional biathlon equipment. PolyHaven
FBX references texture filenames different from downloaded names: missing-file
repair is needed if reusing it. Prefer original sporting prop geometry and keep
these files as measured references. Source4K claims do not describe these actual
downloaded packed maps: they measure2048x2048.

Current presentation uses G2 Hero,17 typed bones,7 typed sockets, procedural hybrid
skiing, ground/contact/grip/rifle adapters and LOD. Animation cannot change race
state/root motion. Retain existing assets and global rendering settings. New
resources belong under `Assets/Biathlon/Characters/Biathlete_16_7/`.

## Sources and quality reference

- Owner-provided CC0 base: https://grab3d.com/models/characters/man-base-character/
- Other original downloaded sources/licenses are retained in source audit.
- Art direction only: https://www.cgtrader.com/3d-models/character/man/biathlete-fully-equipped-rigged
- Official portable Blender4.5.15 downloaded and SHA256 verified; toolchain
  manifest and actual source Blender renders retained in local analysis.

The paid reference's geometry, textures, logos and design are not redistributed.
Its silhouette, fabric layering, sport-specific boots and vertical harness carry
guide the review. New character uses original PULSE branding and bib27. No claim
of reference-equivalent photorealism or owner visual approval has been made.

## Current checkpoint — 2026-11-05

Editable asset, scripts, studio and review build are available. ADR-0097 records
the boundary and the rejected UI-only capture. Technical qualification is recorded
in `quality/m16-7/qualification.json`; art quality and race motion remain OPEN.
See the asset's `Documentation/README_RU.md` for paths, regeneration and specific
remaining work. M16.6 CLOSED by owner; M16.7 IN PROGRESS; M17 not started.


2026-11-05 owner correction: M16.7 art gate FAILED (neck/jaw and studio motion rejected). BUILD HOLD until actual geometry and transferred main-G2 motion are visually checked. Existing21PASS/Windows build is technical evidence of the rejected candidate, not art acceptance. Natural anatomy and existing production motion transfer are in progress. Race G2 unchanged. PROJECT_STATE latest append controls.


## Latest checkpoint — 2026-11-06 (supersedes earlier candidate rows)

M16.6 CLOSED by owner. M16.7 IN PROGRESS; BUILD HOLD; no new Windows player.
The active studio uses Natural anatomy, official CC0 skin, transferred actual
G2 production motion and presentation-only contact adaptation. Head/neck are
continuous/unwarped; garment weights follow the body. The rifle reuses licensed
CC0 geometry/PBR, with decorative biathlon fittings. The latest owner-directed
full-palmar UPPER-handle grip replaces the rejected fingertip pinch.

Focused native tests: 57 PASS. Saved source-surface full-palm/5-finger/handle
interior/wrist-loop tests PASS; former pinch fixture FAILS as expected. Numeric
PASS is not art acceptance. Actual current frames, full PlayMode-run scope,
source hashes and limitations: `quality/m16-7-natural/qualification.json` and
`quality/m16-7-natural/REVIEW_RU.md`.

Open: manual cloth/finger/equipment polish, full standing/prone shooting and
rifle transitions, terrain/LOD/30-athlete-field integration and visual approval.
The race factory still uses G2. No simulation/balance/career/save changes.


2026-11-06 final verification: 69 native PlayMode PASS (57 asset/rig cases plus affected regression checks). Expanded 686-case run was cancelled after a saved 161-case snapshot (158 PASS/3 FAIL); all three found failures are fixed and included in the 69-case retest. Full-assembly PASS is not claimed. Details: docs/quality/m16-7-natural/regression-followup.json. BUILD HOLD; M16.7 remains open.

## Latest operational checkpoint — 2026-11-06 wrist correction

The owner rejected the preceding wrist orientation. The old swing-only test
missed 130.66 degrees of axial twist; current Tuck twist is <=0.087 degrees,
swing <=21.98 degrees. Forearm rotation, anatomical hand points, actual distal
palmar patches, separate fingers and rounded handles are checked together.
A retained claw-grip fixture fails the new distal test. Glove shell/URP
materials and safe opened-preview regeneration are included. 71 PlayMode +
2 EditMode PASS; final Console interval has no new Error/Exception/Assert.
Actual 37 PNGs and complete four-technique cycles at normal/0.25x speed:
`quality/m16-7-wrist/REVIEW_RU.md` and qualification.json. This supersedes the
active candidate in m16-7-natural; that folder remains historical evidence.
Full-assembly PASS and art acceptance are not claimed. M16.7 OPEN, BUILD HOLD;
race G2 and authoritative gameplay unchanged.

## Latest owner follow-up — working grip, 2026-11-06

The owner rejected the ordinary grip: handle in front of fingers. The old
distal test covered Tuck only; all ten ordinary pads failed by7.18–20.47mm.
Both variants now use the same surface fitter and pass the same full-palm,
all-finger, actual distal-patch, rounded-handle, closed-strap and inter-finger
checks. The retained old working source fails the extended test. Upper-end
thumb positioning also passes the retained thumb-side test. Actual runtime
handle attachment is sampled in all12states/96phases, both hands.

Final82PlayMode +3EditMode PASS; source gate6positive +2expected-negative PASS.
Repeated full authoring preserves importer-owned HumanDescription scale/root
metadata; new-struct initialization reset globalScale and caused meta-write
errors, caught by a new regression. Final Console interval has no new errors.
51currentPNG+1rejectedPNG and actual complete normal/0.25x motion videos:
quality/m16-7-wrist. No new Windows build, full-assembly PASS or art acceptance.
M16.7 OPEN/BUILD HOLD; race G2 and authoritative gameplay remain unchanged.


2026-11-06 M16.7 clothing checkpoint: UV-printed bib replaces floating glyphs; GDI winding preserves overlapping contours. Shared suit/bib partition and normalized weights remove independent-layer cuts; covered runtime triangles excluded with full editable underlay retained. Surface-following harness, matte textile and continuous panels refined. Tuck pelvis presentation lift6cm with both foot bindings restored by IK removes torso/thigh crossings. Actual imported BakeMesh16poses:0strict interior garment crossings, negative former poses reject; source layer/bitmap negative fixtures retained.86PlayMode+3EditMode PASS, source grip6positive+2expected-negativePASS. ActiveLOD0149094tri vs197883; allstored229616, no field FPS claim. Evidence/actual videos and source hashes: docs/quality/m16-7-cloth/qualification.json. Full assembly and final art acceptance not claimed. Race G2/authoritative gameplay/balance/shot geometry/career/saves unchanged. BUILD HOLD, no new Windows player; M16.6 CLOSED, M16.7 OPEN, M17 not started. Equipment follow-up with owner downloaded CC-BY Skiing Lady and independently authored reference rifle remains active. Scoped publication pending; historical STATE bytes preserved.


2026-11-06 M16.7 equipment/owner eyewear choice checkpoint: original sports glasses retained as default, saved source6parts have0vertex displacement. Optional mask derives selected lens/frame/padding from Skiing Lady by lancehuang526(CC-BY-4.0), welded/refined/fitted, with project strap. Studio dropdown switches actual imported renderers exclusively through all poses/4motion techniques. Donor alpine boots/rig/animation excluded; Nordic toe hinge and verified full-palmar grip retained. New PULSE4K ski surface graphics, carbon poles, ferrules and correctly oriented3spoke baskets. Independently authored broad sport rifle furniture/cheek rest/butt/sights/4magazine props follows owner animod visual reference; no paid source mesh copied, no exact likeness/art acceptance claimed. Natural wood material isolated from legacy.
Windows loaded-meta truncation reproduced Win321224; byte-identical atomic replacement immediately before Unity settings write preservesGUID/settings, same repeated-authoring regression now PASS and checks current remap persisted to disk. Mapping holder unconfirmed; no error suppression. Final89PlayMode+3EditMode PASS, Console after427296 has0newError/Exception/Assert. Source grip6positive+2expected-negativePASS; actual equipment bitmap/geometryPASS, clipped-print negative rejects; BakeMesh16poses0strict interior cloth crossings. Current source SHA256/evidence in docs/quality/m16-7-equipment/qualification.json. Full assembly/continuous collision/field FPS not claimed. Previous clothing checkpoint621964599a7765f412799ed2b39002306e975e61 confirmed on origin;17LFS objects66MB uploaded. Historical STATE/QA preserved. M16.6 CLOSED; M16.7 OPEN; BUILD HOLD/no new Windows player; raceG2/authoritative gameplay/balance/AI/shooting geometry/career/saves unchanged. Next: further artistic equipment/cloth polish, shooting transitions and separate terrain/LOD/field qualification. Equipment scoped GitHub publication pending.


2026-11-06 M16.7 solid mask webbing and walnut polish checkpoint: optional mask now has1.6mm closed solid cap-fitted webbing, woven PULSE print, temple clips and curved adjustment buckle. Source/actual imported volume checks reject the former flat strip; retained small rejected_flat_mask_webbing.blend has322boundary edges,14.18mm rear gap/4.4mm penetration. Current0boundary edges,20.16cm3volume,500rear vertices with0.267–2.395mm outside-cap clearance. Original glasses6parts remain vertex-exact and selectable. Rifle furniture uses CC0 Natural Walnut Veneer by Jenelle van Heerden/Poly Haven with preserved2Ksource/hash/license, longitudinal UV, warm stain, satin finish and softened aperture rims. Independently found floating magazine/cheek/brace fittings corrected: all10actual decorative mounts now overlap real stock; old8failures and intermediate upper-mount failure retained, no tolerance relaxed.
Final91PlayMode+3EditMode PASS/0FAIL/0SKIP; Console after427369 has0newError/Exception/Assert. Source grip6positive+2expected-negativePASS; actual equipment geometry/printPASS; BakeMesh16poses0strict interior cloth crossings. Current source/evidence SHA256 in docs/quality/m16-7-polish/qualification.json; actual Unity close views retained. Full assembly, continuous collision, field FPS and final art/reference equivalence not claimed. Lens/padding, rifle harness/cloth folds and full shooting/terrain/LOD/field qualification remain open. Historical STATE/QA preserved. M16.6 CLOSED; M16.7 OPEN; BUILD HOLD/no new Windows player. RaceG2/authoritative simulation/balance/AI/shot geometry/career/saves unchanged. Scoped GitHub publication pending.


2026-11-06 owner correction — rifle scale/carry: source and imported-mesh gates now reject the previously oversized prop. Width relative to shoulders82.1%→42.5%; stock thickness45→32.4mm, thin round external barrel. Independently found head/neck clipping fixed by side carry; new actual BakeMesh check spans12states x4phases x9rays. Final93PlayMode+3EditMode PASS; source proportions,10mounts,equipment,mask,6positive/2negativegrips,16posedcloth checks PASS; Console0newerrors. [Current evidence/review](quality/m16-7-rifle-scale/REVIEW_RU.md). Previous technicalPASS remains historical, not art acceptance. M16.7 OPEN/BUILD HOLD; no Windows player, raceG2/authoritative gameplay unchanged. Next: rifle harness and remaining art/shooting/field qualification.


2026-11-06 centred-carry follow-up: owner rejects side carry and stock/pelvis merge; new actual-import tests reproduce both. Current prop centred, body gap retained across49poses, barrel/head testPASS; source/socket/prop parent HumanoidSpine.4closed weighted suspension bridges added; interim2floating anchors caught/fixed. Final96PlayMode+3EditModePASS, source/grip/cloth/mount gatesPASS, Console0newerrors. [Review/current Unity evidence](quality/m16-7-centred-carry/REVIEW_RU.md). Technical checks are not final art approval. M16.7OPEN/BUILD HOLD, no Windows player; raceG2/authoritative gameplay unchanged. Remaining art/full shooting/terrain/LOD/field qualification open.


2026-11-06 M16.7 separate national textiles and completed carry loops: independent closed pullover bib over full suit, original knit PBR maps,7kit variants from canonical game palette with studio selector. Front strips now return under arms to rear suspension; actual topology and seated buckle gates added, prior terminating strips/floating buckles rejected.104focusednativePASS(101PlayMode+3EditMode),8source gatesPASS, grip6positive+2negativePASS,16actual imported cloth poses0strict interior crossings, Console0newerrors. [Current review/evidence](quality/m16-7-textile-kits/REVIEW_RU.md). Full assembly/continuous collision/field FPS/final art acceptance unqualified. M16.7 OPEN/BUILD HOLD, no Windows player; raceG2/authoritative gameplay unchanged. Further edge/fold polish and shooting/terrain/LOD/field qualification remain.


2026-11-06 M16.7 owner harness revision: removed the explicitly marked redundant rear verticals; two solid shoulder paths connect directly to4full-width rifle junctions. Smooth padded fronts, narrower returns, tangent junctions and local cloth skinning replace jagged/hidden segments.108focusednativePASS(105PlayMode+3EditMode),9source gatesPASS,6positive/2negativegripPASS,49native harness posesPASS,16cloth poses0strict crossings; Console0newerrors. [Actual current review/evidence](quality/m16-7-harness-refit/REVIEW_RU.md). Prior closed-loop requirement superseded by owner route correction. Full assembly/continuous collision/field FPS/art acceptance remain unqualified. M16.7 OPEN/BUILD HOLD, no player build; raceG2/authoritative gameplay unchanged.


2026-11-06 М16.7: по повторному указанию владельца новая модель использует существующие движения игры — G2 locomotion и цепочку shooting/rifle/contact/grip. В студии8действий, отдельные настройки экземпляра и адаптация контактов; исходная анимация сохранена для сравнения.154проверкиPASS(151PlayMode+3EditMode),129фаз на действие,0новых ошибок Console; FBX/Blend неизменны. [Текущий отчёт, реальные кадры и видео](quality/m16-7-sport-motion/REVIEW_RU.md). Пальцы/спуск/прицельный глаз, ремни и ткань в стрелковых позах требуют следующего прохода. М16.7 OPEN/BUILD HOLD, без Windows-билда и интеграции в гонку; симуляция/баланс/карьера не менялись.


2026-11-06 М16.7: исправлены пересечения винтовки на проверенных фазах, траектория снятия через плечо и рывки контактов. Подъезд включает укладку палок и подъём выбранных очков/маски рукой; экипировка восстанавливается при возврате к лыжному ходу.174focusednativePASS(171PlayMode+3EditMode),5действий x129фаз с0найденных пересечений ложа/ствола и тела,257фаз плавности на переход; Console0новых ошибок. [Текущие реальные кадры, видео и границы проверки](quality/m16-7-range-fit/REVIEW_RU.md). Точная прицельная посадка, пальцы и деформации ремней/ткани остаются открытыми. М16.7 OPEN/BUILD HOLD, без Windows-билда и интеграции в гонку; баланс/симуляция/карьера сохранены.


2026-11-07 М16.7: снятие винтовки без найденных пересечений рукавов с корпусом, отдельный хват при переносе и непрерывные направляющие локтя. По уточнению владельца сохранена симметричная V-постановка лёжа с раздельными хвостами; поперечные варианты отвергнуты. Ноги вытянуты, носочные шарниры сохранены; палки не пересекают лыжи.197профильных native PASS(194PlayMode+3EditMode),0FAIL/0SKIP,Console0новыхошибок. [Текущие реальные кадры, видео, красные регрессии и ограничения](quality/m16-7-range-anatomy/REVIEW_RU.md). Это не финальная художественная приёмка: опора локтей/прицельная посадка/пальцы/ткань и terrain/LOD/поле остаются открытыми. М16.7 OPEN/BUILD HOLD, без интеграции в гонку; FBX/Blend/симуляция/баланс/карьера сохранены.


2026-11-07 М16.7: направление палок на подъезде, близкая укладка с разрешённым владельцем контактом с лыжей, хват ствола выше плеча и непрерывный поворот кистей. Найденный рывок36.84° устранён отдельным смешиванием поворота ладони/скручивания; контроль относительного поворота включён на257фазах. Высота диоптра лёжа теперь у зрачка (±2мм), полная боковая/продольная прицельная посадка пока открыта.213focusednativePASS(210PlayMode+3EditMode),0FAIL/0SKIP; исходные строгие проверки пересечений,1мм контакта, анатомии и0warmallocation сохранены. [Реальные97кадров, видео, красные проверки и ограничения](quality/m16-7-range-contact/REVIEW_RU.md). FBX/Blend и принятая V-постановка лыж сохранены. М16.7 OPEN/BUILD HOLD, изолированный ассет; без Windows-билда/интеграции, raceG2 и авторитетная игра не менялись.


2026-11-07 М16.7: локти на подъезде опущены и приближены к корпусу; боковой выход≈21→7,7см. Правая кисть снимает винтовку через верх, затем за плечо, с плавным завершением дуги на стволе. Перегиб при удержании палок и промежуточный зазор контакта устранены без ослабления допусков.218 профильных native PASS(215PlayMode+3EditMode),0FAIL/0SKIP,Console0новыхошибок. [Сравнение,66реальныхкадров,видео и ограничения](quality/m16-7-upper-reach/REVIEW_RU.md). Прежний213PASS не покрывал естественность положения локтей и порядок прохода руки. Полная прицельная посадка/пальцы/ткань/лямки/terrain/LOD/поле ещё открыты. М16.7 OPEN/BUILD HOLD, без билда/интеграции; FBX/Blend и авторитетная игра не менялись.


2026-11-07 М16.7: нижняя опора ладони на геометрии орехового ложа, отдельные точки стоя и лёжа. Посадка лёжа приближает голову и диоптр вдоль линии прицела на 51,8 мм. 221 профильный тест прошёл, Console без свежих ошибок. [Нативная проверка и видео](quality/m16-7-aim-fit/REVIEW_RU.md). Боковое смещение, гребень, пальцы и опора локтей открыты. М16.7 OPEN/BUILD HOLD.


2026-11-07 М16.7: опора лыж на плоскость с кантом, низкий возврат палок и носочное крепление в V2, разгоне и V1. Проверки на 513 отсчётах на действие, 225 профильных PASS. [Результаты и видео](quality/m16-7-skate-fit/REVIEW_RU.md). Следом переменный склон, связка V2/V1 и постановка палки в мире при движении. М16.7 OPEN/BUILD HOLD.


2026-11-07 М16.7: разделены высокий вынос, опора, завершение и возврат рук в V2, разгоне и V1. Проверено на реальном меше, 231 профильный тест прошёл. [Сравнение и видео](quality/m16-7-skate-arms/REVIEW_RU.md). Работа пальцев, темляка и проверка на движущейся трассе открыты. М16.7 OPEN/BUILD HOLD.


2026-11-07 М16.7: исправлена кромка шапочки под поднятыми очками и маской. Обновлены форма ботинок, карбоновая манжета и шасси, закрытая подошва и материалы. 233 профильных native PASS, дополнительно 74 повторных проверки после финальной карты карбона. [Реальные сравнения и замеры](quality/m16-7-cap-boots/REVIEW_RU.md). М16.7 OPEN/BUILD HOLD.


2026-11-07 М16.7 — подготовлена совместимая с карьерой внешность нового спортсмена: шесть форм лица на трёх LOD, восемь оттенков кожи, две причёски/шесть цветов волос, четыре цвета глаз, шапочка/повязка/открытая голова и положения очков/маски. 21 графический файл MakeHuman CC0 сохранён с provenance/SHA-256. 150 прежних мешей сохранили геометрию и веса. Национальная повязка сохраняет личные цвета. 253 PlayMode + 15 EditMode = 268 профильных PASS; Console без новых ошибок. Исправлено предупреждение имён повторно генерируемых материалов. [Нативные портреты, лицензии, результаты и ограничения](quality/m16-7-appearance/REVIEW_RU.md). Поддержка подготовлена в изолированном ассете; рост/телосложение/тренировочная одежда ещё не адаптированы визуально. Следующий проход — материалы волос/глаз и оставшиеся параметры тела. М16.6 CLOSED, М16.7 OPEN/BUILD HOLD; без Windows-билда/интеграции в гонку, симуляция/баланс/сохранения не менялись.


2026-11-07 М16.7 — исправлено парение поднятых спортивных очков. Новая проверка воспроизвела отрыв нижней кромки линзы от шапочки до 19,4 мм. Изменены три значения pose-data: поворот −55° → −10°, смещение вверх 40 → 55 мм и назад −15 → −35 мм. Теперь зазор на шапочке 2,7–3,6 мм, без неё 5,3–7,1 мм. На реальном импортированном меше проверены 51 поза и 153 лучевых замера, повторные вызовы и жёсткая привязка к голове. 256 PlayMode + 15 EditMode = 271 профильный PASS; Console без новых ошибок. [Кадры до/после, красная проверка и протокол](quality/m16-7-eyewear-seat/REVIEW_RU.md). Исходники/FBX/формат сохранений/симуляция/баланс не менялись. Посадка маски в этом проходе не переутверждалась. М16.6 CLOSED, М16.7 OPEN/BUILD HOLD, изолированный ассет без Windows-билда.


2026-11-07 М16.7. Исправлена посадка задней части поднятых спортивных очков. Отдельное кольцо заменено замкнутой по сечению лентой, соединённой с оправой. Пять вариантов посадки и 17 форм подъёма на каждый вариант рассчитаны по шапочке, повязке, коже и волосам. Для зачёсанной причёски подогнана зона вокруг очков. Новый тест воспроизвёл задний отрыв 41,6–45,0 мм. После исправления максимум 2,9 мм на шапочке и 5,4 мм по остальным вариантам, проверены оба крепления, 40 поз и 640 замеров. 151 исходный меш сохранил геометрию и веса, изменены лента и зачёсанные волосы. 262 PlayMode + 15 EditMode прошли, Console без новых ошибок. [Кадры сбоку и сзади, замеры и тесты](quality/m16-7-glasses-rear/REVIEW_RU.md). М16.7 OPEN / BUILD HOLD. Работа в студии, проверка на трассе остаётся впереди.



2026-11-07 М16.7. Исправлены дефекты, отмеченные в сравнительном ролике. В студии измерены фактические плоскости лыж и коврика, положение кистей и работа коленей. Проверены хват, геометрия винтовки, носочные крепления и плавность изготовки. 269 PlayMode по двум прогонам и 15 EditMode прошли. Новый ролик показывает G2 и М16.7 в общем ракурсе на Woodland Basin и добавляет вид лыж сверху. [Сравнение, кадры и протокол](quality/m16-7-support-motion/REVIEW_RU.md). М16.7 OPEN / BUILD HOLD до проверки в гонке.



2026-11-07 М16.7. Переработан ход после замечаний владельца. Новые проверки воспроизводят чрезмерно прямую ногу, высокий локоть при низкой кисти и быстрый возврат. Прежний ориентир сильного разгибания заменён мягкой опорой, у V1 проверяется более низкая левая кисть. Сохранены проверки реальной геометрии рукавов и корпуса, хвата, носочных креплений и опоры наконечников во время толчка. 273 PlayMode и 15 EditMode прошли. [Видео, замеры и тесты](quality/m16-7-skate-rhythm/REVIEW_RU.md). М16.7 OPEN / BUILD HOLD.



2026-11-07 М16.7. Наружный изгиб коленей устранён сохранением игровой плоскости сгиба. Дополнительный перенос таза уменьшен до 0,12; сохранён видимый перенос веса. Разгибание локтя ограничено отдельно для V2, разгона и V1, короткий толчок на подъёме завершает плечо. Три новые красные проверки теперь проходят. Финально 276 PlayMode + 15 EditMode PASS, 0 FAIL / 0 SKIP, Console без новых ошибок. [Сравнения на 24 и 58 секунд, кадры и протокол](quality/m16-7-skate-tracking/REVIEW_RU.md). М16.7 OPEN / BUILD HOLD; проверка стыков техник на рельефе остаётся впереди.


2026-11-08 М16.7 — новый спортсмен внедрён в карьерные гонки по разрешению владельца (ADR-0098). Убраны периодические боковые покачивания; сохранены реакции на повороты/контакты. Новая модель в гонках и редакторе внешности отображает персональные лица/цвета, национальную форму, головной убор, очки, перчатки, ботинки, выбранные лыжи, номера и майки лидера. Проверены 7 копий карьер / 210 спортсменов; 18 исходных файлов неизменны по байтам/SHA-256. Собранный player прошёл реальный карьерный старт и первый рубеж с 30 новыми спортсменами, без записи результата. Привязка к фактическому снегу, принятый прицел, скрытие тела в прицеле и дальние тени проверены регрессиями. 314 разных профильных PlayMode + 3 нативных съёмки + 62 итоговых EditMode PASS, Console без ошибок. Старая погрешность валидационного времени контактного привода исправлена отдельным коммитом без настройки траектории/баланса. Windows-билд M16_7_New_Athlete_Review: 0 ошибок, 636 предупреждений сторонних шейдеров. Видео было/стало 58 секунд, 1440p/30, 14 эпизодов, включая лыжи сверху, полностью декодируется. [Сборка, игровые кадры, протокол и ограничения](quality/m16-7-race-integration/REVIEW_RU.md). BUILD HOLD отменён последним разрешением; М16.7 OPEN / OWNER REVIEW. Прежняя история сохраняется. Далее проверка владельцем; отдельная окраска тренировочной одежды и полноценная анимация пальцев остаются открытыми.


2026-11-08 М16.7. Темп и размах рук привязаны к фактической скорости. При медленном старте и разгоне хват движется мягче. В подъёмном V1 ведущая кисть выше при постановке, один парный толчок приходится на два шага. Проход кисти за бедро проверен. Подгонка рук на рубеже использует предварительно рассчитанные коэффициенты и отсечение заведомо проигрывающих вариантов. Скрытый в прицеле риг пропускает расчёт, стрелки за кадром обновляются реже. Предел отрисовки 100 FPS. На RTX 5080 / Ryzen 9 9950X3D при 2560×1440 Ultra, Woodland Basin, 30 участников, 6134 отрисованных кадра: среднее 92,23 FPS, лыжный ход 92,96, рубеж 88,95, минимум за скользящую секунду 61,55. 95-й процентиль 17,48 мс, пик 66,75 мс на первых тиках старта. 271 PlayMode и 2 прогона нативной съёмки прошли, Console без ошибок. Windows-билд M16_7_Pace_Review, 0 ошибок. Карьерная копия прошла ручной старт и первый рубеж, 25 исходных файлов совпали по SHA-256. Сравнение 58 секунд, 2560×1440/60, 3480 нативных пар кадров, оригинальный звуковой фон и синхронные постановки палок. Оба потока полностью декодированы. М16.7 OPEN / OWNER REVIEW. Далее проверка владельцем в новой сборке. [Замеры, кадры и ролик](quality/m16-7-pace/REVIEW_RU.md).


2026-11-08 М16.7. Изготовка продолжает позу подъезда. Спортсмен передаёт обе палки в правую руку и кладёт их вместе, затем берёт винтовку. Разворот стоя выполнен поочерёдными переступаниями, лыжи и ботинки проходят раздельно. Пять направлений входа проверены по 181 позе. 277 PlayMode и два прогона съёмки прошли. Windows M16_7_Mat_Review, 0 ошибок, проверка копии карьеры с 30 спортсменами прошла. 25 исходных файлов профилей совпали по SHA-256. Ролик 70,5 секунды, 1440p60 со звуком, пять студийных глав длиннее на 2,5 секунды каждая. Оба потока декодированы полностью. М16.7 OPEN / OWNER REVIEW. Следующая проверка владельцем охватывает подъезд к коврику и изготовку обеих стоек. [Проверки и материалы](quality/m16-7-mat/REVIEW_RU.md).


2026-11-08 М16.7. Устранены рывки A/D, отпускания усилия и раннего входа в стрелковую сессию на коврике. Доработанная поза плавно смешивается с исходным приводом. Ранний активный Outside включён в сохранение позы подъезда. 281 PlayMode прошёл, повторно проверен физический подъезд двух спортсменов. Windows M16_7_Continuity_Review собран без ошибок. Свежая карьерная копия с 30 спортсменами прошла первый рубеж, 25 исходных файлов совпали по SHA-256. М16.7 OPEN / OWNER REVIEW. Далее проверка владельцем A/D и обеих изготовок. [Проверки и кадры](quality/m16-7-continuity/REVIEW_RU.md).
