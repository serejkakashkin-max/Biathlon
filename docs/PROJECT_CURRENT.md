# BIATHLON — latest operational state

2026-10-13. G1/G2 approved. M13 closed by owner acceptance.
M14: **19/19 WPs implemented and verified; awaiting owner test/acceptance**.

Current player build: `Builds/M14_UI_WOW_Review/START_M14_TEST.cmd` (FROSTLINE; 0 build errors/warnings; ordinary startup and isolated Windows menu/race/shooting review passed).
Previous accepted M13 build remains `Builds/WoodlandM13BotRecovery-20261011/START_WOODLAND_REVIEW.cmd`.

Woodland keeps maximum 30 athletes, High default, separate start/finish, manual steering,
attack/recovery, player-selected tuck, IBU mat allocation and automatic range transit.
Owner-accepted M13 performance fixes and normal athlete shadows remain.

M14 delivers the modern front end, durable career profiles, pre-race facts, compact HUD,
full results, calendar/dashboard, training, equipment, standings/history/rivalries,
compressed deterministic replays, event highlights, broadcast cameras and optional TV graphics.
Accessibility: RU/EN, text scaling, contrast, non-color target feedback and assist explanations.
Keyboard/gamepad navigation and career/race/replay flows have native coverage.

A complete final Sprint30 replay produces the identical authority protocol/hash at 28941 ticks.
The full saved Sprint-to-Pursuit career transition completes with 30 athletes.
Headless production career races now bind the same manual movement as the playable host;
shared steering/braking changed explicitly; graphics quality is unchanged.

New completed career races create compatible replay files. Old races are not reconstructed.
Training drafts are session-local until the training week is committed.

See [M14 WP status](M14_WP_STATUS.md), [completion report](reports/TASK_M14_COMPLETION_20261012.md),
[replay/TV decision](adr/ADR-0058-m14-recorded-races-and-tv.md).
`docs/PROJECT_STATE.md` retains the full append-only history from Foundation onward.
M14 is not declared owner-accepted; next work is owner testing, then M15 or requested corrections.

GitHub publication receipts on `codex/wp-078-ai-pack` are recorded append-only in PROJECT_STATE.

Owner review revision 2026-10-12: snow/ink/teal card UI and paged protocols; six timing lines and Tab latest-split standings; sharper A/D, S physical brake and paid low-speed pivot; true HR guidance before shooting; live post-finish athlete/overhead/side cameras and bounded full-field completion. See [review report](reports/TASK_M14_OWNER_REVIEW_20261012.md). M14 remains pending owner acceptance.

Latest owner correction: actual corner grip fixed; retained W/S pace dial and initial stop;
3-second start preparation; junior/senior age selection and career circuit progression;
shared player/AI form, competitive rival tactics; three rectangular timing gates and
14m finish; World Cup leader bibs/countback; visible selected ski colors and animated HR.
90 focused core checks, 31 native PlayMode and 2 native EditMode checks passed.
Eight older experimental planner failures were baseline-reproduced, not concealed.
See [latest report](reports/TASK_M14_BALANCE_REVIEW_20261012.md). Test new starts with
a new career. M14 remains open for owner acceptance.

Latest correction: WP-184 reuses TIMING at the actual loop-end split; WP-182 adds
a shared short paid burst, compact reserve/time overlay and retained pace on
release. Fresh AI no longer brakes for distant moving bodies or sheds speed
through excessive passive-inverse deflection. Actual first-split maximum gaps
are 4.669–7.956 s across three thirty-athlete cohorts. Full Mass Start30 and
deterministic career Sprint30 replay pass; graphics quality is unchanged.
78 final core passes plus 2 known experimental failures are reported explicitly;
21 native PlayMode and 10 EditMode passes. See
[current build/report](reports/TASK_M14_BURST_PACK_REVIEW_20261012.md).

Latest visual revision: generated ski catalog art and shared Woodland venue preview;
flagged, aligned TAB split rows plus final/season tables; compact animated pulse
trace. WP-181/182/184/185/188/189/196/197 corrections complete. 16 native PlayMode
and 7 EditMode checks pass; RU/EN normal/large screenshots inspected. Gameplay
and balance unchanged. [Visual build report](reports/TASK_M14_VISUAL_REVIEW_20261012.md).
M14 stays OPEN for owner acceptance.

