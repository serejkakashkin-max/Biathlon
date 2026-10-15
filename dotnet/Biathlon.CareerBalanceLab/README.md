# M15.6 authoritative career competition study

This lab runs the actual `CareerRaceProvider` and Woodland venue with its manual
signed movement composition, shared 20 Hz field, race snow/weather, physical
shooting geometry, penalties and classified results. It does not create aggregate
training outcomes and call them races. The separate `Biathlon.DevelopmentLab`
owns the hundreds-of-trajectories development experiment.

From the repository root:

```powershell
dotnet build dotnet/Biathlon.CareerBalanceLab/Biathlon.CareerBalanceLab.csproj -c Release --disable-build-servers -m:1 -p:UseSharedCompilation=false
dotnet dotnet/Biathlon.CareerBalanceLab/bin/Release/net8.0/Biathlon.CareerBalanceLab.dll . all
python dotnet/Biathlon.CareerBalanceLab/validate_study.py .
```

`sprints`, `openings`, `fairness`, `counterfactuals`, `smoke` or an exact case ID may
replace `all`. Rebuild after
an authority source change; the executable has its own copied dependency DLLs.
Do not time/profile the game simultaneously with this CPU workload.

The fixed career seed is 71000. Main race seed is 123; matched World Cup presets
also run on seeds 124 and 125. Cases are 9 own-athlete presets
(three circuits × balanced/skier/shooter), the real strongest and weakest World
Cup catalog entrants, one full-effort junior controlled through the actual player
input boundary, three fresh MassStart first-split openings and a complete repeated
Sprint for determinism, plus the six additional matched races. There are 19 full
Sprints and three openings. Full runs use 30 athletes. The initial ability and peers
are fresh creation/catalog snapshots, not invented post-training ratings.

The controlled case requests 1000 effort and a precise deterministic shooting
controller, including actual manual bolt commands and breathing. It explicitly
retains automatic physical route steering/braking so human driving mistakes do
not confound the sports-capability probe. It is an upper-skill experiment, **not**
a human win-rate prediction or a different player physics mode.

The 80–180 m opening window includes both climbing and a real short descent.
`TuckTicks` counts all tuck requests in that window; `UphillTuckTicks` counts only
positive prior-step grade (>0.005), matching the actual command observation.
Do not interpret legitimate downhill tuck as a climbing-posture bug.

Every bound skiing/shooting stat is checked against the actual 13 career stats and
the production half-even integer projection. Saved appearance and legacy scalar
potential are changed in a matched counterfactual: the authority context must be
identical. A separate one-tick non-neutral fatigue/form/confidence experiment
checks that player input receives the same readiness and psychological projection
as AI. It intentionally submits fresh readiness in the raw device command to
verify that the host replaces it with the saved athlete's state.

Outputs under `artifacts/m15-6/authoritative-race-study/` include per-case classified
rows, elapsed first-climb crossings, actual first-split gap, consecutive sub 0.3 m/s
ski intervals, penalties, shots/hits, event/result hashes, timing and definition
and authority assembly hashes. The low-speed measure excludes prestart and shooting phase; brief mat and
traffic braking can still be included. Summary files retain their selected scope.
Failures are recorded with their actual exception; no fallback or synthetic
result is counted. Stopwatch times are laboratory evidence, not game FPS claims.

Three seeds are a representative regression sample. Do not infer universal format
suitability, guaranteed rank, or a final shooting accuracy distribution from it.
