# TASK-002 — WP-002 physiology contracts

## Objective and dependencies
After verified WP-001, establish immutable physiology configuration/state, documented units,
explicit invalid-input/clamping policy, cold-path JSON serialization, and numeric determinism ADR.
Authority: Master Plan M1/WP-002, GDD §5–6/21/25, ADR-0001.

## Scope and constraints
Domain contracts and EditMode/shared .NET tests; one provisional JSON tuning source.
No authoritative skiing, no Unity references, no change to Foundation fingerprint or input encoding.
Use BCL serialization, no dependency installation. Numeric policy must not claim universal bitwise floating determinism.

## Acceptance and verification
- Required fields, version, stable ID, finite values and cross-field relationships validated on load.
- Round-trip and missing/invalid/version-rejection tests in .NET and Unity.
- Explicit seconds, watts, joules, bpm and normalized ratios; state ranges validated.
- Foundation regression unchanged; Unity compilation and Console checked.

## Risks and rollback
Tuning is provisional, not final physiological/game-feel acceptance. Restore only TASK-002 files to roll back;
existing Foundation/owner changes must remain. No production save exists to migrate yet.
