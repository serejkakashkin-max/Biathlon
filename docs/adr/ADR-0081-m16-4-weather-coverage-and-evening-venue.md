# ADR-0081 — Career weather coverage and evening venue lighting

2026-10-31. Owner requests every developed weather look in actual races, including
fog, snowfall, clear/overcast and evening races with automatic course lamps.

## Findings and scope

The four recorded career rows already reached snowfall, clear, overcast/ice and
wet thaw. M15.5 supplied light snowfall and warm winter haze, while dawn/late-day
lighting existed only for previews. The domain whitelist admitted only three
atmospheres and two light profiles. There was no evening forecast or floodlight
layout. A new calendar-coverage test fails on missing fog before implementation.

Extend the existing versioned weather catalog with fog, clear evening, light snow,
dawn, late day and evening snow. Preserve all four original rows/IDs and their
values. Ten rows rotate by the existing event/season rule without RNG consumption.
Existing recorded weather snapshots continue to bind their original conditions;
legacy metadata remains neutral. No save or replay schema migration. Upcoming
unrecorded events may select the newly requested conditions. New fog/light-snow
IDs and dawn/late-day/evening lighting are admitted by strict validation. The
forecast, race, audio and replay all use the same recorded preset. RU/EN preparation
shows weather and the relevant time of day. Physical snow/wind remain the existing
catalog/geometry model; fog/evening add no hidden speed or hit modifiers.

An initial native run caught a second binding boundary: actual Woodland and
environment fixtures load `g2_lighting_visual_v1`, rather than the preview's
common catalog. Retain its audited four profiles exactly and explicitly inherit
new common profiles via `base_resource`. Evening tuning therefore has one source.
Native review switching also reads the bound venue catalog, preventing preview
lighting from disguising a production binding omission. The failing receipt is
retained; neither the missing evening checks nor their assertions are removed.

## Layout and illumination

Optional `floodlights_resource` belongs to the venue presentation recipe. Cold
layout samples every authored corridor, including format branches/start/finish
and penalty loop. Masts sit outside all corridor envelopes, on sampled snow,
avoiding coach footprints and existing tree trunks. Staggered mast rows also
cover the firing line and targets. No gameplay collider, route or terrain edit.
One validated resource owns spacing, dimensions, finite mast budget and lighting.

Course fixtures use shared instanced metal/LED meshes (two submitted batches).
The stationary diffuse field is integrated once at loading into a 256² RGBAHalf
world map (0.5 MiB), not rebuilt per frame or centered on the camera. Snow and
athlete/bib/coach shaders sample it only when lamps are on. Original texture,
normal, groomed detail and shadow paths remain intact. This avoids Forward URP's
four-lights-per-object limit darkening a long course mesh, and avoids dozens of
realtime lights and shadow passes. No extra world camera or render texture.

A small fixed set of shadowless target fill lights lights ordinary target-bank
materials. It is budgeted separately (at most eight), stays in the range and
turns off by day. It does not follow the camera or switch at approach thresholds.
No per-tick work, animated light noise or hidden race authority. Warmed render
allocation and native day/evening/fog/snow views are acceptance checks.

The evening light profile owns lamp gain: automatic on in evening clear/snow,
off in daytime, dawn and late-day looks. Lightmap globals have an explicit lease
and restore on environment disable/shutdown. Replay/spectator/aim views share the same
static illumination. Weather remains the sole sky/fog/directional-light owner.

This is a static diffuse approximation, not a photometric or light-transport
certification. It does not model multiple dynamic athlete shadows from each mast.

## References and interpretation

- [IBU September2026 ECR3.1.2.4](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf),
  page28: common lighting for entrants, uniform course coverage and stronger
  target/finish illumination. The game uses that layout/readability principle;
  no claim to calibrated real-world lux compliance.
- [Gautefall venue](https://www.visitnorway.com/listings/gautefall-biathlon/241524/):
  floodlit whole trail and stadium. [Kontiolahti official race photographs](https://www.biathlonworld.com/de/news/statistics-biathlon-kontiolahti/pL7XfxJanqlViEnoi0CBM)
  inform tall outside-course masts and clustered LED heads.
- [Unity6 Forward and Forward+](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/use-built-in-shader-methods-additional-lights-fplus.html):
  Forward light-per-object constraints; [shadow mapping cost](https://docs.unity3d.com/6000.6/Documentation/Manual/shadow-mapping.html)
  explains avoiding local shadow maps. Existing renderer/quality assets unchanged.

M16.4 remains open pending owner test. Evidence and fullscreen candidate are
recorded under `docs/quality/m16-4/weather-lights/` and the WP/state ledger.
