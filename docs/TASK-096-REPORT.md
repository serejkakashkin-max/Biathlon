# TASK-096 — Shooting cohort calibration

WP-096 complete, 2026-09-11. The suite now supports an explicit athlete definition and an imperfect wind estimate per scenario. Existing suites omit both optional fields and preserve their old results. `shooting-calibrate <suite> <data> <output>` exports every bout, all athlete/controller contexts, distributions and a deterministic digest.

Three provisional cohorts use stats 85/60/35. Their reaction times are 0.25/0.30/0.35 s; tracking error is held at 0.0002 rad. All other controller coefficients retain the existing defaults. A 32-point grid (reaction 0.20–0.35 by 0.05; tracking error 0.00015–0.00050 by 0.00005) used seeds 10000–10199, both stances, all three cohorts. Selection prioritizes the GDD top windows and a normal-condition cohort ordering; these are experimental profiles, not a global difficulty rewrite.

Held-out seeds 30000–30999: 18 scenarios, 18,000 bouts, 90,000 actual geometric shots. Top normal hit rates: **87.20% prone / 85.08% standing**, within provisional GDD 82–94% / 72–89%. Mid: 50.94% / 67.30%; developing: 47.64% / 60.06%. Full distributions and times are in `quality/wp096/heldout.json`.

Conditions hold HR at 185 bpm; normal fatigue/arm fatigue 0.2, tired 0.8. Wind uses actual crosswind 2.5 m/s and an authored estimate of 2.0 m/s. The estimate changes the requested aim direction only. It never changes ballistics, target geometry or hit resolution. The initial uncorrected top prone windy rate was 0.08%; this exposed the absent wind-reading input. The new optional observation supports imperfect correction without giving the controller the actual impact. Existing race compositions keep their existing zero-wind observations.

Validation: .NET 7/7 (new calibration, legacy shooting balance, AI tests); Unity MCP 2/2, including identical full held-out CSV SHA-256. No physics constants or old snapshots changed. Unity compilation/Console checked after testing.

Limitations: these are fixed-condition bouts, not population forecasts. Cohorts combine stats with decision latency and are not a causal stat experiment (WP-098 covers that). Mid/developing windows are observations, not approved difficulty targets. At high fatigue the developing controller can be more accurate than mid while taking longer: fixed reaction/handling timing and aim deadlines create non-monotonic accuracy. Higher shooting speed does not promise higher accuracy under an unchanged trigger policy. No target-window claim is made for those conditions. The provisional tuning lock must retain this finding rather than imply universal cohort dominance.

Next: WP-097 matched draft counterfactuals, then stat/equipment sensitivity and AI parity.
