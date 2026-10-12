# ADR-0057 — Saved result observations and results presentation

2026-10-12. M14 WP-185.

The authoritative result already contains classification clocks and a SHA-256,
but not target closures or checkpoint observations. After a complete field,
`CareerPreparedRace.CollectResult` now copies per-athlete closed target masks,
authored stances and checkpoint elapsed/global clocks into optional versioned
`CareerResultDetails`. IDs remain stable; Career still depends only on Domain.
The race result hash and authority payload retain their existing meaning.
Collection is read-only, repeatable and runs at completion, never each tick.

The optional data member preserves schema-1 legacy saves: absent details remain
absent on round-trip, and the UI does not fabricate missing shot/split history.
Validation rejects unsupported detail versions, invalid masks/stances, clocks
out of order or after finish, mismatched athlete IDs and inconsistent penalties.
Arrays are copied and exposed by indexed getters. Details live within bounded
current-season results; existing season archival discards those records.

`RaceResultsSummary` projects standings on a detached copy of the pre-race save
using Career's existing accounting. It retains the completed season's points
even when completion rolls the career to another season. Initial zero-race
standings have no invented prior ranking. Publication occurs only after the
atomic profile replacement succeeds; save failure retains the race for retry.

UI Toolkit results show race classification, elapsed times, classification gaps,
misses by bout, penalties, saved personal checkpoint/sector times and points/rank
impact. Pursuit elapsed duration and classification gap use their distinct saved
clocks. Sector time includes range/penalty time between checkpoints and is labelled
accordingly. A two-line protocol row keeps text readable at 4:3 and 130% size.
Shooting uses symbols as well as color. Gameplay authority, scoring, balance,
render quality and input law are unchanged.

M14 remains in progress. This delivery neither closes its gate nor produces a build.
