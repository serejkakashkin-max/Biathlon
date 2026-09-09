# TASK-071B5A — Periodic continuous body queries

Implement forward single-branch linear sweeps over one common time interval, with explicit
travel of at most one circuit. Use the shared body rectangle and relative-motion query, with
periodic images on closed loops. Require body separation smaller than the closed-loop length.
Keep open loops unwrapped. Validate all batch identities before returning any conflict; choose
the earliest conflict with ordinal athlete-pair tie breaks. Recheck stationary suffixes after
trajectory shortening. Test seam/lapped occupants, complete circuits, lateral crossing, boundary
touches, invalid inputs, input order, sampled geometry and allocation. This is a query, not a
trajectory resolver or branch-transition protocol.
