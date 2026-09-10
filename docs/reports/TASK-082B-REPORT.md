# TASK-082B — Shared Pursuit field and finish-order classification

Status: DONE — 2026-09-10. WP-082 DONE.

Connected versioned WP080 qualification and WP081 exact starts to the existing independent/shared
field composition. Exact profile and canonical roster checks run before events. The same athlete
physics, AI shooting controller, lane branches, continuous collision sweeps and penalties execute
all five laps/four PPSS bouts. Added a separate PursuitResultSnapshot ordered by exact global
finish time, with context-bound reload validation and deterministic exact ties. Sprint results
retain individual elapsed registration. Updated AI perception format, checkpoint clock and place
calculation to match Pursuit's global clock. See ADR0015.

Four new tests cover a complete real AI Sprint feeding two repeated shared AI Pursuits, twenty
accepted shots per athlete, all laps/bouts/penalty distance, physical final-lap bypass, matching
frames/events/results, reload and source start gaps. Result tests exercise faster-individual-time
but later arrival, sub-display-resolution crossing order, exact ties, invalid ranks/times/context,
duplicate/incomplete rosters and nonterminal rejection. Wrong field profile/roster emit no events.

Focused .NET field/Sprint tests14/14; result/start/Sprint regression/replay14/14. Unity MCP field2/2,
results2/2, existing Sprint perception3/3 and Sprint field3/3 (10/10 total). Full standalone463/463 in5m51s.
Evidence: artifacts/physiology/wp082b-*; compact verification is in docs/quality/wp082b.

Limits: complete finisher fields only; active withdrawal/terminal nonfinisher composition is not
implemented here. No visual/presentation change or new PlayMode run. Existing static physical
range lanes are synthetic, not production stadium art or the Mass Start lane policy. WP083 gap
strategy and WP084 robustness remain; M7 is not marked PASS.

.NET and Unity repeated Pursuit:15326ticks; result SHA256 a7a0eaea6bc0767a9e734ebf7e6c705c246f57c22f670d73bf0d6e162e584e0c.
Three WP078 canonical Sprint result hashes unchanged. Console0 errors,4 existing MCP warnings.
