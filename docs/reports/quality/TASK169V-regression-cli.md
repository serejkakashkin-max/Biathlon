# TASK169V — explicit Venue01 checkpoint regression CLI

The regression CLI now checks current and historical Venue01 catalogs with explicit recipe inputs. Two real generations reproduce every byte of the current 205-file O34/fog catalog, and two reproduce the pinned historical 203-file checkpoint. Eight focused CLI tests pass. This is data reproduction evidence, with no WP169 or WP170 completion claim.

The implementation changes `scripts/content/m13/regress_venue01_defaults.py` and adds portable tests in `scripts/content/m13/test_regression_cli.py`. `--expected-resource-count` is optional and checks the canonical count before generation; it never replaces byte comparison. `--environment-recipe` forwards an explicitly chosen atmosphere/forest recipe. Omit that option for the historical absent-recipe path. The historical 203 report keeps its existing ID and fields; other checkpoint counts use `venue01_resource_regression_v1`.

The CLI rejects equal or nested canonical/output roots in both directions before any writes. It requires a fresh or empty output root, preventing stale JSON from masking a missing output. Each of the three generator processes has a 600-second timeout, and the CLI runs exactly two independent generations. Every canonical/generated resource hash is compared, while the canonical inventory, all known generator/helper hashes and all selected recipe hashes are checked again at completion. The dependency inventory includes `environment_serialization.py`. A changed helper or recipe fails acceptance even when generated JSON otherwise matches. A failed byte comparison writes an honest FAIL report; invalid preflight inputs create no output.

Current 205-file reproduction:

```powershell
python -B scripts/content/m13/regress_venue01_defaults.py --canonical-root artifacts/wp169s/scaled-draft --output-root artifacts/wp169v/current205-regression --expected-resource-count 205 --environment-recipe scripts/content/m13/recipes/woodland_basin_environment_recipe_v1.json
```

Historical 203-file reproduction:

```powershell
python -B scripts/content/m13/regress_venue01_defaults.py --canonical-root artifacts/wp169j/formats-draft --output-root artifacts/wp169v/historical203-regression --expected-resource-count 203 --geometry-recipe artifacts/wp170/venue01-regression/pinned-inputs/woodland_basin_geometry_recipe_v1.json --formats-recipe artifacts/wp170/venue01-regression/pinned-inputs/woodland_basin_formats_recipe_v1.json
```

These output directories now contain acceptance evidence; use a new empty output root to reproduce them. Both runs report PASS, zero differences, identical independent outputs, unchanged canonical bytes and unchanged source/recipe snapshots. The proof publisher independently rehashed all canonical and generated JSON files. The old historical addendum remains valid for its recorded snapshot; TASK169V records the current S generator hash and serialization-helper dependency without rewriting that earlier evidence.

Portable tests run with `python -B scripts/content/m13/test_regression_cli.py` and pass eight cases. Synthetic isolated generator processes verify optional count/environment forwarding and dependency hashes, explicit 203 compatibility, identical-count byte mismatch rejection, differing-generation rejection, helper/recipe mutation detection, all three root-overlap cases before writes, count/stale-output preflight rejection and missing-helper rejection before output creation. The original artifact test and log are retained as historical evidence; the portable test log is `artifacts/wp169v/portable-cli-tests.log`.

The four actual generations above are the frozen pre-U V snapshot and were not rerun for portable test publication. They exercise the production generators at the exact hashes recorded in their original reports. Later approved U generator/recipe changes do not inherit those byte-reproduction claims; the compact proof records any later repository hash differences explicitly. Source/report/test hashes and commands are in `TASK169V-regression-cli-proof.json`; full inventories and logs are under `artifacts/wp169v`.

No generator, imported resource, authoritative simulation, T mesh source, PROJECT_STATE or Git mutation was performed. Pine content remains unimported. Race/runtime/visual acceptance is outside this regression CLI contract, and future approved generator changes require new snapshot evidence.
