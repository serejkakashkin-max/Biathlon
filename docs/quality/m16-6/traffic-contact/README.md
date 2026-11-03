# M16.6.10 — shooting, moving passes and visible impacts

2026-11-03. Owner extended the skiing performance task with shooting checks,
faster-athlete overtakes and clear collision reactions. Source decision:
[ADR0093](../../../adr/ADR-0093-m16-6-moving-overtakes-and-impact-readability.md).
Performance-only changes: [performance evidence](../performance/README.md).

## Reproduced moving-blocker defect

Two production athletes start in one file,6m apart, on the original wide start.
The leader uses normal player effort150/1000 and zero steering; the trailer has
a verified achieved-speed advantage. Run200 fixed20Hz ticks with the real
ManualStepper. Original policy: maximum forward gap-1.526735928m, maximum lateral
separation0.297159970m. Red receipt retained. The cause was an explicit moving-body
exclusion in ManualScopedAiMovement, not insufficient power or penetration.

| Policy/control | Maximum forward gap | Maximum lateral separation | Result |
| --- | ---: | ---: | --- |
| Original v2 | -1.527m | 0.297m | Follows/brakes |
| Current v3, reserve gate unavailable | -1.527m | 0.297m | Correctly follows/brakes |
| Current v3, free line and sufficient reserve | +1.619m | 1.004m | Physical pass |

v3 requires an observed closing body, at least0.2m/s forward advantage and reserve
at least0.2. Existing full rotated-body forecasts, occupied-side checks, course
width/bend reserves and shared contact/force solver remain authoritative. No
speed/power/stamina/body-size bonus. Normal effort and maneuvering cost remains.
No free side means braking, not clipping.

The200-tick pass journal replays without AI: exact pose, velocity, route credit,
physiology, LastFrame and accepted impact observation match. Metadata records
moving_overtake_version; missing/zero usesv2, including backward seek; current
records usev3. Unknown versions reject. Existing careers are not rewritten.

## Physical impact and readability

The existing angular solver already turns both bodies on an eccentric collision.
With10m/s rear vs3m/s front and0.45m offset, both receive-1.070518267rad/s spin;
forward speeds become6.740866610 and6.259133390m/s. Momentum/energy checks pass.
A central symmetric impact receives no arbitrary invented torque.

Read-only strongest-impulse observations publish atomically with the accepted
tick. Provisional/aborted contacts cannot trigger a new hit. Ordinary next ticks
clear old impulses. Accepted command journals reconstruct them; observations
change no force, world pose, credit, physiology or RNG.

The rig reads these observations for torso pitch, signed shoulder twist/roll and
arm spread, recovering over0.8seconds. Root rotation remains the physical heading.
The effect precedes ground/equipment/grip corrections. Shooting/waiting/finished
states and backward seeks clear it; same-tick rendering cannot retrigger. Native
before/reaction/recovered screenshots are inspected: bone rotation exceeds10deg,
world root stays unchanged, and baseline pose returns exactly.

## Verification and delivery

Initial focused190-case shooting/weapon/overtake/angular-contact auditPASS.
Focused16-case new-policy/metadata/impact transaction/journal groupPASS. Final
full/native receipts, build/startup evidence and hashes are retained here. Initial
visual fixture errors (quality initialization and unrendered target) were fixed;
final captures contain real rig pixels. An earlier diagnostic Console message in
a replay test was resolved with an isolated passing rerun. No assertion relaxed.

Shooting geometry, recoil, breath/bolt controls and weapon balance are unchanged.
Requested passing decisions can change positions/times and are versioned for
replay. Performance-only witnesses keepv2 and exact pre-optimization physical
hashes. No graphics or shadow-quality reduction.

Owner launcher: `Builds/M16_6_10_Performance_Review/START_TEST.cmd`.
M16.5closed;M16.6open for ownerreview;M17notstarted. Next owner check: affected ski
section, overtaking with a free side, glancing collision, prone/standing shooting.
