# TASK-132 — Range presentation

WP132 complete. RangePresentation renders five circular apertures from target_geometry_v1 in a lane-local camera stage. The camera follows copied authoritative sight angles; red screen-centre reticle does not cast physics rays or decide hits. White closures, lane, stance, shot count and last observed outcome come from detached snapshots. Prone/standing diameter is read from shared geometry. View hides outside the active range and resets on race reset.

Validation: Unity PlayMode actual first bout1/1 (15 discovered): camera visibility, authoritative closure mask and return to skiing. .NET and Unity EditMode snapshot twin-race tests1/1 each retain movement/event parity. Game View inspected after two accepted shots: two white closures, three black apertures and lane/stance/result label (docs/quality/m10/wp132-range.png). Final TestRunner inactive; Console0 errors/1 warning, compilation_failed=false. Initial enum/cast compilation errors were corrected before passing verification.

No gameplay/balance changes. Greybox view uses an isolated local target stage and primitive disks; no weapon model, animation or audio yet. The host must publish/refresh each completed shooting tick to retain every transient outcome; closure count survives skipped observations. Zoom/feel awaits G1. Shooting device input follows WP133.
