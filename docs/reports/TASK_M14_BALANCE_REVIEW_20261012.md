# M14 — final owner correction build, 2026-10-12

M14 remains open for owner testing. All requested corrections are implemented;
this report does not declare owner acceptance.

Test build: `Builds/M14BalanceReview20261012/START_M14_TEST.cmd`.
Unity 6000.6.0f1 Windows64 Development, DX11: succeeded, 0 errors, 0 warnings,
361,458,430 bytes. Independent player startup exited 0; menu render probe passed
at 1600×900 with 1,437 visible samples. Evidence is in
`docs/quality/m14/balance-review-20261012/`.

## Delivered

- Shared physical grip corrects actual A/D travel direction at race speed. The
  opening bend and tight finish turn have real-drive regressions. Neutral steering
  still seeks neither road centre nor tangent. S lowers pace and physically brakes.
- W/S selects a retained pace dial, initially zero. Zero holds the shared brake;
  focus loss no longer requests recovery propulsion. Compact HUD shows the dial.
- Three seconds of preparation leave race, input and accumulator clocks frozen.
  There is no catch-up burst at GO; pause also freezes preparation.
- New career offers junior/senior debut and age. Junior, development and World Cup
  calendars have actual eligibility and season transitions. Opponents have varied
  correlated abilities and use the existing shared weekly growth/training.
- Live player now receives the same prepared form/fatigue/pressure projection as
  AI. Rival pace/recovery/aim strategy is more competitive, with shared costs.
- Three timing gates/blue snow stripes replace six marks, anchored to real timing.
  The post-range split remains after penalty rejoin. Gate supports clear the route;
  the finish portal is 14 m wide, preserving height and the red finish stripe.
- Actual World Cup standings determine yellow/red/combined leader bibs, with IBU
  2026 points and placing countback. Junior/local races keep ordinary bibs.
- Shop and athlete skis share the same deck/accent catalog and cached instanced
  materials. A small retained animated heart follows observed HR. No graphics
  quality reduction, new authoritative colliders or per-frame geometry generation.

## Validation

Final combined .NET run: **81/81 passed** (`combined-final.trx`). Additional real
production corner regressions: **9/9 passed** (`manual-corners-final.trx`). This
includes physical impulse/energy constraints, neutral steering, career starts,
form fairness, scoring, saves, actual 30-athlete races and deterministic replay.

The final Sprint30 record/replay has identical authority protocol at **31,135 ticks**,
SHA-256 `8003092257f9b376370eadc7f67f03e4716e169cd1e417df75d1107c022413f2`.
The changed hash/tick count is expected from explicit steering/AI/career changes.

On the final shared-grip source, the fixed-seed novice with perfect route/precise aim
at effort 1000 finishes first with zero misses and a 22.940 s lead; effort 900 finishes
tenth with three misses, 80.272 s behind the best peer. These are diagnostic control
cases, not a promise about a human's result or all seeds. Both reject the old
uncontested one-minute novice advantage. Earlier calibration numbers in the career
sub-report predate the final grip change and remain historical evidence.

Native Unity: **31 PlayMode passes**, **2 EditMode passes**:
start/pace 4, device input 8, timing gates 4, appearance 2, front-end flow 10,
HUD 1, profile/age/equipment screenshot acceptance 2; EditMode pace/countdown 2.
RU normal and EN large text forms fit without scrolling. Pixel checks verify actual
shop colors; material tests verify the same colors on both actual skis and retained
instancing. Console has no compile/runtime errors from these checks.

## Scope and known limits

Eight pre-existing experimental AI solver tests remain red in a broader diagnostic
group. An isolated HEAD-resource baseline reproduces every one with byte-identical
failure messages: baseline 38 pass/11 fail, current 41 pass/8 fail. The three new
corner witnesses turn red to green. No tests were disabled or loosened to conceal
these failures. See `docs/quality/m14/review2-20261012/shared-grip-baseline-comparison.json`.
These experimental exact-boundary/planner tests are tracked separately from the
shipping field/replay and owner corner scenarios above.

Old profiles retain their generated athletes; choose a **new career** to test the
new age/league starts. Existing replay recordings require compatible definitions.
The current calendar still uses one venue and the three implemented race formats.
Circuit changes replace the active peer cohort after archival; a simultaneous global
multi-league population and defending first-event discipline champions are not
invented from missing historical data. Tight turns still require sensible speed.

Next action is owner testing of this build. M14 is not closed and M15 is not started.
Details/sources: [ADR-0060](../adr/ADR-0060-m14-fair-career-starts-and-owner-controls.md),
[career calibration](TASK_M14_CAREER_BALANCE_20261012.md),
[timing gates and IBU references](TASK_M14_TIMING_GATES_20261012.md).
