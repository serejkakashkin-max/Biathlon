# TASK-169I1 — physical venue authoring and presentation

2026-10-01. WP-169 prerequisite. G2 remains APPROVED; this task does not certify Venue01 or complete M13.

Schema2 venue manifests bind the exact physical range paths and canonical occupancy definition. Optional authored open start approaches bind the same track/main circuit and a directed endpoint seam. Legacy schema1 serialization and fixtures remain unchanged. The plan exposes individual mat anchors and stable authored lane IDs.

Cold world validation now checks actual widths, distances, grades, body containment, translated mat placement, side entry, penalty access and world seams. Overlapping corridors require complete local guard coverage, including the configured body envelope; this includes parallel ribbons and self intersections. An actual guard containing several independent mat forks is rejected even when its declared port anchors appear local. Adjacent retracing and reversed ribbons are also rejected.

Unity authoring writes each personal mat and its explicit shooting direction. It uses an isolated preview scene, restores selection, and preserves the caller's opened scenes, roots and dirty state. Mat presentation reads each physical anchor. Athlete presentation turns toward the authored target direction during rifle setup and returns to the path heading during exit. These presentation changes do not move authoritative bodies, modify shooting geometry or tune hit difficulty.

## Validation

- .NET: 70/70 authoring/world/local-guard/legacy-validator tests passed (`artifacts/wp169i/authoring-final.trx`).
- Unity EditMode: schema5/5, world11/11, local guard14/14; separate Editor authoring assembly7/7. The generator tests include invalid direction rejection and preservation of active scene and selection.
- Unity PlayMode: individual mats4/4 and proxy direction2/2 passed. Real personal-path prone and standing bouts verify setup, aiming, bolt, exit, rifle direction and return, alongside unchanged authoritative motion, sight, events and snapshots.
- Final full Unity PlayMode176/176 passed on the frozen I1/I2 source tree. Console verification on2026-10-02:0 errors, no compilation failure;3 MCP connection lifecycle warnings inspected in actual Editor entries. The console-clear MCP tool hit a locked log file; its failure artifact is retained, and the actual Editor console was subsequently cleared after preserving the baseline.
- Initial proxy test failures are retained in the local evidence. The fixture first asserted an inactive prestart proxy and then fired within the same tick as entering aiming. It now observes only active bodies and fires on a subsequent aiming tick. Acceptance assertions were preserved.

## Limits and next work

Static guards establish required conflict coverage; they do not establish dense dynamic throughput. The new authoring fixtures are contracts, not production course acceptance. The 30-athlete production draft currently exposes an exclusive start-region conflict despite safe grid spacing. That needs an explicit physical continuation proof, preserving crossing traffic and incoming tails. Venue01 also needs reusable30-mat admission for60-entrant formats, exact format routes, environment integration and runtime/camera evidence before WP-169 can become DONE.

OWNER-FEEL-001 remains OPEN for WP229/230/232. No hidden RNG or authoritative shooting tuning changed. The owner-approved target018 delivery remains the latest playable build.
