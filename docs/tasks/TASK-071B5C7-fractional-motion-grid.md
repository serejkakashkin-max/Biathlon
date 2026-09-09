# TASK-071B5C7 — Common fractional motion grid

Define pack-only quantization of timed motion receipt boundaries to integer billionths of a
tick, nearest with ties to even. Merge the complete field's original times before quantizing;
reject any distinct boundary collision, including zero/tick-end, instead of silently dropping
an interval. Keep output unchanged on failure. Sample piecewise motion in quantized time while
retaining exact route/travel endpoints, penalty-route invariants and stopped suffixes. Validate
the full trajectory before sampling. Verify a complete Sprint and collapse/allocation cases.
