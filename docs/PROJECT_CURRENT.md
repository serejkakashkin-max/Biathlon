# BIATHLON — latest operational state

2026-10-10. M13 owner review OPEN; M14 has not started.
G1/G2 APPROVED. One Woodland venue, max30 racers, High default.

Latest owner build:
**Builds/WoodlandM13OwnerFollowup-20261010/START_WOODLAND_REVIEW.cmd**.
Russian controls/review notes: READ_ME_RU.txt beside the executable.
Supersedes WoodlandM13OwnerControls-20261010.

Manual reload policy now survives AI movement rewrites and applies on every
bout, including the third. RMB chambers before shot1 and between shots;
LMB fires, no reload after shot5. Handling kick occurs on reload, while ordinary
breathing/sway continues. Space holds breath; F fine aim; Tab overview.
Production sway is stronger, HR/breath debt remain meaningful, geometric
115/45mm apertures and deterministic hit resolution remain unchanged.

W/S ordinary pace ceiling800; Shift attack1000; Ctrl recovery350. Reserve
recovery120W, drain2400W and slower fatigue recovery increase lasting costs.
Existing career stat curves apply. Space tuck remains player-controlled on all
terrain; A/D manual turns and achieved-line release remain.

Main lap960.005m (20% shorter), penalty150m unchanged. Heights, stations,
guards, paths, profiles, roadside dressing and terrain are rebound. Mat capture
requires actual assigned endpoint proximity; all30 first arrivals pass within
0.5m. Automatic exit no longer applies scripted braking for moving bodies;
physical collisions may change speed. Penalty fence feet sit outside the road,
connecting roads remain open. Whole-grove visibility pruning and334 fewer trees.

Focused .NET19 cases PASS; first-arrival/third-bout2 PASS; explicit CPU1 PASS;
final context arrival/third-bout/replay3 and playable startup2 PASS.
Native Unity19 cases PASS. Rounded branch endpoints rebind to exact main samples.
Final Release CPU5270ticks: mean1.502ms, setup1.957ms, cold tick1=104.641ms;
no >100ms tick after1. All30 first bouts complete in this bounded diagnostic.
This is CPU evidence, not visible Windows FPS/GPU or full-race acceptance.

Build Succeeded,0 errors/0 warnings. Hidden Mass30/Sprint30
startup/control/transition smoke PASS,EXIT0; rendering suspended. Two native
captures inspected; recent Console has no errors/exceptions. Owner still tests
visible setup/off-course and post-range hitches and final shooting/recovery balance.

[Report](reports/TASK_M13_OWNER_FOLLOWUP_20261010.md).
[Decision](adr/ADR-0046-owner-m13-range-reload-reserve-and-short-course.md).
Evidence: docs/quality/m13/owner-followup-20261010/.
All2933802 pre-task PROJECT_STATE bytes preserved; only dated progress appended.
Master Plan unchanged. Next: owner tests this build and decides M13 closure.
