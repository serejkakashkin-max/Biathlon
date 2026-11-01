# M16.6.3 — Absolute race weather

2026-11-01. Implementation and qualification in progress; M16.6 OPEN.

ADR-0086 replaces the old static-only restriction for new versioned races.
The race seed chooses an isolated deterministic scenario (60% stable; remaining
warming/cooling/snowfall/ending snow/wind/rare two fronts). No career RNG draws.
Authored fronts begin gradually over240seconds; snow waits180seconds and blends
over600seconds. Frames depend only on absolute race seconds, including all Sprint
intervals, Pursuit gaps and simultaneous starts. Existing SnowCatalog and
SkiEquipmentSystem outputs compose once. Rifle geometry and athlete stats stay
unchanged. Signed world driving and drafting use the same vector wind as shooting.

New replay metadata freezes the resolved timeline; missing timeline metadata
explicitly keeps static legacy weather. Equipment/start/contact versions remain
independent. Front events enter the authoritative field event sink, including
journal playback, rather than a UI timer. Forecast observes the authoritative
personal scheduled start. Only owned skis may be selected in the detached prepared
snapshot; source save, training result and race seed do not reroll.

HUD reads actual accepted race time/equipment, with current air and ski glide.
TAB retains checkpoint history and adds snow/temperature/wind/glide outlook.
WeatherPresentation remains the single global atmosphere lease. The environment
observes frame wind/visual/precipitation without becoming authority.

Initial pure focused six tests PASS; actual20-run,8-lap time-trial matrix is in
artifacts/m16-6/weather/measured-weather-matrix.csv. Different skis lead in ice,
fresh snow and a thaw; weather alters measured finish times. This is measured
simulation output, not a fabricated rank/reward scenario. Further native runtime,
replay, build and standalone qualification are pending.

Focused qualification:45/45.NET weather/neutral traces/Sprint field/metadata;
12/12 historical v25 Git-blob journal and start-order compatibility regressions;
6/6 native EditMode;4/4 native PlayMode (actual frame/lease restoration, forecast,
production launch/HUD and replay UI);3/3 final Mass Start replay/backward seek/
legacy snapshot and forecast recapture. Forecast overlaps the4-case run.
Expanded native weather batch exceeded the300s tool response limit and is not
counted as passing. Smaller focused groups returned complete positive receipts.
Build tool reconnected mid-call, but build-summary/output prove Succeeded,
0errors/520existing warnings,430930148bytes,37.951s. Actual fullscreen standalone
PASS/exit0/2560x1440,bib15,personal3-2-1-0,tick8405,owner profiles unchanged,
no captured errors. No FPS or optional navigation benchmark inferred.

The full pre-weather.NET assembly remains running and has found5 existing legacy
AiStartTraffic failures. Exact1717c530 export reproduces all5: four stalled bypass
cases with identical nanometre remainders and a front-row fixture expecting zero
while the current authored launch is87.832m before the merge. Baseline receipts
are retained. Separate diagnosis is required; no test or threshold is disabled.
A sixth equipment-palette assertion assumed3models; it now checks the actual
8-model career catalog and original stable colors,6/6 regression PASS.
M16.6 remains open and is not represented as a globally green assembly.
