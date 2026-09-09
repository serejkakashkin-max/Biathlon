# TASK-040 — Breathing sway

Status: PASS, 2026-09-07. Added versioned BreathingDefinition/JSON, BreathingSystem/Frame
and tests. Fixed-tick oscillator exposes phase, frequency, amplitude and vertical angle.
HR range comes from PhysiologyDefinition. Higher HR/fatigue increases amplitude; higher
Breath Control reduces amplitude; HR also increases cycle frequency. No hit outcome logic.

219/219 .NET and Unity EditMode pass; zero Console errors, four upstream warnings.
Evidence: artifacts/physiology/wp040-standalone.trx and wp040-{editmode,console}.json.
Full cycle checked at 20/30/144 Hz, amplitude/frequency monotonicity, invalid-input atomicity,
exact same-command replay and active zero-allocation checks. Prior regressions unchanged.

Tuning is provisional. Stance multipliers and hold/debt compose in WP043/044; this oscillator
has no hold override. Cross-frequency phases obey ADR-0002 tolerance, same-build replay exact.
No ski balance changes or commit. Next: WP041 heartbeat cadence/impulse.
