# M13 — oriented pair contact and linear time of impact

Added a deterministic event-free contact primitive using actual world headings
and unchanged occupied body dimensions. Side contact preserves shared forward
motion; rear contact transfers normal momentum instead of stopping both bodies.
Sliding friction and restitution are data-defined, with no production tuning
selected yet. Stable ID ordering preserves exact argument-swap symmetry.

A separate constant-velocity/fixed-orientation sweep finds the first instant of
contact, including a fast crossing invisible to endpoint-only checks. Static
separation never authorizes an entire movement interval. Existing overlaps are
reported; no position correction or body shrink is performed.

Validation: broader .NET movement/contact selection 61 PASS; native Unity contact
class 13 PASS. Tests cover momentum, energy, side/rear contact, separating bodies,
orientation, swap symmetry, high-speed crossing, invalid input and warm allocation.
The earlier 23 legacy replay tests remain a separate receipt from before this
disconnected primitive. A test helper initially had a namespace/method naming
conflict; it was corrected before the passing .NET and Unity runs.

Limitations: angular contact response, multi-body continuous admission, slip/snow
coupling, course edges and playable race integration remain open. Rotating contact
is explicitly unresolved, not silently treated as a safe stationary pair. No new
race events occur because the primitive has no live caller or commit side effects.

Contract: ADR-0035. Evidence: docs/quality/m13/owner-force-heading-20261006/.
M13 and the owner queue issue remain open. No intermediate Windows build.

## Angular-contact addition

The separate angular API now computes the contact-point velocity and impulse using
both translational mass and rectangular moment of inertia. Turning a ski corner
into a neighbour is detected even when centre velocities are zero. Glancing
impulses can rotate the bodies while preserving linear/angular momentum; normal
and tangential impulse coupling does not create kinetic energy. Original pair and
linear sweep contracts remain intact. The new angular context is separately named.

Native final contact suite: 16 PASS. Expanded .NET movement/contact suite: 64 PASS
in eight repeated runs. One previous run failed the composed-motion allocation
assertion (6,984 bytes); its isolated rerun and all eight unchanged repeats passed.
No assertion or production implementation was changed to hide it. The source of
that isolated allocation is not established. Retain the failed and successful
receipts and investigate further if it recurs. Test-only naming conflict was fixed
before these runs; Unity compilation is complete.

Remaining: contact time for continuously rotating/accelerating bodies, group
response, ski/snow slip coupling, edge admission, route progression, AI and host
wiring. The new angular impulse does not itself prevent tunnelling or fix queues
in the running build. The consolidated owner build is still deferred.
