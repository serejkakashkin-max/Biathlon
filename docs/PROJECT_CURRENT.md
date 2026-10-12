# BIATHLON — latest operational state

2026-10-12. G1/G2 approved. M13 closed by owner acceptance.
M14: **19/19 WPs implemented and verified; awaiting owner test/acceptance**.

Current player build: `Builds/M14Revision20261012/START_M14_TEST.cmd` (build succeeded; standalone startup/render passed).
Previous accepted M13 build remains `Builds/WoodlandM13BotRecovery-20261011/START_WOODLAND_REVIEW.cmd`.

Woodland keeps maximum 30 athletes, High default, separate start/finish, manual steering,
attack/recovery, player-selected tuck, IBU mat allocation and automatic range transit.
Owner-accepted M13 performance fixes and normal athlete shadows remain.

M14 delivers the modern front end, durable career profiles, pre-race facts, compact HUD,
full results, calendar/dashboard, training, equipment, standings/history/rivalries,
compressed deterministic replays, event highlights, broadcast cameras and optional TV graphics.
Accessibility: RU/EN, text scaling, contrast, non-color target feedback and assist explanations.
Keyboard/gamepad navigation and career/race/replay flows have native coverage.

A complete Sprint30 replay produces the identical authority protocol/hash at 31602 ticks.
The full saved Sprint-to-Pursuit career transition completes with 30 athletes.
Headless production career races now bind the same manual movement as the playable host;
shared steering/braking changed explicitly; graphics quality is unchanged.

New completed career races create compatible replay files. Old races are not reconstructed.
Training drafts are session-local until the training week is committed.

See [M14 WP status](M14_WP_STATUS.md), [completion report](reports/TASK_M14_COMPLETION_20261012.md),
[replay/TV decision](adr/ADR-0058-m14-recorded-races-and-tv.md).
`docs/PROJECT_STATE.md` retains the full append-only history from Foundation onward.
M14 is not declared owner-accepted; next work is owner testing, then M15 or requested corrections.

Latest owner-review code: `4d522bd` on `codex/wp-078-ai-pack`; qualification and append-only history are recorded in PROJECT_STATE.

Owner review revision 2026-10-12: snow/ink/teal card UI and paged protocols; six timing lines and Tab latest-split standings; sharper A/D, S physical brake and paid low-speed pivot; true HR guidance before shooting; live post-finish athlete/overhead/side cameras and bounded full-field completion. See [review report](reports/TASK_M14_OWNER_REVIEW_20261012.md). M14 remains pending owner acceptance.
