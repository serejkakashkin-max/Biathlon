# M15.6 — Character creator, ratings and deep career

Started 2026-10-15 from integrated 9867b24b. Owner closes M15.5. M16 not started.
Branch: codex/m15-6-career-creator. Brief: owner/OWNER_M15_6_MASTER_PLAN_V2_RU.md.

| WP | Deliverable | Status | Evidence / remaining work |
|---|---|---|---|
| A | Audit and architecture | COMPLETE | ADR-0070; existing core/UI/rig/save/test boundaries audited; unrelated local changes retained |
| B | OVR and group ratings | COMPLETE | Read-only13-stat engine;9 native calculation cases, native GC.Alloc/positive control,10 CoreCLR cases |
| C | Creation domain / three tiers | COMPLETE | Detached draft,6 presets/6 origins, weighted limits, World Cup start, independent potential, durable confirmation and provenance |
| D | Live 3D creator | COMPLETE | Saved height/shoulders, hair/eyes, headwear/goggles, gloves/boots/training accents; native rifle8/8, appearance5/5, authoring1/1, expanded own flow→reload→real race and compact130% buttons pass; root inspected genuine captures |
| E | Existing athlete selection | COMPLETE | Stable90-person fictional catalog; league/nation/age/OVR/specialization filters, exact saved identity and30-person race parity; reviewed real-roster pack contract (no licensed pack shipped) |
| F | Individual potential / growth | COMPLETE | ADR-0071; grouped potential, separate age curves, shared training/ageing, stagnation and bounded observations;11 native cases |
| G | AI / persistent world | COMPLETE |90 initial /180 maximum detailed athletes, shared off-field weeks, persistent league moves, individual retirements/compact archive;5 native cases including15 annual transitions |
| H | FROSTLINE career UI | COMPLETE | Own wizard/catalog, live athlete/rival dossiers,5 groups/13 stats/deltas, OVR history/peak, achievements, filters and separate archive; native UI2/2 after3 real annual boundaries/reload |
| I | Balance / validation / build | COMPLETE |1800 scenario trajectories ×15 annual observations plus360 controls;19 full Sprint +3 MassStart openings PASS, separate2 full MassStart saved repeats;142 CoreCLR PASS;181 unique selected native PASS; strict v25 replay,11s native demo, Win64 build and isolated player smoke PASS |

M15.6 implementation COMPLETE / OWNER REVIEW; milestone remains OPEN until the
owner tests the Windows build. M16 not started. Test launcher:
`Builds/M15_6_Career_Creator_Review/START_TEST.cmd` (desktop borderless fullscreen).
Build0errors/0warnings; isolated player creation/reload/race/countdown PASS,
owner save directory unchanged. This Windows session currently has disconnected
display/audio devices (WinDisc640×480; Unity display0×0), so final player smoke
uses explicitly labelled windowed QA. Fresh fullscreen confirmation on an active
desktop remains an owner test; the approved startup/launcher policy is unchanged.
The Course/clock fault is fixed and strict v25 recorded-physics compatibility
passes using independently generated original-runtime fixtures. Current races
use v26; old recordings keep v25. Graphical quality is not reduced.

Delivery: `docs/reports/TASK_M15_6_DELIVERY_20261015.md`.
Genuine visual gallery/video and exact cumulative checkpoint scope:
`docs/quality/m15-6/review.html`, `verification.json`. Initial red evidence and
historical PROJECT_STATE entries are preserved.
