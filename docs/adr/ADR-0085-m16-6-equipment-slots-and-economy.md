# ADR-0085 — Career equipment slots, bounded rifle handling and economy

2026-11-01. M16.6.2 authorizes equipment/economy development, not new athlete
power, target radii or random hit rules. Implementation/qualification in progress.

## One equipment path

Keep the existing three-ski base catalog and its original values intact for
legacy/core fixtures. An additive career catalog supplies five authored
specializations and inherits the existing 8% combined modifier /3% condition
affinity limits. There is one live definition per model; a race snapshot records
the resolved catalog for reproducible playback. Existing snow/equipment systems
compose resistance and grip once; the shop only observes those same outputs.

Use an explicit selected-rifle slot alongside selected skis and the shared owned
inventory. Save version4 migrates1/2/3 by retaining every existing value/profile
and adding owned/selected neutral rifle only. No money or selected skis are
rewritten, and loading cannot reroll looks. Unknown/unowned selection and duplicate
purchase remain errors; UI publication remains an atomic detached save update.

ShootingEquipmentDefinition extends additively: recoil describes stability,
handling retains bolt/settling timing, ergonomics controls setup/exit handling.
Bound every factor and combined setup factor by the existing equipment_envelope.
The neutral version1 encoding remains byte-exact. Different rifles are supplied
per entrant to the same ShootingModelDefinitions/ShootingHandlingSystem/state
machine. No target, ray, projectile, hidden accuracy or ability change.

## Versioned race context

New metadata records the complete resolved equipment snapshot and version.
Missing equipment metadata explicitly uses the unchanged base ski catalog and
neutral rifle, preserving old command-context identities independently of start
draw, weather and course-reaction compatibility. New equipment is part of the
field context and must survive replay/seek. Do not silently substitute a new
catalog into an old journal.

## Economy and AI

Starting gear remains competitive and free. Purchases, camps and recovery spend
the same finite budget; AI may only acquire offered equipment with its own saved
money, retaining a service reserve and selecting through real condition outputs.
No complimentary premium gear. Career references Simulation to reuse its existing SkiEquipmentSystem for purchasing decisions; Simulation remains independent of Career (acyclic). Costs/prize tiers are authored in career data;
calibrate weak/mid/strong rank scenarios across all three starts for two seasons.
Record hypothetical financial scenarios separately from measured race outcomes.
Premium gear is a savings goal, and weak finishers retain participation income,
free rest/training and the existing starter gear to avoid a debt trap.

Keep the existing six training formulas unchanged. Economy/gear may change future
shopping and race results as authorized, so publish calibration receipts and
deterministic regressions before declaring this stage complete.
