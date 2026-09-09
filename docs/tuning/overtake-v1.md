# Provisional overtaking controller

Controller owns only manoeuvre state; caller supplies authoritative current placement and roster.
Nearest forward athlete intersecting the current lateral body corridor is the target; stable ID breaks
ties. A static swept-space check chooses left then right, requiring both lateral exit and passing lane
clearance. Actual movement still needs simultaneous resolution in WP026; planning is not permission
to push another skier. While active, the stable target remains fixed.

MovingOut → Passing → Returning → Idle. Returning starts after full longitudinal clearance, waits
when occupied, and clamps original home lane to a narrowed course's valid width. Cancellation,
timeout, lost target or prohibited segment trigger one abort event then a safe return. Request is a
pulse, not a held button; cancel is explicit. No tactical blocking action is generated.

Lateral movement adds bounded effort control before physiology and loses a proportional amount of
useful propulsion after technique. At maximum effort the coordination loss still applies. If occupancy
blocks the proposed lateral step, that step's surcharge is zero. Downstream movement rejection can
still consume attempted exertion; it cannot grant free motion. Tuck cannot silently erase these costs
in the later runner composition; effort/posture ordering must be explicit.

IRaceEventSink records start(side payload), completion and abort once per transition. Target ID and
phase are observable output for richer runner trace; Foundation event encoding stays unchanged.
