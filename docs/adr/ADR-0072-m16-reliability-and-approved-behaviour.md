# ADR-0072 — M16 reliability without approved-behaviour regressions

2026-10-15. Owner approves M15.6 and authorizes M16 through a test Windows build.
Branch `codex/m16-reliability`, baseline `9a8b34d4`. Owner explicitly directs that
already implemented and agreed behaviour wins conflicts with old milestone text.

## Scope and retained contracts

Master plan WP-206–215 covers persistent saves/settings, safe writes/recovery,
autosave/migration, input rebinding, localization, diagnostic export and a
three-season save/load soak. "Save format v1" means a versioned contract, **not**
a downgrade from the approved career schema3. Existing v1→v2→v3 migration,
stable athlete/world/appearance IDs, raw replay payloads and RNG remain intact.
Replay v25/v26 recorded-policy compatibility is retained. No race tuning,
graphics-quality reduction, animation rewrite or legacy control rollback.

Career publication remains validate → durable compare-and-swap write → publish
in memory. Before a race, a verified checkpoint stores the exact current saved
source, without advancing training, RNG or race ticks. After a race, the existing
transaction publishes the completed result exactly once. Abandoning a race does
not count it. No unsupported mid-race resume is advertised.

Recovery inspects the selected career's primary, backup and pre-race checkpoint.
Corrupt/missing primary may fall back to that same career's known-good backup,
then checkpoint, with an explicit receipt. It must not silently choose a
different old career. Abandoned temporary candidates are uncommitted and ignored,
preserved for diagnosis rather than promoted speculatively. Subsequent repair
must retain the known-good fallback and quarantine invalid bytes.

Settings use a separate schema1 contract and migrate existing preferences.
Approved default actions (including manual bolt, breath and effort scale) stay
unchanged. Keyboard/gamepad rebinds map device input into the existing commands;
they do not change authority, physics or assists. Accepted FROSTLINE pages,
RU/EN,130% text and controller navigation are extended, not replaced.

Localization adds an extensible data catalog and a migration bridge for existing
bilingual labels. New reliability text is keyed data. Diagnostic bundles contain
allowlisted codes, version/settings metadata and anonymous save structure;
never raw logs, user paths, names, credentials, complete saves or replay snapshots.
Export is local and explicit; no automatic upload or telemetry service.

## Acceptance

Fault-injected interrupted/stale/corrupt IO, schema fixtures and recovery must
pass without weakening assertions. Native UI checks cover rebind/reset/cancel,
settings persistence, friendly recovery/errors and local diagnostic export.
The soak traverses3 actual seasons/156 weekly boundaries and every eligible
production event through save/load, distinguishing full authoritative races
from limited native transition fixtures. Preserve existing deterministic guards.
Final evidence includes WP state, tests, limitations, real native captures and
a Windows build. Owner testing decides final closure. PROJECT_STATE is append-only.
