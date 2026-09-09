# TASK-064 — Tactical intent report

Status: PASS, 2026-09-08. Added TacticalIntentDecision (Domain) and TacticalIntentTests.
Eight approved intents have fixed numeric identifiers, stable actor/stream IDs, decision tick,
exclusive expiry and explicit target requirements. Follow/Draft/Overtake require a distinct
athlete; untargeted intents reject stray targets. Cold diagnostic JSON has required fields
and schema 1; default/unknown/stale decisions are rejected. No physical multiplier is present.

334/334 standalone and Unity EditMode tests pass; Console zero errors/four upstream warnings.
Tests cover all eight JSON round trips, boundary ticks, wrong actor, target/lifetime/schema
errors and zero-allocation warmed construction/validation in .NET. Existing deterministic
race/shooting regressions pass unchanged. No presentation change; M5 PlayMode 6/6 retained.

This is the intent contract only: WP065 selects a utility winner, WP066 schedules decisions,
WP067 converts intent to ordinary control input. Target selection/availability is a caller
responsibility backed by WP063 observations. Next: WP065 utility evaluator.
