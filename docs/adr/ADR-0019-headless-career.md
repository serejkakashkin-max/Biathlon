# ADR-0019 — Headless career state and race boundary

M9 implementation decision, 2026-09-12.

Career owns deterministic weekly state, fictional identities, calendar, training/development, money/inventory, history and eligibility. It remains Unity-free and depends on Domain only. The host injects the existing seeded IRandomSource; saves retain its state and reject a mismatched restored generator. Identity allocation uses a persisted monotone ordinal, independent of random names.

Permanent attributes use integer thousandths of a stat point (0–100000). Temporary fatigue/form/morale/confidence/health use integer basis points. Money uses integer minor units. Weekly adaptation rounds explicitly at the career boundary. These are serialization units, not duplicated gameplay coefficients. All progression, economy and calendar choices are versioned data.

Player and AI use the same advancement functions. AI may select a simple training plan. Health risk is observable only in this milestone; no severe injury, compulsory absence, hidden punitive roll or permanent stat loss from injury is applied without the separately required owner-approved behavior.

The career engine requests a race through a boundary interface and imports validated stable-ID results. The integration host must use existing deterministic race authority for player/AI race outcomes; no stat-weighted hidden hit roll replaces geometric shooting. Headless multi-season qualification uses this integration, while small unit tests may provide explicit result fixtures to test accounting and transitions.

Saves include explicit schema/version, world/season/week, roster/player, RNG state, standings, inventory and history. Version migration is explicit and tested; unknown future versions fail. Failed commands/imports must not partially mutate money, standings or progression. Retired detailed states can be bounded while compact championship/history records retain stable IDs.

Fictional competition tiers and an authored calendar avoid external branding dependencies. This milestone is a headless living career, with presentation deferred to the roadmap's later UI/vertical-slice work.
