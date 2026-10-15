# M16 — Saves, settings and reliability

Owner-approved start2026-10-15 from M15.6 `9a8b34d4`.
Branch: `codex/m16-reliability`. M15.6 CLOSED / OWNER APPROVED.
Conflict policy: ADR-0072; preserve agreed implementations and career schema3.

| WP | Deliverable | Status | Evidence / remaining work |
|---|---|---|---|
|206|Versioned career/world/settings contracts|IMPLEMENTED / VERIFIED|Career schema1/version3 retained; settings schema1; explicit migration receipts|
|207|Atomic/corruption-safe saves|IMPLEMENTED / VERIFIED|33 native reliability cases, verified primary/backup/checkpoint, fault-injected writes|
|208|Autosave policy|IMPLEMENTED / VERIFIED|Transactional weekly/results publication; exact idempotent pre-race checkpoint; interrupted race unchanged|
|209|Save migrations|IMPLEMENTED / VERIFIED|15 native pinned v1/v2/v3 migration fixture cases; future versions preserved/rejected|
|210|Settings system|IMPLEMENTED / VERIFIED|Separate safe file, legacy migration; 9 pure and 7 native settings cases; actual failed-write RED→GREEN|
|211|KBM/gamepad rebinding persistence|IMPLEMENTED / VERIFIED|Real keyboard/gamepad capture, conflict/cancel/reset/reload; native skiing8/shooting4/audio10 regressions PASS|
|212|Localization-ready string system|IMPLEMENTED / VERIFIED|5 native catalog cases; 295 approved pairs as translator-editable keyed data; RU/EN product locales retained|
|213|Local error/report bundle|IMPLEMENTED / VERIFIED|3 native privacy/export cases; anonymous codes/config/save counts; no raw logs/saves/paths/secrets|
|214|Crash/recovery scenarios|IMPLEMENTED / VERIFIED|Corruption, lock/IO/future-version/missing-calendar faults and 4 native recovery/support UI cases; readable notice above navigation|
|215|Three-season save/load soak|IMPLEMENTED / VERIFIED|Fresh156-week/48-real-race run PASS:1199866ticks/23400geometricshots; independent payload/save-chain validator PASS. Paid uphill restart, exact checkpoint reuse and observed penalty yaw fixes retained; five native penalty cases and final historical-runtime oracle PASS|

WP206–215 implementation COMPLETE. M16 OPEN / OWNER REVIEW; owner decides final closure after testing.
Final Windows build: `Builds/M16_Reliability_Review/START_TEST.cmd`.
Unity6000.6.0f1 build0errors/0warnings; final isolated compiled-player acceptance PASS at2560×1440 borderless fullscreen, owner profiles unchanged.
124 unique selected native cases PASS across applicable checkpoints; no skipped case and no claim of one monolithic full-assembly run.
Known historical limitation:8 standalone AI-test failures reproduced unchanged atbbd01e62, retained in penalty-yaw baseline evidence; no failing test deleted/disabled. M17 not started.
