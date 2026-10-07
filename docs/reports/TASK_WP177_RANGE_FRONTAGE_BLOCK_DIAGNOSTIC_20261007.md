# WP-177 / M13 — Woodland firing-frontage stop diagnosis

Status: diagnostic complete; player-visible queue defect and WP-177 remain OPEN.
No gameplay rule or Windows review build is changed by this task.

The production Woodland Mass Start has 30 athletes and 30 authored firing
lanes. A fixed-seed (`123`), packed-neutral full race finished in 21,011
fixed ticks. The [headless Release .NET receipt](../quality/m13/wp177-woodland-course-20261007/wp177-mass30-traffic-diagnostic-final.trx)
passed 1/1. Its test now emits a machine-readable stop matrix by physical
loop, active bout and initial blocking kind, plus drafting/overtake counts
and the exact blocking chain for the longest mat stop. Event hash:
`7863778188216105058`.

| Moving-intent stop location | Athlete-ticks |
|---|---:|
| Start approach | 32 |
| Main | 581 |
| Exit aisle | 320 |
| Penalty | 74 |
| Mat paths | 2,031 |

Mat admission capacity accounted for **0** of those stops. Initial physical
athlete blocks accounted for 1,903; 128 had no initial physical block in
this classifier. The longest continuous mat stop was 271 ticks (13.55 s).
At tick 3,464, athlete 5 was at `mat_06` station 0.798 m and blocked by
athlete 3 on Main station 1,080.463 m. Athlete 3's initial movement was in
turn blocked by athlete 5. Neither was shooting; both had already been
assigned distinct lanes. This is a reciprocal physical ingress obstruction,
not a shortage of firing mats or a reservation wait. The longest exit-aisle
stop remains 26 ticks (1.3 s) and involved another athlete body.

I tested two proposed fixes and removed them because they did not meet
acceptance. A narrowly scoped joint paid-sweep trial of the reciprocal pair
found zero collision-free admissions and left the race/stop counts unchanged
([first](../quality/m13/wp177-woodland-course-20261007/wp177-mass30-reciprocal-rescue-first.trx),
[second](../quality/m13/wp177-woodland-course-20261007/wp177-mass30-reciprocal-rescue-second.trx)
completed experimental receipts). A trial restricting AI to the outer side
of the frontage ran much slower than the baseline; it was interrupted
without a completion receipt and removed. Neither experiment is part of
the committed simulation or AI. No body ghosting, invisible speed change,
or artificial priority was introduced.

Next correction must address the approach geometry and paid early steering
before athletes enter adjacent mat junctions, then run the same full-field
seed, player-controlled held-obstruction cases, deterministic replay and
Unity checks. An all-AI completion alone is not acceptance of the owner's
range-exit queue report. Manual world steering in the playable host and
remaining WP-175/178 work also remain open.
