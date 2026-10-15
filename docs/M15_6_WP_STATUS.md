# M15.6 — Character creator, ratings and deep career

Started 2026-10-15 from integrated 9867b24b. Owner closes M15.5. M16 not started.
Branch: codex/m15-6-career-creator. Brief: owner/OWNER_M15_6_MASTER_PLAN_V2_RU.md.

| WP | Deliverable | Status | Evidence / remaining work |
|---|---|---|---|
| A | Audit and architecture | COMPLETE | ADR-0070; existing core/UI/rig/save/test boundaries audited; unrelated local changes retained |
| B | OVR and group ratings | COMPLETE | Read-only13-stat engine;9 native calculation cases, native GC.Alloc/positive control,10 CoreCLR cases |
| C | Creation domain / three tiers | COMPLETE | Detached draft,6 presets/6 origins, weighted limits, World Cup start, independent potential, durable confirmation and provenance |
| D | Live 3D creator | IN PROGRESS | Reused native G2 studio, orbit/face/full views, poses,6 faces/8 skin tones/3 builds, v3 save migration and actual race parity; height/hair/eyes/accessory expansion remains |
| E | Existing athlete selection | QUEUED | Stable world catalog, authentic selection, licensed-pack contract |
| F | Individual potential / growth | QUEUED | Extend shared training/ageing; deterministic save migration |
| G | AI / persistent world | QUEUED | Bounded pool, off-field growth, varied generations |
| H | FROSTLINE career UI | IN PROGRESS | Three-pane own-athlete wizard/confirmation/OVR/13-stat allocation works in RU/EN; dashboard, rival cards and seasonal trends remain |
| I | Balance / validation / build | QUEUED | Multi-season simulations, native screenshots, regressions, Win64 |

M15.6 remains OPEN. There is no M15.6 test build yet; the M15.5 build remains the
approved visual baseline. Native UI/race captures and selected verification are in
quality/m15-6; foundation report records limitations and initial red evidence.
WP-D/E/F/G/H/I must be completed before the milestone test build/owner acceptance.
