# TASK-131 — Ski debug HUD

WP131 complete. SkiDebugHud reads detached snapshots by stable athlete ID, showing km/h, delivered effort relative to sustainable power, watts, HR, anaerobic reserve, muscular fatigue, draft drag reduction/leader and physical loop/local/lateral position. Snapshot observations now include copied EffortOutput and DraftOutput. No live authority references escape; text is cached per snapshot and resets explicitly when the feed resets or selection is unavailable.

Validation: .NET snapshot/deterministic twin-race test1/1, Unity EditMode1/1, PlayMode HUD1/1 (14 discovered). The twin race retained identical movement and event fingerprint while observations were captured; telemetry matches authoritative completed-tick output. Game View screenshot inspected at tick400: docs/quality/m10/wp131-hud.png. Console0 errors/1 warning, no compilation errors.

No gameplay/balance change. This is an IMGUI diagnostic panel; physical position deliberately does not claim elapsed race ranking (WP135). Effort above100% means above sustainable power, not invalid controller input. Race scene composition follows WP136. Next: WP132 range presentation.
