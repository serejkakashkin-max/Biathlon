# TASK_M13_OWNER_TRAFFIC_01 — Stationary start bypass and moving range departures

2026-10-05. Status: implementation/focused checks PASS; final native/full regression,
three-format exact replay and Windows delivery acceptance IN_PROGRESS.

Owner request: AI tries to pass a stationary player at the Mass Start; range/penalty
departures find separate moving lines rather than an artificial exclusive queue.
Authority: owner hands-on directive, AGENTS, Master Plan, GDD, ADR0031.

Start policy observes paid stationary travel for1s and an obstacle at≤0.15m/s, then
requests ordinary safe lateral overtaking. Moving grid hold and player commands remain.
Perception joins only the first exact paid Start/Main seam, not later laps.
The actual full field exposed eight trapped rear athletes despite the first-rear
test passing. A strict all29 regression failed first. Destination-line lookahead
now uses the existing body/tail proof across the curved start and Main continuation;
probe stride is body separation plus configured passing clearance and goal hold is
bounded by the existing overtake timeout. Only ordinary next-step intent is accepted.
All29 now pass the stationary player in .NET and native Unity(67.95s); the player
remains at zero paid travel. This does not weaken any world contact/width check.

Optional schema2 world-body admission binds exact complete geometry and the named
range/penalty merge. Paid fractional traces, configured bodies, pose envelopes and
incoming tails remain mandatory. Indexed bounds exactly match the exhaustive reference.
The actual30-body red reproduction had a circular wait between private entries, exit
aisle and penalty return; fixed-line AI could not resolve it. Safe bounded escape now
includes periodic bodies/tails and post-shoot private departure, rejects a clipped no-op,
and never redirects an approach away from its mat. Commands pay ordinary lateral costs.
The actual qualified Pursuit40 schedule exposed a second circular wait after a
departing actor left the range planner and joined Main. The final bounded escape also
covers stalled Main actors, and draft planning no longer overrides that active escape.
The retained actual Sprint qualification input reproduces this defect. A strict new
regression finishes all40 at tick28947, observes a paid Main escape and maximum
non-shooting stationary interval463ticks(23.15s). This proves the circular wait clears;
it does not claim that every transient pause in every dense merge has disappeared.

The official rule is [IBU2026,7.1.4](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf):
athletes on/exiting the penalty loop have priority over those entering it. This is
limited to the actual matching penalty region and grants no collision exemption.

Focused evidence:13 start tests PASS;42 departure/legacy-apron tests PASS; latest
22 final world/start/departure checks PASS. Actual30-body9,000-tick prefix has15,869 paid departure
frames,9,211 penalty frames and maximum stopped departure27ticks(1.35s), previously450.
The regression requires<100ticks and exact body separation. Safe transient yielding
remains physically necessary. Permanent queues and collision pass-through are prohibited.
Read-only query bounds/serialization/zero-allocation tests and exact projection reference
remain strict. Earlier native start13 was verified in separate runs; the new all29
native receipt is `native-start-all29-final.xml`. Native world5 PASS386.50s; the actual
30-body regression after the final start fix PASS139.478s. The new qualified-Pursuit
native test and final full suite are recorded separately at delivery.

All rejected prefixes, timeout receipts and superseded complete records are retained
under `artifacts/owner-traffic-guidance-20261005/`; they are not final acceptance evidence.
The old global career/balance policy is byte-identical; new traffic changes are opt-in.
Race output changes for Woodland and must pass fresh full races and persisted replay.
M13/WP175–178 and full playable manual heading remain OPEN.


## Shipping evidence and owner correction — 2026-10-05

The frozen implementation(a79e4cc/58567d9/c9a660c) passed the full core suite:
1067/1067, zero skips. Fresh completed Mass30(23087ticks), Sprint40(43157ticks),
and actual-Sprint-qualified Pursuit40(46092ticks) all passed persisted exact
state/event/result replay without AI. Final native all29 stationary-start bypass,
actual qualified Pursuit40, guidance and build-lease checks passed.
The Windows Development/High build was delivered and subsequently played by the
owner. Hidden startup/400paid-tick checks passed for all three formats; rendering
was suspended, so these checks are not sustained GPU/FPS evidence. Build reported
0errors and496warnings(36Sentis pixel/460ConvGeneric compute shader warnings).
Owner23 disk paths and6 native settings assets/dirty flags were verified preserved.
Portable replay receipt hashes: `docs/quality/m13/traffic-guidance-shipping-20261005.json`.

Owner hands-on review requests additional changes. Long merge delays remain
OPEN(Sprint actor16 waited2003ticks/100.15s at Main150.106m). Early mat navigation,
tuck pose and visible shooting preparation require correction. Full manual direction
is still disconnected from the playable host. No whole-traffic or M13 closure is
claimed. New work follows the owner directive permitting rare physical contacts;
the previous strict no-contact fixtures remain valid legacy evidence.
