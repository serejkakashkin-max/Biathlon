# M13 — Shared controlled braking and ordered bend approach, 2026-10-09

Status: partial integration checkpoint; M13 / WP-175 / WP-177 / WP-178 OPEN.
WP-176 PASS. No intermediate Windows build. Production host remains lateral-v1.

## Implemented

Shared manual motion and controlled slip drives accept a validated brake fraction
in [0,1]. Braking uses the existing athlete-mass-bound cornering cap (4 m/s² in
the current source definition), combines explicit and automatic braking with max,
and retains physical travel, stopping breakpoints and clock. No velocity reset,
extra braking budget, player assist, new hidden RNG or authoritative shooting tuning.
The full field validates every fraction before preparing any session or handoff.

AI queries only the already selected ordinary occurrence chain, stopping its
lookahead at closed lifecycle gates. It anticipates shared grip/yaw bend limits,
reserves body and control response distance, requests real braking and reduced
effort, and shortens its target before entering a sharp bend. Querying never
admits a successor. All inputs come from existing versioned physical/body/AI
definitions; no tuning source was duplicated. Neutral future grip is explicit:
this does not qualify weather-aware planning. Player signed input remains passed
through, and AI brake intent is observable. Context v4 binds every planner hash.

An owned wider-aisle body was rejected after the mat handoff by an inscribed
minimum-width polygon. The existing conservative connected edge tubes can now
independently prove the same overlapping full-body disk cover even when that
polygon is simple. Authored widths, turn reserves, body sizes, continuous proof
and separate phase gates are unchanged. No convex hull or disconnected bridge.
The actual provisional rejected actor is retained as diagnostic state without
publishing that failed tick.

## Verification

Evidence: `docs/quality/m13/field-stepper-20261008/`.

- Shared braking focused qualification: 67/67 .NET PASS, including non-stacking,
  continuous stopping, exact zero allocations, parity, and invalid last-row atomicity.
- Final braking/cover/planner subset: 67/67 .NET PASS.
- Final all-manual regression: **466 PASS / 1 FAIL / 467 executed**,
  `manual-braking-cover-planner-regression.trx`. No tests removed or disabled.
- Native Unity: controlled drive 21/21, field brake 1/1, invalid fractions 3/3,
  ordered bend planner 5/5, width-transition cover 10/10 PASS. These are actual
  Results counts; MCP discovery totals are not execution counts. JSON receipts saved.
- Unity refresh succeeded; compilation settled; recent Error Console empty.
  No visible standalone or FPS/GPU performance claim.

Earlier experimental TRX receipts are preserved, including millimetre sampling
validation, late speed-error braking and the wrong-lane diagnostic fixture.
The old controller context assertion and boxed test observer were corrected;
production limits and exact allocation assertions remain intact.

The previously watchdog-aborted **legacy** complete persisted replay passed in
its standalone rerun: 1/1, 30 athletes, 20,691 ticks, 169,026 command runs,
428,175,403 JSON characters, event hash 15908983522868725793 (179 seconds).
`woodland-full-replay-standalone.trx` records that result on the prior checkpoint.
This proves that legacy stream only, not complete manual replay.

## Still failing / next work

`AiRosterReachesItsFirstShootingBoutOnTheManualClock` remains active and FAILS.
The physical AI now passes the tight bend; the latest failure is tick3214,
`MovingFiringArrival`, athlete2/lane03, at the exact FiringLine cap with residual
velocity and spin. The provisional clock is aborted, not paid as a wait. Next:
physical stationary mat docking and lifecycle continuation, retaining complete
body/phase proof. Earlier report's blanket mat_01 label did not identify every
actor's assigned lane; the new rejected-working-actor diagnostic identifies it.

Reusable Sprint40 mats, finished actors, achieved-world drafting, complete manual
commands/replay, host selection and final WP-175/177/178 qualification remain OPEN.
Manual output/context changes are intentional and require new complete replay and
balance evidence. Owner's too-easy-shooting issue remains open for tuning. One
Woodland venue, max40/Mass30, G1/G2 approved, WP170–174 deferred; roadmap unchanged.
