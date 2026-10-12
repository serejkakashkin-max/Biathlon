# M14 — timing, fresh Mass Start field and short burst

2026-10-12. All three requested corrections are implemented. **M14 remains OPEN
for owner testing and acceptance.** WP-182 and WP-184 corrections are delivered.

Test build: `Builds/M14BurstReview20261012/START_M14_TEST.cmd`.
Unity 6000.6.0f1, Windows x64 Development/DX11. Zero build errors/warnings,
361,467,558 bytes. Standalone startup exited 0 and rendered 1,437 visible menu
samples at 1600×900. Restricted-network Unity analytics DNS messages occur in
the startup log; no gameplay exception or crash occurred.

## Delivered

The duplicate 935 m gate is removed. The existing TIMING gantry now owns the
actual `range_exit_split` at main 960.005 m, after the penalty rejoin. Its snow
stripe, Tab standings and accepted timing use the same checkpoint. The other two
timing gates remain; no time-zero split is manufactured.

Fresh AI lost speed through two control errors: braking for moving bodies far
outside the stopping/reaction envelope, and optimizing one-tick passive snow yaw
by selecting nearly transverse skis even at roughly 400 W delivered power.
Ordinary course pursuit now retains bounded physical steering; precise private
mat movement retains the inverse. Shared force, grip and contact solvers are
unchanged. No hidden AI speed/power bonuses or graphics reductions.

The first crest alone did not catch the second cause. Final checks measure the
actual first registered checkpoint with thirty athletes in all three cohorts:

| Cohort | Before p90 / maximum gap | Final p90 / maximum gap |
| --- | --- | --- |
| Review | 34.438 / 49.024 s | 4.151 / 7.956 s |
| Junior novice | 40.405 / 59.442 s | 6.960 / 7.708 s |
| Senior debut | 6.581 / 12.704 s | 4.268 / 4.669 s |

Shift requests a short paid burst through shared physiology. Normal control
through 950 retains its previous requested-effort meaning; full Attack rises to
1.6 relative effort versus 1.3 normal maximum. Individual anaerobic capacity
determines duration; typical fresh full-power windows are about 12–18 seconds.
Power fades below 20% reserve. Existing fatigue, pulse and slow recovery charge
the work; releasing Shift does not refill the reserve.

The warm burst readout temporarily replaces the pace card and shows actual
reserve and approximate full-power seconds. Release restores saved W/S pace
through the normal control response. Brake/tuck suppress false burst readouts.
Normal gamepad trigger shares the 800 pace ceiling; right shoulder explicitly
requests Attack. Legacy physiology/device definitions retain their behavior.
No per-frame texture generation or new tick allocation.

## Verification

Final expanded core run: **78 passed, 2 known baseline failures** out of 80
(`qualification-final.trx`). All new burst/device, checkpoint, fresh-field,
closing-contact and full shipping-race scenarios passed. The two passive inverse
diagnostics reproduce the previously recorded messages exactly; see
`../quality/m14/review2-20261012/shared-grip-baseline-comparison.json`.
No tests were disabled or loosened.

Production Mass Start30 finishes every athlete at **18,638 ticks**; headless
wall time 43.839 s, about 21.26 times realtime. This measures simulation, not
rendered FPS on the owner's computer.

Career Sprint30 records/replays without AI to the identical authoritative
protocol at **28,941 ticks**, SHA-256
`c0e0bb0b82de70cb726e9d98b10776b8360e71279d93e1f9e94d0a8424f54647`.
Fixed-seed precise-route/aim novice at continuous effort 1000 finishes fourth,
zero misses, 28.360 s behind the best peer; effort 900 finishes sixteenth, two
misses, 62.217 s behind. These reject the old uncontested minute win, without
promising those placings for every seed or human driving style.

Additional physiology qualification: 61/61. Native Unity acceptance:
**21 PlayMode passes** (burst 4, input 8, timing gates 5, start/pace 4) and
**10 EditMode burst passes**. RU normal / EN large isolated HUD captures fit
inside the existing card. Unity compiled; Console had no errors.

Broader legacy traffic probes exposed nine older failures. Exact HEAD source/data
baseline reproduces them in `exact-head-baseline.trx` and `exact-head-start30.trx`.
These lateral-v1/nonshipping fixtures are tracked separately from the complete
production manual field. Historical failure evidence is preserved.

## Limits and next action

Evidence uses fixed cohorts/seeds and one venue. Existing careers retain their
generated athletes. Recorded races require compatible definitions; the changed
AI/physiology/checkpoint context deliberately changes new result hashes.

Owner should test the opening Mass Start climb/first split, Shift drain/release
and reused TIMING gate, then decide whether M14 can close. M15 has not started.
[Decisions](../adr/ADR-0061-m14-short-burst-and-fresh-pack.md).
Evidence: `docs/quality/m14/burst-pack-20261012/`.
