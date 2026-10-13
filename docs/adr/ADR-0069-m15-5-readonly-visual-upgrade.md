# ADR-0069 — M15.5 read-only visual upgrade

2026-10-13. Implemented for owner review between M15 and M16.

## Context and decision

The owner requests stronger skiing animation and winter art without changing
the accepted mechanics, M14 interface or M15 audio. Keep G2 Hero and its typed
17-bone/prop contract. The new `AthleteHybridLocomotionDriver` consumes the same
detached animation frame as the original driver. Retain the original as an
explicit fallback and side-by-side review path.

Use original normalized manual curve keys, smooth numeric profile transitions,
speed-derived cadence and bounded limb IK. Terrain chooses V1 uphill, V2 alternate
on the flat and V2 during fast effort; no source motion dataset or mocap is imported.
Twelve presentation states share existing exertion, ground-contact and grip adapters.
Ski edge correction runs once after ground alignment. The adapter protects model
and race roots. No pose result feeds simulation, steering, stamina or shooting.

Keep the four existing joint-volume shapes as the first channels. Append three
small original cosmetic face/shoulder shapes, chosen once from the stable athlete
ID. National kit, bib and purchased ski bindings remain independent.

Improve the existing snow shaders and bounded tracks/spray. New original log,
stump and drift geometry reuses audited fir bark, shrub and rock assets. A small
versioned cluster recipe projects onto the existing terrain, checks clearance
against every course corridor and uses explicit-camera instancing/culling/LOD.
It adds no gameplay collider or terrain/path edits.

`WeatherPresentation` remains the sole scene lighting/sky/fog owner. The original
three physical career weather IDs are retained. Presentation adds light snowfall
and winter haze: existing wet overcast at/above the visual temperature threshold
uses haze; snowfall density crossfades over a data-driven 120-second visual cycle.
The first 45 seconds retain the base look; the light interval fades in/out over
eight seconds. Physical weather, wind, snow friction and saves/replays are unchanged.
High forest visibility reserves the maximum fog reach of both snowfall looks.

Legacy quality IDs remain `low/balanced/high` for saved compatibility, displayed
as Performance/High/Ultra. All retain the same hero and texture assets. Existing
shadow/forest profiles and bounded spray budgets distinguish the three levels.
Graphics preference is local presentation data, not career state.

## Validation and limits

The work has native zero-allocation/root/contact tests and a comparison scene.
Production native before/after capture uses identical observation inputs, cameras,
FOV and base light preset. Gallery-only manual skin matrix recalculation avoids
stale meshes during repeated offscreen rendering; it is not enabled on every
gameplay athlete. CPU/render counters are measured separately from screenshots.
Unavailable GPU/VRAM timings remain unavailable, not zero. Full evidence and
owner acceptance are tracked in `docs/M15_5_VISUAL_WP_STATUS.md`.
