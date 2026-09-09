# TASK-057 — Checkpoints and timing gaps

PASS, 2026-09-08. CheckpointLayout projects authored IDs into stable route-section occurrences,
ordered by exact distance, section and ordinal ID. Colocated lap-end and next-start markers
remain distinct. CheckpointTracker emits each once with canonical global/individual elapsed
timestamps and CheckpointCrossed events. Zero-distance start markers use the athlete's start
boundary; notification is emitted on the first tracking tick. Penalty/shooting holds leave
main-route progress unchanged, so their duration appears in subsequent splits.

CheckpointTimingBoard accepts compatible roster/layout/clock receipts and explicitly selects
elapsed or global ranking. Later faster starters replace the elapsed leader; gaps update from
stored exact times. Equal times share zero gap; stable athlete ID chooses only the display
representative, not a competitive tie-break. Rank/results policy follows WP058.

290/290 .NET and Unity EditMode pass; Console zero errors, four upstream warnings.
Evidence: artifacts/physiology/wp057-{standalone.trx,editmode.json,console.json}.
Multi-checkpoint ticks, fractional timing, stationary periods, replay, invalid-input atomicity,
late starters, ties, foreign/duplicate receipts and zero timing-loop allocations pass.
Foundation, skiing and shooting deterministic regressions unchanged. No tuning or commit.

Interpolation follows ADR-0004. The spatial orchestrator must supply accepted main-route
progress; splits are diagnostic precision, not rounded official result times. Layout identity
is intentionally shared within a run; replay rebuilds receipts from inputs. No new visual UI.
Next: WP058 classification/results. No owner gate.
