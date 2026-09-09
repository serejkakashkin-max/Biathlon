# M2 — Track Model and Ski Dynamics — PASS

WP013–029 satisfy the mathematical-course gate: a headless athlete completes all five synthetic
courses with fixed-tick movement, bounded resources, lap/finish stamps and reproducible traces.
Track/route contracts, technique, forces, tuck, cornering, snow, equipment, drafting, occupancy,
overtaking and safe separation have focused regressions. No Unity physics authority was introduced.

## Verification

- .NET Release: 167/167; Unity MCP EditMode: 167/167; PlayMode adapter regression: 2/2.
- Foundation and M1 fingerprints unchanged; five complete time-trial CSV hashes match .NET/Unity.
- Repeated Balance Lab exports/summary byte-identical; every consumed input copied and hashed.
- 100,000 integrated ticks / 5,000 simulated seconds: .NET 84.4634 ms, Unity 109.4149 ms,
  zero managed bytes in both; exact final distance 42729774346621 nm. Snapshot benchmark, not
  a performance guarantee for later graphics/pack AI. Dense draft/occupancy/separation also tested.
- Console: no errors or compilation failure. Four known upstream reload warnings at EditMode run.
- PlayMode restored the saved Foundation scene with two roots; no scene content discarded.

## Measured comparisons

All figures are controlled complete 1 km fixture runs, with remaining resources at finish.
Full source data: ../quality/m2-comparisons.json; plotting command: scripts/Plot-SkiMilestone.py.

| Comparison | Result |
|---|---|
| Technique skill 30 → 90, flat | 125.275 → 113.065 s |
| Climbing skill 30 → 90, climb | 292.789 → 260.579 s |
| Neutral → soft snow, flat | 116.633 → 122.542 s |
| Neutral → glide skis, flat | 116.633 → 116.108 s; gear effect smaller than skill |
| Steady → final sprint, flat | 116.633 → 115.462 s; reserve 0.946 → 0.765 |
| Steady → continuous attack, flat | 116.633 → 110.230 s; reserve 0.946 → 0.386 |
| Upright → tuck, rolling | 146.804 → 150.844 s; fatigue 0.0161 → 0.00159 |
| Packed → ice, technical descent | 133.609 → 157.562 s; grip loss outweighs glide gain |

![Course comparisons](../quality/m2-course-comparisons.png)
![Resource trade-offs](../quality/m2-resource-tradeoffs.png)

Draft regression checks the same acceleration with explicitly reduced power under aerodynamic
shelter, no automatic physiology refund. Slow speed and crosswind reduce shelter, leader effects
do not stack. Pack tests verify continuous nonintersection, lane swaps, blocking and replay episodes.

## Qualification and remaining work

Tuning is provisional. Short ski-only attacks remain fastest at a substantial resource cost; this
does not establish an optimal strategy for full biathlon races with shooting and later fatigue tuning.
Tracks are scalar synthetic topology, not authored 3D courses. Time trials are solo; pack systems
are separately integrated in tests, awaiting race scheduler composition. Contact resolution is
conservative and may limit throughput during competing lane changes. Finish fractions use documented
endpoint interpolation, not a claim of physical nanosecond accuracy. No graphics quality gate implied.

M2 required no owner gate. Next M3/WP030 player command schema. Preserve uncommitted Foundation/owner
baseline; no remote operation or bulk commit performed. Evidence: artifacts/physiology/wp029-*,
artifacts/ski/lab and retained docs/quality/m2-*.
