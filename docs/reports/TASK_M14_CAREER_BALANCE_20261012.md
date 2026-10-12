# M14 owner review — career balance and progression

2026-10-12. M14 remains open for owner acceptance.

## Diagnosis and changes

The production career already projected saved attributes into the same physical
and geometric race systems for every athlete. Three weaknesses remained:

1. Manual player input bypassed the career readiness projection. The Unity host
   supplied form 1 and default pressure, whereas AI used saved form, fatigue and
   confidence. `PackAiRaceRunner` now applies the entrant's authoritative career
   form/pressure at the player boundary as well; effort and aim stay player-owned.
2. Production opponents used the earlier cautious tuning: hold effort 750,
   180 m pre-range recovery with up to 350 effort reduction, and cautious aim
   timeouts up to 12 seconds per target. New explicitly selected gameplay AI
   definitions use hold effort 835, 90 m recovery/up to 180 reduction, and
   4.5/2.5 s cautious/fast aim limits. These are recorded control decisions,
   not speed bonuses, power multipliers, hidden hits or altered physics.
3. A newly generated roster mixed adults with a 19-year-old rookie, with every
   attribute independently drawn. Explicit career starts now choose a coherent
   age/ability cohort with bounded specialization and a real calendar.

## Career starts and progression

`FrontEndSession.StartProfiles` exposes the two authored choices.
`CreateCareer(id,name,nation,seed,startProfileId="junior_novice",age=null)` creates
the selected cohort. The original `CareerFactory.Create` overload retains its
historical data and exact legacy save fingerprint.

| Start | Age at 31 December of first season year | Rookie baseline | Opponent baseline | Initial circuit |
|---|---:|---:|---:|---|
| `junior_novice` | 18–20; default 18 | 42–52 | 45–67 | Junior U21 |
| `senior_debut` | 21–30; default 22 | 52–62 | 57–80 | Development |

Each athlete receives one baseline ability level plus independently bounded
specialization of ±6 stat points. The first season begins in 2026. The U21
calendar requires age at most 20 in that year; an 18-year-old may start in the
higher age group. The current IBU 2026 age table and permission to start higher
were researched by the coordinating agent. These age rules are represented by
the versioned start/calendar data, not a hidden race adjustment.

Player and opponents continue to use the same weekly training, fatigue, form,
ageing and permanent adaptation systems. Junior age-out and earned promotion
are evaluated only after season archival. Age-out selects development; earned
International tier selects World Cup. A new circuit introduces its own stable-ID
opponent cohort while preserving the player's identity, earned attributes,
equipment and compact season history. World Cup peers have a 74–92 baseline,
±5 specialization, subject to each athlete's existing potential cap.

World Cup scoring uses the verified IBU 2026 standard and Mass Start points
tables (90 for first, standard rank 40 = 1, Mass Start rank 30 = 2).
Format-aware scoring is shared by standings, result summaries and dashboard
rows. World Cup standings break equal points by the count of first places,
then seconds, thirds and every later finishing place; equal race times share
the same points position. These behaviors are explicitly enabled by the new
scoring definition, preserving legacy standings behavior. World Cup leader-bib
presentation is a separately coordinated change.

`start_profile_id` and `first_season_year` are optional saved fields. Known old
calendar IDs and unselected legacy saves retain their previous behavior; old
profiles are not silently regenerated or converted to another league.

## Measured evidence

The fixed-seed production Sprint used the actual career provider, 30 entrants,
authored venue, signed physical movement, shared physiology and geometric shots.

| Measurement | Earlier production AI | Revised production AI |
|---|---:|---:|
| Winner elapsed | 597.103 s | 579.074 s |
| Typical range phase including approach/exit | 61.65–97.35 s | 47.55–55.25 s |
| Mean field input effort range | 665–767 / 1000 | 681–818 / 1000 |

The cohort and seed were held fixed for this AI-only comparison. It does not
pretend to reproduce the owner's exact manual run. The novice test then used
the new junior career cohort and a precise geometric aim controller with exact
route following, isolating race balance from driving mistakes:

| Sustained input | Misses | Novice place | Gap to strongest opponent |
|---|---:|---:|---:|
| 900 / 1000 | 0 | 5 / 30 | +36.393 s |
| 1000 / 1000 for the whole race | 1 | 14 / 30 | +65.489 s |

The all-out strategy depleted resources and increased the shooting burden
through the normal systems. The rookie can compete near the front with a clean,
efficient run, and does not receive an automatic minute-long victory despite a
miss. No result is fabricated and the test does not change a shot outcome.

These times are calibration evidence before the coordinating agent's final
shared grip refinement. Final combined-source replay and balance checks should
use the final build's source rather than treating these timestamps as immutable
expected results.

## Tests

Evidence: `docs/quality/m14/balance-review-20261012/`.

- `career-balance-baseline.trx`: production diagnostic, 1/1 passed.
- `career-balance-tuned.trx`: identical production diagnostic after explicit AI
  tuning, 1/1 passed.
- `career-starts-final.trx`: 13/13 passed — deterministic new cohorts, valid age
  selection, actual junior age-out, earned World Cup promotion, AI weekly
  adaptation, legacy save fingerprint, scoring and equal manual/AI readiness.
- `career-starts-countback-final.trx`: 12/12 passed — the same core career
  boundaries plus World Cup placement-count ties and equal race-time ranking.
- `novice-precision-balance.trx`: 2/2 complete 30-athlete production Sprints
  passed at 900 and 1000 effort with actual geometric shooting.
- The broader earlier 24-test run passed the full saved production Sprint and
  exact replay integration; its only failure was an old UI expectation for the
  default calendar ID, subsequently corrected and checked green.

Unity compilation, visual review, combined-source validation and final player
build belong to the coordinating agent. No Unity operation, Git commit or push
was performed by this subtask.

## Changed systems

Career: `CareerStartDefinition`, `CareerCircuitTransition`, `CareerFactory`,
`CareerSave`, `CareerCalendar`, `CareerSeasonEngine`, `CareerCompetition`.

Race authority/AI: `PackAiRaceRunner`, `ProductionVenueRaceFactory`,
`AiSkiPolicyDefinition`, the venue AI ski policy and the three new gameplay AI
pace/recovery/shooting strategy definitions.

Pure presentation: `FrontEndSession`, `CareerDashboardSummary`,
`RaceResultsSummary`. New data: career starts, transition cohorts, IBU scoring,
and junior/development/World Cup calendars. Tests: `CareerStartProfileTests`,
`CareerOwnerBalanceTests`, profile-aware `FrontEndSessionTests` expectations.

## Limits

This is a fixed-seed, representative calibration, not a promise that every
future season/track produces the same finishing gaps. The production season
still uses the approved single venue and supported Sprint/Pursuit/Mass Start
formats. The 30-person active cohort is replaced at a circuit change; long-term
global simultaneous multi-league population simulation is outside this review.
