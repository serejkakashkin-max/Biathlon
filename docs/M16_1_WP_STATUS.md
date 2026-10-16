# M16.1 — Winter venue refinement

2026-10-16. Owner approves M16. M16 CLOSED / OWNER APPROVED.
M16.1 OPEN / OWNER REVIEW on `codex/m16-1-winter-venue`, from a6bb3e2e.
M17 not started. No merge to main authorized.

| Work package | Status | Acceptance |
| --- | --- | --- |
| A — Baseline and geometry diagnosis | COMPLETE | Native baseline and per-panel reasons retained; 683-file source manifest audited |
| B — Continuous natural snow transitions | COMPLETE / OWNER REVIEW | Rounded/adaptive profiles; exact original buffers; whole-composite contact at yaw 0/45; strict overlap/ground/apron guards |
| C — Range national flag composition | COMPLETE / OWNER REVIEW | Six backstop-relative 10.5m poles; 20 native cases; all target planes clear; zero warm cloth allocations; wind/weather captures |
| D — Native geometry, visual and performance qualification | COMPLETE WITH METRIC LIMIT | 19 matched views per phase; 30-athlete Windows measurements across three tiers; GPU timing unavailable, explicitly documented |
| E — Deterministic regressions and Windows delivery | COMPLETE / OWNER REVIEW | Five fresh full races and 14 save boundaries byte-identical; fullscreen test build and isolated player acceptance PASS |

Scope is presentation only. Preserve all accepted motion, graphics quality,
race geometry, course distances, corridors, timing, shooting, AI and saves.
Existing unrelated local modifications are retained, including ai_range_traffic
and format_bindings; capture their current bytes as the starting baseline.
Prior results are not substituted for M16.1 qualification.

2026-10-16 checkpoint: original source buffers preserved. Adaptive-neighbor trial
has 1767 tapered / 2927 retained vertical panels before apron clipping, compared
with baseline 1027 / 3667. These are construction counters, not visible defect
counts. Remaining angular lines are not accepted as final. Newer seam-normal and
crest variation changes require a fresh applicable qualification.

Native results: flags 20/20; actual Sprint bank/contact 3/3; whole venue capture
1/1 at the prior applicable checkpoint. Captures include actual Unity output,
not generated images. Editor GPU/zero render-counter samples are not proof of
performance. M16.1 is not complete and no M16.1 test build is claimed yet.

Final checkpoint 2026-10-16 supersedes the earlier checkpoint above. Implementation
and delivery complete; M16.1 OPEN / OWNER REVIEW until the owner's art/play test.
Native latest selected cases: 19 EditMode + 56 PlayMode PASS, plus two before/after
capture executions. Counts exclude replaced/overlapping earlier runs and inventory
totals. Five newly executed full races (121817 ticks, 2400 shots) and 14 weekly
save/reload boundaries retain the exact approved M16 results. No authority/motion
source change in the 683-file starting-workspace audit.

Build Builds/M16_1_Winter_Venue_Review/START_TEST.cmd: Windows64 Development,
Unity6000.6.0f1, 0 errors / 518 categorized warnings, 384799261 bytes. Ordinary
desktop fullscreen acceptance PASS at 2560x1440, actual career/Continue/recovery,
settings, checkpoint, countdown and tick 6; owner profiles unchanged. Two native
player comparison runs PASS with 30 real athletes. Observed frame-time change
-2.47%..+2.28%; no GPU timing available and no minimum-spec/FPS guarantee claimed.
Textures, lighting/shadow quality, accepted animation and gameplay retained.

Evidence, exact scope/limitations and gallery: docs/quality/m16-1/README.md,
docs/quality/m16-1/review.html. M17 not started; no main merge.
