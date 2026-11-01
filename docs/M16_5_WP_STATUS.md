# M16.5 — Results, official distance and evening scenery

2026-11-01 owner closes M16.4 and starts M16.5. M16.5 OPEN.

| WP | Status | Acceptance |
| --- | --- | --- |
| A — Compact modern results | COMPLETE / OWNER TEST PENDING | Compact Frostline summary, 10 rows/page, player flag/highlight, three-page navigation; RU 1600×900 and EN large 1024×768 captures checked |
| B — Official discipline distance | COMPLETE / OWNER TEST PENDING | Canonical Men/Women profiles supply total distance; per-lap mapping preserves official boundaries without changing the approved compressed route/clock |
| C — Cumulative timing gantries | COMPLETE / OWNER TEST PENDING | Every accepted checkpoint occurrence has increasing official km, including the reused stadium gantry; TIMING removed; passage hold/replay rewind; HUD/TAB share the mapping |
| D — Evening crowd and advertising | COMPLETE / OWNER TEST PENDING | Both crowd shaders and cloned static Lit scenery use the existing venue field; source colours/textures/passes/shadows retained; no additional lamps/passes |
| E — Qualification and fullscreen build | COMPLETE / OWNER TEST PENDING | 30 tests pass; six compiled evening views and ordinary fullscreen/loading/countdown startup pass; history and authoritative source/DLL guards pass |

Source: IBU September2026 ECR1.4.1/1.4.2. Existing canonical rules/profile files
remain authoritative; no career/scoring/physics/steering or save/replay change.

Delivery: `Builds/M16_5_Results_Distance_Review/START_TEST.cmd`.
Evidence: [quality report](quality/m16-5/README.md).
Recheck: `python scripts/quality/report_m165.py`.
M16.4 is closed by the owner's explicit approval. M16.5 remains OPEN until the
owner reviews this build; no subsequent milestone is started.
