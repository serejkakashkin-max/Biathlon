# TASK-091–093 report

PASS — 2026-09-11. WP091/092/093 DONE.

Added BalanceBatchDefinition, BalanceRaceRunner, constant-memory event/metric collection,
BalanceDistribution and the `batch` CLI. Added three contract/accounting/statistics tests,
data suites, metric schema and BALANCE_LAB_BATCH.md. Physics, shooting geometry and AI
capability remain unchanged; pacing inputs and the standard expert aim controller are explicit.

Validation: build0 warnings/errors; .NET5/5 including unchanged Sprint regression hashes;
Unity MCP3/3 (489 discovered), candidate metrics SHA256
fdcc8b6cd32f71475f8f46dc3700b65f832945ed14ab41e5f84114732aebe34a was the Unity output. The original statement that it matched .NET was incorrect; WP094 diagnosed/fixed export formatting and verified the complete canonical suite. See TASK-094-REPORT.
10002 complete solo Sprint races across3334 fixed seeds and3 configurations passed in15.57s
on4 workers. Serial and2-worker smoke artifacts match exactly. Runtime is a local observation,
not a performance target for30-person fields.100020 accepted shots; complete race accounting
is enforced per row. Definitions/contexts/seeds and report hashes are retained in evidence.

The short flat baseline favors aggressive pace (364.4s versus406.3/410.6s in smoke) while
increasing fatigue. This is an observed calibration input, not a claim that pacing is final.
Next: WP094 versioned regression windows, then WP095 longer/rolling paired pacing calibration.
Limitations: solo Sprint, tick-attributed sub-times, missing group/stat causal effects explicitly
null. M8 as a whole is not complete. See batch contract for metric semantics and outputs.
