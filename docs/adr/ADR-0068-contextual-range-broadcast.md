# ADR-0068 — live shooting broadcast

## Current decision · M16.2 · 2026-10-30

Supersedes the original eight-row/player-pin/page policy below. Reuse the same
`RangeBroadcastModel`, `GameplayRangeBroadcast`, JSON resource and accepted feed.
Presentation only: no ShootingSession, state machine, handling, physics, AI,
mat assignment, gameplay timer, result, career/save or authoritative replay edits.

The bound observer allocates fixed per-athlete/per-visit arrays once. Current
production configuration admits 30 athletes with a20-row target at1920x1080.
Sort by observed accepted arrival tick, then actual mat and stable ID for ties.
Already admitted athletes keep their rail priority; shot updates never reorder
them. Neither player nor recent misses get a reserved slot. Overflow stays in the
same data model; no five-second pagination. Active rows cannot expire. Finish on
the fifth accepted shot or the accepted completed-bout transition, including final
shot and session release in the same snapshot. Hold3accepted seconds, fade0.4s,
then admit the next queued entry. A result that finishes while queued receives its
hold starting at first admission, so a hidden result is not silently discarded.

Every configured bout has independent rows; changing the contextual visit never
reuses another visit's outcomes. Completed data remains available after a row
expires. Observer/composition uses tick/frequency only; no wall-clock timers.
Duplicates are idempotent. Rewind, changed clock or nonadjacent snapshot resets
transient history: rebase current accepted data, no miss/hit/completion pulse,
no resurrected historical finished row. Unknown past arrival order uses first
observed tick and mat; it is not claimed as recovered historical arrival time.

`ClosedTargetsMask` identifies physical targets, **not shot ordinals**. Record
accepted shot-count and closed-count deltas into five chronological marks.
This also correctly handles reverse target selection, a miss and a final-shot
release. After a seek across multiple shots, an aggregate mask cannot identify
mixed chronological outcomes. Use an outlined dash for unavailable order and
retain the exact accepted score; never invent a hit/miss sequence. All-hit/all-miss
prefixes are inferable without guessing. Ordinary sequential feed has exact marks.

Dark Frostline rails include actual existing athlete bib presentation number,
separate mat ordinal, career flag/name, five shot symbols, score and active/done
symbol. Read the existing bib binding rather than generating a new race ordinal.
Outlined pending, filled hit and crossed red miss differ by shape. A thin player
rule is cosmetic only. Enter/hit/miss/complete/fade animation follows accepted
ticks with bounded interpolation; reduced motion removes scale/slide/pulsing.
The board is input-transparent. Accepted publications refresh it immediately;
general telemetry retains its existing lower refresh cadence. Fixed elements,
cached numbers/scores/identities and zero-allocation model updates avoid churn.

Use actual screen/target pixel and existing HUD bounds to reduce row capacity on
900p/720p and with large text. The existing readiness card sits beside bottom
indicators while the live board is open, preventing overlap; its original layout
returns when coverage closes. TAB replaces the board outside shooting as before.
The original250m entry/exit route context remains, including penalty transit.

Current authority supports Sprint, Pursuit and MassStart; all three use this model.
Individual and Relay are not implemented race formats in this project, so no
fake UI-only races or new shooting simulation is introduced for those names.
The observer itself has no start/penalty-format branch, ready for future accepted
snapshot adapters. Extra relay cartridges require an explicit future contract.

Verification/evidence and exact limitations: `docs/quality/m16-2/README.md`.
Official context references consulted (no copied IBU graphics/logos):
https://www.biathlonworld.com/fr/discover-biathlon/how-it-works/rules-overview
https://www.ebu.ch/news/2024/02/ebu-members-to-deliver-extensive-live-coverage-of-ibu-biathlon-world-championships-2024

## Original decision · 2026-10-13 · historical, superseded

2026-10-13. Owner-requested follow-up to M14 WP-182/183; uses the Frostline HUD.

The panel observes the accepted race feed and opens from 250 route metres before
the authored range entry to 250 metres beyond the authored exit for each shooting
visit. These are canonical route distances, crossing lap seams; they are not a
Euclidean radius around the stadium. Penalty transit holds canonical progress, so
the panel remains available throughout that transit. Prestart/finished views hide it.

Snapshot adds read-only canonical route distance and the last completed target mask.
No shot outcomes, lane assignment, physics, timers, rules, AI or saves are changed.
The observer keeps a fixed row per athlete. Five circles show actual target closures;
misses equal accepted shots minus closed targets and appear as separate small red
dots to the left. An accepted increase in misses highlights the row for one second,
with two gentle pulses per second; reduced-motion mode uses steady highlighting.
Duplicate observations, rewinds and large tick gaps rebase without false flashes.
Completed series remain visible after the athlete releases the mat.

Eight retained rows fit at the right edge above the pulse/readiness cards. The
current athlete and fresh misses are pinned; remaining rows automatically page
every five accepted seconds. A count/page footer makes this explicit. Active
shooters precede finished series; displayed mat ordinals are distinct from race
classification. Flags/names use the existing career roster. No new rank is invented.

The definition has schema/id and contains distances, timing, row and width budgets.
The Unity-free model owns fixed arrays; insertion sorting occurs at the existing
HUD cadence. There are no additional snapshots, cameras, render passes, raycasts
or per-athlete MonoBehaviours. Elements ignore input; TAB temporarily covers the
panel with the split table. UI scope is presentation only, not a new milestone.

Verification: route-boundary and final-series/seek tests, actual thirty-athlete career
Mass Start adapter run, flags, miss flash, automatic pagination, retained results,
read-only authority and native RU/EN large-text bounds. See the delivery report.
