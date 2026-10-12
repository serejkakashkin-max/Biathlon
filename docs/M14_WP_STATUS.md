# M14 — work-package status

Updated 2026-10-12. M13 is closed by owner acceptance. All 19 M14 WPs are implemented and verified; M14 awaits owner build testing and acceptance.

| WP | Status | Delivered |
|---|---|---|
| 179 | COMPLETE | UI Toolkit tokens, compact layouts, resolution scaling, RU/EN and visible keyboard/controller focus. |
| 180 | COMPLETE | New career, continue, settings, quit; independent atomic profiles. |
| 181 | COMPLETE | Actual venue/weather, prepared athlete, snow, selected skis and event rules. |
| 182 | COMPLETE | Compact race HUD with clock, lap, pace, pulse/resources; F1 help and F3 diagnostics. |
| 183 | COMPLETE | Targets, bolt, breath, wind, pulse and penalties; shape and contrast feedback. |
| 184 | COMPLETE | Accepted fixed-tick timing, actual checkpoint ranks/gaps and finish timing. |
| 185 | COMPLETE | Full protocol, per-bout shooting, penalties, personal splits and season points/rank impact. |
| 186 | COMPLETE | Actual calendar, readiness, next event, ranking and saved outcomes. |
| 187 | COMPLETE | Real weekly training budget, qualitative costs/benefits and readiness forecast; atomic commit. |
| 188 | COMPLETE | Actual ski catalog, snow tradeoffs, atomic purchasing and selection. |
| 189 | COMPLETE | Season table, saved race protocols and completed-season history. |
| 190 | COMPLETE | Tracked shared races/podiums, close finishes and standings battles. |
| 191 | COMPLETE | Versioned compressed command journals, saved pre-race snapshot, events and compatibility/result hashes. |
| 192 | COMPLETE | AI-free replay, pause, 1/2/4 speed, bounded seeking, athlete/camera selection. |
| 193 | COMPLETE | Actual lead changes, overtakes, final range, key misses and close finishes; Sprint uses registered timing. |
| 194 | COMPLETE | Event-based broadcast shots, readable shot holds and manual camera override. |
| 195 | COMPLETE | Optional live F2 TV standings; replay timing/shooting boards, gaps, penalties and finish feedback. |
| 196 | COMPLETE | RU/EN, 100/115/130% text, high contrast, target symbols and assist explanations. |
| 197 | COMPLETE | Native keyboard/gamepad menu navigation, career/race pause flows and replay seek/focus regression. |

19 of 19 complete. No remaining M14 implementation WP. Owner acceptance is pending.
Test build: `Builds/M14Revision20261012/START_M14_TEST.cmd` (build succeeded; standalone startup/render passed).

Latest evidence: [M14 completion report](reports/TASK_M14_COMPLETION_20261012.md),
[recording/TV decision](adr/ADR-0058-m14-recorded-races-and-tv.md),
`docs/quality/m14/completion-20261012/`.

Full 30-athlete Sprint records/replays to the identical authoritative career protocol.
Saved Sprint-to-Pursuit career transition completes with 30 athletes; the obsolete separate-start/directed-passage exclusion is repaired.
Replays are created for newly completed career races and require compatible race definitions.
Training drafts remain session-local until the week is committed.
The next milestone begins only after owner review of M14.

2026-10-12 owner-review corrections: WP-179/181/185-189 redesigned as cards and paged protocols; WP-184 six timing lines + Tab latest-line standings; WP-182 pulse guidance; WP-194/195 live post-finish cameras and accelerated authoritative completion; WP-196/197 responsive accessible navigation retained. Shared steering/braking/paid low-speed recovery improved. All corrections implemented and verified; owner acceptance pending. Evidence: [review report](reports/TASK_M14_OWNER_REVIEW_20261012.md), `docs/quality/m14/review-20261012/`.
