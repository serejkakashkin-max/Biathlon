# TASK-091–093 — Batch execution, metrics and distributions

Atomic scope: portable data-driven solo Sprint workloads, bounded parallel CLI execution,
versioned metrics, CSV/JSONL/JSON/Markdown reports and deterministic worker-order checks.
Dependencies: M7. Existing pack/three-format harness remains available.
Acceptance: thousands of complete races without graphics; independent fixed seeds and paired
case ordering; bounded tick failure; exact serial/parallel artifacts; unit and Unity tests;
explicit time attribution, missing counterfactual semantics and reproducible input provenance.
No numerical physics/shooting tuning change is part of this task.
