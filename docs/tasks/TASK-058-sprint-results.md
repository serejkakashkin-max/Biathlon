# TASK-058 — Sprint classification and result contract

Classify completed Sprint entrants by individually elapsed registered time, preserve precise
finish timestamps for replay, assign equal ranks with skipped places for ties, and list
DNF/DNS/invalid entrants without a rank or invented finish time. Stable athlete IDs order
equal-rank display rows independently of arrival/submission order. Validate roster/status,
duplicate receipts, profile capacity and clock before mutation. Retain a versioned result
snapshot and test JSON, late finisher ranking, ties, terminal states and deterministic ordering.

Audit result precision and tie rules against current IBU ECR; document any project-specific
registration policy separately from sourced rules. Other formats and jury adjustments follow
their Work Packages. Verify all pure-core regressions and Unity Console through MCP.
