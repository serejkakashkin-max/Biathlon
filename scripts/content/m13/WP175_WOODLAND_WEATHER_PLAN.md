# WP175 — single-Woodland conditions and readability plan

Owner scope dated 2026-10-05 leaves Woodland Basin as the sole production venue.
Master Plan WP175 still requires conditions, readability and performance validation.
This recipe and CLI prepare explicit inputs only. They do not execute Unity, qualify
performance, complete a race or mark WP175 done.

Run from the repository root with the configured Python runtime:

```powershell
python scripts/content/m13/wp175_weather_matrix.py --output artifacts/wp175-woodland-weather-plan-20261005
python -m unittest discover -s scripts/content/m13 -p test_wp175_weather_matrix.py -v
```

The source recipe contains references to canonical definitions, not numeric tuning.
The resolver records current resource SHA-256 values. It generates five environment
copies and four `RaceSnowConditions` definitions **outside Assets**. Every physical
track surface is bound explicitly to one selected canonical snow profile.

| Presentation row | Lighting reference | Purpose |
| --- | --- | --- |
| High / clear | `clear_cold` | Primary quality, direct light and shadow readability |
| High / overcast | `overcast` | Diffuse silhouettes, forest and stadium readability |
| High / snowfall | `overcast` | Full weather density, shooting and course visibility |
| Balanced / snowfall | `overcast` | Reduced density and LOD readability |
| Low / snowfall | `overcast` | Lowest supported density and LOD readability |

Use a fresh environment initialization for each row. Production forest visibility
binds its fog horizon during initialization; merely changing `WeatherPresentation`
on an already initialized clear environment would not qualify the snowfall row.
Preserve Woodland's existing fog-distance scale and geometry. The CLI derives
resolved horizons and candidate particle ceilings from the canonical files. A
candidate ceiling is not the actual submitted flake count after height/camera cull.

Each row needs ordinary `screenshot-game-view` inspection at these six scopes:
actual chase skiing, technical descent/tuck, Mass Start 30 stadium traffic, an
explicit broadcast stadium camera, an actual paid prone bout and an actual paid
standing bout. Use accepted simulation observations for athletes and shooting;
do not manufacture shooting poses or completion counters to produce a picture.
Record venue/format/field/tick, physical condition ID, visual weather/light/tier,
camera pose/FOV, actual flake/forest/crowd counts, source hashes, image hash and
native Console state. Inspect course edges, rival separation, snow contact,
forest horizon, event landmarks, both target apertures and weather occlusion.

The physical condition axis is `packed_neutral`, `fresh_soft`, `icy_fast`,
`wet_slow`, using `Biathlon/snow_catalog_provisional_v1`. Visual cloud/snowfall
selection does not mutate authoritative wind, shooting or race snow. Generate
condition bindings before creating a race, and record those bindings in replay
context. The planner lists Sprint 40, Pursuit 40 and Mass Start 30 for each physical
condition; Pursuit must consume the actual corresponding Sprint result. Planned
rows and bounded prefixes are not complete races. Existing WP169 neutral proof
is historical acceptance, not evidence for the other three condition bindings.

Focused implementation verification can reuse `WeatherTests`, `G2LightingTests`,
`ProductionEnvironmentTests`, `ProductionForestVisibilityTests`,
`SnowConditionTests`, `SkiForceTests`, `CorneringTests` and replay regressions.
Before writing new Unity tests, check whether the existing tests already cover
the intended behavior. The missing useful cross-system case is a parameterized
actual Woodland environment lease for all five rows, checking the initialized fog
horizon, particle bounds, copy isolation, unchanged core resources and restoration.
The missing physical case is a real Woodland condition binding sampled on every
loop, then a deterministic force/cornering probe proving soft/wet resistance and
ice grip trade-offs through the existing shared systems. These proposed tests have
not been added or executed by this planning task.

For performance, retain separate evidence for warmed CPU/GC, actual submitted
geometry/memory and any counters really available in a normally rendered frame.
Synthetic loop timings, detached fixtures, hidden-player frame suspension and
unavailable GPU/render-thread counters do not qualify sustained FPS. The owner
still permits only hidden standalone checks; preserve that limit and report any
remaining performance qualification as unavailable, rather than recording PASS.

No new asset imports, authoritative tuning, Unity scene configuration, state-file
edits or Git operations are performed by this planner.
