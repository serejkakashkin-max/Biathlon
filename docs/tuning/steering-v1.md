# Steering intent

Normalized command maps to the usable track half-width after body/edge clearance. Fixed-tick
lateral rate is reduced at high speed and by explicit posture responsiveness. Optional centered
deadzone is remapped continuously outside the deadzone; all values live in versioned JSON.
Continuous occupancy checks reject blocked lateral steps, final simultaneous proposals still pass
through SeparationResolver. This evaluator is stateless; stream owner enforces target-tick order.

Line quality measures remaining tracking error to the requested line, not a hardcoded ideal
racing line. Movement costs useful propulsion. No hidden slip or skill-based random steering is
introduced. Tuck responsiveness is observable and shared by player/AI adapters.
