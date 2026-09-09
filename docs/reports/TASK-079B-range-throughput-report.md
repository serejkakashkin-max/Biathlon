# TASK-079B — Physical range throughput

2026-09-09. M6 technical remediation after WP079. Final test status: PROJECT_STATE and verification.json.

Implemented optional versioned physical range alternatives, exact-route mapping, actor-specific
receipt projection, explicit branch connections and two generated30-lane synthetic stadium
fixtures. See ADR0014 for the topology and deliberately bounded schema. The shared pack coordinator
still validates every continuous trajectory and seam tail. Base skiing/physiology, geometric
shooting, utility and difficulty are unchanged. No-lane behavior/context remains the old baseline.

Reproduce fixtures with scripts/New-SyntheticRangeLanes.ps1 (default flat); pass
-SourceTrackId synthetic_rolling_v1 -TrackId synthetic_stadium_rolling_v1 for rolling. All8 generated
JSON files reproduce byte-for-byte. Run BalanceLab ai-stadium with the same arguments as WP079 ai.
The first attempt with the legacy24m-wide junction region is retained locally at
artifacts/physiology/ai-stadium-wp079b. The final explicit900m conflict region removes the unused
920m logical-rejoin port; actual body overlap/tail checks are unchanged. No old fixture was relaxed.

## Final four-field evidence

All cases:30 athletes, seed123, identical capability, alternating profiles, interval2s,20Hz,
3000m plus actual150m penalties, two complete bouts and10 accepted shots per athlete.

| Case | Field ticks | Median / p95 elapsed seconds | Stationary on-course ticks | Share of all athlete ticks | Sprint starts | Step p95 ms |
|---|---:|---:|---:|---:|---:|---:|
| Flat expert | 10288 | 425.35 / 458.91 | 7287 | 2.65% | 30 | 0.730 |
| Flat developing | 16525 | 704.75 / 791.03 | 34716 | 8.08% | 30 | 0.977 |
| Rolling expert | 11992 | 518.69 / 570.31 | 13752 | 4.13% | 30 | 0.751 |
| Rolling developing | 17178 | 717.94 / 813.53 | 32772 | 7.23% | 30 | 0.938 |

Compared with WP079 flat expert, stationary ticks fall99.13% (834788→7287); the median finish
falls1986.24→425.35s. This is removal of artificial queueing, not increased physical capability.
Headless step throughput is90.6–93.6× realtime on this run, excluding setup/reporting; total Step
allocation is5.50–5.73MB including bout/controller initialization. Zero steady observer allocation
remains separately tested. No Unity rendering/performance claim is made.

The twice-repeated30-athlete flat expert test reports10288ticks, maximum skiing stall161ticks
(8.05s), peak simultaneous shooting9, zero precision fallbacks, no endpoint body overlap and
one final-sprint start per athlete. It retains the120-second individual stall limit and requires
at least90% reduction from the fixed WP079 stopped-tick baseline. Every athlete completes two
bouts and exact penalty-inclusive travel; full result/context/AI-and-motion trace/events repeat.
Trace FNV64:780be17e3d4dee9c. Result SHA256:
6f32f9845c13629d6ad004160bc9731d67b2cc0925d9c82af64039edc1166085.
Unity MCP confirms the same result/trace and all counts. Final standalone450/450 and focused Unity9/9 pass; legacy WP078 hashes remain unchanged. M6 PASS.

## Focused validation

- Contract/serialization/metadata rejection, explicit boundary ownership and final-lap bypass.
- Projection conserves exact travel and rejects receipts crossing an unsplit branch boundary.
- Missing directed connections or insufficient unique lanes reject before any event/tick.
- Solo stadium motion and physiology equal the original runner on every tick, including exact finish.
- Two athletes shoot simultaneously in distinct physical lanes; full frames/events replay.
- Thirty-athlete repeated race: live physical bodies, bounded waiting, exact results and event replay.
- Legacy physical projection tests remain unchanged. Full .NET suite includes all WP078 soak goldens.

The earlier work-in-progress test failure caught an important distinction: a route section's
HasRange flag does not mean that lap requires a bout. The final map uses the exact versioned
CompetitionRouteRequirements and preserves the final-lap bypass. No failing test was disabled.

Limitations: static unique lane assignments, one constant matched-metadata range segment, synthetic
headless topology; production3D venue embedding and Mass Start dynamic lane rules remain later work.
The four fixed-seed fields are not a population balance calibration. M6 gate decision follows final
regression and Unity evidence; broader balance remains M8.
