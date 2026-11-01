# M16.5 F/G — Sprint snow junctions and readable gaps

2026-11-01. WP-F/G COMPLETE / OWNER TEST PENDING. M16.4 is owner-approved;
M16.5 remains OPEN. Test build:
`Builds/M16_5_Snow_Gaps_Review/START_TEST.cmd` (desktop borderless fullscreen).
The earlier results/distance build is preserved separately.

## Change and visual evidence

Sprint's selected visual ribbon now includes exact authored route-interval
endpoints. Cross-rows at those connections retain their tangent, with bounded
nearby rotation. Actual terrain clearance constrains each affected row instead
of reducing smoothing over the entire loop. Every original sample remains;
track/visual/path resources, physical route, row centres/heights/full widths and
private mats remain protected. The explicit policy enables exact cuts for Sprint;
Pursuit/Mass Start retain their existing construction path. All geometry is built
at loading; no new roof overlay, renderer, collider, light or per-frame mesh work.

The photographed return's largest exposed vertical face was 0.145028859 m².
The current independent reproduction reports zero exposed faces. Three native
High views were inspected; this is targeted junction evidence, not a claim that
every view across the full course has received subjective owner acceptance.

![Before](snow-before.png)

![After](snow-after.png)

HUD, checkpoint notice, TAB, broadcast and replay TV share one presentation gap
formatter: `70.2` -> `+1:10.2` (EN), `+1:10,2` (RU). Seconds remain below a
minute; hours are supported. Round to tenths before selecting units, so `59.96`
becomes `+1:00.0`. Leader text is retained. Numeric checkpoint binding formats
both actual elapsed/gap labels consistently, including reuse back to seconds.
No timing values, results, accepted ticks, scoring or recorded events are edited.

## Verification

| Receipt | Passed | Scope |
| --- | ---: | --- |
| `followup-core.trx` | 29 | Gap boundary/locale/invalid-input cases, HUD model and canonical distance |
| `deterministic-replay.trx` | 3 | Complete field frames/events/results, context guards and warmed allocation checks |
| `native-snow.json` | 12 | Seven actual-format contact variants, exact samples, three rim cases and original return reproduction |
| `native-ui.json` | 18 | Actual RU/EN row text/reuse, TAB history/cursor, timing gantries, ten-row results, cumulative distance, evening scenery, live broadcast and native replay seek |

62 distinct selected checks PASS; none skipped. The contact matrix includes
Sprint/Pursuit at 0/45 degrees, both finish runouts and Mass Start. Full terrain,
coverage, original-buffer and float-face assertions remain enabled. The Sprint
inside/outside witness derives from its real branch connection instead of the
discarded whole-panel overhang; its tolerances are unchanged. Earlier failing
gap and return receipts remain in `gap-red.trx` and `snow-red.json`; rejected
roof-envelope experiments remain diagnostic artifacts outside active assemblies.

Unity 6000.6.0f1 development build succeeded: **0 errors**, 520 existing warning
instances, 430,859,420 bytes, 23.966 seconds. Warnings are the retained 24 API
deprecations and 496 inference compute-shader variants. Actual Editor Console was
inspected after tests/build: zero errors; its broader warning history is archived
in `console-final.json` (includes Editor/test warnings and repeated entries).
The inherited build-summary identifier belongs to the common builder;
`milestone.json` identifies this M16.5 snow/gaps delivery.

`startup-player/review.json` succeeds, exit 0, no captured runtime errors:
2560×1440 FullScreenWindow, actual drawn Sprint bib/start ordinal 30, accepted
tick 17405, skipped waiting and personal 3–2–1–0. All four loading stages paint
with held clock, unarmed countdown and hidden gameplay HUD. Creator, recovery,
settings/rebinding, diagnostic export and checkpoint checks pass; owner profiles
are unchanged. Optional advanced navigation probes were not enabled or claimed.
The native compiled race capture was inspected. This is startup/runtime
qualification, not a whole-race FPS or hardware benchmark.

All 722 protected core source/meta files match the task-entry baseline. Five
Domain/Simulation/AI/Career/Replay DLLs are byte-identical to the preceding results
build. No physics, AI, balance, duration, RNG, replay or save-schema change.
The complete 3,166,685-byte PROJECT_STATE task-entry prefix is retained.
Unrelated pre-existing settings/packages/QA changes are preserved.

Recheck receipts, current source/build fingerprints, protected sources and history:

```text
python scripts/quality/report_m165_snow_gaps.py
```

Next: owner tests Sprint's affected junctions and minute-plus TAB gaps in the new
fullscreen build before deciding whether to close M16.5. No M17 work is started.
