# M16 — Saves, settings and reliability

Owner-approved start2026-10-15 from M15.6 `9a8b34d4`.
Branch: `codex/m16-reliability`. M15.6 CLOSED / OWNER APPROVED.
Conflict policy: ADR-0072; preserve agreed implementations and career schema3.

| WP | Deliverable | Status | Evidence / remaining work |
|---|---|---|---|
|206|Versioned career/world/settings contracts|IN PROGRESS|Retain career3; audit settings migration and metadata|
|207|Atomic/corruption-safe saves|IN PROGRESS|Verified primary/backup/checkpoint recovery and fault injection|
|208|Autosave policy|IN PROGRESS|Retain transactional weekly/results publication; exact pre-race checkpoint|
|209|Save migrations|IN PROGRESS|Retain supported v1/v2/v3 fixtures, add explicit migration diagnostics|
|210|Settings system|IN PROGRESS|Migrate approved graphics/audio/UI preferences to safe versioned settings|
|211|KBM/gamepad rebinding persistence|IN PROGRESS|Data catalog, actual adapters, conflict/cancel/reset and reload|
|212|Localization-ready string system|IN PROGRESS|Extensible catalog and accepted-label migration bridge|
|213|Local error/report bundle|IN PROGRESS|Allowlisted anonymous diagnostics; no raw logs/saves/paths/secrets|
|214|Crash/recovery scenarios|IN PROGRESS|Fault tests and friendly native recovery/content/config errors|
|215|Three-season save/load soak|IN PROGRESS|156 weekly transitions/all eligible production events; native interaction fixture|

M16 OPEN / IMPLEMENTATION IN PROGRESS. No M16 build claimed yet. M17 not started.
