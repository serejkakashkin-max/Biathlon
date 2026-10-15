# M15.6 — Character creator, ratings and deep career

Started 2026-10-15 from integrated 9867b24b. Owner closes M15.5. M16 not started.
Branch: codex/m15-6-career-creator. Brief: owner/OWNER_M15_6_MASTER_PLAN_V2_RU.md.

| WP | Deliverable | Status | Evidence / remaining work |
|---|---|---|---|
| A | Audit and architecture | COMPLETE | ADR-0070; existing core/UI/rig/save/test boundaries audited; unrelated local changes retained |
| B | OVR and group ratings | COMPLETE | Read-only13-stat engine;9 native calculation cases, native GC.Alloc/positive control,10 CoreCLR cases |
| C | Creation domain / three tiers | COMPLETE | Detached draft,6 presets/6 origins, weighted limits, World Cup start, independent potential, durable confirmation and provenance |
| D | Live 3D creator | IN PROGRESS | Height/shoulders, hair/eyes, headwear/goggles, gloves/boots/training accents implemented; native rifle8/8 including independent skinning calibration; final face art/control-to-race acceptance remains |
| E | Existing athlete selection | COMPLETE | Stable90-person fictional catalog; league/nation/age/OVR/specialization filters, exact saved identity and30-person race parity; reviewed real-roster pack contract (no licensed pack shipped) |
| F | Individual potential / growth | COMPLETE | ADR-0071; grouped potential, separate age curves, shared training/ageing, stagnation and bounded observations;11 native cases |
| G | AI / persistent world | COMPLETE |90 initial /180 maximum detailed athletes, shared off-field weeks, persistent league moves, individual retirements/compact archive;5 native cases including15 annual transitions |
| H | FROSTLINE career UI | COMPLETE | Own wizard/catalog, live athlete/rival dossiers,5 groups/13 stats/deltas, OVR history/peak, achievements, filters and separate archive; native UI2/2 after3 real annual boundaries/reload |
| I | Balance / validation / build | IN PROGRESS |1800 scenario trajectories ×15 annual observations plus360 controls;19 full Sprint +3 MassStart openings PASS, separate2 full MassStart saved repeats;142 CoreCLR cases PASS; final replay compatibility/Win64 pending |

M15.6 remains OPEN. There is no M15.6 test build yet; the M15.5 build remains the
approved visual baseline. Native UI/race captures and selected verification are in
quality/m15-6; foundation report records limitations and initial red evidence.
WP-D/I must be completed before the milestone test build/owner acceptance.
The owner's additional M15.5 Course/clock fault is reproduced and fixed:67 focused
CoreCLR regressions,12 native fault cases and2 full MassStart saved repeats pass.
Earlier rejected fixes remain documented. Context v26 is explicit; preserving
strict v25 replay playback is a final integration gate. Graphical quality is not reduced.
