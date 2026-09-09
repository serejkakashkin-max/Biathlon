# TASK-038 — Target geometry

Status: PASS, 2026-09-07. Added TargetGeometryDefinition/JSON, ShotRay, TargetGeometrySystem,
TargetBankState, TargetGeometryTests and ADR-0007. Five plane-local apertures; explicit stance;
independent closure flags. Inclusive nanometre-encoded boundary comparison uses exact decimal
squares. Invalid zero rays/overlapping definitions fail; parallel/behind/out-of-range rays miss.
No athlete statistic, RNG or Unity physics enters the result.

209/209 standalone and Unity EditMode pass; zero Console errors, four upstream warnings.
Evidence: artifacts/physiology/wp038-standalone.trx and wp038-{editmode,console}.json.
Tests cover all five centres, both stances, exact edge/one-nanometre-outside, translated/scaled
rays, closed-state idempotence, invalid data and 10k zero-allocation intersections. Prior
Foundation/ski/player replay regressions remain passing.

Aperture diameters/distance verified against IBU public overview (linked in ADR-0007).
Spacing is provisional authored data. No bullet-radius/impact-momentum certification or
ballistic drop here; WP045/046 will compose projectile influences. No ski balance change.
No commit: unrelated baseline retained. Next: WP039 authoritative aim state.
