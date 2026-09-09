# TASK-044 — Breath control

Status: PASS, 2026-09-07. BreathControlDefinition/JSON and BreathControlSystem track held
state, normalized debt, discomfort, rebound and rearm lock. Short holds reduce breathing;
long holds lose their benefit and force release. Holding the button cannot automatically
restart after exhaustion. Skill reduces but cannot erase cost; prone recovery is applied.
ShootingSwaySystem accepts the multiplier only for breathing. Event IDs 23/24 record starts
and ends, with forced release payload. No heartbeat suppression or hidden hit roll.

236/236 .NET and Unity EditMode pass; zero Console errors, four upstream warnings.
Evidence: artifacts/physiology/wp044-standalone.trx and wp044-{editmode,console}.json.
Tests: prolonged hold, debt/rebound/rearm, expertise cost, prone recovery, component isolation,
replay events, invalid input and zero allocations. Rebound test corrected to accumulate
meaningful debt: one tick's small debt can fully recover on the next tick under current data.
No failing assertion was disabled. Earlier deterministic regressions pass.

Debt is a gameplay control resource, not a medical oxygen simulation. Tuning is provisional;
short repeated timing windows remain intentional skill input, with long-hold abuse bounded.
No ski balance changes or commit. Next: WP045 wind projection and projectile influence.
