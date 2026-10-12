# Compact HUD — owner visual revision

Owner requested a more modern HUD covering substantially less of the screen.
Redesigned `GameplayHud` and its visual JSON: compact race-clock/lap strip,
isolated speed, pulse/energy/fatigue instrument, small target rings and concise
weapon/breath feedback. Typography, spacing, translucent slate surfaces, cyan
accents and thin bars now belong to the HUD rather than inheriting menu scale.
Long help, athlete name and shooting history are available with F1. Critical
penalty/assistance messages remain contextual. F3 telemetry is unchanged.

Measured panel bounding rectangles: skiing 4.300%, shooting 6.240% at 1600x900;
shooting 12.887% at 1024x768/130% text. These are captured states, not a guarantee
for all strings or explicitly opened help/debug panels. Tests enforce layout
clearance and normal/large-text area budgets. The center stays free.

Validation: native PlayMode HUD lifecycle/shooting/layout test PASS, all seven
menu tests PASS (including real career launch, pause, keyboard and gamepad),
.NET HUD model 13 PASS. Unity compiled; recent Console Errors/Exceptions empty.
Inspected both transparent shooting HUD captures and actual career Game View.
During editing a text-encoding issue in the test file was corrected; all seven
menu cases then passed. No failing test removed or disabled.

Evidence: `docs/quality/m14/compact-hud-20261012/`. No simulation, balance,
graphics-quality or save-schema changes. No build. M14 still in progress;
WP-185 results remains next. PROJECT_STATE history preserved byte-for-byte
before the new appended entry.
