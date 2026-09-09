# TASK-071B5C1 — Prepared admission transaction

Implemented a revision-bound JunctionAdmissionPreview in JunctionAdmissionSession. Prepare
performs the existing geometry, priority, suffix and roster validation using private scratch
buffers. Positions, owner reservations, admitted flags, tick and event sink remain unchanged
until Commit. Value-copy accessors expose accepted sweeps, endpoints, decisions and resulting
owners for the future field coordinator. Step retains the original Prepare+Commit behavior.

Default, foreign, consumed and replaced handles fail validation. Even a failed Prepare
invalidates any previous handle before checking its input. Mutating caller arrays after
preparation cannot change accepted state. Nonthrowing, nonreentrant event sink remains required.

Added two tests in JunctionAdmissionTests covering authority immobility, direct/staged replay,
caller-array isolation, failed replacements, foreign handles and zero warmed .NET allocations.
Standalone and Unity MCP EditMode 381/381 PASS, Console zero errors/four upstream warnings.
Evidence is recorded in PROJECT_STATE and wp071b5c1 captures. Prior PlayMode 6/6 from B5B
is retained; this change adds no new presentation or MonoBehaviour behavior.
No physics, tuning, old Sprint event ordering or deterministic regression output changed.

Limitation: one pending preview per session. Cross-session atomic commit is not established
by this API alone: a coordinator must validate all prepared components before consuming any,
with no intervening mutation. Directed branch connectivity and retained incoming footprints
are still pending in parent TASK-071B5C; WP071 remains IN PROGRESS. No owner gate applies.
