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
