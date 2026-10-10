# BIATHLON — latest operational state

2026-10-10. M13 owner review OPEN; M14 has not started.
G1/G2 APPROVED. One Woodland venue, max30 racers, High default.

Latest owner build:
**Builds/WoodlandM13OwnerControls-20261010/START_WOODLAND_REVIEW.cmd**.
Russian controls/review notes: READ_ME_RU.txt beside the executable.
Supersedes WoodlandM13RangeWorld-20261010.

RMB manually chambers before shot1 and between shots, no reload after shot5.
LMB fires; Space holds breath; F fine aim; Tab overview. HUD exposes actual HR,
breath debt/recovery and chamber status. Sharper front post and 3.5°/2° optical
view; geometric115/45mm hit apertures unchanged, no hidden RNG.
W/S ordinary pace ceiling800; Shift attack1000; Ctrl recovery350. Existing
reserve/fatigue pay the cost. Hold Space on any course terrain to remain tucked,
including steering; release to rise. Distinct offset uphill poling added.

Existing40m start is straight with a separate original gantry; painted boxes
removed. Low155 T-post dividers,30 real-wind cloths, penalty V-board fencing,
penalty-entry arrows on misses,100 blue finish pickets; yellow penalty paint
removed. Separate outside finish/manual turns/achieved-line release/IBU lane
selection/arrow capture/automatic mat departure remain.

Pathological snow-boundary disk-cover work is pruned by conservative group
bounds over the SAME leaf grid. Full bodies/thresholds/contact/clock retained.
68 focused .NET cases PASS, then27 final cases PASS; native Unity14 cases PASS.
Final Release CPU12000ticks: mean2.348ms, setup2.774ms, cold tick1=134.203ms;
no >100ms tick after1. This is CPU diagnostic evidence, not Windows FPS/GPU.
Player completed one bout;29 AI first bouts by the bounded horizon, not a
full-race pass. Four native screenshots inspected. Console: no recent errors
or post-build exceptions. Build Succeeded,0 errors/496 existing shader warnings;
hidden Mass30/Sprint30 startup/control/transition smoke PASS,EXIT0, rendering
suspended. Owner still tests visible setup/off-course and post-range hitches.

Code commits d518195 and044eb21. No new third-party asset imports.
[Report](reports/TASK_M13_OWNER_CONTROLS_AND_STADIUM_20261010.md).
[Decision](adr/ADR-0045-owner-shooting-controls-and-stadium.md).
Evidence: docs/quality/m13/owner-controls-20261010/.
All2930138 pre-task PROJECT_STATE bytes preserved and prefix hash verified;
only dated progress appended. Master Plan unchanged. Next: owner tests this
build and decides M13 closure. No automatic M14 or gate approval.
