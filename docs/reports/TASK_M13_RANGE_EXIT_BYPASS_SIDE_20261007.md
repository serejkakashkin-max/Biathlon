# M13 range-exit passing-side correction — 2026-10-07

Status: partial defect reduction. Owner queue issue and M13 remain OPEN.
No Windows review build was made.

The existing v3 production Mass Start completed with 30 athletes in 21,047
fixed ticks, but moving intent had 344 stationary actor-ticks on `exit_aisle`;
the longest single exit stall was 53 ticks (2.65 s). 342 of those stopped
actor-ticks had an athlete blocker, two a width block. At the longest stall,
athlete 25 was at exit station 140.057 m, line −1.3 m, and requested a shift
further left to −1.6 m while athlete 22 was 1.522 m ahead on line −2.05 m.
The full-field diagnostic is recorded in
[v3 TRX](../quality/m13/wp177-woodland-course-20261007/wp177-mass30-stops.trx).

The AI's bounded lane search previously tried left first regardless of the
observed body. It now chooses the first trial side away from the nearest
forward athlete inside the authored lookahead and physical lateral
separation. Both candidate sides still pass the same read-only lookahead
and the field's authoritative body/width sweep; the player command, ski
physics, shooting and collision dimensions are unchanged. The production
AI decision context advances from the prior v3 marker to v4. Existing
command tapes intentionally remain replayable without running AI, because
they bind field physics and the actual recorded inputs, not AI decisions.

On the same fixed seed, the side-only v4 Mass Start completed in 21,011
ticks in the isolated Release .NET test. Exit stopped actor-ticks fell from
344 to 320, the longest continuous exit stop from 53 to 26 ticks; Main
stops fell 801→581 and penalty loop stops 178→74. Mat-path stopped ticks
rose 1,569→2,031, so this is not a claim that all traffic was improved.
The 26-tick exit stall remains a visible possible delay. Evidence:
[v4 TRX](../quality/m13/wp177-woodland-course-20261007/wp177-mass30-stops-side-only-v4.trx).

An attempted extra change that rechecked every held escape goal every tick
made the full-field run markedly slower; the attempt was interrupted without
a completion receipt and removed. It is not included in v4 or counted as a
successful test. The first third-bout regression with a historical fixed
player index failed its fixture precondition: changed AI order left no
follower behind that athlete. This is retained in the
[failed fixture TRX](../quality/m13/owner-exit-third-bout-20261006/owner-third-bout-bypass-side-v4.trx).
The strengthened headless test scouts a fixed-seed athlete whose **first**
third-bout exit position has an actual follower, then reconstructs the
player-controlled run. It found athlete 2 at tick 11,695. With zero player
effort for 240 ticks, one follower passed with zero stopped ticks; the
[scouted third-bout test](../quality/m13/owner-exit-third-bout-20261006/owner-third-bout-scouted-bypass-side-v4.trx)
passed 1/1. The separate
[second-bout test](../quality/m13/owner-exit-third-bout-20261006/owner-second-bout-bypass-side-v4.trx)
passed 1/1, with two followers passing and zero stopped ticks. Zero effort
does not force a full stop under the existing ski physics: the controlled
athlete was still moving at the end of each scenario. These tests prove
passing in the sampled traffic, **not** a stationary-player queue cure.

Unit/integration selection for AI side choice and range traffic passed
[13/13 Release .NET](../quality/m13/owner-exit-alignment-20261006/range-bypass-side-unit.trx),
including command replay; both new side-selection methods passed targeted
Unity MCP EditMode 2/2. Recent Unity Error Console was empty.

Remaining acceptance: remove the 26-tick exit stall with actual body-safe
earlier steering, distinguish mat transitions from avoidable waits, prove a
genuinely stationary player can be passed, integrate full manual world
direction for player and AI across start/range/penalty, and let the owner
review the single post-batch Windows build. No claim of M13 PASS.
