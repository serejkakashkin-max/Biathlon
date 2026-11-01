# ADR-0086 — Absolute race weather, snow inertia and replay

2026-11-01. Implemented for M16.6.3; focused/native/standalone evidence in docs/quality/m16-6/weather. Global legacy regression audit remains open.
Supersedes ADR-0063’s static-race restriction for explicitly versioned new races.

An immutable timeline holds the initial RaceWeatherPreset and zero to two fronts.
Front targets and timing/weights are authored data. A race-seed-derived isolated
selector never advances career or athlete RNG. Most selectors produce static
weather. Evaluate at absolute race ticks, including staggered starts; never use
athlete elapsed time, Unity Time or wall time for authority.

Air/wind/precipitation interpolate gradually. Snow temperature and surface blend
start later and transition more slowly than air. Sampling is pure and order
independent, so seeking requires no accumulated weather state. SkiEquipmentSystem
blends its existing snow/equipment outputs once; there is no second friction model.
Route following and signed manual world motion use the same sampled output.
Player and AI observe the same wind and snow at a given global tick.

Freeze the complete timeline in new replay metadata/context. Missing metadata
selects old fixed weather explicitly; never inject a dynamic timeline into an old
journal. Keep weather, equipment and start-order versions independent. UI forecast
uses the actual scheduled player start; owned-ski selection updates the detached
prepared snapshot without spending funds, rerolling training, seeds or opponents.

WeatherPresentation remains the sole lease owner for RenderSettings/sky/fog/light.
The environment reads accepted race time for visual changes and wind; shaders,
particles and HUD cannot modify authoritative state. Existing TAB timing controls
remain usable; weather details integrate into that panel.
