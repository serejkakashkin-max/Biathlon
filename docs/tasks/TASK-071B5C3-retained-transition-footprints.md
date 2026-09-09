# TASK-071B5C3 — Retained incoming transition footprints

Add immutable physical footprint previews for an explicitly authored seam. Preserve the incoming
tail until actual forward travel exceeds half of body length plus personal clearance; touching
the seam at exact clearance remains occupied. Shrink the old footprint using travelled distance,
never endpoint subtraction. Closed-loop circuits must not recreate an old tail. Query every
junction potentially touched by the incoming interval and the outgoing body, including junctions
other than the seam's own region. Swept queries must retain start-of-interval tail occupancy.

Validate foreign/default states, negative/excessive travel, open-loop overrun and cumulative
overflow before returning any answer. Keep previews immutable and allocation-free after setup.
This primitive does not authorize transitions or release session reservations. Multiple retained
incoming branches and a coordinated transition-admission commit remain separate work.
