# TASK-026 — WP-026 safe separation/contact presentation contract

Dependencies WP024/025 PASS. Resolve simultaneous proposals in stable forward-position/ID order,
using relative continuous sweeps against accepted trajectories and stationary not-yet-processed bodies.
Conservatively shorten unsafe motion without reversing, crossing bodies or mutating inputs.
Preallocated scratch/output, atomically reject invalid frame; expose contact reason/blocker/limited
motion and replay contact episode starts/ends. Presentation consumes observations, cannot alter race.
Tests rear-end catch, lane swap/crossing, order invariance, widths, long dense safety, atomicity and GC.
