# WP167 — venue authoring schema/tools

2026-09-30. G2 explicitly APPROVED; first M13 Work Package.

TASK167A: immutable, versioned VenueManifest and pure .NET VenueAuthoringPlan. Binds track/world geometry/range lanes/junctions/connections, derives semantic zones from authoritative segments, validates stable IDs and anchored camera/ambience/asset metadata. No UnityEngine dependency in Domain.

TASK167B: Unity generator creates a presentation prefab and normalized manifest from Resources. All external prefab references are checked before output mutation. Temporary scene preserves the active editor scene; all cameras start disabled. Stable typed markers identify semantic zones, ambience events, assets and every physical lane stop; no Inspector setup is required.

TASK167C: synthetic fixture, deterministic/culture/path/reference tests, generated-prefab integration and regression runs. Fixture reuses old geometry only to exercise tooling; it is explicitly not a production venue.

Verification completed 2026-10-01: Unity EditMode VenueAuthoringTests **15/15 PASS** (593 discovered in the current assembly; 15 executed); .NET authoring **15/15 PASS** within the combined 40-test authoring/validator/intersections run. Unity full PlayMode **168/168 PASS** on the final WP167/168 tree, including generated prefab/resource integration. Final ID/reserved-name and save-success guards passed EditMode and actual prefab regeneration. Console: **0 errors,5 existing warnings, compilation_failed=false**.

The optional broad .NET run from30September ended with the interrupted session and produced no retained completion result; it is **UNVERIFIED**, not PASS. Final targeted .NET verification used an isolated artifacts directory to avoid testhost file locks and passed. No authoritative race logic or tuning was changed, so no new race-output regression is required for this authoring-only package.

Evidence: `docs/quality/m13/wp167-verification.json`. The required authoring and Unity integration checks passed. Source validation now also rejects reserved/duplicate generated IDs, and prefab-save failure cannot produce a successful generator result.

Changed systems: Domain authoring contract, Unity editor generator/presentation marker, venue JSON/generated prefab, EditMode/PlayMode tests, ADR0023 and authoring guide. Simulation, scoring, AI tactics and tuning were not changed. No race result change is intended. Existing local rendering/package/project settings remain outside these commits.

Next: WP168 geometric validator. A successful schema build does not prove actual course distance, seam coincidence, safe side approach or queue-free dense admission. Those checks must not be silently skipped when upgrading the benchmark to Venue01.
