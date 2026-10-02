# Pine Ridge — WP170 isolated content draft

Historical initial candidate. The recipes now describe the [U34/compound replacement draft](wp170-pine-ridge-u34-compound-draft.md); the measurements and captured source hashes below belong to the preceding candidate and are retained as history. Neither candidate is imported or accepted as completed WP170.

The draft has static data acceptance only. It is outside Unity Assets, with no catalog/calendar publication and no WP170 completion claim. See `pine-ridge-draft-verification.json` for the complete measured sectors, proof/resource/code hashes and pending acceptance.

| Actual Main metric | Woodland Basin checkpoint | Pine Ridge draft |
| --- | ---: | ---: |
| Counted Main length | 3,000 m | 3,000 m |
| Sustained climb travel | 600 m | 820 m |
| Sustained climb rise | 78.060837 m | 157.415236 m |
| Sustained climb grade | 0.131217 | 0.195608 |
| Total Main ascent | 78.060837 m | 171.415236 m |
| Highest Main Y | 78.060837 m | 164.415236 m |
| Outer widths | 6 m | 6 / 7 / 7.5 / 8 m |
| Draft-eligible travel | 2,576.400 m | 704.796 m |

Pine Ridge's two actual rolling ridge bends use eight exact world chords with ±24 m lateral control offsets and ±7 m elevation controls. The ridge covers 580.784 m of travel across a 571.196 m return span. Its first 290.392 m has 6 m width, 0.95 wind exposure and no drafting/passing; the following 290.392 m is 8 m wide with 0.8 exposure and drafting/passing. The lower climb has exposure 0.22 and no drafting; the return descent is 831.780 m at grade −0.192734. These are physical route and authored tactical differences, rather than atmosphere or seed changes alone.

The shared stadium retains all 30 personal paths, 33 physical private/exit/penalty/start loops, 6 m lane pitch, exact range layout and 150 m penalty geometry from the checkpoint. Shooting distance remains 50 m. All formats use this one range.

Sprint adds an actual 513.333 m western branch between Main 619.208 and 799.208 m. Its complete laps are 3,333.333 / 3,333.333 / 3,333.334 m, totaling 10,000 m. Pursuit uses an actual 852.948 m branch between Main 684.808 and 2,037.756 m; five 2,500 m laps total 12,500 m. The wide Pursuit turns have explicit 55 m seam guards covering measured full-width intersections. Mass Start remains five 3,000 m laps.

The native evergreen recipe creates 23,864 explicit trees in 80 groups: 16,279 pines (68.2157%) and 7,585 firs. Actual scales span 0.900017–1.379992, with a 1.138937 mean. Two full-canopy snow clearings and deterministic thinning above 145 m distinguish the exposed ridge from sheltered lower forest; 1,868 trees remain above that height. Dawn/clear presentation uses existing audited native assets.

Verification results:

- Production static validator: PASS, zero diagnostics.
- Independent serialized route proof: 1,329 exact chords, maximum error 1.14575e−13 m; 69 directed connections, zero seam gap; all format distances exact to the millimetre.
- Independent terrain proof: all 8,508 float32 ribbon triangles, 277,678 overlay vertices, zero buried triangles; gap 0.029992617–0.597948654 m. All 11,619 repeated terrain borders agree.
- Independent forest proof: full canopy/course/apron/clearing exclusions pass; maximum stored tree-foot terrain error 0.49996 µm. Explicit recipes quantize stored X/Z before sampling terrain Y; the accepted default recipe path is preserved.
- Eight Python geometry/serialization tests pass. Command: `python -m unittest discover -s scripts/content/m13 -p 'test_*.py' -v`.
- Two independent Venue01 regenerations using the current generic code and pinned checkpoint recipe inputs match all 203 accepted checkpoint resource JSON files byte for byte. See `venue01-checkpoint-regression/regression.json` and `venue01-generator-hash-addendum.json`. The separately authorized WP169 downstream-tail recipe changes current Woodland geometry intentionally; historical checkpoint inputs are retained under `venue01-regression/pinned-inputs` for this compatibility proof.

The clearance maximum is a visible-edge risk on the steeper Main climb near 638–640 m. Snow joins, actual groomed closed seams, tree silhouettes and dawn readability need Unity inspection. Runtime race/admission/replay, dense pack liveness, balance, weather and performance acceptance remain pending. The existing generated `mass-draft/course-plan.svg` retains a legacy Venue01 caption and is not visual acceptance evidence.

Reproduce the Pine data with the three `pine_ridge_*_recipe_v1.json` inputs under `scripts/content/m13/recipes`. Run `generate_venue01.py`, `generate_format_branches.py`, `generate_environment.py`, then `verify_format_branches.py`, the production `venue-validate` CLI, `verify_environment.py` and `audit_pine_ridge_draft.py`. Every output goes to `artifacts/wp170`; imports remain root-owned work.

Replacement candidate, 2026-10-02: this document retains the initial draft snapshot. The [updated unimported draft](wp170-pine-ridge-u34-compound-draft.md) adopts the 34 m exit tail, compound range rejoin and explicit fog scale 2.7. The recipes at the original source filenames now describe that replacement; use `artifacts/wp170/u34-compound-draft/previous-recipes` for the retained initial inputs. The initial JSON proof and historical 203-file source addendum remain unchanged; they are not new current-source generation claims. WP170 remains incomplete.
