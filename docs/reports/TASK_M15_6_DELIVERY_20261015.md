# M15.6 — test delivery

Implementation work packages A–I are complete. This is an owner-review Windows
build; M15.6 remains open until the owner tests it. M16 has not started.

## Delivered systems

- Native own-athlete wizard and authentic90-person fictional catalog, three
  starting circuits, limited24-point allocation,6 specializations/backgrounds.
- Live G2 creator with saved face/skin/build, height/shoulders, hair/eyes,
  headwear/goggles and cosmetic equipment. One renderer/8 slots/3 LOD; approved
  M15.5 movement and rifle contact retained.
- Read-only OVR1–99 and5 group ratings from13 real simulation attributes;
  individual grouped potential,7 development styles, age curves, stagnation,
  shared player/AI training and individual retirement.
- Persistent90-person starting world, at most180 detailed athletes, bounded
  compact archive, familiar rivals moving between circuits and new prospects.
- Live athlete/rival dossiers, full abilities, actual seasonal observations,
  peak/history graph, real-counter achievements, world filters and archive.
- Versioned appearance/save migration preserves old attributes, RNG checkpoints,
  saved race results and raw replay payloads. Recorded v25 physics remains
  available to old journals; current races use corrected v26.

The owner's Course/clock failure is fixed with a passive, fully admitted curved
edge continuation. No paid-clock, geometric footprint, interval or reaction
budget guard was relaxed. This changes some actual race trajectories/results;
the old and new MassStart hashes differ and both are documented. Existing v25
recordings retain their old law and strict identity checks. No graphic-quality
reduction was used.

## Verification and evidence

Selected cumulative native checkpoints:181 unique executed cases,113 EditMode
and68 PlayMode, all PASS at their latest recorded checkpoint,0 skipped. This is
not a single full-suite run. Inventory totals from the Unity tool are excluded.
Final general career CoreCLR run142/142 PASS; focused replay/course run33/33 PASS;
earlier independent course regression67/67 PASS. Native UI assertions cover actual
control edits → confirm → Continue → production race, plus compact1024×768 at130%.
Native dossier capture crosses3 real annual history/ageing boundaries; no race
results or past seasons are fabricated. Approved arm movement8/8 and rifle8/8
include both extreme heights and independent weighted-skinning calibration.

Actual race study:19 full30-athlete Sprints plus3 first-split MassStart openings,
548954 ticks,5700 geometric shots,8580 stat-binding checks. A repeated Sprint and
three diagnostic opening repeats match their exact hashes/ticks. A separate
course fixture completes two full MassStart races from the same saved state,
600 shots each. Three fixed race seeds are a regression sample, not a universal
win-rate claim. All6 starting presets spend24 points; specializations retain
measured tradeoffs. No hidden player/AI power multiplier was introduced.

Development study:1800 starting-scenario trajectories plus360 controls across15
annual observations. Shared training/adaptation/ageing runs until individual
retirement, then freezes the controlled entrant. Race load is aggregate; this
does not claim thousands of fully simulated race calendars. Retired frozen years
are excluded from stagnation/decline and active-population means. Starting at
World Cup strength is distinct from a novice; OVR95+ is rare, not guaranteed.

The old-journal native verification initially failed because CoreCLR and Mono
historically serialize identical IEEE doubles into different JSON text. An
independent original-source Mono fixture now verifies exact native playback;
the original CoreCLR fixture remains for .NET. Production hashes and assertions
were not loosened. Earlier red art/fixture/calibration evidence is retained.

Machine evidence: `docs/quality/m15-6/verification.json`; visual gallery:
`docs/quality/m15-6/review.html`. The11-second video consists of88 real sequential
Unity captures, presented at8fps; it is not a game-performance measurement.

## Build and practical limits

`Builds/M15_6_Career_Creator_Review/START_TEST.cmd` starts the game in desktop
borderless fullscreen. Direct EXE launches retain the approved desktop-startup
adapter. Build receipt records Unity6000.6.0f1,0 compile/build errors,0 warnings.
The opt-in standalone probe uses an isolated profile store and checks actual
creation/reload, production race loading, personal countdown and advancing ticks;
the owner's saved profile directory remains unchanged.

The final execution environment has a disconnected Windows display (`WinDisc`,
640×480; Unity reports system display0×0) and no usable audio output. A fresh
fullscreen assertion is therefore unavailable on this host state. The explicit
windowed QA run is labelled as such; it does not claim fullscreen success. The
previous approved M15.5 fullscreen evidence remains historical, and the startup
adapter/launcher policy is unchanged. A device-output FMOD error after desktop
disconnect is distinguished from compilation/gameplay failures; audio quality
was not modified to hide it. Normal testing on an active desktop is the next
owner acceptance step.

Real athlete names/portraits are not bundled. A reviewed pack contract permits
future licensed content; the shipped catalog is fictional. Hair currently has
two short styles; facial hair is not advertised. All new eye/scalp derivatives
and existing credits are accounted for in `docs/licensing/g2_visual/creator/`
and the build's `THIRD_PARTY_CREDITS.txt`.

The dedicated branch is `codex/m15-6-career-creator`. Unrelated pre-existing local
render/settings/package edits were retained. PROJECT_STATE is append-only.
