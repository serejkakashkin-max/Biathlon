# ADR-0042 — Released heading and admitted mat endpoint

2026-10-10. Accepted under the owner's latest range/build correction.
Clarifies ADR-0041; M13 still awaits hands-on owner review.

## Context

The owner requests gentle straightening where A/D was released, without a
centreline target or automatic following of subsequent bends. The supplied
player log has one rejected AI26 firing arrival followed by thousands of
secondary consecutive-command-tick exceptions. Its final physical velocity
was approximately 0.49 m/s. The folded mat endpoint was admitted, but planting
additionally required a different narrow centreline plane.

## Decision

Capture the local course heading once after a steering gesture. Apply the
existing delayed, bounded return intent toward that fixed heading. Do not
query a future course target or a centreline position. New steering overrides
the assist immediately. Automatic range turns clear the old release heading.

After whole-body, snow corridor and phase gate admission, use the catalog's
admitted firing endpoint for the existing bounded foot-placement decision.
The existing 2.2 m/s speed and 2.4 rad/s spin limits still apply. Record its
dissipated energy and preserve X/Z, heading, elapsed time and route credit.
The shooting session still requires an exactly stationary admitted body.
This correction does not broaden ordinary contact/course admission.

The host previews effort before stepping the field and commits its effort
clock only after success. A device command remains pending until its tick was
accepted. A faulted simulation reports the original exception once and stops
issuing commands against the unpaid tick. It does not skip ticks or pretend a
failed race can continue; a new race reconstructs it.

Private mat braking uses the actual sampled turn/straight geometry and shared
speed-dependent yaw capacity. A future private turn cancels propulsion only
when its stopping envelope is reached. Ordinary course braking retains its
previous policy. Player range effort comes from the same versioned guidance:
approach 650/1000, departure 800/1000. Shared force, grip and physiology costs
apply to these requests; there is no AI/player speed multiplier.

## Consequences

Race output and context identities can change. Effective signed commands and
braking remain recordable/replayable. Old foreign contexts are rejected.
Cold private geometry samples allocate during composition; per-tick queries
reuse arrays. No presentation transform owns gameplay motion.

The owner log does not contain complete preceding force intervals. The exact
AI26 post-admission pose is therefore a minimized plant-decision regression,
not a claimed replay of the owner's entire race. Native range/input tests and
the new Windows build provide separate integration evidence. Hidden-window
checks cannot qualify sustained FPS or the owner's perceived game feel.
