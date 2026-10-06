# M13 — manual world tick to base-route race commit (2026-10-06)

## Result

A `SprintAthleteSession` can now replace its legacy route-following movement
preview with distance, speed and credited station from a jointly safe manual
world-roster proposal. The replacement keeps the same prepared physiology,
uses one continuous timed motion segment for checkpoints and race lifecycle,
and invalidates the replaced preview. It refuses an unbound or stale world
proposal and refuses the legacy travel clip afterward. The world ledger can
be reconstructed at the accepted race tick, with that tick recorded in its
odometer context.

The bridge is deliberately restricted to one selected base-route section
before its next range entry. It does not admit a route seam, start approach,
range lane, mat, penalty loop or contact; each needs physical geometry and
group-safe transition handling. It is not called by the playable field/host.

## Verification

- Release .NET bridge 2/2 PASS: accepted Woodland Sprint base-section tick
  reproduces the accepted world cursor/odometer in the race session; a world
  proposal consumed before race commit is rejected without moving the session.
- Release .NET bridge, roster, Sprint athlete, pack field and pack replay
  regression 40/40 PASS. Evidence:
  `docs/quality/m13/manual-base-bridge-regression.trx`.
- Unity EditMode bridge 2/2 PASS, XML:
  `docs/quality/m13/manual-base-bridge-unity-editmode.xml`.
- Unity Editor idle after refresh and recent Error Console empty.
- Initial focused test failed because a mid-race reconstructed world roster
  started at tick zero. Its constructor now accepts the actual accepted race
  tick; the final focused and broader regression runs pass.

## Remaining work before Windows review build

The entire roster must share one race/world transaction. Force/technique and
physiology preview must feed world motion for player and AI, with contact
resolution, section/start/range/penalty transition, complete events and v2
replay. Presentation and input must then use the accepted world pose, and the
owner correction batch must pass Unity and hands-on smoke checks. M13 and
WP-175–178 remain open; no Windows review build was issued here.
