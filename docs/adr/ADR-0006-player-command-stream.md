# ADR-0006 — Fixed-tick player command stream

Status: accepted WP030 schema contract, adapters follow WP031–036.

PlayerCommand is an immutable intent value identified by schema_version/id and target tick/athlete ID.
Tick zero is initial state; commands target positive simulation ticks. Analog effort is 0..1000;
line intent -1000..1000 is desired normalized lateral position (left/right usable track extent),
not accumulated per-frame steering. WP032 rate limits the movement toward this intent.

EffortPreset is a stable numeric enum: Analog=0, Recovery=1, Race=2, Attack=3, FinalSprint=4.
Preset values belong to the later data-driven adapter; the schema stores intent, not tuning.
Tuck is held state; overtake request/cancel are mutually exclusive one-tick pulses. Technique ID
null selects automatic; a supplied stable ID is validated against the active technique catalog
at the simulation adapter. Unknown schema/version/enum or missing fields fail, never silently default.

JSON is a cold persistence/debug representation with exact Int64 tick parsing. No frame time,
Unity object/input-device reference or hidden RNG is part of a command. WP035 will own command
ordering, duplicates, stream identity, missing-tick behavior and replay. Schema id identifies the
versioned command contract; (athlete_id,target_tick) identifies one command in a stream.
