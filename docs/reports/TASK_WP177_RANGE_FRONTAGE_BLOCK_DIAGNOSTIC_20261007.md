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

The [IBU Event and Competition Rules, September 2026](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf)
specify range entry from the left, exit to the right, 30 shooting lanes,
2.75–3 m lane widths, and 2 × 1.5 m mats (articles 3.4.2.3, 3.4.3.1–2).
Woodland's 30 lanes, 3 m pitch and 2 × 1.5 m mats match those dimensions.
The measured obstruction is within the approach geometry and athlete
trajectory, so widening the firing lanes beyond the rule's maximum would
be the wrong cure.

Follow-up instrumentation of the first denied full-speed candidate shows
`WorldBodyConflict=true` for **all 1,903** mat-path athlete blocks, including
both sides of the tick-3,464 reciprocal pair. The authored cross-loop
world-body sweep itself rejects the moves; a coarse junction lease is not
the cause of this sample. The same seed still completed in 21,011 ticks
with the same event hash after this read-only diagnostic was added:
[world-body proof receipt](../quality/m13/wp177-woodland-course-20261007/wp177-mass30-conflict-proof.trx).

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

Three further full-field tuning trials isolated early planning. Leads above
75 m used a temporary observation-radius cap at half the shortest periodic
loop; both the data change and cap were removed. All trials completed 30/30,
but none improved the long-stop tail consistently:

| Trial | Main stops | Exit stops | Mat stops | Longest mat / exit (ticks) |
|---|---:|---:|---:|---:|
| Committed baseline | 581 | 320 | 2,031 | 271 / 26 |
| [100 m alignment](../quality/m13/wp177-woodland-course-20261007/wp177-mass30-lead100-capped-radius.trx) | 1,166 | 238 | 1,663 | 273 / 17 |
| [150 m alignment](../quality/m13/wp177-woodland-course-20261007/wp177-mass30-lead150-capped-radius.trx) | 584 | 128 | 1,677 | 514 / 17 |
| [12 m world lookahead](../quality/m13/wp177-woodland-course-20261007/wp177-mass30-lookahead12-experiment.trx) | 855 | 443 | 1,827 | 358 / 90 |

The 150 m lead cut aggregate stops but doubled the worst mat wait; the 12 m
lookahead worsened the exit. These are rejected experiments, not new balance
defaults. The world-body proof points to physical clearance and earlier
divergence before each private mat turn.

Validation of the retained diagnostic: the fixed-seed full Mass Start
passed 1/1 with the same 21,011 ticks and event hash; four existing dense
pack deterministic hash cases passed 4/4 in Release .NET
([receipt](../quality/m13/wp177-woodland-course-20261007/wp177-world-proof-deterministic-regression.trx)).
Unity MCP refreshed assets, passed the focused Pack Sprint EditMode test
1/1, reported no recent Console errors, and was idle after compilation.
The extra world-body flag is read-only telemetry; no command, result,
event, player control, balance constant or replay context changed.

Next correction must address the approach geometry and paid early steering
before athletes enter adjacent mat junctions, then run the same full-field
seed, player-controlled held-obstruction cases, deterministic replay and
Unity checks. An all-AI completion alone is not acceptance of the owner's
range-exit queue report. Manual world steering in the playable host and
remaining WP-175/178 work also remain open.
