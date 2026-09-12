# TASK-101 — Bounded automated tuning

WP-101 complete, 2026-09-11. The optional optimizer is implemented as a finite, deterministic, **proposal-only** search. A schema1 definition declares allowed values, hard bounds, initial values, target windows and evaluation budget. The M8 allowlist contains only `reaction_seconds` and `tracking_error_rad`; hidden hit probabilities and undeclared coefficients are rejected.

The objective first minimizes normalized window violation, then distance to window centers; ties preserve declared enumeration order. Missing/nonfinite/overflowing metrics, duplicate parameters/targets, malformed ranges and budget overflow fail explicitly. Candidate dictionaries and returned proposals are read-only. The optimizer performs no filesystem writes or automatic application; its CLI adapter records all evaluations, source hashes and old/proposed values.

Training fixture: 32 candidates × 2 stances × 200 fixed seeds10000–10199 = 12,800 geometric bouts. Selected reaction **0.25 s**, tracking error **0.0002 rad**, with **86.5% prone / 85.5% standing** on training seeds. This reproduces the WP096 top candidate. Independent held-out evidence remains WP096's seeds30000–30999, never an optimizer input. The optimizer does not tune mid/developing targets, physiology or target apertures.

Validation: .NET3/3, Unity3/3; CLI32 evaluations, targets_met, applied=false. Final CLI build has no warnings/errors. See `quality/wp101/proposal.json` and `evaluations.json`.

Command: `Biathlon.BalanceLab tune-shooting <definition-json> <training-suite-json> <data-directory> <output-directory>`. Use `tests/fixtures/balance/shooting-tuner-v1.json` and `shooting-tuner-training-v1.json`. A proposal is evidence, not permission to silently alter a locked tuning version.

Next: WP-102 first playable tuning lock and milestone report.
