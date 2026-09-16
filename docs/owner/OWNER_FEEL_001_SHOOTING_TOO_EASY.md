# OWNER-FEEL-001 — Player shooting feels too easy

Status: **OPEN**. Category: owner gameplay-feel / tuning issue. Origin: [G1 owner decision, 2026-09-13](G1_DECISION_2026-09-13.md), following hands-on Windows greybox Sprint and Pursuit testing.

Owner observation: **«СТРЕЛЬБА ИГРОКА СЕЙЧАС ОЩУЩАЕТСЯ СЛИШКОМ ЛЁГКОЙ.»**

This issue does not block M11. It remains a mandatory follow-up through the relevant presentation and later tuning stages. G1 is approved for core controls/gameplay direction; neither that approval nor provisional automated shooting balance results establish final player shooting feel.

## Invariants and M11 scope

Player hits and misses must remain physically and geometrically explainable. Never use hidden RNG, a per-shot probability roll or stats that invalidate a geometrically valid player shot.

M11 preserves authoritative shooting values and mechanics. Do not silently adjust numerical shooting difficulty while implementing animation. Inspect the presentation and correct a demonstrated presentation defect with an explicit cause, before/after evidence and relevant tests; do not treat that allowance as permission for an unrelated balance pass. Animation, rifle/hand alignment and camera changes must not take authority over hit calculation.

The greybox may conceal, exaggerate or simplify the visual aim challenge. Compare visible rifle/reticle/target behavior with authoritative aim components and shot geometry before assigning a cause. The owner observation is not yet proof of a simulation defect or of a specific parameter needing adjustment.

## Required review checklist

For every item, record the authored configuration, observed simulation effect, player-visible cue, trade-off where applicable, and evidence under comparable conditions. Check whether the effect is sufficiently strong **and** sufficiently clear to the player.

- [ ] Breathing sway.
- [ ] Heartbeat impulses and current HR.
- [ ] Muscular fatigue.
- [ ] Stance-specific stability, including prone versus standing.
- [ ] Pressure/composure.
- [ ] Wind and ballistic influence.
- [ ] Trigger timing.
- [ ] Breath-hold trade-offs.
- [ ] Rebound after holding breath too long.
- [ ] Fine aim control.
- [ ] Athlete shooting stats.
- [ ] Difficulty/accessibility assists, including the actual default assist configuration.
- [ ] Greybox presentation compared with authoritative shooting telemetry: target/aperture scale, camera framing, visible aim motion and rifle/reticle alignment must not make the challenge artificially easier or hide its cause.

## Mandatory roadmap follow-up

| Work package | Required action | Current status |
|---|---|---|
| WP-147 Shooting procedural/IK layer | Check aim/rifle/hand alignment and legibility of existing stability effects; preserve shot authority and numeric tuning. | Presentation review completed; [findings](../quality/m11/wp147/OWNER-FEEL-001-presentation-review.md). Third-person sight mismatch corrected; issue remains OPEN. |
| WP-151 Animation validation scenes | Retain prone/standing, transition and shooting-camera evidence; identify presentation defects separately from future tuning needs. | Review completed; [evidence and retained actions](../quality/m11/wp151/transition-and-feel-review.md). Issue remains OPEN; no authoritative retuning. |
| WP-229 Gameplay feel tuning pass | Revisit the complete owner checklist with player feedback and telemetry; assess controllable difficulty, timing and recovery costs. Record each finding and any explicit versioned tuning. | Mandatory, pending |
| WP-230 Difficulty/accessibility tuning | Revisit the complete checklist across default settings and assist/difficulty profiles; show what each assist changes without hiding shot outcomes or giving AI physics advantages. | Mandatory, pending |
| WP-232 Race balance finalization | Revisit the complete checklist in Sprint, Pursuit and Mass Start; verify ski-versus-shooting trade-offs, fatigue/HR arrival costs, accuracy and range time. Preserve deterministic regression and balance evidence. | Mandatory, pending |

Later follow-up reports must reference `OWNER-FEEL-001` explicitly. A presentation fix may resolve one cause while leaving the tuning issue open; carry remaining findings forward rather than silently dropping them.

## Closure evidence

Before marking resolved, record the tested build/configuration, conditions and athlete/assist profiles; before/after telemetry and representative captures; which findings were corrected, deferred or disproved; relevant tests/deterministic regressions; and player feedback demonstrating the intended effect. Every mandatory WP-229/WP-230/WP-232 checkpoint must retain an explicit review outcome even if an earlier pass improves the feel.

Any authoritative tuning belongs to an explicit logged/versioned tuning change under the Master Plan. No new owner gate is created by this tracking document; unresolved findings must remain visible in project state and the relevant milestone reports.
