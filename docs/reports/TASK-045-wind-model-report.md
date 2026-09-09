# TASK-045 — Wind model

Status: PASS, 2026-09-07. Added ShootingWindDefinition/JSON, WindVector and ShootingWindSystem.
World air-travel vector projects to lane heading (clockwise from north), then exposure scales
head/crosswind. Horizontal projectile drift follows GDD crosswind × scale × flight time.
Physical telemetry is separate from a coarse strength/direction WindIndicator for UI.

239/239 .NET and Unity EditMode pass; zero Console errors, four upstream warnings.
Evidence: artifacts/physiology/wp045-standalone.trx and wp045-{editmode,console}.json.
Rotation/sign/shelter/half-exposure, zero wind, geometric displacement of perfect aim, vector
magnitude validation, repeat equality and zero allocations pass. Earlier regressions pass.

This is GDD v0.1 simplified horizontal ballistics. No drag/drop/headwind time correction,
weather generator or perfect numeric aiming advice in the presentation contract. Tuning is
provisional, not ballistic certification. No ski balance change or commit. Next: WP046
accepted discharge → wind-adjusted geometry → independent target closure/events.
