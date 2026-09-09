# TASK-066 — Decision cadence and planning horizon

Data definition chooses whole-number 2–5 Hz tactical cadence and horizon in seconds. Exact
integer scheduling uses ceil(n * simulation_frequency / decision_hz) from a fixed origin;
non-divisible rates distribute intervals without clock drift. Each due decision receives next
deadline and absolute planning end. Simulation input clocks continue every tick independently.

Reject skipped/backward ticks and arithmetic overflow before clock mutation. Validate counts
at 20/30/60 Hz, non-divisible 3 Hz sequences, shifted origins, horizon rounding, repeatability,
intent lifetime binding, and no warmed allocations. No game/controller stepping in this task.
