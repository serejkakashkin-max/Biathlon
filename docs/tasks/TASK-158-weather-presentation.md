# TASK-158 — Weather presentation

Status: DONE. Full PlayMode70/70, final presets/captures, pixel-exact rewind and CPU budget verified; see TASK-158-weather-presentation-REPORT.md.

Implement bounded snowfall, explicit visual wind, fog/visibility and cloud/light presets for the benchmark. Preserve fixed-tick race weather and shot physics; presentation reads explicitly supplied values and must not invent authoritative gusts. Provide reversible scene/camera/light ownership, deterministic sampling/rewind, quality budgets and generated original assets. Validate bounded particles, repeatability, state restoration, preset constraints, Console, fixed captures and runtime cost. Keep shooting readability and G1 cameras; no silent difficulty changes. Integrate with final sector in WP164 and carry camera-specific tuning into WP165. Then WP159 breath vapor.
