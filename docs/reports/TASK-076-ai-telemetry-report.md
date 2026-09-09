# TASK-076 — AI composition and explainability

Added AiRacePolicyDefinition with a provisional JSON source and PackAiRaceRunner. The runner composes
existing physical/race perception, tactical cadence, utility, personality, difficulty, terrain pace,
draft choice, overtaking, pre-range recovery and shooting strategy. It collects all actor commands
before one shared field tick. Shared physics alone charges lateral work and decides shot geometry.
Separate actor/bout seed streams and exact definitions are retained in ContextJson. Initial shooting
pressure now explicitly tests InitialTick instead of assuming a bout begins at global tick zero.

AiDecisionTelemetry owns value snapshots of all eight scores/eligibilities, target IDs, draft and
overtake reasons, recovery reduction, shooting plan, pace error/adjustment and aim/lateral output.
AiDecisionBuffer exports invariant CSV and reports overwritten rows. A caller needing every row
can supply a streaming IAiDecisionSink. Sinks must not throw/reenter. RaceEventType adds values37/38
for changed intent/policy, leaving existing numeric values intact. GetLastInput/GetLastLateral allow
command replay; GetLastIntent supports debug inspection. No physical capability modifier exists.

Tests cover exact complete two-athlete frame/event/result replay, owned snapshots surviving ring
wrap, CSV under fr-FR, explicit retention, malformed policy/fresh-field validation and warmed .NET
allocation. Initial focused integration tests: 2/2; an additional policy test is in the final suite.
Final verification: standalone441/441, all441 Unity cases verified, PlayMode7/7. See WP078 artifacts. The .NET allocation counter is authoritative for the
warmed interval; Unity's counter alone is not an independent allocation measurement.

AI controllers are fail-stop after a failed plan, not transactional: reconstruct the runner from
its context/commands. Field authority still commits only validated plans. The default composition
uses zero external competitive-pressure input and fixed data-driven shooting urgency; future live
race policy can supply competitive context. Trace output remains a debug API, not new visual UI.
Next WP077 scenario acceptance; see ADR0013 for integration and subsequent fairness correction.

