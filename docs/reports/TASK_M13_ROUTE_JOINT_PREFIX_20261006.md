# M13 — common body and course prefix

Added a non-committing query that takes every candidate world trajectory,
the reached-contact pairs, any explicit local edge planes, and one bounded
selected-route window per actor. It returns the earliest unproved time from
whole-body contacts, local edges or the continuous route inner tube. Stable
athlete IDs make ties independent of array order. Missing, foreign and duplicate
route windows are rejected; no actor can silently skip one of the checks.

On Woodland geometry, a lateral departure is limited by the course, while
an overtaking athlete approaching a stopped body is limited first by contact.
The reversed actor order gives the same pair/time. Clear repeated previews
allocate no managed memory after construction.

The work-limit regression now checks both the route-tube query and the common
joint query. When the interval proof exhausts its one-check budget during an
outward departure, neither query may report any clear travel. This prevents a
future optimization from accidentally interpreting an incomplete proof as an
unobstructed path.

Validation: 23/23 focused .NET spatial/route tests PASS, native Unity joint
class 5/5 PASS, and native route-tube class 5/5 PASS. Unity scripts compiled,
Editor is idle, and the recent Error Console is empty. An earlier
hidden PlayMode ribbon test had logged an FMOD output-device error, unrelated
to this EditMode check. Evidence:
`docs/quality/m13/owner-manual-route-joint-20261006/`.

The query does not move or bounce athletes, commit route progress, handle all
range/penalty geometry, record events or bind replay. The playable race still
uses v1 route following. M13/WP175–178 stay open; no intermediate Windows build.

The next intermediate query now samples every actor's actual world pose at the
common proven prefix, then projects it only inside that actor's selected route
window. It validates the full roster before filling any caller-owned result
slot. A failed second athlete therefore leaves the first athlete's slot
untouched. No heading change, odometer award or branch admission is implied.
Focused .NET route/spatial tests 25/25 PASS; Unity joint class 7/7 PASS, Editor
idle, recent Error Console empty. The earlier hidden PlayMode FMOD diagnostic
remains separate. Additional receipts are `common-projection-dotnet.trx` and
`native-common-projection.xml` in the evidence directory above.

The solver now also offers an atomic shared-time frame proposal containing
world pose, planar velocity and route projection for each actor. A later actor
projection failure leaves the entire caller-owned frame array untouched.
Actual Woodland pair-contact and failure fixtures pass; warm repeated queries
allocate zero managed bytes. Focused .NET joint class 9/9 and native Unity
EditMode joint class 9/9 PASS. Evidence:
`docs/quality/m13/owner-manual-route-joint-20261006/m13-manual-common-frames.trx`
and `native-common-frames.xml`. Unity compiled, Editor idle, Error Console
empty. No pose or race state was committed; live manual control remains open.
