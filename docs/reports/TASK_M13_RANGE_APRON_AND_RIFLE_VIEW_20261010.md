# M13 — range crash, playable forest and venue rifle view

2026-10-10. Implemented and built for owner review. M13 owner review remains
OPEN; M14 is not started. G1/G2 remain approved. One Woodland venue, max30,
High default. This task follows the owner’s report and four screenshots.

## Result and changed systems

Fixed the reproducible `WorldRejected / Course / woodland_basin_v1_athlete_29`
at final shooting setup. A waiting player changes pack contacts; the all-AI
first-bout diagnostic alone had missed it. The actual owner screenshot failed
at tick 4256; the new desktop regression reproduced the same failure at tick
4306 before the fix. A contacted athlete was still on the existing visible
flat apron, but only its narrow private mat curve was recognized as snow.

`ManualRangeApronSnowRegion`, production composition, world admission and
ribbon clearance now recognize the exact existing whole apron during moving
range occurrences. Whole-body continuous admission and a continuous clearance
field preserve ordinary passive edge response. Track/visual/path/body source
binding and replay context make this deterministic geometry explicit. Source
snapshots reuse cold serialization; there is no new per-tick JSON allocation.
No body collision, IBU mat assignment, phase boundary, player shooting geometry
or shared clock gate is disabled. Actual post-contact trajectories can change.
See [ADR-0044](../adr/ADR-0044-range-apron-snow-and-world-rifle-view.md).

`prune_playable_forest.py` and its versioned recipe reduce loaded trees from
8244 to **2959**: **5285 removed, 64.1%**. Loaded groups reduce 33 to 23. Dense
original trees remain within 45m of the authored visual routes; sparse original
silhouettes extend through 80m. All retained IDs, species, poses and density
ranks match the source. Two hidden structures (16 mesh parts) are removed.
Empty source groups stay archived on disk but are not loaded. Existing High
settings, terrain, crowds, roadside landmarks and paid route geometry are kept.
The stadium screen moves onto existing flat stadium ground, away from the
slope where its old foundation intersected snow.

`RangeVenueDressingPresentation` adds 30 actual Mass Start slots with painted
edges/numbers, 30 numbered mat signs, target-bank number panels and **90 wind
flags**, three per lane. Wind uses detached real shooting inputs; calm cloth
hangs down. These signs are original project art with no external asset or brand.

`RangePresentation`, `RifleSightPresentation`, two presentation resources and
the detached snapshot now show the production venue through the original
HeroRifle’s diopter/front post. Neighboring installations and wind flags remain
in the world. RMB keeps fine aiming; **held Tab** opens the wider mat overview
with the player model visible. Only one gameplay camera renders the world.
Stance eye height does not change the canonical ray: native tests check its
intersection with the exact target plane for prone and standing. The legacy
isolated benchmark fallback still passes. Simulation hit/miss and sway tuning
are unchanged; easy shooting remains an M15 tuning issue without hidden RNG.

## Verification

Evidence: [range-world-review-20261010](../quality/m13/range-world-review-20261010/).

- **24 focused .NET cases PASS**: 20 arrival/ribbon cases; three complete signed
  Mass3/Sprint1/Pursuit3 replays; one actual production Sprint first-bout command
  replay comparing world pose, velocity, physiology and recorded race events.
- **7 native Unity cases PASS**: both actual Mass30 player arrival variants,
  clearance continuity, production apron replay, High/Balanced/Low host world
  joins with exact apron bounds, physical dressing audit and range fallback.
  Test inventory totals in MCP output are not executed counts.
- The previously failing delayed arrival now reaches actual aim at tick **5881**
  and passes through shooting/departure to tick **6182**. The standalone desktop
  run averages **5.139ms**, worst **65.586ms**, with 55 ordinary course edge
  responses across the whole run. The fast arrival reaches aim at tick **4207**
  and continues to **4508**. No shared clock rejection occurs.
- Native Game View captures show the painted start, the original rifle sight,
  readable prone aperture, numbered positions/wind flags and the pruned forest.
  The actual native range review has field/world tick **4212**, Aiming.
- Windows Unity 6000.6.0f1 build **Succeeded, 0 errors**, 26.573s, 359351634 bytes.
  It reports **496 warnings**, all reviewed inference-package shaders: 460
  ConvGeneric and 36 Hidden/Sentis warnings from `com.unity.ai.inference`.
  No gameplay/rifle shader warning is identified. That pre-existing package is
  preserved; this is not reported as a warning-free build.
- Hidden Mass30/Sprint30 build startup, paid400-tick controls and real scene
  switching **PASS, exit0**. Both entries load 2959 trees/23 groups, 30 mats and
  30 banks. The hidden window suspended rendering; this supplies no FPS claim.
- Final timed Unity Console query returns no Error entries. Intermediate red
  diagnostic/tests and the review-script duplicate-publication error are not
  claimed as final passes. Source compile errors are resolved.

The art audit now follows the owner’s practical acceptance direction: 2.5cm snow
footing allowance and a 1m minimum for noncollidable tape; rigid scenery retains
3m separation. Exact render joins and simulation tolerances are unchanged.
The audit caught and fixed the screen’s old snow intersection. No failing test
was deleted. An initial new replay assertion incorrectly included source AI
planning journal entries; the corrected test compares recorded command-result
race events with replay, and still compares source physical/resource state.

## Delivery and limits

Launch **Builds/WoodlandM13RangeWorld-20261010/START_WOODLAND_REVIEW.cmd**.
Russian instructions are beside the executable. The build defaults to Mass
Start30 / High, and the existing review controls can select Sprint30.
`build-files.json` and `player-verification.json` identify the delivered payload.

EXE SHA256: `718444f6a718fa668da96eb0a58445b009673310e6129152a63dbf7a92b8de00`.
Build-file manifest SHA256:
`dad0dc4a14e339c33fe8c8901786178ca21c2c3b960ea71f66b9f71e17046471`.
The unchanged Unity launcher EXE hash alone does not identify gameplay content;
the complete payload manifest does.

Sustained visible FPS, subjective shooting feel and full-field later bouts are
for the owner’s playtest. No exhaustive full30 race sweep was added. The new
physical apron is scoped to this flat +Z venue; a future venue needs bound
geometry. The rifle view changes magnification/presentation, so the owner should
review target readability and Tab overview. M14 shooting HUD polish remains
future work. Earlier separate finish, manual bend steering, line-preserving
released heading, arrow capture and automatic mat departure remain implemented.

`PROJECT_STATE.md` receives only a dated append. Its entire pre-task 2924203-byte
prefix is verified against SHA256
`9a8a9fbac5105a4efb7ee7f2a8033b47ed7d20645768a587f1bef87f747906fc`.
`PROJECT_CURRENT.md` is updated separately. Only task changes/evidence are
committed; unrelated existing workspace changes are preserved.

Next: owner tests final setup, shooting and 30 seconds after departure in this
build, then decides M13 closure. No automatic M14 transition or owner-gate pass.
