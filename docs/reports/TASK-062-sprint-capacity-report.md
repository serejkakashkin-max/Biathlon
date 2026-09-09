# TASK-062 — Sprint capacity report

Status: PASS, 2026-09-08. Added SprintCapacityTests, SprintReplayPlayModeTests and the
PlayMode AI assembly reference. Production gameplay and tuning are unchanged.

325/325 standalone and Unity EditMode tests pass; 6/6 PlayMode tests pass. Sixty interval
starters finish 45,269 global ticks (2,263.45 simulated seconds), with 600 shots, 480 actual
penalty circuits, 6,000 events and 60 results. Exact event/result SHA-256 values match .NET
and Unity and are pinned in the capacity regression test. All prior trace fixtures pass.

Remaining race from tick 101: .NET 2,083.501 ms; Unity 2,825.48 ms. Construction and export
are excluded. A warmed 100-tick .NET window allocates zero bytes; the remaining complete
race allocates 9,021,024 bytes for lifecycle setup/results. Unity's allocation counter fails
the explicit 4 KiB calibration probe, so its raw zeros are NOT allocation evidence. Earlier
Unity GC-counter-only reports have the same measurement limitation; standalone evidence
remains valid. Retained machine evidence: docs/quality/m5-benchmark-{dotnet,unity}.json.

PlayMode emitted an FMOD output-device initialization error (60) during audio restoration.
Archived wp062-post-play-logs.json identifies the engine/audio source; no project exception
or compile failure occurred. After archiving/reset, the final EditMode/Console capture has
zero errors and four upstream reload/disposal warnings. This does not validate audio hardware.

Limitations: synthetic 3 km fixture, independent paths/dedicated lanes, scripted center aim
for capacity. Pack interaction is M6, shared lane queues M7; graphics/audio are outside this
headless gate. Six full regression scenarios include geometric perfect/one-miss shooting,
resource costs and snow/equipment effects. Next: WP063 perception snapshot.
