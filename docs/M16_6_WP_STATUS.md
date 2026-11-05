# M16.6 — Career equipment, dynamic weather and training UI

Owner brief: [M16_6_OWNER_TASK.md](M16_6_OWNER_TASK.md). Owner explicitly closed
M16.5 on 2026-11-01. M16.6 OPEN for owner review; implementation and automated qualification complete. M17 not started.
Branch: `codex/m16-6-equipment-weather-training`, based on the actual current
checkout at published M16.5 commit `1717c530` (no automatic main merge).

| Stage | Status | Scope |
| --- | --- | --- |
| M16.6.1 — Appearance | COMPLETE / OWNER TEST PENDING | Complete no-goggles visibility, persistent identity-based rival profiles, legacy preservation, creator/race/LOD qualification |
| M16.6.2 — Equipment/economy/shop | COMPLETE / OWNER TEST PENDING | 6–8 ski specializations, rifle slots and trade-offs, migration, fair AI purchases, two-season economy scenarios and shop UI |
| M16.6.3 — Dynamic weather | COMPLETE / OWNER TEST PENDING | Absolute race-time timeline, snow inertia, existing snow/equipment composition, forecast/owned ski selection, HUD/TAB and recorded replay compatibility |
| M16.6.4 — Training UI | COMPLETE / OWNER TEST PENDING | Six illustrated sport cards, existing preview-backed week plan, RU/EN/accessibility/keyboard/gamepad without changing training formulas |

Each completed stage requires focused tests, native Unity verification, a new
Windows fullscreen review build, append-only PROJECT_STATE entry and scoped
GitHub publication. The complete milestone remains open for owner testing.

2026-11-01: all four feature stages implemented and qualified in focused/native/standalone runs. Legacy broad-regression repair continues in this task by explicit owner instruction. M16.6 remains OPEN; no globally green suite claim. Latest review: Builds/M16_6_4_Training_Review/START_TEST.cmd.

2026-11-02: recover historical fixture inputs (ADR-0089), version the complete
start-world union and exact finite snow-intent search (ADR-0090). Fresh current
full assembly/native replay qualification is running. Historical equipment-stage
run was stopped as obsolete after2078passed/71failed; it is not acceptance for
current code. Next review build M16.6.5 pending qualification and GitHub push.

2026-11-02 latest review: Builds/M16_6_5_Regression_Review/START_TEST.cmd. Current35native policy/metadata/venue +5original-v25Edit cases,5Play replay/seek cases and actual fullscreen startup PASS. Full current.NET assembly still pending; M16.6 OPEN.

2026-11-02 audit correction: previous full.NET run used unoptimized Debug and was stopped incomplete; canonical Release full assembly now pending. Third-exit600s timeout and all strict assertions restored unchanged after same-input configuration control. Current native full Mass/Pursuit repeats2/2PASS. M16.6 OPEN.

2026-11-02 11:10 latest operational state: all feature stages and automated legacy repair complete. Full current Release2188PASS/0FAIL plus5Explicit separately PASS =2193unique cases; recent native52Edit/5PlayPASS, Console0errors. Original third-exit600s gate passes128.543s. Current fullscreen review Builds/M16_6_5_Regression_Review/START_TEST.cmd unchanged and qualified. See docs/quality/m16-6/regression/release-acceptance.json. M16.6 OPEN FOR OWNER REVIEW ONLY; M17 not started.


2026-11-02 latest owner-directed visual follow-up — M16.6.6:18original generated training/ski/rifle images integrated into photographic cards; responsive layout, selection/focus and top shop paging.19nativeEdit+4nativePlay+18.NETPASS; actual2560x1440fullscreen menu probePASS,18resources/3captures/ownerprofilesunchanged/exit0. BuildSucceeded0errors. Evidence:docs/quality/m16-6/menu-art; design/prompts:docs/ui/m16-6-menu-art. New review:Builds/M16_6_6_Menu_Art_Review/START_TEST.cmd. Gameplay/balance unchanged. M16.6OPEN for ownerreview;M17notstarted.


2026-11-02 owner-directed M16.6.7 menu UX follow-up: grouped direct career navigation/current-page marker, real next-week action, Continue priority, training plan beside forecast, shop tabs/balance above products and visible scrolling.68unique focused testsPASS(final6affectedPlay rerunPASS),fullscreen2560x1440menu probePASS/ownerprofilesunchanged/18resources,build0errors/520warnings. Review:Builds/M16_6_7_Menu_UX_Review/START_TEST.cmd. Evidence:docs/quality/m16-6/menu-ux;research:docs/ui/m16-6-menu-ux. Gameplay/balance unchanged. M16.6OPEN for ownerreview;M17notstarted.


