# M14 — career weather correction, 2026-10-12

M14 remains **OPEN / owner testing pending**. The weather variants existed, but
the career provider selected neutral snow and the menu/live/replay adapters
always loaded Woodland's clear environment.

## Delivered

- WP-181: one data-driven forecast for each calendar event/season; Russian and
  English pre-race weather, snow condition, air temperature and wind speed.
- WP-182: snowfall/fresh snow, clear frost/packed snow, overcast/ice and thaw/wet
  snow. Physics uses the existing friction/grip/equipment models for every actor.
  Player and AI share geometric shooting wind; observed wind drives range flags.
  Fresh and icy trails reuse the authored loose/ice material layers. Wet snow
  retains blended terrain structure and all detail textures. High quality is
  unchanged. Snowfall follows the active chase/shooting/replay camera.
- WP-192: full weather snapshot in new replay metadata; forward/backward seek
  retains it. Replays without this optional field keep legacy neutral conditions.
  Existing career saves need no migration or restart.

Core: RaceWeatherCatalog, CareerRaceProvider, ProductionVenueRaceFactory,
PackAiRaceRunner and snow-condition telemetry. Unity: CareerWeatherPresentation,
career/menu/replay adapters, ProductionVenueEnvironment/Host and owned blended
snow materials. Data: career_weather_v1, weather_visual_v1, localized text.
Decision: [ADR-0063](../adr/ADR-0063-m14-career-weather-binding.md).

## Verification

- .NET: 31 core/snow/wind/command-replay checks passed; 28 career/menu/replay
  checks passed (seven shared weather checks overlap those runs).
- Unity: six EditMode and ten PlayMode checks passed, including actual career
  launch, four material/atmosphere bindings, active shooting camera, bounded
  particles, zero managed allocation after snowfall warmup and replay seeking.
- Full 30-athlete snowfall Sprint repeated after saved-career reload: identical
  authoritative result and event hash; **29,546 ticks**, **300 shots**.
  Result SHA256: `3fbd4af262dcac02ad2b791590e29c8f84ddbbbf94bce88334a16c5b0f4703c7`.
  Event hash: `7692012904331965847`.
- Wet-material integration failure fixed: terrain configuration previously
  reset its roughness to cold after applying the wet preset. Test fixture player
  binding order and floating-point color tolerance corrected; no failing test
  was disabled. Final runs pass. Final Unity build/Console are clean.
- Windows x64 Development/DX11, Unity 6000.6.0f1: build succeeded with zero errors
  and zero warnings, **370,479,138 bytes**. Standalone startup exits zero.

Evidence: [weather-20261012](../quality/m14/weather-20261012/).
Launch: [START_M14_TEST.cmd](../../Builds/M14Weather20261012/START_M14_TEST.cmd).

## Scope and next check

Conditions are fixed within each race, as in GDD §15. The prototype calendar
rotates weather between events and seasons; it is not a meteorological model.
Rain, dynamic fronts, temperature physiology and new ski aerodynamic wind
effects are outside this correction. Weather changes race results through
shared physical conditions; no athlete statistics or hidden AI speed bonuses
were changed. Next: owner tests the build and decides further M14 corrections
or acceptance.
