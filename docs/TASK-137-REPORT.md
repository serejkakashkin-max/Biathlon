# TASK-137 — Playable Pursuit

WP137 complete. After the entire playable Sprint field finishes, Continue to Pursuit creates a new host from a serialized/reloaded Sprint result and the authored Pursuit qualification policy. PlayerIndex is resolved by stable athlete ID in the reordered qualifying roster, not assumed to remain zero. Existing input, cameras, HUD and penalty presentation follow that athlete. The old race host is disabled/destroyed; venue remains.

Validation: Unity PlayMode complete Sprint-to-Pursuit chain1/1 (22 discovered). The player deliberately misses all Sprint shots, finishes second, becomes index1, receives a later scheduled start than the leader and completes the subsequent Pursuit. .NET qualification/field6/6; Unity EditMode qualification4/4. Game View finish/transition button inspected: docs/quality/m10/wp137-transition.png. Console0 errors/1 warning, no compilation errors.

No simulation balance changes or invented start gaps. Qualification rejects a player missing from the qualified roster. Transition currently uses the completed in-memory Sprint result with serialization validation, not a save-file selection UI. Automated commands complete the chain; hands-on comfort remains G1. Next: WP138 playable Mass Start with30 athletes.
