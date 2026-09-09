# TASK-060 — Complete race replay

PASS, 2026-09-08. SprintFieldSession exposes an exact context covering model definitions,
profile, track/route, conditions, timing, roster, start origin, seeds and synthetic lane/path
policy. SprintRaceCommandTape stores complete ski/aim/trigger/hold/wind/pressure intents as
per-athlete contiguous runs. JSON schema 1 validates ordering, coverage, identities, command
domains and canonical merged runs. Independent replay cursors drive the same field session.

322/322 standalone and Unity EditMode pass; Console zero errors/four upstream warnings.
Final evidence: artifacts/physiology/wp060-final-{standalone.trx,editmode.json,console.json}.
Complete changing-intent replay matches direct simulation frames, events and final result
JSON. Concurrent cursors, malformed coverage, changed seed/context, exhausted capacity and
unchanged-intent zero-GC recording/replay pass. Existing deterministic traces are unchanged.

The tape is replay input, not authenticated storage; field/model compatibility is enforced
before replay. Cold JSON/context encoding and bout/result initialization allocate; active
unchanged command runs do not. File/CLI artifact export follows WP061's regression harness.
Next: WP061 Sprint fixtures, then WP062 60-athlete benchmark/M5 gate. No owner gate or commit.
