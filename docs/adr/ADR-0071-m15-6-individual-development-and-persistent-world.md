# ADR-0071 — Individual development and persistent career world

M15.6 implementation decision,2026-10-15. Extends ADR-0019/0070 under the
owner's Master Plan V2. This is the next implementation contract, not evidence
that WP-F/G is already complete.

Keep13 real attributes as capability authority. Add a versioned GrowthProfile:
5 grouped ceilings/responses, individual age shift and decline response, a varied
retirement age and pre-generated stagnation-age bits. Weighted talent bands span
ordinary/national/international/elite/exceptional prospects; style is independent
of the player's initial allocation/origin/cosmetics. Current ability may exceed
a future ceiling; initialization never reduces current stats or rewards allocation
with a higher hidden ceiling. New own-athlete ceiling floors use the immutable
tier baseline, rather than the editable allocation.

Generate profiles with the project's injected IRandomSource in a separate, derived
substream keyed by career checkpoint and numeric athlete ordinal. Never draw the
race/career RNG to preview or migrate development. A saved profile is immutable
and re-used. A failed confirmation or locale/model change cannot reroll it.
Historical results, journal payloads and hashes remain byte-identical on opening.

career_growth_v1 is the single source for talent weights, responses,13 stat/group/
curve mappings, separate physical/recovery/technical/shooting/mental age curves,
learning rates, repeated-session saturation and history limits. Extend existing
CareerAdaptation/Ageing/AiTraining/TrainingWeek. Readiness/fatigue is applied once;
repetition saturation counts repeated session kinds, not fatigue a second time.
Both player and AI use these same functions. CareerHealth restrictions remain.
Legacy direct APIs retain their documented scalar mode when no growth definition
is supplied, keeping their pinned fixtures meaningful.

Initialize missing development lazily on a detached candidate at creation or weekly
preparation. Continuing an old disk profile must not create unpersisted metadata
that breaks FileCareerProfileStore's compare-and-swap boundary. FromJson remains
IO-free. Preserve existing scalar Potential for compatibility; grouped profiles
become the new future-adaptation authority when enabled. No fabricated past OVR
records: migration starts a baseline at the actual current season/week.

Record bounded baseline/end-of-season13-stat snapshots with observed OVR/age and
rating policy ID. Retain lifetime peak/age/season separately when trimming history.
These are observations, never power multipliers or instant win bonuses. Seal the
development record before the annual age transition; next year's current delta
includes that transition's decline. Expose useful profile/gain state to diagnostics.

Keep active fields at30. A bounded persistent off-field pool retains known athletes
across circuit transitions; the creation catalog's other cohorts enter that pool.
Aggregate off-field training/ageing with the same functions instead of simulating
every off-field race. Retirement/replacement uses individual profiles. Preserve
IDs, progress and compact retired/history records; do not replace the whole world
on promotion. Competition tier grants no hidden power. New cohorts/prospects and
transitions are deterministic and data-defined.

WP-I must measure hundreds of10–15-season development trajectories and representative
authoritative competition runs separately, rather than presenting aggregate races
as real race simulations. Tune from distributions, peak ages, stagnation, elite
fractions and competing specializations. Old save/replay, shared player/AI cost,
failed-command atomicity and active-field budgets remain acceptance gates.
