# ADR-0019 — Headless career state and race boundary

M9 implementation decision, 2026-09-12.

Career owns deterministic weekly state, fictional identities, calendar, training/development, money/inventory, history and eligibility. It remains Unity-free and depends on Domain only. The host injects the existing seeded IRandomSource; saves retain its state and reject a mismatched restored generator. Identity allocation uses a persisted monotone ordinal, independent of random names.

Permanent attributes use integer thousandths of a stat point (0–100000). Temporary fatigue/form/morale/confidence/health use integer basis points. Money uses integer minor units. Weekly adaptation rounds explicitly at the career boundary. These are serialization units, not duplicated gameplay coefficients. All progression, economy and calendar choices are versioned data.

Player and AI use the same advancement functions. AI may select a simple training plan. Health risk is observable only in this milestone; no severe injury, compulsory absence, hidden punitive roll or permanent stat loss from injury is applied without the separately required owner-approved behavior.

The career engine requests a race through a boundary interface and imports validated stable-ID results. The integration host must use existing deterministic race authority for player/AI race outcomes; no stat-weighted hidden hit roll replaces geometric shooting. Headless multi-season qualification uses this integration, while small unit tests may provide explicit result fixtures to test accounting and transitions.

Saves include explicit schema/version, world/season/week, roster/player, RNG state, standings, inventory and history. Version migration is explicit and tested; unknown future versions fail. Failed commands/imports must not partially mutate money, standings or progression. Retired detailed states can be bounded while compact championship/history records retain stable IDs.

Fictional competition tiers and an authored calendar avoid external branding dependencies. This milestone is a headless living career, with presentation deferred to the roadmap's later UI/vertical-slice work.

WP121 integration (2026-09-12): AI assembly may reference Career to host the race provider; Career still depends only on Domain. ChampionshipRaceFactory accepts explicit entrants and personality/readiness arrays while omitted inputs retain the historical behavior/context. Career permanent thousandths are rounded to the existing race integer-stat contract with MidpointRounding.ToEven; fractional development remains saved. Temporary race form is the authored form multiplier times an authored fatigue penalty. This projection applies identically to every career entrant, including the player represented by AI control in a headless run. It does not mutate permanent attributes. Mental pressure is passed through the existing shooting pressure context.

Current-season race records optionally retain the complete authoritative result JSON and its SHA-256. Pursuit reconstructs qualification from the saved Sprint authority result; detailed payloads are discarded when the season is archived. The weekly engine clones saves/RNG checkpoints, applies shared training, imports a complete validated eligible field, accounts results once, recovers, and performs history/progression/ageing/generation transitions. A rejected provider result leaves the caller's save intact.
