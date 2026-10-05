# TASK M13 Woodland dressing 01 — course landmarks and updated Windows review

Date: 2026-10-05. Status: PASS for this bounded presentation task. One production venue remains Woodland Basin; M13 and full manual steering remain OPEN.

Added six course banner groups, two short fences, two real grandstand/crowd pockets and one support hut along the Main loop. The two stands provide336 actual seating anchors. Existing audited G2 meshes/materials and owner-approved simplified crowd are reused; no additional asset download, proprietary Biathlon2006 content, brand copying or license change. The Windows third-party credits remain byte-exact.

The authoring recipe evaluates complete native mesh/prefab footprints against every paid corridor and exact terrain-triangle overlays. Narrow trapezoids preserve spectator sightlines. Sixty original tree instances were removed with full IDs, instances and reasons retained;26,554 trees remain in90 groups. Banner footings use the lowest terrain contact, with bounded snow embedding rather than floating above a slope; maximum static embedding0.157214m is below the explicit0.25m limit. Stands, fences and hut keep level foundations. Native tests separately verify full rendered terrain coverage, no floating footing, the embedding limit, unit authored scales and at least3m full-footprint corridor clearance.

Import boundary:216 venue JSON sources,11 presentation JSON changed (environment plus ten forest groups),205 byte-exact unchanged. All authoritative track, topology, formats, ranges and terrain are unchanged; all .meta GUIDs preserved. Original sources match Git baseline86ae181 with the original Windows CRLF checkout policy; Git text blobs use LF. Regenerate the candidate in an isolated baseline checkout, not over the already imported landmark IDs. The before-import bytes and complete candidate reproduction are retained under `artifacts/owner-woodland-scenery-20261005/candidate-03/`.

Validation: nine authoring tests PASS, byte-identical independent candidate regeneration, independent terrain/forest verification, ordinary native GameView inspection of both stands, hut and slope footing, and one native PlayMode acceptance PASS. [Evidence](../quality/m13/course-dressing-20261005/acceptance.json) contains exact sources, counts, hashes and [native result](../quality/m13/course-dressing-20261005/native-test.json). Its discovery total241 is not the executed count. The visual views use an actual detached normal-camera environment with a frozen field at tick0; they are landmark review, not race-completion evidence.

Three development fixture failures are retained: the first incorrectly used a source-ribbon helper after apron clipping; the second compared derived float lossyScale exactly; the third compared groomed closed seams against an ungroomed builder. Corrections use real terrain renderer buffers, require exact local unit scales throughout the hierarchy with a bounded derived float residual, and compare initialized native mesh arrays before/after dressing. Production geometry, helper guards and acceptance limits were not weakened. The separate legacy full-core allocation failure remains OPEN.

New Windows build: `Builds/WoodlandReview20261005/Biathlon.exe`, startupHigh, Unity6000.6.0f1,0errors/0warnings,359,783,913bytes. Use `START_WOODLAND_REVIEW.cmd`. It contains the minimized torso motion and new course landmarks. The playable controls still use legacy route-follow/line input; the new manual-heading kernel is disconnected and is not advertised as delivered.

Hidden content checks PASS for Mass30, Sprint40 and real-qualified Pursuit40, each400 paid ticks; Pursuit consumes the accepted actual Sprint40 record/replay. These are initialized-content/control checks, not new complete races. Hidden normal-camera rendering was suspended, so no Windows visual, GPU or sustained FPS qualification is claimed. Six native owner assets retain exact serialized values/dirty flags and frame-timing state;20 existing owner files and three already absent owner paths retain exact state. Final native Console0errors/0warnings, compilation_failed=false.

![Actual plateau spectator pocket](../quality/m13/course-dressing-20261005/plateau.png)

![Actual course support hut](../quality/m13/course-dressing-20261005/hut.png)

Next: shared world-motion/route-station integration for full manual direction, then fresh WP175 weather views and complete condition flows; WP176/177/178 remain open. G1/G2 approvals and the authoritative Master Plan are unchanged.
