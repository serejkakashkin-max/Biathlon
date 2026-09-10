# TASK-082A — Shared Pursuit athlete lifecycle

Status: DONE — 2026-09-10. WP-082 remains IN PROGRESS.

The existing SprintAthleteSession now also accepts a Pursuit profile. All skiing, physiology,
range, shooting and penalty composition already follows the profile; no parallel physics or
reset logic was introduced. The historical public class name is retained for compatibility.
Mass Start is still rejected until its own work package.

Two new tests run repeated complete five-lap PPSS races with 20 accepted shots, exact actual
5000 m plus 150 m per miss, all four bouts and one finish event. Full frames/events repeat.
A two-tick start offset preserves exact individual duration and final physiology/motion.
The initial test sampled ActiveBout after the completion tick; this property then describes
the next bout. Fixed the test to capture the bout before advancing, without changing gameplay.

.NET lifecycle/Sprint tests6/6; Pursuit plus Sprint regression/replay14/14. Unity MCP Pursuit
lifecycle2/2 and existing Sprint athlete4/4. Console0 errors, compilation_failed=false,
four existing MCP warnings. Evidence under artifacts/physiology: wp082a-lifecycle-final.trx,
wp082a-regression.trx, wp082a-unity-lifecycle.json, wp082a-unity-sprint.json, wp082a-console.json.
The initial failed test output is retained separately as wp082a-lifecycle.trx.

Limits: this is an athlete-level milestone, not a complete Pursuit field. TASK-082B must bind
qualification roster/start scheduler to field execution, use global arrival order rather than
Sprint individual elapsed ranking, and connect shared physical field behavior. Do not mark
WP-082 DONE or start claiming a complete Pursuit game from these tests. Existing Sprint
regression/replay remains green; no tuning or hidden performance modifiers changed.
