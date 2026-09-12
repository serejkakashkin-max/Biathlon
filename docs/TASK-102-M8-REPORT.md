# TASK-102 — M8 Balance milestone

**M8 PASS — first playable provisional tuning locked.** WP091–102 are complete. The next unlocked package is WP103 (M9 career schema). No mandatory owner/blocker gate is pending; broad game-feel review remains recommended before visual polish.

The lock `docs/balance/tuning-lock-v1.json` identifies `biathlon_first_playable_tuning_v1` and verifies **127** resource/track/experiment inputs. Domain schema, read-only verifier and CLI detect changed/missing entries and reject unsafe/duplicate paths. Git newline conversion does not invalidate the text hashes. See ADR-0018 for revision policy.

| Package | Evidence and outcome |
|---|---|
| 091–094 | 10,002 complete seeded races; bounded workers, JSON/CSV/Markdown metrics/distributions; exact canonical snapshots and explicit target diffs. |
| 095 | 448 training races, 1,536 independent held-out races; selected pacing saves 4.135/5.754 s on flat/rolling long tracks with lower range HR/fatigue. |
| 096 | 18,000 held-out bouts / 90,000 geometric shots; top normal 87.20% prone, 85.08% standing, within provisional GDD windows. Imperfect wind reading affects aim only. |
| 097 | Eight actual shared-race draft pairs; mean 3.195 s saved per athlete; bounded aerodynamic effect, occupancy and downstream race costs retained. |
| 098 | 96 +10 ski marginals; 6,000 shooting bouts; separate time/accuracy effects, no aggregate single-stat dominance in the declared terrain matrix. |
| 099 | 32 equipment comparisons; -2.24% to +1.80% finish benefit, below broad athlete progression in every matched condition. |
| 100 | Expert/developing on flat/rolling: copied AI commands reproduce complete physics, resources, geometry and results in a field without an AI controller. |
| 101 | 32-candidate bounded search; explicit allowlist/budget/windows, full evaluation log, independent holdout, immutable proposal, no automatic application. |
| 102 | Versioned 127-input lock; missing/changed data regression, full milestone evidence and state publication. |

Validation at closure: **505/505 full .NET regression**, plus **5/5** newly added tuner/lock tests on the final code (510 discovered in total; this is not a claim of one combined 510-test run). New WP096–102 Unity EditMode tests **15/15**. Unity PlayMode **7/7**, including full shared Sprint, shooting and persisted replay. Final CLI build: zero warnings/errors. Final Unity Console: **0 errors, 1 warning, compilation_failed=false**; the MCP warning cache returned no entries after PlayMode reload, so no origin is inferred for that retained warning. Full TRX/XML and CLI artifacts are under `artifacts/balance`; their counts/hashes are recorded in `quality/m8/verification.json`.

Determinism: old shooting/competition regressions pass; held-out shooting and draft/stat/equipment JSONL are exact across .NET/Unity. CLI tuner proposal/evaluation output repeats byte-for-byte. Core assemblies remain independent of UnityEngine. No hidden speed/power/hit multiplier was introduced. No global physiology, draft, equipment or target-aperture coefficient was changed by this calibration; new cohort/controller profiles are explicit experimental data.

Known limits remain visible in this provisional version: synthetic courses and fixed-condition cohorts are not population forecasts; mid/developing difficulty windows are not approved targets; fixed shooting timing can produce non-monotonic accuracy under an unchanged trigger policy; reserve/recovery have small marginal effects in the chosen ski workload; expert neutral full-race shooting is still very accurate. The top-cohort target result must not be presented as a global retune of all race AI. Career population calibration follows in M9. Production presentation, real venues and subjective feel approval belong to later milestones.

Next: WP103–124, living multi-season career headlessly. WP112 permits risk calculation only; severe punitive injury behavior still requires explicit owner-approved design and is not part of the autonomous implementation scope.