2026-10-12 national-kit/bib correction: WP-181/188 bind the six roster countries
to consistent shared suit colors; WP-189 verifies IBU 2026 yellow/red/combined
classifications and displays leader badges; WP-192 restores saved appearance in
replays and seeking. 13 EditMode + 11 PlayMode focused checks pass; Windows build
and standalone startup pass. Graphics quality, authority and balance unchanged.
[Current test build/report](reports/TASK_M14_NATION_BIBS_20261012.md). M14 remains OPEN.

2026-10-12 weather correction complete: WP-181/182 bind shared career forecasts, physical snow, shooting wind and visible material variants; WP-192 records conditions and preserves legacy replay weather. 31 core plus 28 career/menu checks pass (7 overlapping), 6 Unity EditMode and 10 PlayMode checks pass. Full snowfall Sprint repeats exactly at 29,546 ticks. Windows build/startup pass; textures and High quality preserved. [Current weather build/report](reports/TASK_M14_WEATHER_20261012.md). M14 remains OPEN.

2026-10-12 continued-career Mass Start correction: WP-181 ranked grid/bibs/first mats and pre-race start number; WP-189 circuit-specific leaders; WP-192 versioned start order and icy-weather solver during replay/seek COMPLETE. Read-only current development save: rank 7 -> start number 7, one yellow leader, no first-Mass red. 56 headless, 8 Unity EditMode and 3 PlayMode checks pass. Complete icy Mass30 repeats at 28,344 ticks. Build/startup pass with 0 errors/warnings. [Current seeding report/build](reports/TASK_M14_MASS_SEEDING_20261012.md). M14 remains OPEN.

2026-10-13 personal-start correction COMPLETE: WP-182/191/192/194/196/197 now provide a personal 3–2–1, visible waiting player, bounded button/Enter skip to the final three seconds, full command recording and pause/input checks. .NET19 + Unity Edit9 / Play7 PASS. New Win64 test build has0 errors/warnings; standalone startup/render PASS. [Report](reports/TASK_M14_PLAYER_START_20261013.md). M14 remains OPEN pending owner testing.

## 2026-10-13 — M14 FROSTLINE visual revision delivered for owner review

The owner brief is implemented in `codex/m14-ui-wow`; M14 remains OPEN for owner
artistic/gameplay acceptance. Build code: `88e433d2`. Launch:
`Builds/M14_UI_WOW_Review/START_M14_TEST.cmd`.

WP-179/180/181/182/183/184/185/186/187/188/189/190/192/195/196/197 visual revision
COMPLETE. WP-191/193/194 authority/highlights/cameras remain delivered and preserved.
No implementation WP for this request remains in progress. Next step: owner tests the
new build and accepts M14 or requests further corrections; no automatic main merge.

Three native visual concepts, shared Frostline design/fonts, career/shop/training/
result/broadcast/replay presentation, bounded transitions and Reduce motion are
integrated. 44 focused core tests,28 distinct selected PlayMode checks and1 native
EditMode panel-template check PASS. 624 frontend plus192 HUD matrix cases have zero
recorded layout findings. Existing HUD coverage budgets remain mandatory and pass.

Final Win64 build:0 errors /0 build warnings. Ordinary startup PASS. Isolated Windows
review PASS:12 native captures, actual venue, personal start and actual shooting;
14 navigation actions, zero collected errors, unchanged owner-profile hashes.
108 frames exported to the native7.2-second MP4. Report:
`docs/reports/TASK_M14_FROSTLINE_REVIEW_20261013.md`; evidence/design under
`docs/quality/m14-wow/` and `docs/ui/m14-wow/`.

The UI CPU comparison is narrow, not a full-race FPS assertion. Mono allocation
counter is unsupported (positive control failed), so zero-GC is not claimed.
Offscreen capture retains known URP DOF/Panini stripping warnings; no graphics
quality workaround was applied. Secondary pages share components; hidden scrolling
remains an overflow/focus fallback. Historical before-field captures retain the
identified stencil-capture artifact. Gameplay/balance/save/replay authority source
was unchanged by the visual branch. Unrelated pre-existing local edits are preserved
and excluded from commits. Owner visual quality approval remains outstanding.
## 2026-10-13 — M15 audio implementation complete; Windows build ready

