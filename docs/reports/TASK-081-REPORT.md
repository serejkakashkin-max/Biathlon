# TASK-081 — Exact Pursuit gap starts

Status: DONE — 2026-09-10. WP-081 follows verified WP-080.

PursuitStartScheduler converts registered source gaps to exact whole ticks using decimal arithmetic
on the cold construction path. The existing lifecycle accepts whole-tick starts: a source gap that
cannot be represented at the selected frequency is rejected, never rounded. Current 0.1-second
results at 20 Hz are exact. Overflow and invalid clocks fail before events. Tied starts are emitted
in one tick in canonical qualifier order; the caller must supply capacity for the entire group.
Invalid tick progression or insufficient output capacity leaves scheduler state/events unchanged.

Every start uses ScheduledStartDue in the existing replay event system. QualificationJson retains
the versioned dependency context. Three new tests cover lifecycle integration, tied arrivals,
invalid resolution/overflow/capacity, repeat events after dependency reload and zero per-tick
managed allocation. Existing interval scheduler and Sprint output remain unchanged.

Validation: .NET Pursuit and IntervalStart tests 10/10; Unity MCP PursuitStartTests 3/3 and
IntervalStartTests 3/3. Console inspection retained in artifacts/physiology/wp081-console.json.
Evidence: wp081-starts.trx, wp081-unity-starts.json, wp081-unity-interval.json in the same directory.
Current discovery457; preceding full450/450 baseline is not a new full-suite run.

Limit: no sub-tick start integration is claimed. Select a compatible clock instead of changing
gaps. WP-082 is next: four-bout/five-lap Pursuit orchestration and finish-order classification.
No physical balance change and no owner blocker.
