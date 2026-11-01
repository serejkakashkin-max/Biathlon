# M16.6 — Career equipment, dynamic weather and training UI

Owner brief: [M16_6_OWNER_TASK.md](M16_6_OWNER_TASK.md). Owner explicitly closed
M16.5 on 2026-11-01. M16.6 OPEN, implementation authorized; M17 not started.
Branch: `codex/m16-6-equipment-weather-training`, based on the actual current
checkout at published M16.5 commit `1717c530` (no automatic main merge).

| Stage | Status | Scope |
| --- | --- | --- |
| M16.6.1 — Appearance | COMPLETE / OWNER TEST PENDING | Complete no-goggles visibility, persistent identity-based rival profiles, legacy preservation, creator/race/LOD qualification |
| M16.6.2 — Equipment/economy/shop | IMPLEMENTED / FULL REGRESSION RUNNING | 6–8 ski specializations, rifle slots and trade-offs, migration, fair AI purchases, two-season economy scenarios and shop UI |
| M16.6.3 — Dynamic weather | NOT STARTED | Absolute race-time timeline, snow inertia, existing snow/equipment composition, forecast/owned ski selection, HUD/TAB and recorded replay compatibility |
| M16.6.4 — Training UI | NOT STARTED | Six illustrated sport cards, existing preview-backed week plan, RU/EN/accessibility/keyboard/gamepad without changing training formulas |

Each completed stage requires focused tests, native Unity verification, a new
Windows fullscreen review build, append-only PROJECT_STATE entry and scoped
GitHub publication. The complete milestone remains open for owner testing.
