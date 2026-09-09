# Headless time-trial composition

TimeTrialSession composes a finite RaceRoute with physiology, technique, tuck, cornering, snow,
equipment, final sprint and finish. Input ordering: explicit sprint effort preset → eligible tuck
workload cap → M1 physiology preview → start-state coordination efficiency → shared forces with
snow/equipment/grip/corner braking and posture drag. Tuck is an explicit recovery posture even if
sprint is also requested; telemetry preserves both requests and effective workload.

Validate all fallible input/force calculations before committing physiology/motion/events. Public
PhysiologySession.Preview is read-only; internal CommitPreview is used only after successful same-tick
composition. Track metadata samples the start-of-tick distance. Completed laps interpolate the
accepted endpoint crossing using the same FinishTime rule as final results. Resource/effort events
precede posture/sprint/overspeed/technique transitions, then lap and final-finish events.

This is a solo ski time trial: no other occupant, draft leader, shooting stop or penalty obligation.
Pack mechanics remain separately verified and are composed with the field/race scheduler later.
TimeTrialInput is an interim complete harness command, not the final WP030 player input schema.

Run scripts/Run-TimeTrialLab.ps1. BalanceLab ski command loads time_trial_scenarios_v1.json and
versioned athlete/config/track inputs, creates validated route projections, writes per-tick CSV,
ordered events, lap splits and summary with source/trace SHA256. Every consumed input is copied
under inputs/. CSV floating fields explicitly round to nine decimal places, invariant formatting;
nanometre positions and tick counts stay exact integers. Signed zero serializes as `0` in both runtimes.
Exports allocate outside authoritative ticks. Summary status stays incomplete until every run finishes,
so a failed rerun cannot leave an old successful summary appearing current.
Repeated CLI output and tests/fixtures/ski-traces-v1.sha256 pin the complete five-course traces.
