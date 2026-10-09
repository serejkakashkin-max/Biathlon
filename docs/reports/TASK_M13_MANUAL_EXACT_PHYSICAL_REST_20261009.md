# M13 — Exact physical rest at arbitrary heading, 2026-10-09

Partial checkpoint; M13 / WP175/177/178 OPEN; WP176 PASS. No owner build.

Three new regression cases reproduced cancellation residue at a fully paid stop:
-6.94e-18, 1.36e-20 and -1.39e-17 m/s. The unconstrained neutral snow minimizer
had already paid the necessary bounded impulses, yet summing them back into
world components left nonzero residual velocity/spin. Strict stationary firing
correctly cannot accept such a state.

`ManualSkiSnowContactSystem` now constructs the analytical free-constraint
null-space state directly. `ManualSkiSteeredSnowSystem` does so only for neutral
deflection, a selected unconstrained minimizer, and a fully paid angular response.
No tolerance rounds unresolved motion to rest. Driven endpoint composition uses
the actual final snow state directly; the initial source and all paid travel are
preserved. The three affected physical contexts are versioned. Shared forces,
grip/impulse/angular budgets and authoritative shooting tuning are unchanged.

New tests require exact rest at three non-axis-aligned headings and preserve
unresolved side-slip/spin on low grip. Existing 1620-case energy/impulse matrix,
continuous prefix/suffix, contact, source, allocation and determinism checks stay.

Evidence in `docs/quality/m13/field-stepper-20261008/`:

- `physical-rest-residual-red.trx`: 0/3 PASS before the correction, preserved.
- `physical-rest-constraint-green.trx`: 46/46 PASS.
- `manual-exact-rest-regression.trx`: 470 PASS / 1 FAIL / 471 executed.
- Native Unity: controlled25/25, passive snow8/8, contact drive14/14 PASS.
  JSON receipts contain actual execution counts. Refresh/compile/Console clear.

The one active failure is unchanged: full manual AI first-bout arrival at tick3214
still has substantial real velocity/spin. This numerical correction does not
pretend to solve that missing physical docking controller. Next inverse stopping
control and terminal mat approach, then the rest of the shared lifecycle.
Production host remains lateral-v1. Complete manual replay, reusable mats,
finished roster, achieved draft and final M13 qualification remain pending.
PROJECT_STATE history is appended, not replaced. Only final M13 Windows build.
