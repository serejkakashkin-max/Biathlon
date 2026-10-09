# M13 — physical terminal mat braking

2026-10-09. Qualified scoped integration; M13 remains OPEN.

The reproducible first-bout rejection at tick3214 is resolved. With the explicit
terminal-braking policy, all three real Woodland AI athletes reach their first
shooting bouts with exactly zero world velocity and spin. They pay the same full
20Hz field clock. No velocity threshold, pose assignment, paid queue, physical
width change or legacy movement fallback is used.

## Change and cause

`ManualMatBrakingDefinition` supplies a separate optional terminal strip. The
candidate selects `manual_mat_braking_v1.json` (0.1m normal distance from its
owned firing plane). This is the braking end of the owner-approved mat assist,
not the full requested2–3m capture/steering implementation. Ordinary paths,
departures, penalty/finish gates and callers without this definition retain
their supplied controls. Production host selection is unchanged.

Within that strip, `2*remaining_normal_distance/normal_speed` becomes a stopping
intent only when the inverse shared-force brake fits the existing force budget.
Every actor's time piece is clipped to the earliest real stop; the complete
contact, body, course, phase and session proofs still decide admission. The
prepared physiology cost remains paid; stopping applies no propulsion bonus.
An infeasible brake cannot create stationary permission.

The initial physical controller experiment also exposed two numerical defects:

- A feasible unconstrained snow minimum could lose to the zero-impulse candidate
  through energy roundoff. A mathematically feasible interior minimum now wins
  analytically, retaining the shared grip and angular-response limits. An exact
  neutral null-space receipt allows a fully paid forward force stop to be
  represented as zero, without cancellation of two nearly equal vectors.
- A nearly double quadratic root could split the final stop about1e-10s early.
  A firing endpoint now recognizes the whole already-paid stop only if its actual
  final velocity/spin are exactly zero, its actual encoded station/plane match,
  and continuous phase-plane proof passes. Moving arrivals remain rejected.

The inverse force calculation reserves64 floating-point ulps outward within the
existing brake cap. A separated genuine terminal stop retains its complete free
law. An already stationary actor cannot trigger that exception for the whole
field. Reached contacts still require common force complementarity.

Earlier1m and0.25m neutralization experiments left the actual body corridor and
were rejected. The selected0.1m strip does not widen that corridor. Their failed
receipts remain. A preliminary broad run also caught an overbroad free-law
condition and reflection helpers expecting the previous five-argument snow
constructor. The condition was restricted to a moving source that actually
stops; the five-argument constructor remains, and test helpers resolve its exact
signature. No failing assertion was removed or relaxed.

## Validation

Evidence: [field-stepper receipts](../quality/m13/field-stepper-20261008/).

| Check | Result / receipt |
|---|---|
| Original failing first-bout reproduction | FAIL retained, `mat-arrival-baseline-20261009.trx` |
| Preliminary broad regression | 482PASS /11FAIL /493executed, `mat-braking-all-manual-20261009.trx` |
| Corrected representation/contact/slice/Mass30 selection | 32/32PASS, `mat-braking-representation-20261009.trx` |
| Final complete manual selection | 493/493PASS, `mat-braking-all-manual-final-20261009.trx` |
| New deterministic two-run arrival, physiology and event comparison | 1/1PASS, `mat-arrival-exact-repeat-20261009.trx` |
| Unity terminal policy/numerical witnesses | 9/9PASS, `unity-mat-braking-20261009.json` |
| Unity actual all-three first-bout arrival | 1/1PASS, `unity-first-bout-all3-20261009.json` |
| Unity final shared controlled forces | 30/30PASS, `unity-mat-control-final-20261009.json` |
| Unity exact two-run arrival/physiology/events | 1/1PASS, `unity-mat-exact-repeat-20261009.json` |
| Final Unity compilation / Error+Exception Console | clear, `unity-mat-final-state-20261009.json` |

Native discovery totals are not execution counts. The final manual regression
includes allocation checks, the400-clock stationary-player Mass30 bypass, body
excursion/phase rejection, contact continuation, geometry and command components.
The two-run witness uses AI in both runs: it is deterministic regression, not a
claim of complete AI-free manual command replay or a full-format race.

## Remaining work

M13/WP175/177/178 remain OPEN; WP176 PASS. Production still uses lateral-v1.
Qualify all subsequent shooting/departure/penalty/finish phases, reusable Sprint40
mats, achieved-world draft, complete manual replay and player host/rig/HUD input.
The full2–3m mat capture and physical shooting preparation also need final manual
integration. Then complete weather/player traffic/readability, balance and hidden
sustained performance checks. Only after all M13 work will a Windows owner test
build be produced. No intermediate build was made.
