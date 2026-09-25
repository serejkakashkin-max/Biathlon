# ADR-0021 — Directional live input, absolute recorded commands

Accepted from owner hands-on feedback, 2026-09-24, during pre-G2 remediation.

The owner reported unwanted return to centre after releasing A/D. The live adapter now converts keyboard/stick direction into a one-tick lateral target based on the actual achieved position and the existing data-driven steering speed. A neutral device holds the achieved line; blocked manoeuvres do not accumulate an inaccessible target. Gamepad deadzone/response curve remain applied once. Focus/device loss cancels actions and requests recovery while holding the current line.

ADR0006 and saved PlayerCommand schemas remain unchanged: LineIntent is still an absolute normalized lateral target. The legacy DeviceCommandBuilder.Build method remains available for existing absolute-position tools and tapes. PlayableSprintHost uses BuildDirectional at fixed simulation ticks, never render-frame accumulation. Collision constraints, edge clamps and lateral effort/propulsion costs still belong to simulation. User driving output changes intentionally; existing recorded commands and AI inputs retain their old meaning.
