# TASK-141 — Humanoid architecture verification

Date: 2026-09-13. WP141 PASS; WP142 next. M11 remains in progress.

Replaced capsule creation with an original reusable humanoid/equipment prefab. The existing stable-ID proxy/camera pivot contract is preserved. Typed bone/socket bindings support a replacement asset without name lookup or gameplay edits. The pose consumer reads detached observations, preserves Animator-evaluated poses and disables root motion. Per-render roster scratch collections are reused.

The original development asset has one skinned body renderer, separate socket-mounted skis/poles/rifle, shared meshes/materials, suit/bib/accessory material separation, and an editor generator. Provenance is recorded in `docs/licensing/m11/asset_licenses.md`; no external asset or reference branding was imported. LOD-compatible asset/consumer separation is established; actual LOD qualification belongs to WP150.

TASK141A adds committed tuck/final-sprint, course grade/curvature and exact shooting phase/sway observations. It only forwards/read-copies existing state. Authoritative movement, controls, shooting numbers, geometry, event output and results are unchanged. A complete observed/control race compares all events/results.

## Verification

- .NET focused observations and related regression: 19/19 passed (`artifacts/balance/task141a-animation-observations.trx`).
- Unity EditMode new observation tests: 4/4 passed, 550 discovered.
- Unity full PlayMode: 28/28 passed, including complete Sprint/Pursuit/Mass Start and new rig replacement/reset/Animator tests.
- Native Console after runtime captures: 0 errors, 1 retained upstream warning, compilation_failed=false.
- MCP Game View: frozen ticks0/400 through unchanged chase camera and a three-quarter rig view inspected; evidence `docs/quality/m11/wp141/`.

Initial filtered runs returned Unknown/0 executed; these were not counted as success. Refresh/domain reload and explicit/full runs produced the passing evidence. Asset generation initially exposed an invalid zero quaternion; the builder now uses exact default-value equality before TRS, and the asset was regenerated and tested.

## Limits

The athlete is a development rig with simple rigid skin weighting and minimal face detail. WP141 captures deliberately show bind pose; locomotion, realistic contact and shooting transitions are not claimed here. The range target stage from M10 is unchanged. Existing schematic venue ribbon overlap is still pending. Final visual quality is not approved by G1 or this architecture test. Windows executable rebuild follows animation integration; these results describe the Unity project, not an updated executable.

OWNER-FEEL-001 remains open. No numerical shooting retuning occurred.
