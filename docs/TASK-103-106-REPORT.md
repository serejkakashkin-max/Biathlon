# TASK-103–106 — Career persistence, creation and calendar

WP103–106 complete. `Biathlon.Career` now owns Unity-free versioned contracts, seeded creation and calendar definitions; ADR-0019 records the boundaries before implementation.

- TASK103: schema1/save2 stores stable career/world/season/player/athlete IDs, week, RNG state, allocation ordinal, permanent stats, temporary readiness and inventory/counters. Explicit save1 migration derives absent confidence from morale. Future versions, missing current confidence, duplicate IDs, allocator collisions, invalid states and mismatched restore RNG/context fail. Zero confidence round-trips correctly.
- TASK104: player name, supported nation and appearance hook; authored initial age/skill/potential ranges. Invalid input is rejected before consuming RNG.
- TASK105: deterministic fictional 30-athlete roster with independent stable ordinal IDs, names, national representation, stats, age, potential and existing personality IDs. Names need not be globally unique; IDs are. AI appearance remains its own default.
- TASK106: authored 52-week calendar with six Sprint/Pursuit/Mass Start events, validated earlier matching Sprint sources, ordinary three-slot training weeks and one-slot race weeks. Rest remains possible by using fewer slots. Initial events are open regional fixtures; tier eligibility is explicit for later progression.

Validation: .NET4/4 and Unity MCP4/4, exact canonical save SHA-256 `a9a4cc348c15a7d5f38f9a592bb7f56709e9020db1373c92825838b9c46d8dc8`; final focused run also includes the M8 lock regression. No existing locked M8 input or race authority was changed. New career data is provisional and will be locked at WP124.

Limits: weekly advancement, standings accounting and real race integration are subsequent packages, not claimed here. The save is an extensible contract; later additive collections default absent/empty without reinterpreting existing fields. Next: TASK107 season scoring/history, then training/development.
