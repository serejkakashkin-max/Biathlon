# ADR-0098 — M16.7 race integration with career preservation

2026-11-08. The owner approves the revised athlete and explicitly authorizes
integration into the game, removal of periodic lateral sway, a comparison video
for YouTube and a playable test build. This supersedes ADR-0097's studio-only
restriction for the validated runtime presentation adapter.

Keep the existing typed G2 rig and detached race animation feed as the motion
and compatibility boundary. Render the anatomical athlete as a sibling visual
under the same stable placement pivot. Retarget the already evaluated pose once;
do not instantiate a second motion reference or sample a synthetic race clock.
Preserve model-root/camera contracts, accepted aim and authoritative simulation.
The source rig's renderers are suppressed independently of animated enabled flags.

The runtime adapter projects saved appearance IDs, nationality, selected skis,
bib number and leader role onto the new visual. Save schemas, stable IDs,
inventory and career statistics are retained. Test real saves through copies;
retain a byte-hashed backup and verify original files remain unchanged.
Creator previews use the same visual adapter. Existing comparison fixtures retain
an explicit legacy G2 view for honest before/after footage.

Remove periodic pelvis transfer and torso twist in the candidate's local motion
definition. Steering lean and impact observations remain presentation inputs.
Keep independent ski motion, contact checks and root protection. Resource-owned
bindings make the new asset available in Windows players without Editor paths.

Acceptance requires native race and appearance tests, repeatable source/target
root and aim preservation, shooting transitions, memory/allocation and field
performance checks, actual screenshots, a successful Windows build and a
career-preserving player smoke test. Document measured limitations explicitly;
numeric animation tests do not establish universal biomechanical correctness.
