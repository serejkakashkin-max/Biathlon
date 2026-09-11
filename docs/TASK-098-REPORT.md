# TASK-098 — Marginal stat sensitivity

WP-098 complete, 2026-09-11. Added deterministic marginal experiments using the real `TimeTrialSession` and `ShootingBalanceRunner`; tuning/physics are unchanged.

Ski matrix: +10 to each of eight stats independently; all-stat baselines 40/60/80; flat, climb, rolling and technical descent; three laps; repeating 60 s at effort850 / 20 s at550, tuck and sprint requested through their normal eligibility rules. 96 marginal comparisons and 12 baselines. Mean fractional time saved: endurance **2.44%**, ski technique **2.35%**, climbing **0.87%**, cornering **0.80%**, downhill **0.28%**, strength **0.14%**, recovery **0.02%**, anaerobic capacity **<0.01%**. No one stat exceeds the other positive effects combined across the matrix. Individual terrain effects can be larger: endurance reaches 9.93% and technique 8.69%; these are provisional observations and remain below the explicit 10% increment alarm threshold.

Shooting: five +10 increments, both stances, seeds51000–51499, 6,000 actual bouts / 30,000 shots. Report accuracy percentage points and bout seconds separately. Breath control gains 4.68 / 2.36 percentage points per +10; composure is neutral without pressure, and the other stance's stability is neutral. Shooting speed can lose 2.08 percentage points prone with this unchanged trigger policy; this is the timing interaction already documented in WP096, not a hidden hit roll. The matrix does not claim strict accuracy monotonicity for an unadapted AI controller.

Outputs: `quality/wp097-100/wp098-ski.jsonl` and `wp098-shooting.jsonl`. Validation: combined .NET17/17; Unity stat/equipment3/3 with exact portable output hashes. The tested workload gives reserve/recovery little marginal influence; it does not establish those stats' value for every tactical situation or career mode. No global curve was changed to inflate an underused stat.

Next: WP-099 equipment sensitivity.
