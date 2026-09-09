# TASK-054 — Race lifecycle and timing

One-athlete state coordinator: prestart, skiing, range, penalty, finish, classification and
DNS/DNF/invalid terminal outcomes. Explicit ordered boundary notifications from route/shot
adapters; validate every transition before mutation. Global fixed ticks continue during range
and penalty; elapsed race time starts at assigned start tick. Finish uses canonical fractional
stamp. Events record lifecycle/lap/bout/penalty boundaries. Check no skipped bout or penalty,
exact timing/replay, terminal behavior and no allocations. Spatial enforcement follows WP056.
