# M15.6 authoritative race balance study

2026-10-15. Competition validation COMPLETE; this report does not close M15.6.
Native visual/build and owner acceptance belong to the main milestone verification.
The final study uses the corrected course-contact authority, rather than comparing
old hashes from a different context. No AI speed/power bonus, shooting RNG, stat weights or
physics settings were changed by this balance work.

`dotnet/Biathlon.CareerBalanceLab` is a separate executable using the actual
CareerRaceProvider, Woodland 30 manual movement, geometric shooting, penalties and
classified results. It complements the long-term DevelopmentLab; aggregate
training trajectories are never counted as these authoritative races.

The initial 19 actual Sprint runs (13 principal/repeat cases and 6 matched WC
counterfactuals) exposed unequal starting allocations: balanced used 0/24 points,
skier 17/24 and shooter 20/24. Skier was the fastest of these three on all three
sampled WC race seeds. That evidence is retained, rather than represented as a
successful final balance check.

Under the producer's explicit follow-up, all six authored creation presets now
spend exactly 24 points. Balanced distributes one point broadly. Skier retains a
real stability weakness while using its spare budget on technique/recovery and
removing unnecessary breath/composure deficits. Shooter preserves base endurance,
improves breath/recovery and redistributes physical deficits toward anaerobic
capacity/strength/downhill/cornering. Endurance, sprinter and technician also use
their full budget. Stable preset IDs, tier baselines/limits, cost curve and hidden
potential rules are unchanged. Manual ResetAllocation still returns zero points.

Final post-calibration matched WC races, all OVR 79:

| Race seed | Balanced rank / elapsed s | Skier rank / elapsed s | Shooter rank / elapsed s |
|---|---:|---:|---:|
|123|6 /524.910|6 /522.629|11 /531.333|
|124|8 /526.495|21 /540.671|25 /561.957|
|125|17 /541.711|5 /521.962|8 /529.748|

This nine-race subset completed with 2700 geometric shots. Balanced beats skier on
seed 124; shooter beats balanced on 125. Equal wins are not imposed. Rank can differ
from an across-case elapsed-time comparison because the controlled athlete's
physical traffic/shooting also changes the other racers' actual results. This
small sample demonstrates viable tradeoffs, not universal format suitability.

The unit fixture `CareerCreationDraftTests` passes 9/9 in CoreCLR Release. It checks
all 18 tier/preset combinations at exactly 24 points, bounded abilities/OVR,
nonaccumulating repeated selection, preserved zero manual reset, independent
potential/RNG checkpoint, atomic invalid edits and detached confirmation. A new
actual ShotResolutionSystem check resolves 5/5 geometrically valid rays for every
tier/preset; handling may change cadence but cannot turn those valid rays into
hidden random misses.

Additional authority diagnostics check all 13 bound race statistics per athlete
against the actual career projection (half-even integer conversion). A matched
appearance/legacy-potential counterfactual leaves the complete authority context
identical. A non-neutral fatigue 6000/form 2000/confidence 2500 control experiment
applies the same effective form 0.83072, pressure 0.25375 and confidence 0.25 through
the AI and player input paths; raw player input cannot grant fresh readiness.

Final calibrated novice upper-skill control (OVR 50) finished 3rd, 13.654 seconds
behind the best rival, with zero misses. It explicitly requested maximum effort,
precise aiming, manual bolts and breathing with route steering automation. The
actual effective average effort was 961.114/1000, minimum anaerobic reserve
0.00163019, finish muscular fatigue 0.220935373 and range-entry HR 178.298/178.062.
The resource cost remains real; this is not a human win-rate test.

Final authority run: **22/22 cases PASS**, consisting of 19 complete Woodland 30
Sprints and 3 actual MassStart openings through the first timing station. The
run processed 548954 shared ticks, 5700 geometric shots and 8580 bound-stat checks.
All own starts used 24 points. Highest consecutive sub 0.3 m/s ski interval was
4.45 seconds; no 30-second stop occurred. Source career snapshots remained unchanged.
The complete repeated junior Sprint matched 29582 ticks, event FNV64
`aab9691a6ee8831a`, and result SHA256
`750db4ae577c9c31d9006060c1dc0d99d858b2f59fa4412a705b7fdc92604af3`.

Three additional diagnostic opening repeats processed 4273 ticks. They confirmed
identical authority assemblies, resource hashes, event hashes and tick counts.
The initial metric validator incorrectly treated the 80–180 m window as entirely
uphill. Actual track data contains negative grades from 118.199–136.372 m, so its
zero-tuck assertion was invalid. The corrected observation samples the positive
prior-step grade (>0.005) separately: uphill-tuck counts are 0 in all three fields.
The original red validator output is retained in `first-validator-red.json`.
No gameplay setting or authority code was changed to fix this metric.

| Fresh MassStart field | Measured ticks | First split p90 gap s | Positive-grade tuck ticks |
|---|---:|---:|---:|
|Junior|1563|8.031|0|
|National|1420|4.520|0|
|World Cup|1290|5.085|0|

The final machine-readable validator passes the full field/count/budget,
determinism, metadata/readiness parity, stop, opening-split and positive-grade
checks. The study is 19 full Sprints plus 3 openings, **not** 22 full races. Complete
MassStart/reload acceptance is separately covered by the course-clock fixture.
Three race seeds support representative regression evidence, not universal
specialization win rates or format suitability. Wall time is not game FPS.

Evidence: `docs/quality/m15-6/authoritative-races/`. Raw per-case laboratory JSON:
`artifacts/m15-6/authoritative-race-study/`. The lab stores fixed seeds, exact
scope/counts, classified rows, first splits/climb state, range-entry HR, minimum
reserve, fatigue, low-speed intervals and result/event/resource/assembly hashes.
Final files are `final-authority-summary.json`, `final-opening-observations.json`
and `final-validation.json`. `validate_study.py` reproduces the measured acceptance
checks from the saved laboratory evidence without synthesizing or replaying races.
