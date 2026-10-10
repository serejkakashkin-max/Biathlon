# BIATHLON — latest operational state

2026-10-10. M13 owner review remains OPEN; M14 has not started.
G1/G2 APPROVED. One production Woodland venue, max30 racers, High default.

New owner build:
**Builds/WoodlandM13RangeWorld-20261010/START_WOODLAND_REVIEW.cmd**.
READ_ME_RU.txt is beside the executable. Replaces the previous SetupFinish build.

The final-setup crash WorldRejected / Course / athlete_29 was reproduced with
a delayed actual Mass30 player. Actual existing common apron snow is now bound
to course admission and continuous course-contact clearance. Shared clock,
body contacts, phase/firing gates and IBU mat selection remain required.
Delayed arrival aims at5881 and continues through shooting/departure to6182;
fast aims4207 ->4508. Authoritative recovery trajectories can change; exact
geometry is versioned and deterministic command replay passes.

Loaded forest: 8244 ->2959 trees,33 ->23 groups. Removed5285 unseen/distant
trees and two hidden buildings; original nearby trees/High settings preserved.
Added actual painted start slots,30 numbered mat signs, target-bank numbers
and90 wind flags observing real shooting wind. Original HeroRifle diopter view
now uses actual venue banks. Hold Tab for the wider mat overview; RMB fine aim.
Prone/standing camera sight axes match canonical target-plane intersections.

Validation:24 focused .NET and7 native Unity cases PASS; native Game View
captures reviewed. Windows build succeeded,0 errors /496 existing inference
package shader warnings. Hidden Mass30/Sprint30 startup/control/scene smoke
PASS,EXIT0. Rendering was suspended; sustained FPS and perceived lag await
the owner's visible test. No exhaustive full30 race sweep added.

Earlier separate outside finish, manual steering through bends, released
heading on the current line, arrow capture and automatic mat departure remain.
Easy-shooting feedback stays OPEN for M15, without hidden RNG. Shooting sway,
hit geometry and assists have not been retuned in this presentation correction.

[Report](reports/TASK_M13_RANGE_APRON_AND_RIFLE_VIEW_20261010.md).
[Decision](adr/ADR-0044-range-apron-snow-and-world-rifle-view.md).
Evidence: docs/quality/m13/range-world-review-20261010/.
The entire2924203-byte pre-task PROJECT_STATE history is preserved and hashed;
only dated entries are appended. Implementation/evidence committed as fe2fd13
and0bc7e9b and successfully pushed to origin/codex/wp-078-ai-pack, including
the four native Game View captures. Unrelated workspace changes preserved.

Next: owner tests final setup/shooting and30sec after departure, then decides
M13 closure. No automatic M14 transition or gate approval.
