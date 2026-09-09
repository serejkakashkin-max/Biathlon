# TASK-057 — Checkpoint timing

Project stable authored checkpoints into ordered route occurrences. Record each once, with
canonical fractional global and individual elapsed time; preserve colocated endpoint/start
markers as separate identities. Stopped/penalty progress cannot emit repeated main-route
splits. Handle multiple crossings in one tick with existing interpolation policy.

Maintain field timing by checkpoint using elapsed or global race time explicitly. Recompute
leader/gaps when later starters post faster splits; reject duplicates and incompatible
rosters/clocks/layouts atomically. Test replay, tie stability, sub-tick ordering, stationary
ticks, skipped boundaries and zero hot-loop allocations through .NET and Unity MCP.
