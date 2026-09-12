# TASK-123 — Initial career balance report

WP123 complete. Reproducible metrics derive from the WP122 actual-race cohort; scripts/Measure-CareerSoak.ps1 rejects incomplete/failed inputs and emits JSON/CSV. No tuning was silently changed after the soak.

| Metric | Observation |
|---|---:|
| Mean field skill after first / fifteenth season | 60.916 / 56.311 |
| Player mean skill after first / fifteenth season | 49.861 / 56.448 |
| Observed player peak | age31, mean skill57.189 |
| Season champion age range / mean | 24–35 / 29.67 |
| Total prizes / purchases (minor units) | 25,650,000 / 11,595,000 |
| Final active-roster money (minor units) | 8,590,000 |
| Player final tier | Regional |
| Retirements / replacements | 21 / 21 |

Development is gradual, with a visible veteran decline and replenishment of the field. The lower prospect skill range causes cohort mean skill to drop during turnover; it does not imply that every continuing athlete regresses. Champion ages are observations across15 correlated seasons, not an independently sampled peak-age distribution.

The generic skill-gain planner did not earn promotion for the low-starting player in this seed. Promotion rules pass direct rank/participation tests, but competitive progression needs richer planning and more seeds before a difficulty promise can be made. Early purchases eventually exhaust this player's discretionary funds; the service API refuses unaffordable spending. Prize inflow exceeds measured purchases, and world-money changes also include departing wallets and new starting grants. These provisional economy values are not a long-run equilibrium claim.

Every entrant uses the same purchasing/training policy in this cohort, expert race controls and a fixed synthetic venue. Health risk remains observable without punitive injury. Balance status: initial provisional baseline suitable for the M9 headless gate; broaden cohorts and player-facing choices in later milestones. Next: WP124 lock these inputs and close M9.
