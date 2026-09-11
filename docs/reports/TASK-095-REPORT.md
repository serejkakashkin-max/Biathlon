# TASK-095 — Pacing calibration

PASS — 2026-09-11. WP095 DONE; requested endpoint reached. M8 remains in progress.

The experiment selects a provisional pacing schedule from a declared finite grid, holding shared
physics and shooter capability constant. Search:28 configurations ×16 seeds5000–5015 =448 complete
races. Each track has14 schedules: effort650/750/800/850/900/950/1000, with0 or100m recovery at550,
and common final effort1000. The lowest mean finish on both tracks selects850/550/100m/1000.
Selection is frozen before holdout seeds20000–20255; see pacing-selection-v1.json.

The independent holdout runs256 seeds ×3 strategies ×2 tracks =1536 complete races. Aggressive
holds1000; conservative holds650 before the common1000 final lap; optimal_candidate uses the
selected schedule. “Optimal” means best of the searched schedules, not global/control-theoretic
optimality. All use the same100m/no100m strategy definition without changing physical capability.

| Track | Aggressive mean s | Candidate mean s | Conservative mean s | Candidate saves s |
|---|---:|---:|---:|---:|
| Flat |1309.724|1305.590|1378.113|4.135|
| Rolling |1649.521|1643.768|1772.092|5.754|

Candidate wins256/256 paired seeds on each track. Its range-entry HR is lower by6.36/4.25 BPM;
mean finish fatigue is lower and final-lap speed higher. Conservative retains more resources but
loses68.39/122.57s to aggressive. This supports the GDD pacing tradeoff without artificial penalties
or changes to shared physiological/physical tuning. Smaller single mistakes are not tested here;
this contrast concerns sustained race-long policies. No irreversible-failure mechanic is added.

Added BalancePairedComparison: identical track and complete matched seed sets required; positive
savings favor changed, other deltas are changed minus baseline. Batch emits paired.json and a
paired Markdown table. Missing/duplicate/unmatched seeds and cross-track comparisons reject.

Tracks are reproducible synthetic3×3800m =11400m Sprint stress profiles, not official race-distance
content. Rolling has18 alternating200m ±8% segments plus the shared200m stadium tail (balanced
ascent/descent). scripts/New-BalanceCalibrationTracks.ps1 uses the existing validated generator
and committed recipe. This avoids judging endurance costs solely on the shorter3000m fixture.

Scope/next: selected data is an experiment policy, not a replacement for the context-aware race AI.
Shooting hit rate is100% for this expert/neutral fixture and is NOT a claim of meeting GDD shooting
targets; WP096 owns stance/skill/condition distributions. Draft/stat calibration remain WP097/098.
No global tuning version is locked until WP102. Existing race/AI/replay hashes are preserved.

Qualification found signed zero in generated curvature: .NET serialized `-0`, Mono serialized `0`.
All1536 numeric metric rows and event audits already matched; only768 rolling context hashes
changed. The new long-track recipe explicitly opts into `normalize_signed_zero`; legacy recipes
retain their original bytes (all5 legacy fixtures verified). Regenerated long fixtures produce
canonical positive zero; the context-only test pins all6 cases before the full race regression.
The source trace comparison is retained under artifacts/balance/wp095-runtime-diff.json and
wp095-zero-fix-audit.json. No tolerance was relaxed to conceal the mismatch.

Final verification: .NET14/14 focused; Unity MCP3/3 (150.48s),495 discovered. The full1536-row metrics/context SHA256 matches CLI/.NET/Unity: 0f9c53725b46754bbc53fc6f2ab65dc227423f04342103aa17c497edacdcc8a0. Five output/report files repeat exactly across4/2 workers; both long tracks regenerate exactly. Console0 errors,4 retained warnings, compilation_failed=false. Unity test output also contains two external AI-generator NoSubscription messages; the tests passed and this workload does not use that service. Prior full484/484 and PlayMode7/7 remain earlier baselines; no new full495-suite claim.
