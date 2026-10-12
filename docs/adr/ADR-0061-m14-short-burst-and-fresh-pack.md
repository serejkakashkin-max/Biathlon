# ADR-0061 — M14 short burst, fresh field and retained timing gantry

2026-10-12. Owner-requested correction; M14 remains pending owner testing.

## Context

The owner reported a duplicate stadium timing gate, rested Mass Start athletes
creeping on the opening climb and excessive gaps at the first split. Shift also
needed a distinct, short acceleration with a visible cost and a clear return to
the selected pace.

## Fresh field movement

Keep the moving-body collision forecast for passing decisions. Apply braking
only within the closing-speed stopping and reaction envelope; detecting a future
blocker must not cause the following athlete to coast long before contact.

For ordinary BaseRoute, StartApproach and PenaltyLoop movement, use the existing
bounded nominal/pursuit steering. The one-tick passive snow inverse could choose
nearly transverse skis, dissipating speed despite substantial delivered power.
It also omitted the active pole pivot used by the actual motion solver. Preserve
the inverse for precise private mat movement.

These changes correct AI control decisions. They do not grant speed or power
bonuses or change the shared ski force solver. The player and opponents continue
to pay the same physical resource costs.

The actual first registered split, rather than only the climb crest, is the
acceptance boundary. Fixed-seed thirty-athlete fields produced these gaps:

| Field | Previous p90 / maximum | Corrected p90 / maximum |
| --- | --- | --- |
| Review | 34.438 / 49.024 s | 4.151 / 7.956 s |
| Junior | 40.405 / 59.442 s | 6.960 / 7.708 s |
| Senior debut | — | 4.268 / 4.669 s |

## Shared short burst

Add an optional, versioned burst profile to physiology. Controls through 950
retain the existing normal curve, whose maximum is 1.3 relative effort. Above
that boundary, the curve rises continuously to 1.6 at control 1000. This mapping
applies to every athlete; it is not a player-only acceleration multiplier.

Full burst power is available above 20% anaerobic reserve. Below that boundary,
available power fades continuously with remaining reserve. Existing anaerobic
drain, fatigue, heart rate and recovery systems charge the work. Individual
capacity determines duration; easing off does not instantly refill the reserve.
Definitions without the optional profile retain their historical effort mapping.

Shift requests Attack at 1000 while retaining the chosen W/S pace. Releasing it
returns to that pace through the existing bounded control response. Production
keyboard and normal gamepad trigger pace both stop at 800; gamepad Attack remains
explicit. The optional gamepad ceiling defaults to 1000 for historical device
definitions.

The compact burst overlay replaces the pace readout temporarily. It shows the
actual reserve and approximate time until full power fades, using the athlete's
individual physiology and accepted power. Braking and held low posture suppress
the burst overlay because they do not deliver burst work. Telemetry is read-only
and allocation-free; presentation never advances resources.

## Timing geometry

Move the stable `range_exit_split` anchor from 935 m to the main loop end at
960.005 m, after the penalty rejoin. Reuse the existing native TIMING gantry and
add only the authoritative snow stripe beneath it. Retain the two other useful
timing portals. The loop-end checkpoint must not create a zero-time split at
the race start.

## Consequences

New production race outputs change deterministically with the corrected AI
controls and paid burst profile. Historical definitions preserve their previous
behavior. Focused movement, first-split, device, resource, replay and presentation
checks accompany the change; build and qualification evidence belong in the M14
review report and append-only production state. Owner approval is still required
before closing M14.
