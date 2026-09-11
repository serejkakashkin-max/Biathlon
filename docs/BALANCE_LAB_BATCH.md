# Balance Lab batch contract v1

`batch <suite.json> <data-directory> <track-directory> <output-directory> <workers>`
runs complete independent solo Sprint races. Suite schema1 controls bounded iterations, base
seed, clock/tick budget, athlete/profile files and cases (track, effort, final-lap effort,
pre-range recovery effort/distance). Case inputs share seed `base + iteration`; no scheduling
RNG exists. Workers1..16 run independent sessions; sorted iteration/case output is invariant.
The championship command remains the three-format pack workload. Batch is a controlled solo
experiment, not a replacement for shared field physics or a 30-person field performance claim.

Example from project root:

```powershell
dotnet run --project dotnet/Biathlon.BalanceLab -c Release -- batch tests/fixtures/balance/batch-soak-v1.json Assets/Resources/Biathlon tests/fixtures/tracks artifacts/balance/soak 4
```

Outputs: status.json (incomplete until success), suite.json, inputs.json (SHA256 of definitions
and canonical tracks), case-context.json (first seed), races.jsonl, races.csv, distributions.json,
report.md. Every row carries its own exact context SHA256 and event FNV audit. Context includes
all physical data, controller policy, seed and strategy. FNV is a replay comparison checksum,
not a cryptographic security claim. Existing replay events are emitted to a constant-memory sink.
An interrupted/invalid run returns nonzero and never writes completed status; partial JSONL is
not a complete experiment. Use a separate output directory for each experiment. Timing/runtime
metadata in status.json is intentionally excluded from deterministic artifact comparisons.

Metrics schema: quality/wp091-093/metrics.schema.json. Race finish is authoritative fractional
elapsed time; ski/range/penalty seconds form an exact partition. Boundary intervals use their
start state (resolution one simulation tick); range means stationary shooting lifecycle including
setup/exit phases, not range approach skiing. HR is interval-weighted end-state sampling;
range-entry HR is the mean at newly created shooting sessions. Final-lap speed is sampled moving
travel/time after the final bout, excluding penalty and stationary shooting intervals. It includes
range-exit movement. This is not a same-position late-sector causal speed comparison.

Hits count newly closed targets from ShotResolved; penalties count completed actual loops.
Prone/standing counters use the Sprint P/S sequence. Solo draft time/overtakes are zero and
position is1. Attack value, draft benefit and stat marginal effect are null until a matched
counterfactual is supplied, never inferred from correlation. `PairedBenefit(base,changed,delta)`
returns seconds saved per declared change. Group draft and stat experiments belong to WP097/098.

Distribution statistics: arithmetic mean, population standard deviation (Welford), sorted linear
(n-1) median/P05/P95, min/max, hit rates and penalty histograms. Pearson finish/fatigue correlation
is observational; zero variance returns null. A paired strategy comparison must hold athlete,
track, conditions and seed fixed. No report asserts an untested globally optimal strategy.

WP094 regression: `batch-check` takes the same arguments as `batch`, then the snapshot path.
Example append `tests/fixtures/balance/canonical-v1.json`. It emits regression.json with specific
diffs and exits1 on mismatch; completed status is withheld. Metrics JSON explicitly rounds
binary64 values to9 decimal places, ties-to-even, before F9 formatting; this changes export only.
Target windows are provisional observations; their exact seed suite is SHA256-bound. A change
outside a window requires diagnosis and reviewed snapshot revision, never automatic acceptance.
