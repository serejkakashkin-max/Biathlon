# TASK-097 — Draft calibration

WP-097 complete, 2026-09-11. `DraftCalibrationRunner` evaluates the actual three-athlete shared Sprint with identical seed, entrants, two-second start spacing, AI policy and track. The counterfactual changes only `maximum_drag_reduction` to zero; no hidden physiology refund or synthetic finish adjustment.

Eight seeds 41000–41007, 16 complete races / 48 athlete finishes: mean finish benefit **3.195297425 s per athlete**, range **0.137287356–6.281688875 s**. Every pair remains below the provisional 5% total-result envelope; draft exposure is nonzero and actual drag reduction never exceeds the authored 35% cap. Detailed paired rows and context/event fingerprints: `quality/wp097-100/wp097.jsonl`.

This measures the total effect of allowing draft under the same policy, including downstream timing, shooting and tactics; it is not an isolated mechanical-watts estimate. The independent existing draft tests verify reduced aerodynamic force at equal delivered power, head/crosswind response, gap/lateral bounds, non-stacking and zero allocations. A small flat Sprint matrix does not guarantee group tactics on every future course; there is no permanent leader attachment or imposed fixed gap in these races.

Validation: combined WP097–100 .NET17/17; Unity draft1/1, exact exported JSONL hash across runtimes. No production draft coefficients changed. Reproduce with `scripts/Invoke-BalanceSensitivity.ps1` (JSONL, CSV, Markdown, PASS/FAIL regression evidence).

Next: WP-098 stat sensitivity.
