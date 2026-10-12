# M14 — work-package status

Updated 2026-10-12. M13 closed by owner acceptance; M14 gate remains open.
Statuses refer to implemented and verified product behavior, not merely existing core code.

| WP | Status | Delivered / remaining |
|---|---|---|
| 179 | COMPLETE | UI Toolkit design tokens, aspect ratio scaling, RU/EN, visible keyboard/controller focus. |
| 180 | COMPLETE | New profile, continue, settings, quit; independent atomic profile saves. |
| 181 | IN PROGRESS | Prepared athlete, venue, snow, skis and rules delivered; broader weather presentation remains. |
| 182 | COMPLETE | Compact race HUD; course remains readable, full help on F1. |
| 183 | COMPLETE | Targets, bolt, breathing, pulse, wind, penalties; target states have non-color symbols. |
| 184 | COMPLETE | Accepted race clock, checkpoint rank/gap and finish timing connected to HUD. |
| 185 | COMPLETE | Full race protocol, per-bout shooting, penalties, personal splits and season points/rank change. |
| 186 | COMPLETE | Actual 16-event calendar, readiness, next race, season rank/points, wins/podiums and saved event outcomes. |
| 187 | PENDING | Training selection and cost/benefit forecast. |
| 188 | PENDING | Player equipment selection/tradeoffs. |
| 189 | PENDING | Full season standings and career history screens. |
| 190 | PENDING | Actual rivalry presentation. |
| 191 | PENDING | Player replay persistence/version compatibility. |
| 192 | PENDING | Replay playback and camera controls. |
| 193 | PENDING | Event-driven highlights. |
| 194 | PENDING | Broadcast camera direction. |
| 195 | PENDING | TV graphics package. |
| 196 | IN PROGRESS | RU/EN, 100/115/130% text and symbol feedback delivered; full accessibility review remains. |
| 197 | IN PROGRESS | Essential menu/profile/pre-race/HUD/results navigation covered; expand with remaining pages. |

Latest delivery: [WP-186 report](reports/TASK_M14_DASHBOARD_20261012.md),
[WP-185 report](reports/TASK_M14_RESULTS_20261012.md).
Earlier delivery: [menu/pre-race](reports/TASK_M14_PRE_RACE_20261011.md),
[HUD](reports/TASK_M14_GAMEPLAY_HUD_20261012.md),
[compact HUD](reports/TASK_M14_COMPACT_HUD_20261012.md).

The recorded Pursuit directed-passage/startContinuation composition failure must
be resolved before claiming the complete production Sprint → Pursuit loop.
No new player build requested for this M14 slice.

7 of 19 WPs complete, 3 partly delivered/in progress, 9 pending.
Next implementation: WP-187 training UI. Latest changes are published to
the GitHub branch `codex/wp-078-ai-pack`; this tracker and the append-only state
are included in the same commits as the delivered code/evidence.
