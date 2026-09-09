# TASK-051 — Shooting statistical Balance Lab

Status: PASS, 2026-09-08. Added versioned nine-scenario/200-seed suite, easy/standard/hard
controller profiles, ShootingBalanceRunner and CLI/scripts/Run-ShootingBalanceLab.ps1.
All 1800 bouts use the complete ShootingSession and shared geometry. End-of-tick Sight now
includes current post-shot recoil for the next AI observation; recorded shot aim is unchanged.

260/260 .NET and Unity EditMode pass; zero Console errors, four upstream warnings.
Evidence: artifacts/physiology/wp051-standalone.trx, wp051-{editmode,console}.json;
artifacts/shooting/balance{,-repeat}, docs/quality/m4-shooting-balance.json and comparison PNG.
All per-seed results hash c05703ae312082883549e7813b6f885845f25963da9dcfdfa17ee6a8c660697d
in repeat CLI, .NET and Unity (tests/fixtures/shooting-balance-v1.sha256).

Measured closures / 1000 shots: prone rest 1000, standing rest 1000, prone high HR 884,
standing high HR 885, standing tired 595, standing standard 970, standing windy 228,
standing easy 800, standing hard 988. Every bout completes with five discharges. Full hit-count
distributions, durations and clean-bout counts retained. Tests also reject invalid suites,
path traversal IDs and exhausted tick budgets. No hidden hit probability or physical AI boost.

Limitations: rest conditions saturate at 100%; easy profile has exactly four hits in this
seed window. These are recorded tuning/controller limitations for M8, not a claim of natural
human distributions. Wind compensation is absent in this primitive, explaining its large
penalty. Prone/standing apertures and sway differ together, so aggregate hit rates need not
be ordered. Samples share fixed external physiology/context and independent deterministic
noise phases; do not infer independent-shot clinical confidence intervals. No tuning altered
to force comparisons. No ski balance change or commit. Next: WP052 M4 technical gate.
