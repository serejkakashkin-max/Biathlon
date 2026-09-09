# TASK-046 — Shot resolution

Status: PASS, 2026-09-07. ShotResolutionSystem composes lifecycle, authoritative ray,
wind drift, aperture geometry and independent closures. Fallible geometry/wind calculations
precede lifecycle mutation. Only accepted discharges can close targets; repeated hits spend
a round without duplicate credit. ShotResolved event 25: positive 1..5 new closure, negative
-1..-5 repeated closed target, zero geometric miss. No RNG dependency exists in resolution.

244/244 .NET and Unity EditMode pass; zero Console errors, four upstream warnings.
Evidence: artifacts/physiology/wp046-standalone.trx and wp046-{editmode,console}.json.
Complete five-hit bouts at both stances, five wind misses, duplicate closures, early triggers,
invalid ray/wind atomicity, complete frame/event replay and active zero-allocation pass.
Earlier Foundation/CSV/player regressions remain intact.

TargetGeometrySystem now exposes the same canonical plane-point test for ballistic endpoints.
Wind flight-distance validation covers angled/translated rays within numeric domain. Pre-shot
ray is supplied by authoritative aiming; the full sway/handling harness follows WP047–050.
No visual collision, penalty assignment, ski tuning change or commit. Next: WP047 handling.
