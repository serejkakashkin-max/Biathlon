# ADR-0063 — Recorded weather for career events

2026-10-12. M14 owner correction, WP-181/182/192.

Weather visual presets, snow friction/grip profiles and cold/wet snow materials
already existed. CareerRaceProvider always selected race_snow_neutral_v1 and the
live host, forecast menu and replay viewer always loaded the venue's clear
environment. Consequently the developed weather variants were unreachable in
the career flow.

career_weather_v1 is the authoritative, versioned condition catalog. Calendar
event ordinal plus season rotation selects one immutable preset; no career RNG
is consumed, no save migration is necessary, and cancel/reload retains the same
forecast. The first season cycles snowfall/fresh snow, clear/packed snow,
overcast/ice and thaw/wet snow. This is an authored prototype schedule rather
than a meteorological forecast. Subsequent seasons rotate the schedule.

The same core preset supplies RaceSnowConditions, shooting wind, menu facts and
the presentation environment. Every entrant uses the existing snow/equipment
physics and geometric wind drift. AI receives estimated drift through its
existing aiming observation; it receives no hidden hit or speed multiplier.
The live player receives the same race wind before the command is recorded.
Legacy/default runners retain their explicit wind inputs and neutral setup.
Ski wind drag and temperature physiology are not newly introduced here.

RaceReplayInfo gains an optional race_weather_json payload containing the
complete versioned preset, not just a catalog lookup. New playback/seek uses
that snapshot; older metadata without the field explicitly selects the old
neutral snow/clear environment. The career save version and command journal
format are unchanged. Existing authority context verification remains required.

Only owned materials are configured at environment load. Blended snow preserves
terrain weights, detail textures and packed/ice/edge structure, with the wet
palette applied after the terrain recipe. The wet integration test caught the
opposite initialization order resetting the material to cold. Snowfall uses the
existing loose snow layer on the trail; icy conditions use its authored ice
color, roughness and normal layer. These changes happen only at load, preserve
detail textures, and introduce no shader operations or additional draw calls.
Snowfall uses the existing bounded mesh, existing High texture quality and existing particle
budget. Weather follows the active camera, including the shooting camera, so
snowfall is not left around a dormant chase camera. Overcast has clouds without
flakes; snowfall is the explicit precipitation preset.

Conditions remain fixed for a race, as specified by GDD section 15. Dynamic
fronts, rain and additional venues remain outside this correction. M14 remains
open for owner testing and acceptance.
