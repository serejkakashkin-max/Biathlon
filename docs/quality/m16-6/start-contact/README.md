# M16.6.12 — Occupied start contact repair

2026-11-05. Owner reported the career race stopping with
`Manual field step rejected / ContactGeometry`. Actual read-only owner Pursuit
reproduced rejection at tick1294: player21 had started at825 and stayed still;
entrant28, scheduled1293, entered the exact same start pose. The SAT gap was
-0.7m. Eight synthetic Pursuit entrants minimize the defect to tick44. Both
cached and uncached original-v2 controls retain that failure.

New start-continuation version3 admits an interval-start body only when its
assigned rectangle fits. A waiting entrant stays off course with zero travel;
official start and elapsed clock continue unchanged. Already admitted skiers
remain physical. No teleport, collision-tolerance increase or unpaid movement.
The accepted tick publishes held/released state and events atomically. RU/EN HUD
explains the wait. Old replay versions0/1/2 keep their original solver behavior;
new3 survives metadata roundtrip and backward seek. See ADR-0096.

## Evidence

- `m16612-owner-start-red.trx`: exact original owner failure at1294.
- `m16612-minimal-red.trx`: portable original eight-body failure at44.
- `m16612-start-regression.trx`:185PASS, including the original complete16-event
  season, start/world/journal/replay/performance regression. All16 finish ticks
  match the prior M16.6.10 qualification; new release events intentionally change
  interval-race event hashes. Baseline physics hashes remain unchanged.
- `m16612-visible-gates.trx`:23PASS after the final presentation guard; overlaps
  the185 group. Covers held/released/paid clock/events/abort/command reconstruction
  and metadata. No single combined unique-test count is claimed.
- `m16612-owner-complete.trx`: explicit local private-fixture diagnostic separately
  PASS. Actual30-athlete Pursuit completes at22211; entrant28 released at1526
  after the player resumed at1500. Ordinary manual aim/trigger/bolt commands
  exercised all four shooting bouts. No result was published into the career.
- `native-edit-81-summary.json`:81 distinct passing native EditMode cases.
  Initial tool transport timed out after300s;50 passing results were archived.
  The remaining two test classes were explicitly rerun:41PASS including10
  overlapping cases. `native-gates-final.json`:final6 affected cases PASS.
- `native-play.json`:5PASS, including new3 and historical1/2 replay/seek,
  accepted-world steering presentation and shooting HUD.
- `standalone-owner-review.json`:actual Development-player owner-copy scenario,
  accepted field/world ticks2300, entrant28 held then released1526, zero errors.
  `occupied-start.png` and `released-start.png` visually inspected.
- `standalone-fullscreen-review.json`:2560x1440 FullScreenWindow PASS,
  RU/EN title,18 art resources, isolated fixture and owner profiles preserved.
  `fullscreen-home.png` inspected. Both standalone processes exit0.
- `build-summary.json`:Succeeded,0errors/526warnings,484849496bytes.
  `console-clean.json`:0 Error/Exception/Assert. `settings-preserved.json`:
  all25 ProjectSettings assets byte-exact to the task-start state.

The private owner JSON and Player.log are **not** included. The explicit owner
test requires a local read-only fixture; normal CI uses the portable synthetic
case. The entire test assembly was not rerun for this bounded repair.

Portable checks from the repository root:

```powershell
dotnet test dotnet/Biathlon.Tests/Biathlon.Tests.csproj -c Release --filter FullyQualifiedName~ManualStartReleaseGateTests
dotnet test dotnet/Biathlon.Tests/Biathlon.Tests.csproj -c Release --filter FullyQualifiedName~M166TrafficVersionTests
npx --yes unity-mcp-cli run-tool tests-run --path C:/game/Biathlon/Biathlon --input-file docs/quality/m16-6/start-contact/native-play-args.json --raw
```

Physics capabilities, stats, shooting geometry/RNG and official distances are
unchanged. Blocked entrants now pay their physical waiting time, so affected
race time/order may change. The assigned line waits until clear; it is not
automatically reassigned. M16.6 stays open for owner review; M17 not started.

Review launcher: `Builds/M16_6_12_Start_Contact_Review/START_TEST.cmd`.
Next owner check: retry the Pursuit from the existing career, pause on the start
line long enough for the next entrant, then resume and finish normally.
