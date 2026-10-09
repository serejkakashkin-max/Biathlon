# M13 scheduled start collision boundary — 2026-10-09

M13 / WP175 / WP177 / WP178 OPEN; WP176 PASS. No owner Windows build.
Production host still selects lateral-v1; full signed player steering is required
before final M13 acceptance. This is an isolated correction, not traffic closure.

The full Sprint40 angular-forecast trial exposed a concrete mismatch at athlete17's
scheduled start, tick10201. The world admitted the body to impulse response at
`tick >= ScheduledStart`, but the actual session keeps it stationary at that event
and first pays movement at `tick > ScheduledStart`. A live skier could therefore
push a body that the same transaction was required to keep exactly still.

World participation now uses the actual first moving interval. The official start
event, scheduled times, elapsed clocks and input semantics are unchanged. Mass grid
bodies still exist before the gun; unbound worlds still include every body. The
presence context is v4 so replay identity captures this deterministic correction.

A new regression exercises actual session preparation, scoped world publication
through the roster batch, and the first moving interval. The existing coincident
body negative control checks real collision admission at that same interval. Both
failed before the fix, with five other presence tests passing. All seven now pass
in .NET (21s) and native Unity EditMode (2m22s). Native discovery1700 is not the
executed count. Error and Exception Console queries are empty.

After removing the unsuccessful integration trial, the focused selection is
22 PASS / 2 FAIL / 0 skipped (24); only the original enabled production field
fixtures fail, at Mass163 and Sprint8724. The independent signed race tape suite
is 12/12 PASS (1m15s), including complete recording and AI-free cold playback.

The spinning-body forecast helper has twelve passing focused checks, including
rotation with no relative centre travel and a zero-allocation warm query. Its
production integration, long/short pursuit and lookahead overrides are NOT kept:
full trials still fail. Diagnostic exception instrumentation was also removed.
Retained trial receipts show Mass336 / Sprint10201 before the boundary correction,
and Mass336 / Sprint7848 afterward. Remaining failures concern course crossing,
not approval to relax body dimensions, course admission, physics or assertions.
The original production fixtures remain enabled.

Evidence: `docs/quality/m13/field-stepper-20261008/manual-scheduled-boundary-*`,
`manual-production-stationary-frame-diagnostic-20261009.trx`,
`manual-production-rotating-short-pursuit-trial-20261009.trx`,
`manual-production-start-boundary-trial-20261009.trx`.

Next: dense-field maneuver correction and full signed Mass30/Sprint40 and actual
Sprint-to-Pursuit qualification, player final mat capture/host, final conditions,
balance and performance. Owner shooting-feel issue remains pending. State history
is appended only. Correction to the preceding state record: “Profiled selection
30/30” meant focused functional tests, not a performance profiling result.
