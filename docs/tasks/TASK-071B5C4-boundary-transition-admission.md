# TASK-071B5C4 — Prepared admission of stationary seam boundaries

Extend JunctionAdmissionSession with optional exact-bound connectivity and per-proposal connection
IDs. A requested transition must start at the owned incoming anchor, preserve lateral position,
and travel zero distance in this common interval. Reserve incoming/outgoing regions before
switching the prepared centre. Keep incoming tail state after commit and advance it only through
accepted subsequent movement. Denial retains the complete old state. Reject another transition
before the old tail clears; do not overwrite occupancy.

Retained incoming bodies must participate in pairwise collision checks outside junction guards
as well. Conservative stationary full-body proxies are permitted for the short tail-clearance
interval and must be documented. Keep failed preparation atomic, append transition events with
stable connection indices, and verify complete penalty-circle entry/exit, input-order replay,
stopping, invalidation and active allocations. This is a boundary-interval primitive, not the
full-tick Sprint field or its shared-force substep coordinator.
