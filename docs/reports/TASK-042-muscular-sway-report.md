# TASK-042 — Muscular sway

Status: PASS, 2026-09-07. MuscularSwayDefinition/JSON defines a finite harmonic spectrum,
weights and amplitude responses. MuscularSwaySystem draws two initial phases per tone from
IRandomSource at construction; ticking never consumes RNG. Fatigue/arm fatigue amplify,
stability skill reduces amplitude; horizontal and vertical components remain observable.

229/229 .NET and Unity EditMode pass; zero Console errors, four upstream warnings.
Evidence: artifacts/physiology/wp042-standalone.trx and wp042-{editmode,console}.json.
Tests: same/different seeds, unchanged RNG state during 2000 ticks, amplitude/smoothness
bounds, monotone modifiers, invalid-input atomicity, Nyquist rejection and 10k zero-allocation
ticks. Initial strict compiler warning for serializer-only fields was fixed with private
serialized properties; no warning suppression. Prior deterministic regressions pass.

Spectrum is a deterministic finite-tone approximation, not physiological measurement.
Pressure and stance composition follow their packages. No hidden shot roll, ski balance
change or commit. Next: WP043 stance profiles.
