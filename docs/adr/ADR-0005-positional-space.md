# ADR-0005 — Deterministic positional space

Status: accepted for M2 mathematical course; provisional body dimensions remain data-driven.

SkiPlacement replaces the draft-specific participant name with a shared pure contract: stable
athlete/corridor ID, longitudinal nanometres, forward speed and lateral metres. A corridor has one
directed distance origin. Separate branches cannot collide/draft merely because progress is equal.
Route adapters must resolve common physical corridors explicitly, including any future lap overlap.

Body occupancy is an axis-aligned rectangle in corridor space with configured personal clearance.
Track edges constrain the whole body plus edge clearance. For motion, use continuous slab intersection
of longitudinal/lateral intervals; endpoint checks alone permit tunnelling. Exact clearance contact
without penetrating the open interior is allowed. Query returns the earliest blocker, ordinal ID
breaks equal-time ties. Coordinates subtract integer longitudinal positions before conversion.

WP024 queries test motion against a static snapshot, useful for planning. They do not claim to resolve
simultaneous movement. WP025/026 must coordinate reservations/relative sweeps and commit validated
placements without overlap; presentation collision never decides authoritative race results.
Caller supplies minimum width along the queried path, rather than just endpoint width on a taper.

WP026 resolves forward proposals in descending start-distance/ordinal-ID priority. Each accepted
linear trajectory is checked against earlier accepted trajectories and later stationary starts.
Unsafe proposals are shortened with a bounded binary search retaining only verified safe candidates;
zero movement is safe because earlier proposals have already respected this start. This is deliberately
conservative at simultaneous lane changes. It does not guarantee maximal throughput of a crowded pack.
Longitudinal progress cannot reverse; constrained end speed cannot exceed proposed speed or accepted
average progress rate. This kinematic contact constraint is separate from the unimpeded force integrator.
Output exposes per-athlete blocker/reason/requested/resolved motion; replay records contact episodes.
Roster IDs/order remain fixed within a resolver, while priority/outcome are independent of initial order.
