# Batch distributions

Population standard deviation; linear (n-1) percentiles. Solo Sprint; no group-benefit claim.

| Case | n | Mean seconds | P05 | P95 | Hit rate | Fatigue |
|---|---:|---:|---:|---:|---:|---:|
| flat_aggressive | 256 | 1309.724 | 1307.970 | 1311.977 | 1.0000 | 0.1055 |
| flat_optimal_candidate | 256 | 1305.590 | 1304.674 | 1306.323 | 1.0000 | 0.1038 |
| flat_conservative | 256 | 1378.113 | 1376.752 | 1379.452 | 1.0000 | 0.0419 |
| rolling_aggressive | 256 | 1649.521 | 1649.349 | 1650.201 | 1.0000 | 0.1759 |
| rolling_optimal_candidate | 256 | 1643.768 | 1643.162 | 1644.742 | 1.0000 | 0.1752 |
| rolling_conservative | 256 | 1772.092 | 1770.931 | 1773.076 | 1.0000 | 0.0656 |

Paired savings relative to the first case on each identical track (positive = changed faster):

| Baseline | Changed | Mean saved s | P05 | P95 | Wins |
|---|---|---:|---:|---:|---:|
| flat_aggressive | flat_optimal_candidate | 4.135 | 1.840 | 5.887 | 256/256 |
| flat_aggressive | flat_conservative | -68.389 | -70.718 | -65.455 | 0/256 |
| rolling_aggressive | rolling_optimal_candidate | 5.754 | 4.794 | 6.297 | 256/256 |
| rolling_aggressive | rolling_conservative | -122.571 | -123.710 | -121.339 | 0/256 |