Owner-authorized M15 WP-198–205 COMPLETE (8/8). No implementation WP remains in progress. Source commit 61159bf5, branch codex/m15-audio. Test launcher: Builds/M15_Audio_Review/START_M15_TEST.cmd. Original 24-clip soundbank, accepted event mapping, actual shooting heartbeat, ski/pole/body/rifle/weather/crowd/UI layers, spatial opponents, bounded 22-source mixer, replay/skip suppression and RU/EN audio settings delivered. Gameplay authority, balance, graphics quality and career save schema preserved; only a read-only heartbeat observation was added to the simulation presentation snapshot.

Validation: 23 selected native PlayMode + 3 EditMode + 12 core tests PASS (38 total). Full 30-athlete deterministic replay: 20,109 ticks; field event hash 9212986234871512072. Unity Console clean. Win64 build 0 errors / 0 warnings. Standalone startup and native audio audition exit 0; mixer recording peak 0.139598, RMS 0.008409, PCM memory 16,925,760 bytes. Native actual venue/personal-start/range review passes with 12 captures, 14 navigation actions, no errors and owner career hashes preserved. Known offscreen URP warnings remain baseline. Performance checks are bounded-pool/narrow CPU checks, not a full-game FPS or zero-allocation claim.

Report: docs/reports/TASK_M15_AUDIO_20261013.md. Evidence: docs/quality/m15/. WP tracker: docs/M15_WP_STATUS.md. The soundbank is stylized original sound design; listening quality requires owner playtest. Next action: owner tests the M15 build. No owner aesthetic approval is inferred. M16 is next in plan but not started. Unrelated pre-existing local edits preserved and excluded from these commits.

## 2026-10-13 — M15 owner corrections: menu music, ski Foley, Mass Start countdown

WP-199/204/205 follow-up and WP-182 start regression COMPLETE. Source 166a564a on codex/m15-audio. Latest test launcher: Builds/M15_Audio_Refinement_Review/START_M15_TEST.cmd. Original 32-second Snowlight menu composition replaces the drone; three pole samples now have soft snow compression instead of a gun-like attack. Accepted ski-only ticks emit zero rifle/bolt/target cues. Reproduced Mass Start loading frame consumed countdown (expected3, actual0); local hold now excludes oversized loading/render frames using the existing authored frame budget. Visible3–2–1 and unchanged authority tick0/first tick1 are tested. Scheduled starts, pursuit gaps, gameplay balance and graphical quality remain unchanged.

30 distinct selected tests PASS (18 PlayMode,3 EditMode,9 core); native ordinary/skip command journal comparison PASS. An initial UI fixture assumed one-second render frames; updated to ordinary frames and rerun successfully without removing assertions. Windows build0 errors/0 warnings; startup and native mixer audition exit0. Mixer recording includes new menu music: peak0.144392, RMS0.008996, no errors/drops;22-source pool and19.16 MiB PCM budget retained. Report docs/reports/TASK_M15_AUDIO_REFINEMENT_20261013.md; receipts in docs/quality/m15-refinement/. No human listening approval inferred. Next action: owner tests these three corrections. Previous build and unrelated dirty files preserved; M16 not started.

## 2026-10-13 — owner contextual rival shooting board delivered

M14 WP-182/183 follow-up COMPLETE. M15 audio remains complete; M16 not started. Source e2095e94, branch codex/m15-audio. Latest launcher: Builds/Range_Broadcast_Review/START_TEST.cmd. Frostline board opens250 route metres before range entry, persists through shooting/penalties and closes250 metres after exit. Flags/names,mat ordinals,five closure circles,red miss dots and brief red row highlight delivered; reduced motion uses steady feedback. Eight retained rows automatically page all eligible athletes every5 accepted seconds, pinning player/fresh misses. No gameplay/shot/balance/save/replay authority changes; snapshot adds read-only route metres and last completed mask.

Validation:21 core +2 native PlayMode selected tests PASS. Actual production-route career Mass30 adapter board/flash/paging/completion/closure run reached tick4344, authority unchanged by refresh. RU FullHD and EN130%1024x768 bounds/captures pass. Unity Console clean. Win64 build0 errors/0 warnings; ordinary startup exit0. Standalone actual venue/start/range review success,12 captures,14 navigation actions,no errors,owner-profile hashes preserved. Bounded arrays/UIcadence,no new snapshots/cameras/physics queries; no full-gameFPS/zero-GC claim. Report docs/reports/TASK_RANGE_BROADCAST_20261013.md; ADR-0068; evidence docs/quality/range-broadcast/. Next:owner tests contextual board in crowded races. Historical state,previous builds and unrelated dirty work preserved.
