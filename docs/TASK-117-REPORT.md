# TASK-117 — Economy foundation

WP117 complete. Versioned fictional prices and rank prizes use integer minor units. Prize credit and lifetime earnings are overflow-checked before mutation. Equipment purchases resolve real catalog IDs, reject duplicates and debit only on success. Paid recovery reduces current fatigue. Camps persist a single pending quality entitlement for consumption by the weekly orchestrator, preserving save/load behavior.

Validation: .NET career23/23; Unity economy2/2, 533 discovered; Console0 errors/4 retained warnings. Tests verify exact balances, persistence, duplicate/insufficient-funds/unknown-equipment rejection and overflow atomicity. Initial save hash remains unchanged because an unused camp entitlement is omitted.

Economy tuning is provisional until the M9 soak/balance gate. Prize awards are a primitive; WP121 owns exactly-once race accounting and camp consumption. Race equipment modifiers remain the existing bounded catalog values. Next: WP118 inventory selection/comparison.
