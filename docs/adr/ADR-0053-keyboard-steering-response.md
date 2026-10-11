# ADR-0053 — Quicker keyboard steering correction

2026-10-11. Implemented. The owner accepts the M13 BotRecovery version and asks
for sharper A/D correction before moving to M14, without another build.

The production keyboard deflection rate in `device_control_gameplay_v3.json`
changes from 2000 to 4000 command units per second. At the authoritative 20Hz
tick rate a held key reaches the full 1000-unit deflection in five ticks (250ms),
instead of ten ticks (500ms). The first tick requests 200 units. Reversal starts
immediately in the new direction; releasing both keys returns neutral.

This changes the keyboard command history openly. The physical steering, snow
grip, yaw limits, power/effort, explicit braking and AI policy stay at the owner's
accepted values. Analog steering remains analog. There is no heading snap,
automatic centring, course following or additional A/D speed penalty.

Focused tests cover both directions through the real device-to-snow boundary,
release/reversal, gamepad analog input, tick/render sampling independence,
absolute command replay and the native Unity Input System adapter. Broader
four-range testing exposed an existing `MovingFiringArrival` failure; an exact
pre-change input/steering baseline reproduces it. It is recorded separately and
the test remains enabled. No new whole-race balance or FPS claim is made.

No build is created. The next authorized work is the M14 interface foundation.
