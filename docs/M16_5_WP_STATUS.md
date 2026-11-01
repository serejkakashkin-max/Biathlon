# M16.5 — Results, official distance and evening scenery

2026-11-01 owner explicitly APPROVES/CLOSES M16.5 after testing and starts M16.6.
M16.5 CLOSED / OWNER APPROVED.

| WP | Status | Acceptance |
| --- | --- | --- |
| A — Compact modern results | COMPLETE / OWNER APPROVED | Compact Frostline summary, 10 rows/page, player flag/highlight, three-page navigation; RU 1600×900 and EN large 1024×768 captures checked |
| B — Official discipline distance | COMPLETE / OWNER APPROVED | Canonical Men/Women profiles supply total distance; per-lap mapping preserves official boundaries without changing the approved compressed route/clock |
| C — Cumulative timing gantries | COMPLETE / OWNER APPROVED | Every accepted checkpoint occurrence has increasing official km, including the reused stadium gantry; TIMING removed; passage hold/replay rewind; HUD/TAB share the mapping |
| D — Evening crowd and advertising | COMPLETE / OWNER APPROVED | Both crowd shaders and cloned static Lit scenery use the existing venue field; source colours/textures/passes/shadows retained; no additional lamps/passes |
| E — Qualification and fullscreen build | COMPLETE / OWNER APPROVED | 30 tests pass; six compiled evening views and ordinary fullscreen/loading/countdown startup pass; history and authoritative source/DLL guards pass |
| F — Sprint snow junction follow-up | COMPLETE / OWNER APPROVED | Actual return reproduction plus all-format/rotated/runout terrain-contact matrix pass; exact cut samples and fixed tangents; three native views inspected |
| G — Readable split gaps | COMPLETE / OWNER APPROVED | Shared seconds/minutes/hours formatter for HUD, TAB, broadcast and replay; RU/EN visible row text, rounding boundaries and reuse pass; fresh fullscreen build/startup verified |

Source: IBU September2026 ECR1.4.1/1.4.2. Existing canonical rules/profile files
remain authoritative; no career/scoring/physics/steering or save/replay change.

Current delivery: `Builds/M16_5_Snow_Gaps_Review/START_TEST.cmd`.
Evidence: [F/G qualification and build](quality/m16-5/snow-gaps/README.md).
Recheck current delivery: `python scripts/quality/report_m165_snow_gaps.py`.
The preceding A–E build and its [archived report](quality/m16-5/README.md) remain
preserved; that report's source fingerprints describe the preceding candidate.
All WP-A–G were delivered and are accepted under the owner's explicit M16.5
closure. Historical test/build reports retain their original pending-review
status. Next milestone: M16.6 equipment, dynamic weather and training UI.
