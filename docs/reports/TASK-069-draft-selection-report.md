# TASK-069 — Stable draft target report

Status: PASS, 2026-09-08. DraftSelectionDefinition/TargetSelector use the existing shared
DraftingSystem for both best available wake and retained target. Authored minimum hold duration
and strength improvement margin suppress switching on small fluctuations. Missing/invalid
wakes release immediately. Stable IDs resolve ties; distinct physical corridors remain separate.
Frame exposes selected/best wakes and acquisition/hold/margin/switch/loss reasons.

349/349 standalone and Unity EditMode pass; Console zero errors/four upstream warnings.
Tests exercise stronger challengers during hold, small improvements after hold, material switch,
branch changes, lost targets, input-order-independent ties, invalid state atomicity and zero
.NET warmed allocation. Previous full-race hashes remain unchanged. No presentation behavior
changed; M5 PlayMode 6/6 retained.

Selection runs at strictly increasing tactical observation ticks; skipped simulation ticks are
allowed. It does not move athletes or reserve lanes. WP071 must bind authoritative pack placements,
validate space and apply actual shared drafting; selecting a target alone never grants a wake.
Telemetry is currently returned as a value; integrated decision/event tracing follows WP076.
Next: WP070 space/energy/terrain-aware overtake decision, then WP071 pack integration.
