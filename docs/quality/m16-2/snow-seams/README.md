# M16.2 WP-I — format-cut snow and modern IBU points

2026-10-31. WP-I COMPLETE / OWNER TEST PENDING. Updated Windows launcher:
`Builds/M16_2_Live_Shooting_Review/START_TEST.cmd`. M16.2 remains owner review.

The owner's screenshot is reproduced by actual format-route cameras. Main's
selected panels have two true cuts in Sprint/Pursuit; the narrower branches do
not cover the full transverse ends of the main ribbon and its snow banks.
An additional buried-rim reservation could prevent a neighboring exposed side
from receiving a shoulder. No terrain/route/AI/scoring tuning was changed.

The reservation filter checks a candidate against the actual complete packed
footprint before indexing a possible neighboring bank. Three parallel-overlap
fixtures are red before this fix and green afterward.

The implementation extends the existing cold bank builder with local rounded
profiles at actual selected cuts and the connected ends of open format branches.
Branches are identified from explicit directed ports joining closed race loops
at both endpoints; private mats and the start approach do not qualify.
It splits the original cut at
packed-boundary crossings, limits free snow against the complete paid footprint,
and uses the existing policy's width, minimum width and cross-section count.
The existing terrain coverage, clipping, world-float and profile closure guards
remain. Equal opposing interior profile caps cancel before publication. The
original curtain remains only where a rounded profile cannot fit. Original ribbon, terrain,
physical paths, private mats, textures, quality tiers, gameplay and saves remain.
Added geometry shares the existing owned mesh/material. No per-tick work, extra
renderer, collider, resource cache or external runtime dependency. Uncut MassStart
receives no transverse cap.

Footprint admission keeps intersection coordinates in double precision until
mesh emission. Widths are shared by incident strips and bounded independently
by each free-space fan. A contacting endpoint can pinch to zero without deleting
the neighboring free endpoint. Constant-width rectangles and a greedy fan choice
had left the photographed notch. Exposed fan boundaries close to actual terrain.

Within the policy's existing end-fade distance of an actual cut, natural profile
caps retain only their positive portions against convex terrain. Rejecting an
entire bank because its end chord briefly crossed a terrain ridge had recreated
a vertical wall. Authored packed-edge clearance checks remain unchanged; custom
snow-profile chords are terrain-clipped, rather than treated as packed edges.

All parts of a cut reuse its original full cross-row direction. Re-normalizing
short bank chords independently had moved a shared foot by float bits, leaving
opposite internal walls. A targeted producer trace confirmed that origin; the
trace was removed after fixing the shared direction.

A single cold roof index removes an internal curtain only when complete edge
matches certify roof coverage at sufficient height on both sides. Endpoint
comparison admits one native float step at the generated venue's horizontal
extent; height comparison admits one downward representable step. No vertices
are moved or welded. Nine new unit cases distinguish two-sided coverage from
single-sided, lower, partial and offset roofs, and test height/scene roundoff
against real gaps. All original independent contact/topology tolerances remain.

Experimental whole-patch runtime clipping and offline constrained authoring were
rejected: independent contact/topology checks exposed holes or their visual shape
was too angular. They are retained as red evidence, not shipped code/assets. All
seven actual-format contact regressions remain: Sprint/Pursuit at yaw0/45, both
formats with career finish runout, and MassStart. Original source buffers, heights,
row centers/widths and full world-space contact/topology checks remain unchanged.
No geometric tolerance is relaxed. Cache-only tests were retired with that
abandoned architecture; none of the failing full-contact cases was removed.

The final Sprint fixture additionally requires a real upward roof at the
photographed return's outward witness(226.96336,-111.8641), outside all paid
ribbons and the apron. An initial negative-Z point was inside the Main ribbon;
it was corrected using actual mesh evidence and retained as a negative control
which decorative snow must not cover. `precise-cuts-sprint-native.json` and
`pinched-cuts-sprint-native.json` are valid red receipts for the corrected outward
witness; `shared-fans-sprint-native.json` passes it together with full contact.

Current IBU WorldCup scoring is90/75/65 for the podium, not60 for first place:
[2026 Event and Competition Rules,15.8.2.1, PDF page72](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf#page=72).
The official [2022/23 change announcement](https://www.biathlonworld.com/news/bi18-institutional-rule-changes/IC2xOiQt6bhRKSWm1uZwt)
explicitly records the increase from60 to90. Existing modern scoring is retained;
the existing WorldCup promotion/scoring CoreCLR regression passes1/1 in
`ibu-scoring-coreclr.trx`. No save/points migration is appropriate.

Qualification is collected by `scripts/quality/report_m162_snow_seams.py`.
Before/after images are native Unity renders at the same high tier, camera and
time of day. The first before matrix includes Sprint's actual forks; it did not
include Pursuit's actual shortcut, so unrelated inactive-branch images are not
presented as a matched Pursuit baseline. Final source guards cover669 starting
core/resource files. Historical candidate capture passes are visual-smoke receipts,
not evidence that their then-failing contact/topology proof passed.

Final native qualification runs six bounded suite calls:7format,16terrain,
3Sprint banks,13coverage,2rim and1matched camera test,42cases in total. The earlier
single all-suite call exceeded the server's300s wait; its completed42passing rows
were recovered honestly in `matrix-timeout-recovered-rows.json` and are interim
evidence, not substituted for the final per-suite receipts.

The669-file guard compares against the task's starting workspace. Preexisting
owner/Unity edits remain preserved and excluded from this change's commits.
This is a cold visual-geometry fix; the camera/startup probes are not an FPS or
minimum-hardware benchmark.

M16.1 remains approved; M16.2 remains owner review; M17 not started. No main merge.


## Final delivery

42native/1CoreCLR PASS. The six `*-final-native.json` receipts are final; the
earlier matrix timeout and red candidates remain diagnostic history. All669
starting core/resource files are byte-exact and the complete state history is
preserved. `qualification.json`, source/build fingerprints and guard files record
the current delivery.

Windows build Succeeded,0errors,385330319bytes,39.34s. Categorized520warnings:
24existing API-deprecation call sites,460UnityAIInference compute variants,
36Sentis pixel shaders. `build-warnings.tsv` retains exact groups. The snippet
budget expired after the blocking build completed; all3tool-timeout Console
messages were classified, with0compiler errors. Fresh outputs and the successful
compiled player independently qualify the build; final actual Console is clean.
The shared build helper's legacy receipt ID is retained honestly; milestone.json
and current source/binary fingerprints identify M16.2.

Ordinary fullscreen player PASS/exit0,2560x1440,personal3-2-1,drawn bib25,accepted
tick14405; no runtime errors and owner profiles unchanged. Native world/UI startup
captures are in `fullscreen/`. The optional cursor/history probe was not invoked;
its raw default fields are not acceptance claims. Previous qualified build backup:
`Builds/M16_2_Cursor_History_Backup_20261031`. No ordinary-game/minimum-spec FPS or
full-race performance claim. Next task is owner visual testing at both formats'
forks; M16.2 remains open for that review.
