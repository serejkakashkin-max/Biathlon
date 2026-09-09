# TASK-006 / WP-006 — PASS

Added AnaerobicSystem, shared ResourceOutput telemetry and AnaerobicTests. Drain/recovery uses
joules and watts, honors fatigue/recovery modifiers, saturates [0,1], and exposes actual rate.
Added scripts/Test-UnityCore.ps1 to save MCP evidence and fail on tests/skips/Console errors.

.NET Release 50/50, Unity MCP EditMode 50/50. Analytic drain, full/empty boundaries, neutral threshold,
larger capacity and refill trade-off, effort integration and zero-allocation intervals pass.
Foundation hash unchanged. Unity 0 errors / 4 upstream reload warnings, compilation successful.
Evidence artifacts/physiology/wp006-*; git diff --check passed. No scene/PlayMode changes.

Balance remains provisional; capacity does not grant hidden watts. Full integrated state/replay pending
WP-008/010. No commit or remote action; prior uncommitted baseline preserved. Next WP-007 fatigue.
