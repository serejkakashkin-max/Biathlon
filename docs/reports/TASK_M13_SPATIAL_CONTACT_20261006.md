# M13 — spatial contact continuation and course-edge checks

Post-contact velocity/spin now produces an analytic spatial trajectory. Lateral
motion survives the impact; it is not projected back onto forward speed. A single
passive snow impulse defines a frozen acceleration ramp and consistent prefixes.
The generic trajectory adapter lets ordinary skiing and contact continuation use
the same continuous pair check.

Added oriented-body checking against admitted local course-edge planes and a
joint query selecting one common clear prefix across all supplied actors/edges.
It preserves actual occupied dimensions, reports unresolved intervals and never
auto-steers or teleports. A three-athlete regression links group impulses, snow,
spatial continuation and joint pair/edge checking; all three continue forward.

Validation: 129 .NET motion/contact/replay tests PASS (23 legacy replay), 13 native
spatial tests and 13 native sweep tests PASS. No warm allocations in the new query
tests. An initial tied-event comparison boxed enums (48 bytes/query); integer
comparison fixes it, and the failed receipt is retained. A compilation error from
an internal Domain validator reference was fixed with local edge-ID validation
before passing .NET/Unity runs. No test was removed or loosened.

Evidence: `docs/quality/m13/owner-spatial-contact-20261006/`.
Contract: [ADR-0037](../adr/ADR-0037-post-contact-spatial-continuation.md).

Limits: this remains a simulation primitive, not a switched playable race. The
snow ramp is a frozen-interval approximation; propulsion/gravity/drag coupling,
successive impact arrival/resolution, real course-cell transitions, route/events,
AI, v2 replay and host wiring remain. Edge planes cover only supplied convex-cell
constraints; they are not a global curved-course proof. Legacy race outputs and
production tuning are unchanged. M13/WP175–178 and owner queues remain OPEN.
No intermediate Windows build was made. STATE is appended without history edits.
