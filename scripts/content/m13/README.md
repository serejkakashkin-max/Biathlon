# M13 original venue content tooling

Run from the repository root with Python3 and .NET available:

```powershell
& scripts/content/m13/validate-draft.ps1 -PythonPath python -OutputRoot artifacts/wp169i/draft
```

`-ProjectRoot`, `-OutputRoot` and `-RecipePath` are explicit parameters. The output directory can be anywhere; its location is not part of the authored geometry. Scripts do not mutate Unity Assets, start Unity or download assets. `recipes/woodland_basin_geometry_recipe_v1.json` is the single geometry recipe. `presentation-resource-recipe.json` points to already audited project resources and attribution/derivation records.

The wrapper runs the geometry generator, environment generator and independent serialized verifier, then builds a unique BalanceLab DLL under the selected output root and invokes that exact DLL. Only an actual zero-diagnostic production policy result writes static PASS to the audit. Each resource remains a draft until its runtime and visual acceptance evidence exists.

Geometry output contains manifest2, track/visual data, thirty equal personal mat paths, explicit local guards/directed seams, start approach and its authoritative continuation contract, and exact Mass Start profile/route. Environment output contains81 terrain tiles and72 explicit forest groups, thirty target banks and licensed stadium placements. `verify_environment.py` checks serialized border equality, terrain contacts, complete canopy/corridor clearance and canonical target placement independently of generation.

The canonical tools are tracked here. Ignored `artifacts` copies are outputs/checkpoints, not the only source. Quality snapshots and schema notes live in `docs/quality/m13`. Full-format branches and route bindings follow `docs/proposals/M13-FORMAT-ROUTE-V2-DESIGN.md`; they are separate from the Mass-only import checkpoint until their static and runtime validation succeeds.

After the Mass checkpoint exists, generate and validate the complete exact formats in a separate directory:

```powershell
& scripts/content/m13/validate-format-draft.ps1 -PythonPath python -BaseRoot artifacts/wp169i/draft -OutputRoot artifacts/wp169j/formats-draft
```

`generate_format_branches.py` adds only two real open physical branches, four directed seams/local guards, exact Sprint/Pursuit profiles/routes and an explicit format catalog. `verify_format_branches.py` independently verifies every actual world chord/grade, unchanged original physical identities/mat paths, zero world seam gaps, exact profile sums and the paid Sprint finish remainder. The full-format environment samples Main and both actual branches, protects every physical corridor and expands terrain/forest from their world bounds.

`refresh_environment_corridors.py` is a bounded schema repair tool: it resamples authoritative per-knot widths from an existing output's track/visual data while preserving all terrain/tree resources. The canonical generator already emits the same contract; this tool avoids unnecessary height/forest regeneration when only detached corridor arrays need refreshing.

Format recipes may override `quarter_segments` and `world_chord_real_slack_m` inside one format. Resolution must be an even integer from 2 through 1024; real calibration slack must be finite, positive and at most 1 metre. Omitted overrides preserve the global recipe values, and an omitted global resolution keeps the original 8 subdivisions per quarter.

Pursuit alone may opt into `analytic_paid_arcs: true`. This path rounds nominal arc chord lengths to whole millimetres, solves each actual chord on the analytic curve, and calibrates each radius to reach the quarter endpoint. It then pays the remaining straight with the existing exact paired-chord solver. It retains the exact source/destination tangents and total loop length. The default path and explicit `false` retain historical resource bytes. Dense sampling through the old paired midpoint solver can increase actual alternating tangent changes, so subdivision count alone is insufficient evidence of body clearance. Measure actual knot angles, validate exact chords, and run the frozen rotating-body kernel at every new guarded knot before importing a content candidate.
