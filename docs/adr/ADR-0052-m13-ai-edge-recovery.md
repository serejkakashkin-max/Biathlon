# ADR-0052 — AI recovery at the ski-course edge

2026-10-11. Implemented for owner review; M13 remains OPEN.

The owner reports occasional AI departures from the course and stopped athletes
on the penalty circuit after removing implicit A/D braking. Partial departures
remain possible, but AI must recover through steering and continue the race.
Player A/D, original textures and restored High shadows are retained.

A deterministic production Mass30 diagnostic reproduced 385 consecutive ticks
below 0.2m/s on a penalty circuit (19.25s at 20Hz). Penalty movement deliberately
disabled the controller's existing behind-target recovery. A target behind the
skis could therefore request almost zero yaw through sin(pi). Enabling recovery
reduced the longest stop to 78 ticks in the same scenario.

At almost zero travel, the passive snow steering preview can predict zero yaw
for every command. Its tie-break then chooses neutral and discards a valid
recovery deflection before the next powered interval. For AI on ordinary course,
start approach and penalty occurrences, preserve the already bounded nominal
deflection below the shared steering definition's minimum speed. The next paid
drive/snow interval remains the sole movement authority. No position/heading
snap, rest pivot, progress credit or hidden speed/power multiplier is introduced.
Automatic range transit and explicitly selected player control are excluded.

AI also requests earlier explicit braking with a 0.85 safety margin under the
same grip/yaw limits on ordinary course and penalty bends. Straight travel and
automatic range approach/departure retain their previous planning law. The
existing shared force cap and recorded brake command own the resulting speed.
New optional policy fields default to the historical behavior when absent.
The AI integration context is v25; tuning/context and race output change openly.

The final Mass30 two-bout diagnostic reduces the longest penalty stop to 11 ticks
(0.55s). Full field completion and signed replay are checked separately. CPU
elapsed peaks recorded while Unity compiled are not treated as FPS evidence.

Exact contact-clearance caching now includes repeated directional support-grid
queries. Its key includes pose, query type, support-grid dimensions, complete
owned windows and apron identity. A velocity/spin impulse cannot change that
field. Repeated 5x3 queries use zero new disk queries; a 7x3 query still samples
its own 21 points. Full body/course admission and all snow geometry are unchanged.

Six older standalone steering-harness failures reproduce with pre-change source
and policy, outside the shipped signed field adapter. The existing Pursuit
composition failure also remains separate. No failing test is disabled; these
are recorded independently of the corrected owner scenario.

Next: owner reviews the new Mass30/Sprint30 build and decides M13 closure.
