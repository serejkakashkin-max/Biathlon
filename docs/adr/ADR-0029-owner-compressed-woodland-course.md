# ADR-0029 — Owner-compressed Woodland gameplay distances

Status: accepted owner scope; implementation acceptance recorded separately.
Date: 2026-10-05.

The owner tested the Windows game and explicitly requested at least a twofold
reduction of actual skiing distance, early turns, meaningful climbs/descents,
course-edge ribbon tape and standing spectators. Approved athlete motion is
preserved. One Woodland venue remains the production scope; Master Plan
WP170–174 remain deferred rather than completed.

Use explicit generated competition profiles and paid route geometry: Main
1200m, Mass Start five laps/6000m, Sprint three laps/4200.001m, Pursuit five
laps/5000m. These are game distances authorized by the owner, not a claim of
regulation race distances. Range stances, shooting geometry, penalties, lap/bout
counts and ordinary physical costs remain unchanged. The 1mm Sprint finish
continuation preserves the existing exact final Main section contract.

Every authored XYZ edge must match its paid distance. Grades come from those
same edges; shrinking only the visual mesh or increasing athlete speed is not
an implementation of this directive. The exterior route has an initial 60m
straight, early rise/descent and technical downhill bends. Format-specific
routes retain explicit catalog and directed-passage bindings.

Preserve the accepted compact stadium, thirty mats at 3m pitch, reusable range
ownership, private approach/exit paths and 150m penalty loop. Relocate event
objects using their complete native mesh bounds so they clear every paid road.
Roadside dressing is presentation only; generated stable IDs, auditable original
meshes/material sources and ground contact remain required.

AI traffic choices may be adjusted through explicit venue definitions and must
still pass the ordinary simultaneous body/guard/tail resolver and paid motion.
This does not authorize reduced collision bodies, teleportation, hidden power
bonuses or changed authoritative shooting tuning. Record all race-affecting
policy data in the replay context. Preserve omitted optional values and historic
generator bytes for existing contracts.

Historical WP169 races qualify the previous course. This revision requires new
static/native checks, complete supported field races and exact persisted replay.
Deliver each newly built Windows review to the owner for hands-on feedback.
Incomplete format verification is recorded explicitly; neither this ADR nor a
review build closes M13/WP175–178.
