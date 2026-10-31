# ADR-0077 — M16.4 live vector minimap

2026-10-31. Owner requests a compact left-side map showing the player and moving
rivals. This new request extends the earlier M14 scope recorded in ADR-0066;
that historical record's “no new minimap” describes its original slice.

Use Frostline Deep#0B1928, Ink#E8F5F8, Muted#B0C4D0, Accent#85D5DF and Gold#D6AA69.
Oswald carries the small heading and Roboto the legend. Place the compact196px
card below the actual timing-card bounds, with a12px gap and155px vector diagram.
Text follows accessibility scaling; the diagram retains a readable compact size.
The signature is the gold five-target firing line within the actual course map.
The player's cyan directional arrow is painted last with a dark outline/halo;
opponents use light circles, waiting starters hollow circles and actual active
shooters gold circles. Shape distinguishes the player without relying on colour.
Nearby markers may overlap in a dense pack; do not falsify positions to separate
them. The scope is a fixed overview, without interactive zoom or mouse capture.

Reference: [Gran Turismo's race-screen manual](https://www.gran-turismo.com/us/gt6/manual/howtorace/racedisplay.html)
uses a separate course-map instrument. Our visual language and biathlon landmarks
are original Frostline art. [Unity6.6 Vector API](https://docs.unity3d.com/6000.6/Documentation/Manual/UIE-generate-2d-visual-content.html)
supports retained custom paths in UI Toolkit. No external images or extra world
camera, render target, terrain pass, blur or icon package are added.

Share the existing rendered-course `RouteUsage` helper by changing only its
assembly visibility. Do not duplicate/change its format/private-mat/penalty/start
slice selection. At loading time, sample these actual authoring intervals into
detached bounded vector arrays. Divide integer millimetres by1000d, and use the
exact final endpoint rather than an arithmetic interpolation that can overshoot
the strict visual-loop domain by a floating-point ULP. Uniform X/Z projection
preserves aspect ratio, reverses Z for screen Y, and fits the selected geometry.
The finish icon samples the actual final route-section endpoint; the firing line
uses the first/last typed mat anchors. No hand-drawn course or inactive-format
route is substituted for the venue.

At the existing data-driven HUD cadence, consume only `RacePresentationSnapshot`.
Use accepted world X/Z and heading where available, including off-centre movement.
Legacy observations use the same authored visual-loop/lateral convention as
athlete presentation. The preallocated128-actor buffer rejects foreign snapshots,
deduplicates the same observation and clears on race reset. Non-starters/DNF/
invalid results are excluded from visible rivals; finished athletes remain.
An extreme off-map marker stays at the viewport boundary instead of stretching
the course or changing other athletes' positions. No scene-object search or
field-step call is introduced into map observation or drawing.

Keep separate retained course and actor vector layers. Moving markers dirty only
the latter, not the static course paths. Cache geometry across locale/contrast/
enable rebuilds. All map elements ignore picking and focus. Pause hides the
existing gameplay document; TAB/F2 tables and the fixed shooting view suppress
the map. This preserves the approved clear sight and whole-HUD area limits,
including the shot-evidence panel. The map returns on skiing/resume/table close.

The definition has stable schema/id and bounded geometry/marker/UI sizes. No
Domain/Simulation/AI/Career/Replay, balance, physics, shot geometry, save or replay
event change. Native geometry/snapshot/zero-allocation-buffer/layer-cache/layout/
input lifecycle and compiled-player evidence: `docs/quality/m16-4/minimap/README.md`.
