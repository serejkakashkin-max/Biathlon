# Pine Ridge — unimported U34/compound replacement draft

This replaces the initial WP170 content candidate outside Assets. WP170 remains incomplete. The old [report](wp170-pine-ridge-draft-verification.json) and historical 203 source addendum are preserved; the old recipe bytes are pinned under `artifacts/wp170/u34-compound-draft/previous-recipes`. The new [compact proof](wp170-pine-ridge-u34-compound-proof.json), [full measured report](wp170-pine-ridge-updated-draft-verification.json) and [source addendum](wp170-pine-ridge-u34-source-addendum.json) describe this candidate only.

The existing Pine recipes add exactly three options: a 34 m downstream exit tail, `compound_range_rejoin=true`, and presentation fog scale 2.7. Format recipe bytes and all forest recipe settings remain unchanged. The longer exit aisle is 230 m; every personal path is now 269.408 m, including its unchanged 39.408 m private loop. All 30 mat positions, private geometries, stop anchors and guards remain exact. All 257 port IDs and guard extents survive; `range_rejoin` owns the original exit/penalty and bypass ports in one region, reducing 128 regions to 127. This adopts the separately validated Woodland infrastructure; Pine's own race/admission/replay acceptance is still pending.

The updated Pine stadium has all 30 lane paths, 33 private/exit/penalty/start physical loops and exact range layout equal to the current compound Woodland draft. Lane pitch remains 6 m, shooting distance 50 m, and penalty travel 150 m. Relative to the original Pine candidate, the common exit grows 34 m, outer Main/branches and penalty/start move 34 m west, and recovery loses 34 m. Its climb, ridge, descent and format lengths remain unchanged. Terrain tile alignment and course exclusions therefore regenerate forest instances; they are not claimed byte-identical to the earlier Pine data.

| Main metric | Current compound Woodland | Previous Pine | Updated Pine |
| --- | ---: | ---: | ---: |
| Counted length | 3000 m | 3000 m | 3000 m |
| Climb travel | 600 m | 820 m | 820 m |
| Climb grade | 0.131217 | 0.195608 | 0.195608 |
| Total ascent | 78.060837 m | 171.415236 m | 171.415236 m |
| Peak Y | 78.060837 m | 164.415236 m | 164.415236 m |
| Draft-eligible travel | 2542.400 m | 704.796 m | 670.796 m |

The 820 m climb remains 7 m wide, exposure 0.22, with drafting/passing disabled. The two physical rolling ridge bends retain 580.784 m travel over 571.196 m span, fixed ±24 m lateral and ±7 m elevation controls. The first 290.392 m is 6 m wide, exposure 0.95 and no drafting/passing; the next 290.392 m is 8 m wide, exposure 0.8 with drafting/passing. The 831.780 m descent remains 7 m wide, exposure 0.35 and no drafting/passing. Widths 6/7/7.5/8 and these tactical sectors distinguish Pine's actual geometry and play choices from Woodland.

Mass Start totals 15000 m. Sprint retains its 513.333 m physical western branch and 3333.333/3333.333/3333.334 m laps, totaling 10000 m. Pursuit retains its 852.948 m physical shortcut and five 2500 m laps, totaling 12500 m. Seam anchors and 55 m Pursuit/25 m Sprint guards stay unchanged in counted coordinates; world endpoints follow the translated actual Main.

The updated environment has 90 terrain tiles, 369,857 unique grid vertices, 79 forest groups and 23,261 native evergreen instances: 15,904 pines (68.3720%) and 7,357 firs. Scales span 0.900017–1.379989, mean 1.139406. The two authored canopy clearings and thinning above 145 m remain; 1,856 trees stand above that height. Compared with the original candidate: 10 fewer tiles, one fewer group and 603 fewer trees, explained by actual translated bounds/exclusions. Dawn/clear and explicit fog scale 2.7 are presentation data; visibility has not been accepted.

Evidence:

- Production static validator: zero diagnostics, using the pinned BalanceLab binary recorded in `input-snapshot.json`.
- Independent serialized format audit: 1329 exact chords, maximum error 9.68114e-14 m; 69 directed seams, zero gap; millimetre-exact full format distances.
- Independent terrain overlay: all 8,542 reconstructed float32 ribbon faces and 279,208 overlay vertices, zero buried faces; clearance 0.029992910–0.597948654 m. All 10,393 repeated terrain borders agree. This reconstructs TrackRibbonBuilder; actual groomed Unity mesh/seam acceptance is pending.
- Independent forest audit: canopy/course/apron/clearing exclusions pass; tree-to-stored-triangle error ≤0.49994 µm.
- Two independent Pine generations match all 186 JSON resources byte for byte; generator/recipe/native inputs and initial Pine/reference Woodland resources stayed unchanged during the independent repeat. Input snapshot was captured after generation one and before generation two; this is stated in the machine proof.
- 29 Python tests pass, including six Pine checks and a negative case rejecting a lost private body guard; command/log/source hashes are in the compact proof.
- No Venue01 generation was run in this Pine task. Current generic source hashes, including `environment_serialization.py`, match the recorded U two-generation 205-file byte proof. Historical 203 proof is retained as a separate snapshot, not represented as rerun under current code.

The 0.598 m Main separation near 638–640 m and 0.551 m Sprint separation require actual snow-join/camera inspection. Cold import, final groomed closed seams, format admission, dense-pack liveness, deterministic result/replay, climb balance, dawn/fog/weather readability and performance are pending. Woodland's completed race proofs do not accept Pine. The generated plan SVG retains a legacy Venue01 caption and is not screenshot evidence.

To reproduce outside Assets, run the following commands from the project root:

```powershell
python -B scripts/content/m13/generate_venue01.py --output-root artifacts/wp170/u34-compound-draft/mass-draft --recipe scripts/content/m13/recipes/pine_ridge_geometry_recipe_v1.json
python -B scripts/content/m13/generate_format_branches.py --base-root artifacts/wp170/u34-compound-draft/mass-draft --output-root artifacts/wp170/u34-compound-draft/formats-draft --recipe scripts/content/m13/recipes/pine_ridge_formats_recipe_v1.json
python -B scripts/content/m13/generate_environment.py --output-root artifacts/wp170/u34-compound-draft/formats-draft --venue-id pine_ridge_v1 --recipe scripts/content/m13/recipes/pine_ridge_environment_recipe_v1.json
python -B scripts/content/m13/verify_format_branches.py --base-root artifacts/wp170/u34-compound-draft/mass-draft --output-root artifacts/wp170/u34-compound-draft/formats-draft --venue-id pine_ridge_v1
dotnet artifacts/wp170/dotnet-validator/bin/Biathlon.BalanceLab/debug/Biathlon.BalanceLab.dll venue-validate artifacts/wp170/u34-compound-draft/formats-draft/resources Biathlon/Venues/pine_ridge_v1/manifest Biathlon/Venues/production_venue_validation_v1 artifacts/wp170/u34-compound-draft/formats-draft/static-report.json
python -B scripts/content/m13/verify_environment.py --output-root artifacts/wp170/u34-compound-draft/formats-draft --venue-id pine_ridge_v1
```

For a fresh proof use a new `--output-root` with `audit_pine_ridge_draft.py --capture-inputs`, repeat the three generators into its `repeat/mass-draft` and `repeat/formats-draft`, then run the audit with the same output root. Never replace a retained input snapshot or old proof. Import/catalog/calendar/state changes remain root-owned follow-up work.
