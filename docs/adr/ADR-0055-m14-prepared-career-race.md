# ADR-0055 — Prepared career week and live race ownership

2026-10-11. M14 WP-181 and the first race/result routing slice.

The career week previously called a synchronous headless race provider. A live
player needs to leave the menu, spend time racing, then return the same event's
authoritative result. `PrepareWeek` now produces detached source/trained JSON,
the calendar event, race seed, completion RNG state and consumed training slots.
`CompleteWeek` validates and accounts a finished field against that preparation.
The original `AdvanceWeek` composes these two operations. Training, standings,
fatigue, rewards and season transition formulas are unchanged. Preparation and
abandonment do not mutate or save the source week and do not redraw a race seed.

`CareerRaceProvider.Prepare` composes the existing production runner with real
career identities, equipment, permanent stats and shared temporary readiness
projection. `CareerPreparedRace.CollectResult` accepts only a completed field
and retains its authoritative result JSON/hash, including Sprint qualification
for the next Pursuit. Headless `Run` uses the same preparation/collection path.
`ProductionVenueHost.InitializeCareer` binds the prepared runner to the existing
manual controls and presentation instead of generating an unrelated test roster.

`FrontEndSession` owns PreRace/Race/Results transitions. The Unity controller
owns one race root, pauses the root rather than changing global time scale, and
destroys it when returning. It checks completion/failure once per frame; it does
not recalculate career data per simulation tick. A failed load or simulation
does not modify the saved week. A failed result write retains the completed
field for explicit retry. Unexpected completion failures stop further retries
and surface one diagnostic rather than throwing continuously each frame.

Profile replacement checks the expected previous JSON under an exclusive
per-profile lock, flushes a unique temporary file and atomically replaces the
profile while retaining a `.bak`. The session publishes new state only after
the durable replacement succeeds. A stale writer cannot overwrite a newer
week. Mid-race crash/resume is not implemented; the previous saved week remains.

The screen displays actual distance, laps, entrant count, stances, penalty
distance, start rules, prepared form/fatigue, equipped skis and authored snow.
It does not invent temperature/forecast data absent from the current model.
Recovery weeks are explicit actions. Full training/equipment selection and
expanded standings/results remain later M14 work. Legacy venues and ineligible
categories are visibly unavailable rather than silently migrated or simulated.

Owner requests a modern illustrated menu. An original static biathlete image
provides the main-menu background, with restrained petrol/snow colors, italic
display lettering and strong keyboard/controller focus. Information screens
use readable light cards and scrollable content. The menu has its own empty
camera (superseding ADR-0054's conditional camera), disabled during the race.
The background is hidden with the retained menu; no animated 3D menu scene or
new per-tick graphics work is introduced. Gameplay graphics remain untouched.

M14 remains in progress; this decision does not close its full gate or require
a new player build.

## 2026-10-30 — M16.2 owner correction: Sprint start draw

Sprint previously reused save-roster order, where the player is stored first.
New career Sprints now draw every eligible entrant, including the player, using
Fisher–Yates with unbiased bounded sampling. CareerSprintStartDraw accepts the
project IRandomSource; CareerRaceProvider supplies a separate SeededRandomSource
from the already-prepared race seed and fixed stream identity SPRINTDR. Drawing
does not consume or mutate the saved career RNG, shooting RNG or AI RNG. Opening,
abandoning, reloading or replaying the same prepared race cannot redraw its start.
New race seeds yield new draws, with every ordinal possible, including first.

Drawn order supplies the existing interval scheduler, bib bindings and stable-ID
player lookup. Start interval/profile, athlete capability, weather and elapsed
result timing stay unchanged. Mass Start keeps ranking seeding and Pursuit keeps
its exact qualifying Sprint order/gaps. Existing completed career results are not
rewritten; old careers need no save migration.

Replay start_order_version2 records the new draw contract. Version0/1 Sprint
journals explicitly restore the old roster order; their existing weather,
course-force and Mass Start compatibility paths remain independent. Unknown
future versions are rejected. New captures use CareerRaceProvider's current
version. Personal waiting/skip/3–2–1 already follows the selected player's actual
scheduled tick, including delayed interval starts; every skipped tick still runs.

This is an owner-authorized race-composition change within M16.2, beyond the
original board-only scope. New Sprint race/event hashes can change intentionally
because start order changes. Determinism is verified by complete repeated races,
save reload, journal playback and unchanged Pursuit qualification, rather than
silently relabeling an older fixed-order golden result as current.
