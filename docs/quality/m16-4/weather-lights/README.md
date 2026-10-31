# M16.4 WP-N/O — recorded weather and illuminated evening venue

2026-10-31. READY FOR OWNER TEST; M16.4 remains OPEN.
Fullscreen candidate: `Builds/M16_4_Weather_Lights_Review/START_TEST.cmd`.

## What is connected

The original four career forecasts already reached clear frost, overcast/ice,
snowfall/fresh snow and thaw/wet snow (warm haze is the existing wet visual).
Light snow and dawn/late-day art existed but were not independent career rows;
there was no fog/evening row or course lamp system. Ten rows now reach clear,
overcast, snowfall, light snow, fog, dawn, late day and clear/snowy evening.
The original four rows and all prior weather/lighting preset values are retained.
Calendar event/season rotation remains deterministic, without consuming save RNG.
Upcoming unrecorded forecasts change as the expanded sequence now includes the
requested conditions. Existing recordings retain their full weather snapshot;
legacy metadata still follows the explicit neutral path. No save/replay migration.

Forecast, preparation text, live environment, snow/wind and replay use the same
recorded preset; light snow also selects the existing soft-snow/precipitation audio.
New time-of-day/atmosphere descriptions have RU/EN labels. No runtime weather menu
or technical captions are added to the racing HUD. Conditions are fixed per race.

## Venue lamps and budget

Actual Woodland layout:63outside-course masts, about36m spacing, ground-projected,
clear of every authored corridor, tree trunks, coach enclosure and shooting lanes.
Both main/format branches, start/finish and penalty loop receive overlapping pools.
Firing line/target area has staggered coverage. Two instanced fixture submissions,
shared steel/LED meshes, no gameplay colliders or route/terrain edits. An evening
profile automatically turns lamps on; daytime/dawn/late-day leave them off.

Stationary illumination is integrated once at load into a256²RGBAHalf diffuse
field (524288bytes). It is never centered on the camera or reconstructed each
frame. Snow, athlete suit/bib and coach shaders retain original textures, geometry,
detail and normal/shadow paths. Daytime bypasses the additional texture sample.
There are no dozens of realtime course lights or local shadow maps. Six fixed
shadowless target-fill lights make standard target-bank materials readable.
No extra world camera or render target. Warmed fixture rendering allocates0bytes.
`illumination-budget.json` records all counts and minimum sampled whole-course
field value0.7889877 (project-relative units; not real-world lux).

Global illumination state has one explicit lease and is restored on environment
disable/shutdown. Native tests exercise reenable, repeated looks and teardown.
Review/spectator/shooting cameras share the same world illumination.

## Verification

42unique checks PASS:27native PlayMode plus15core/deterministic cases.

- `native-final.json`: actual career snowfall launch, eight recorded weather
  bindings including evening/fog/light snow, actual Woodland all10look gallery,
  bounded layout/coverage/no obstacles, shared lighting/lease/allocation tests,
  unchanged G2 profiles and all three quality-tier weather bindings.
- `native-readable-final.json`: reruns all three floodlight cases after final
  target fill tuning and shader warning correction. Adds actual front-plate pixel
  readability assertions at lanes0/7/14/21/28, rather than accepting enabled light
  components alone. Final `evening-target-*.png` images are retained. No test was
  removed, disabled or weakened to make a failure disappear.
- `weather-core-final.trx` / `core-summary.json`:15PASS, including full30athlete
  snowfall Sprint twice with exact result/event hash equality;120tick manual
  journals for new fog/light-snow/clear-evening/snowy-evening snapshots; old/drawn
  Sprint journal playback; forecast/save RNG preservation; recorded overrides,
  legacy replay and invalid-data rejection.
- `weather-red.trx`: the pre-fix calendar-coverage test fails because fog is absent.
  `native-first.json`: expanded actual binding catches missing evening in the
  venue-specific G2 catalog. Explicit base inheritance now supplies common new
  looks without copying tuning or changing its original four profiles. An older
  light-binding test assumed the first hierarchy Light was the sun; it now selects
  the unique directional light and asserts all local fills are shadowless/off
  during day. The five affected cases pass in the final27case receipt.

The first compiled visual inspection found target plates too dim despite enabled
lights. Tune their finite fill spacing/setback/intensity, then add the real pixel
checks above and rebuild. Original target geometry, scoring radius, hole colour,
shot traces and closure rules are untouched. An early DX shader compilation
reported possibly uninitialized branch output; explicitly initialize the output
before its daytime bypass. Final build returns to520existing warnings, no new
Biathlon shader warning or shader error. Five touched shaders support native GPU.

Final Windows development build: Unity6000.6.0f1, Succeeded,0errors,
520existing warnings,430672299bytes,24.408s. Shared historical build receipt ID is
unchanged; milestone.json and launcher identify M16.4. Final standalone startup
and same-binary day/evening cases are stored in the adjacent receipt folders.
The daytime/evening matrix is1920×1080 offscreen native world plus30real athletes,
same accepted tick/cameras/geometry/textures, two positions × three quality tiers,
240samples/case =2880samples. Menu/HUD/swapchain cost is excluded. Unsupported
GPU counters remain unavailable, never zero. See `performance-summary.json` for
measured wall/draw/triangle counters and scoped comparison; this is not a whole
game FPS certification.

Final six pairs: day/evening median wall times1.202–1.605ms/1.334–1.675ms;
maximum paired median increase0.132ms, ratios0.994–1.110. Different sun orientation
changes shadow submission work, so this does not isolate lamp-only GPU overhead.
GPU/frame CPU timings were unavailable. Whole harness median allocation34bytes
is identical in each pair; the separate warmed fixture renderer measures0bytes.
Final compiled evening reports63masts/6target fills, lamps on,0captured errors;
day reports the same fixtures with lamps off. Final ordinary2560×1440
FullScreenWindow startup exited0, random Sprint bib/start ordinal15,3–2–1–0,
all4loader stages hidden from HUD, owner profiles unchanged and no captured errors.
Optional advanced navigation probe was not enabled and is not claimed.

## Authority, history and limits

Only core change is the `RaceWeatherPreset.Validate` whitelist. Physics, scoring,
AI, career and replay source files remain byte-identical to task entry. New rows
reuse existing snow/wind behavior for every entrant; evening/fog introduce no
hidden speed/hit modifier. Full task-entry PROJECT_STATE prefix is preserved.
Owner profiles are untouched by isolated compiled qualification. Renderer/tier
assets and unrelated local changes are excluded from this publication.

Lighting is a static diffuse approximation, without per-mast dynamic athlete
shadows, specular transport or volumetric beams. It follows the IBU uniform
coverage/stronger range principle, not calibrated photometric certification.
References and rationale: [ADR-0081](../../../adr/ADR-0081-m16-4-weather-coverage-and-evening-venue.md).
Run `python scripts/quality/report_m164_weather_lights.py` to verify this archive.

Next: owner tests forecast variety and evening Sprint/Pursuit/Mass Start on the
fullscreen candidate. No owner approval or milestone closure is inferred.
