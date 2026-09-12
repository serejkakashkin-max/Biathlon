# TASK-116 — Retirement and new generation

WP-116 complete. Authored retirement age and prospect age/skill ranges replace retiring AI athletes one for one. Monotone persisted ordinals prevent identity reuse. The transition returns a new save and restores an independent random checkpoint, preserving the source on failure. Current-season results must be archived before replacement.

Validation: .NET career21/21, Unity generation2/2 (531 discovered), Console0 errors/4 retained warnings. Fifty annual transitions preserve 30 athletes, replace multiple generations, retain the player, never reuse IDs, and reproduce identical save/load branches. Invalid RNG checkpoints and unarchived results fail without source mutation. Initial save fingerprint remains unchanged.

Player retirement/succession presentation is deferred; this operation does not forcibly remove the player's identity. Compact historical retirement records are integrated with WP119/121. Next: WP117 economy.
