# ADR-0004 — Ski force integration

Status: accepted technical policy for WP-017; baseline tuning remains provisional.

GDD pseudophysical power/force model uses shared delivered athlete watts as mechanical input.
Propulsion = min(force ceiling, power / max(speed, configured regularization speed)).
Grade means rise/run: sin(theta)=grade/sqrt(1+grade²), cos(theta)=1/sqrt(1+grade²).
Snow force = coefficient × mass × gravity × cos(theta). Signed drag uses relative flow
(athlete speed + exposed headwind): 0.5 × air density × drag area × flow × abs(flow).
Tailwind can assist; model never fabricates different player/AI forces.

Fixed-tick explicit force evaluation at start state; velocity integrates acceleration and is bounded
to nonnegative forward motion/configured speed envelope. Position integrates the linear velocity ramp;
when stopping or reaching the envelope within a tick, integrate to that crossing then hold the bound.
This avoids adding spurious distance for a full tick after an early stop. Reverse skiing is outside scope.

Velocity quantizes to 1e-9 m/s, position increments to integer nanometres, accumulated checked in Int64.
Track query coordinates derive by integer floor to millimetres, so slow motion is not lost each tick.
Force/speed math inherits ADR-0002's qualified binary64 policy. Numeric constants define encoding;
physical coefficients/limits live only in the versioned ski JSON. Future finish timing uses overshoot
and a documented crossing rule (WP-027), not a rounded display coordinate.

Baseline snow coefficient/drag represent neutral conditions until WP-021/022; technique/tuck/cornering
and drafting will apply explicit observable modifiers in their own WPs. No Unity physics authority.

WP027 finish ordering uses linear interpolation between the accepted tick endpoints (including any
contact projection), not rounded track-query millimetres. Remaining/displacement nanometres map by
decimal division and ties-to-even rounding to one billion integer parts per tick. Whole tick plus
fraction is canonical and lexicographically ordered for a shared tick frequency. This convention is
deterministic; it is an interpolation estimate, not an assertion of sub-nanosecond physical accuracy.
An exact endpoint crossing normalizes to the next whole tick. Finish event uses the containing tick;
results use FinishTime to distinguish finishers within that tick. Later photo-finish presentation
must consume this authoritative stamp rather than render-frame timing.
