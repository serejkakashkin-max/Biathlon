# ADR-0070 — M15.6 athlete creation and career development

2026-10-15. Owner-approved new milestone; M15.5 closed by owner. Implementation
starts from integrated `9867b24bfcaed10dc354267477eb9d665fe50d14`, on
`codex/m15-6-career-creator`, preserving pre-existing local work. Main is not merged.
Scope: `docs/owner/OWNER_M15_6_MASTER_PLAN_V2_RU.md`.

## Audit and decisions

The existing Career assembly owns 13 permanent integer-thousandth attributes,
weekly training, fatigue/form/mental/health, generation, standings and history.
`CareerRaceProvider` already projects every one of those 13 attributes into the
same skiing/shooting contracts for player and AI. Keep that boundary. No new OVR,
league, cosmetic or player power multiplier is allowed. GDD v0.1 lists some future
attributes; this owner directive explicitly retains the current 13-stat contract.

`FrontEndSession.CreateCareer` writes only a confirmed, validated candidate via
`FileCareerProfileStore`. Extend it with a detached `CareerCreationDraft`; no save
or simulation RNG may be consumed by preview/navigation. Existing call sites and
legacy careers stay loadable. Draft allocation derives from an immutable tier
baseline and absolute allocation, never repeated incremental preset bonuses.
Creation policy, weights, limits and developmental curves are versioned data.

OVR and five group ratings are read-only projections of permanent stats, not
readiness. The suggested owner weights sum to 100; normalize by the actual sum
so authored rescaling remains equivalent. Map the weighted 0..100000 mean to
1..99 with integer half-to-even rounding.
Do not claim format suitability until measured race evidence supports it.

Add World Cup to the current start-profile definition/calendar selection, keeping
the stable junior_novice and senior_debut IDs. Age eligibility is a documented
game abstraction, validated against current IBU age classes. A selected existing
athlete becomes PlayerId in the same world, preserving ID/state without duplication.
Licensed roster packs require season/source/rights metadata; fictional rosters are
the distributable default. Do not import real portraits/likenesses without rights.

Current saves are version 2 with a single Potential ceiling. Version 3 will add
stable-ID AppearanceProfile, creation provenance, grouped potential, GrowthProfile
and bounded seasonal development records. Migration is deterministic/idempotent,
does not draw the simulation RNG or mutate existing stats/results/replay payloads.
Legacy confidence v1 migration must run before v3 migration. Unknown versions fail.

Extend CareerAdaptation/Ageing/AiTraining rather than adding parallel growth.
Player and AI use identical functions and bounded, data-defined individual curves.
Keep the existing CareerHealth restrictions. Current circuit transition replaces
all 29 rivals; replace that destructive turnover with a bounded persistent pool
and aggregated off-field development. Active races remain at most 30 entrants.

The retained CC0 G2 rig has 3 LODs, 6 face identities, 8 skin colors and the approved
motion/rifle bindings. Appearance uses explicit resource IDs with a deterministic
legacy fallback. Studio preview reuses one model, private camera/RenderTexture and
isolated lighting; never changes RenderSettings, weather or race authority. Preview
and race apply the same profile/binding. Body edits must preserve equipment fitting
and may not change collision/physical capability. No fake controls or PNG preview.

## Delivery and validation

Initial implementation checkpoint: v3 currently persists appearance and creation
provenance. Further v3 growth fields are not implemented yet; v3 is an unreleased
M15.6 development schema. Legacy identity profiles retain the frozen v1 hash
selection, while explicit profiles store face/skin/build resource IDs. Other
appearance controls remain work in progress and are not represented by fake UI.
Initial allocation must never raise the separately generated Potential: the
current scalar may be below an athlete's starting ability, and training simply
has no positive room there until the grouped growth model is implemented.

Track A–I in M15_6_WP_STATUS. Unit/integration coverage includes allocation bypass,
selection identity, migration/RNG/history preservation, ratings, shared development,
pool transitions and replay compatibility. Native PlayMode checks cover real preview
resources, all appearance edits and preview/race parity. Final acceptance needs
the 12 owner-requested native screens, input/accessibility checks, fixed-seed
hundreds-of-careers 10–15-season evidence, deterministic race regressions and a
fullscreen Windows test build. No premature milestone closure.
