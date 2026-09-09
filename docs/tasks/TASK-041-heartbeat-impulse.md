# TASK-041 — Heartbeat impulse

Fixed-tick phase accumulator tied to actual HR. Data-driven rapid attack/smooth decay impulse
and HR amplitude response, observable beat count/phase/envelope/vertical angle. Use integer
nanobpm accumulation so exact minute beat counts do not drift from rounded phase integration.
Test cadence at multiple tick rates, variable HR, quiet intervals, replay and no allocation.