2026-11-02 owner-directed M16.6.8: stadium timing glyphs fit actual header; shared gantry blank at launch/seek0. Official BIATHLON: PULSE / Биатлон: Пульс гонки integrated in localized menu/window/launcher.58unique focused testsPASS,final2affectedmenu rerunPASS,fullscreen2560x1440probePASS,bothlocalizedtitles/ownerprofilesunchanged. Finalbuild0errors/520warnings. Review:Builds/M16_6_8_Pulse_Review/START_TEST.cmd; evidence:docs/quality/m16-6/pulse. Stable persistence identity and balance preserved. M16.6OPEN for ownerreview;M17notstarted.


2026-11-03 owner-directed M16.6.9: Russia available in new roster_v2careers, blue/white/red kit, UI/range flags, thirteenth coach+scope. Existing v1saves/drafts and numeric generation/RNG preserved. Independent production tree/crowd/dressing shadow casters repair offscreen/farLOD disappearances without changing visible geometry or quality shadow distance.81nativePlay+37.NET+3nativeEditPASS(119distinct cases),fullscreen2560x1440RUSprobePASS/ownerprofilesunchanged. Finalbuild0errors/520warnings. Review:Builds/M16_6_9_Russia_Shadow_Review/START_TEST.cmd;evidence:docs/quality/m16-6/russia-shadows;ADR0091. M16.6OPEN for ownerreview;M17notstarted.

2026-11-03 publication confirmed: M16.6.9 commit 5b3b590e29e6636a871b7dd7b7fc1372c85aa99e pushed to GitHub; owner review build ready. M16.6OPEN for owner testing.


2026-11-03 M16.6.10 owner follow-up implemented: exact ski-query reuse, moving-body overtakes with versioned replay policy, read-only physical-impact pose reaction, shooting/standalone qualification. Native79Edit/26Play+2seek+3impactPASS (overlapping groups), focused.NETPASS; complete.NET assembly and5explicit diagnostics still RUNNING. Fullscreen build Builds/M16_6_10_Performance_Review/START_TEST.cmd:0errors. Evidence docs/quality/m16-6/performance and traffic-contact; ADR0092/0093. Overall qualification OPEN; append-only STATE controls latest status. M16.6OPEN;M17notstarted.


2026-11-03 M16.6.10 final qualification complete for owner review: exact ski-query reuse; physical moving passes on permitted free corridors; impulse-driven visible impact; shooting checks. Initial full2204PASS/1FAIL retained; failure fixed with immutablev4 private-mat bend reserve and authored moving-pass zones(ADR0094). Current32-case mass/season+124shooting/steering+203career/parity groupsPASS(overlap359executions); original16-event seasonPASS, both captured reprosPASS. Final native39Edit/16PlayPASS,Console0errors. Full assembly afterv4 not rerun. Finalbuild0errors/522warnings,fullscreen2560x1440/shooting/profilePASS,ownerprofilespreserved,all25settingsbyteexact. Review:Builds/M16_6_10_Performance_Review/START_TEST.cmd. Evidence:docs/quality/m16-6/traffic-contact/acceptance.json. NewAI decisions may change order/times; physics/shooting unchanged,oldreplaysretained. M16.6OPEN FOR OWNER REVIEW;M17notstarted.


2026-11-05 M16.6.11 owner follow-up: natural inward pelvis transfer/distributed16degree turn bank; first range overlay prepared while hidden, conditional resize recomposition.45nativePlayPASS+1final affected range rerunPASS(overlap),40.NETPASS;actual first-reveal layout0.3179→0.1527ms at same tick/route;no whole-frame baseline stall reproduced on this hardware. Finalfullscreen2560x1440PASS/ownerprofilespreserved;build0errors/524warnings;all25settingsbyteexact;Console0errors. Review:Builds/M16_6_11_Turn_Range_Review/START_TEST.cmd. Evidence:docs/quality/m16-6/turn-board/acceptance.json;ADR0095. Authoritative gameplay unchanged;complete simulation suite not rerun. M16.6OPEN FOR OWNER REVIEW;M17notstarted.


2026-11-05 M16.6.12 owner crash repair: occupied Sprint/Pursuit start now waits for physical spawn clearance under version3; original clock ticks and old0/1/2 replay behavior preserved(ADR0096). Exact owner1294 and portable44 ContactGeometry repros fixed. Affected.NET185PASS including original16-event season with same finish ticks; final23PASS(overlap), explicit actual-owner full30-field Pursuit1PASS at22211/release1526. Native81distinctEdit+6affected rerun+5PlayPASS;Console0errors. Finalbuild0errors/526warnings;actual owner-copy player scenario and2560x1440fullscreenPASS,ownerprofilespreserved,25settingsbyteexact. Review:Builds/M16_6_12_Start_Contact_Review/START_TEST.cmd. Evidence:docs/quality/m16-6/start-contact/acceptance.json. Only blocked-start waiting can change time/order; physics/shooting unchanged. Full assembly not rerun. M16.6OPEN FOR OWNER REVIEW;M17notstarted.
