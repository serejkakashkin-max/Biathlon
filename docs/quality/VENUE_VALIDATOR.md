# Venue validator — WP168

The validator is an offline production preflight. It never edits source definitions, changes race state or awards a gameplay result. `static_checks_pass` means only the listed static checks passed; it is not venue, performance or dense-race acceptance.

## Entry points

Unity menu: `BIATHLON/Venues/Validate Pipeline Fixture`. JSON goes to `artifacts/venue-validation/`.

Unity MCP `script-execute` can call:

```csharp
Biathlon.Unity.ProductionVenueValidation.Validate("Biathlon/Venues/venue_pipeline_fixture_v1").ToJson();
```

Standalone:

```powershell
dotnet run --project dotnet/Biathlon.BalanceLab -- venue-validate Assets/Resources Biathlon/Venues/venue_pipeline_fixture_v1 Biathlon/Venues/production_venue_validation_v1 artifacts/venue-validation/result.json
```

CLI exit codes: 0 static PASS, 1 diagnosed FAIL, 2 invalid/unreadable input. CLI checks prefab file existence; Unity resolves actual GameObject resources. Unity is required before approving asset imports. IDs are relative extensionless Resources paths, not arbitrary filesystem paths.

## Checks

- Existing schema readers enforce identity, snapshots, anchor bounds, complete visual loop distance spans and exact closed seams.
- Every visual edge's three-dimensional length is compared to its declared race distance. Closed race circuits and penalty loops are mandatory; explicit range branches may be open.
- Authoritative/world grades, grade agreement over the complete overlapping segment interval, and widths must satisfy the versioned production policy.
- Directed connections must coincide in world space. Every lane needs an entrance/main rejoin and reachable penalty access from its endpoint; an arbitrary mid-lane seam cannot satisfy this check.
- Range layout entry/exit must bind the actual physical fork/rejoin and lie beside the firing line within the rear apron; each lane stop must match its right-to-left mat slot.
- Route centrelines must not enter the firing corridor. The entire edge is checked, including edges crossing the corridor with both endpoints outside it. Penalty geometry must have the required length.
- A shared exclusive admission region serving multiple lane starts is explicitly diagnosed, including common starts concealed under multiple junction IDs. This prevents treating the old funnel as proof of independent access.
- Nonadjacent edges within each loop are checked for crossings, endpoint touches and collinear overlaps; interpolated height separation must meet the configured clearance. Cross-loop body envelopes still require physical connectivity/venue acceptance.
- Every manifest prefab must exist. Findings have stable codes/paths, invariant numbers and deterministic ordering.

Policy: `Assets/Resources/Biathlon/Venues/production_venue_validation_v1.json`. Tolerances and minimum dimensions are project acceptance values, not a claim of official venue homologation. The 50m firing corridor is an authoring exclusion volume, not a shooting-model override.

## Fixtures and boundaries

`tests/fixtures/venues/validator_valid` is a small flat one-lane geometry fixture with a 150m penalty loop. Generate it with `scripts/Generate-VenueValidatorFixture.py`. Its test policy explicitly requires one lane; the unchanged production policy requires 30 and rejects this fixture's capacity. No one-lane fixture is shipped as a production venue.

The old `venue_pipeline_fixture_v1` intentionally FAILS production checks. Preserve its diagnostics; do not relax thresholds to turn a legacy fixture green.

Static checks do not prove collision-free swept body envelopes, turn dynamics, terrain/prop intersections, event dressing quality, athlete-to-athlete separation, admission fairness or GPU performance. Those need authored paths plus deterministic full-race and visual evidence in WP169–174/177/178. The firing corridor test is conservative in plan view; it does not certify an overpass. OpenAI/Unity warnings from existing diagnostics and deprecated API calls remain separate from validation issues.
