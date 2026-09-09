# TASK-064 — Tactical intent model

Implement the eight approved intents as a validated value contract with actor, optional target,
decision tick and exclusive expiry. Follow/Draft/Overtake require another athlete; other intents
are untargeted. Preserve stable schema/stream ID in cold JSON diagnostic serialization. Reject
unknown intents, missing/foreign targets, inverted lifetimes and malformed versions. No effort,
power or speed modifiers belong to this contract. Utility selection is WP065; control is WP067.
