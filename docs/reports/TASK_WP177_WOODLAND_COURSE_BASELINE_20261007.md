# WP-177 — Woodland authored course baseline

Date: 2026-10-07. Diagnostic baseline only; WP-177 remains OPEN.

The reproducible [data-only audit](../../scripts/quality/woodland_course_metrics.py)
reads the committed physical track, visual polyline and three production
route plans. The [JSON receipt](../quality/m13/wp177-woodland-course-20261007/authored-metrics.json)
includes SHA-256 of both inputs; no authoritative physics, snow, AI or
camera values were changed.

The Main loop is 1,200 m with 27.893 m total climbing and 27.897 m total
descending. Its visual elevation spans 0–20 m. The largest continuous
descent steeper than 2% begins at station 736.159 m, runs 168.012 m and
drops 11.709 m; two technical 34.282 m drops occur near stations 654 and
933 m. The maximum per-segment difference between authored visual height
change and integrated grade is 0.009327 m. This supports the conclusion
that the owner's reported flat-looking descents are principally a
presentation/readability problem, not evidence of missing physical grade.
That conclusion is an inference from authored geometry and still needs a
chase-camera visual review from the final build.

Production route lengths are Sprint 4,200.001 m, Pursuit 5,000 m and Mass
Start 6,000 m. Main allows drafting and overtaking on 415.5 m, about 34.6%
of its circumference. Those are authored flags, not a measurement of actual
pass success. The diagnostic does not establish the climb-stat advantage,
AI difficulty fairness, winner times, traffic stopped-time or frame rate.

Next WP-177 work: record finish distributions and stopped-time by phase for
each full format; compare bounded athlete-stat variations on the same seed,
snow and course; inspect chase-camera views along both major descents and
use terrain/roadside depth cues where needed. The long Unity career test
timed out through MCP with very high Editor memory, so no simultaneous
full-field balance run was started. WP-178 must measure the memory cause.

## Fixed-seed production career times, 2026-10-07

Three actual 30-athlete Woodland races completed in isolated Release .NET
processes after the calendar/production-boundary change. The Sprint and its
saved-result qualified Pursuit passed 2/2 in the
[Sprint/Pursuit TRX](../quality/m13/wp177-woodland-course-20261007/wp177-career-sprint-pursuit-times.trx);
Mass Start passed 1/1 in the
[Mass Start TRX](../quality/m13/wp177-woodland-course-20261007/wp177-career-mass-times.trx).
Times below are **elapsed athlete times**, not the final field-scheduler
tick; Sprint starts are staggered.

| Format | Route | Winner | Middle rank | Last rank | Penalty loops, field |
|---|---:|---:|---:|---:|---:|
| Sprint | 4.2 km | 10:23.585 | 10:52.511 | 11:36.452 | 21 |
| Pursuit | 5.0 km | 13:04.426 | 14:21.343 | 16:44.548 | 40 |
| Mass Start | 6.0 km | 15:08.606 | 17:14.228 | 18:02.491 | 40 |

The winner's route-average speeds are approximately 24.2, 22.9 and 23.8
km/h respectively, including shooting/penalty time. These are game-length
Woodland routes approved by the owner, not claims of official race distance.
The fixed-seed field accrued 21 penalty loops across 300 Sprint shot
opportunities and 40 across 600 opportunities in each four-bout format.
That is a low miss/penalty fraction in this sample relative to the GDD's
initial top-level AI hit-rate windows, but these totals do not separate
prone from standing or isolate athlete ability, pressure and conditions.
It strengthens the existing owner shooting-feel review item; it does not
authorize hidden RNG or an unmeasured numerical rebalance.

WP-177 remains OPEN. Next acceptance work is paired climb-stat and difficulty
comparisons, phase-level stopped-time/traffic evidence after the manual
steering batch, and chase-camera readability on the real descents.
