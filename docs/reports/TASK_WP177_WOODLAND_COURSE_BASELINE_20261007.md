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
