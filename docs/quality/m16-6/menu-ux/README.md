# M16.6.7 — Menu UX acceptance, 2026-11-02

- Research/design: [decision and primary references](../../../ui/m16-6-menu-ux/DESIGN.md).
- 31 .NET Release career/session/training/equipment tests passed: career-menu-regression.trx.
- 18 native PlayMode menu tests passed, including actual race launch/pause/abandon, result boundary, save/plan preservation, keyboard/gamepad and new direct navigation: native-playmode.json.
- Final toolbar placement and week-action wording: 6 affected native PlayMode cases rerun, all passed: native-playmode-final.json. These are a subset, not six additional unique tests.
- 19 native EditMode resource/panel tests passed: native-editmode.json. Total68unique focused cases.
- Inspected native RU1600x900 and EN1024x768/130% high contrast screenshots. Training plan is beside the forecast, shop balance/categories above products. Slim scrollers reveal overflow; large text retains vertical scrolling.
- Actual fullscreen player2560x1440FullScreenWindow, exit0,18resources checked,5screens captured, no errors, isolated profile store, owner profile fingerprint unchanged: standalone/standalone-review.json. This menu probe does not claim a full standalone race completion.
- BuildSucceeded,0errors,520warnings,481310504bytes,36.5138435seconds. Warnings remain; no zero-warning claim. Console0Error/Exception/Assert. Binary/source hashes in delivery.json.
- All25ProjectSettings asset hashes equal the pre-build snapshot. Unity persisted a temporary High URP binding while building; only the exact SHA-matching pre-build representation was restored. Receipt: build-settings-preserved.json.
- Initial visible-scroller implementation intercepted gamepad moves; the failing original regression was retained and fixed by routing button navigation before the ScrollView. All final runs passed.

Review build: Builds/M16_6_7_Menu_UX_Review/START_TEST.cmd.
No core gameplay, tuning, economy, duration, save or replay changes. Prior full2193case race qualification is historical and was not repeated for this UI task. M16.6OPEN pending owner review, M17not started. Unrelated pre-existing work remains outside this publication.
