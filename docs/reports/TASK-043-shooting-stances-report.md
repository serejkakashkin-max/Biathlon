# TASK-043 — Shooting stances

Status: PASS, 2026-09-07. Added versioned ShootingStanceDefinition/profiles/JSON and
ShootingSwaySystem. Profiles bind breathing, heartbeat and muscular amplitudes; actual
WeaponAimSystem rate and ShootingStateMachine setup/exit durations now accept locked stance
modifiers. Neutral callers preserve previous results. Prone stability/recovery advantage
is validated; larger transition costs and reduced aim movement are data-driven trade-offs.

232/232 .NET and Unity EditMode pass; zero Console errors, four upstream warnings.
Evidence: artifacts/physiology/wp043-standalone.trx and wp043-{editmode,console}.json.
Cross-system exact setup readiness and aim rate, sway component comparisons, schema identity
and invalid advantage inversion are tested. Earlier timing/replay/zero-GC regressions pass.

Recovery multiplier is an explicit contract for subsequent bout/debt composition, not yet
applied to a full race recovery loop. Target apertures stay those from WP038. Visual stance
animation follows M11; no ski balance changes or commit. Next: WP044 breath control/debt.
