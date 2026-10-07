# WP-177 — Woodland range fork traffic, 2026-10-07

Status: bounded M13 correction verified in headless race; WP-177 and M13 remain OPEN. No Windows review build from this increment.

## Cause and change

The fixed-seed 30-athlete Mass Start exposed a reciprocal stop on the shared Main approach: an outer athlete assigned the earlier mat fork and an inner athlete assigned a later fork requested intersecting full-body sweeps. The authoritative world-body check correctly rejected both. Mat ownership was not the bottleneck (zero capacity waits). Reducing or bypassing body collision would conceal the problem.

The Woodland definition now gives every private mat corridor 3 m of physical and rendered width while retaining 3 m lane pitch, 30 places, the 2 × 1.5 m mats and unchanged full-body collision geometry. The short-course source recipe, production track and environment corridor widths agree. Other track widths and the independent historical fixtures remain unchanged. The 3 m corridor lies within the 2.75–3 m firing-lane width in IBU Event and Competition Rules, September 2026, articles 3.4.2.3 and 3.4.3.1–2; it is not an increase in lane pitch.

An optional Woodland AI policy compares the selected actual fork positions while both competitors are still on the common physical approach. An inner athlete briefly caps effort at a *positive* 400/1000 when a close outer athlete must take an earlier fork. The 3 m neighbor limit, 0.5 m lateral separation, outer-side sign and effort are versioned data. Player commands, shooting, physical body admission and power statistics are unchanged. The policy and its exact source snapshot enter the replay context. A production-path test caught and corrected double-counting the nonzero range entry in the new fork coordinate.

## Evidence

- Baseline 30-athlete seed 123: complete at tick 21,011; 2,031 mat-path moving-intent stopped athlete-ticks, 320 on the exit aisle, longest stop 271 ticks. Mat body rejections: 1,903, all world-body.
- Corrected complete race: tick 20,706; 590 mat-path stopped athlete-ticks, 133 exit-aisle stopped athlete-ticks; longest moving-intent stop 55 ticks, longest mat stop 42, longest exit stop 14. Zero mat capacity waits. Event hash `10573282619558191407`. [Full race TRX](../quality/m13/wp177-woodland-course-20261007/wp177-correct-fork-full-mass30.trx).
- Focused Release .NET path, AI policy and production venue/replay checks: 31/31 PASS after positive-effort validation, including exact persisted command replay. [TRX](../quality/m13/wp177-woodland-course-20261007/wp177-final-policy-replay-profile.trx). Complete player-held second- and third-bout exit regressions passed in corrected-data headless runs. The Unity-native second-bout run exceeded the MCP 300-second response limit and has **no completion receipt**; it is not counted as PASS. Python short-course 9/9 and stadium placement 8/8 PASS. All 36 production environment corridor width arrays match the track sampler. Unity MCP refresh, two focused EditMode path/policy tests 2/2 PASS, recent Editor Error Console empty.

The bounded full-field regression now asserts a completed field plus limits on the longest stop and aggregated mat/exit stops. These thresholds detect a return of the observed multi-second jam; they do not prove every possible player obstruction or weather/seed configuration is queue-free. The player-held test uses zero drive and may coast under physics, so it does not prove a perfectly stationary player obstruction. Manual world steering is not connected to the playable host yet. Final owner hands-on traffic acceptance and WP-177 race-balance calibration remain open. No hidden shooting RNG or artificial speed increase was introduced.
