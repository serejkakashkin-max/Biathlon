# TASK-004 / WP-004 — PASS

HeartRateSystem implements GDD target/overload/ceiling and exponential rise/recovery.
HeartRateOutput exposes next bpm, target bpm and load. RecoveryModifier scales recovery tau;
the career-stat curve itself belongs to WP-009. Added shared HeartRateSystemTests.

.NET Release and Unity MCP EditMode: 41/41 each, no skips. Analytical 180s work/60s recovery
curves verified within 1e-6 at 20 and 30 Hz; recovery-modifier comparison, inertia/ceilings,
invalid inputs, repeated 10k interval ticks and zero allocations pass. Foundation hash unchanged.
.NET 10k interval diagnostic: 175.574132404 bpm (a pinned integrated trace follows WP-011).
Evidence: artifacts/physiology/wp004-{standalone.trx,editmode.json,console.json}.
Unity Console: 0 errors, 4 upstream reload warnings; compilation successful. No presentation changes.

No final balance claim: no drift/resource evolution yet. Existing uncommitted owner/Foundation baseline
preserved; no commit or remote mutation. Next WP-005 cardiac drift. No owner gate.
