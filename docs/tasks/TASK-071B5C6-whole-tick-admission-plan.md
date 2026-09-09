# TASK-071B5C6 — Isolated whole-tick admission plan

Add a reusable private planning copy of JunctionAdmissionSession. Share immutable geometry only;
copy every authoritative position, owner, admitted flag, retained tail/ghost and clock field into
independent storage. Buffer planned events without publishing them. Permit multiple fractional
substeps; apply the complete sealed result to authority once, followed by ordered buffered events.

Track authoritative revisions to reject stale concurrent plans. Beginning a plan requires a
completed tick, including rejecting zero-time partial ticks at fraction zero. Late invalid
substeps or buffer overflow invalidate the entire plan without touching authority. Capacity is
fixed at construction; buffer sink must not throw during working-session commits. Test late
failure, event overflow, stale plans, tail persistence, one-time commit and warmed allocations.
This transaction covers admission only; joint Sprint physiology/path/force planning remains.
