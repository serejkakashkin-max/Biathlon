# TASK-046 — Shot resolution

Compose shooting lifecycle, authoritative ShotRay, wind and five-target geometry. Precompute
all fallible shot math before lifecycle mutation; only accepted triggers close targets and
emit resolution events. Repeated target hits spend ammunition but cannot close twice. Test
complete five-hit/miss bouts, blocked triggers, wind, invalid-shot atomicity, exact replay
and zero allocations. Full .NET/Unity regressions. No player/AI hidden hit randomness.
