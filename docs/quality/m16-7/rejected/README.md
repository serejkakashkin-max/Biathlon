# Rejected intermediate checks вЂ” not qualification

The imported FBX initially had its automatic LODGroup in addition to the authored
prefab group. The normal native test failed with2 instead of1; the authoring
step now removes only the automatic child groups. The final10-case gate passes.

The first hidden-window capture was black. A second attempt captured only the
native UI; its old whole-image brightness check falsely reported success. Visual
inspection rejected it. `model-area-red.json` reproduces9 failures while excluding
the left UI rail; `ui-only.png` is one actual rejected frame. The final explicit
camera/native-UI composite and independent model-area verifier both pass.

An earlier test also rejected an ordinary Unity AI NoSubscription Log message.
The test now checks Error/Assert/Exception severity, in addition to Unity Test
Runner's unchanged error gate. No failing test was removed, disabled or skipped.
Other initial failures and iteration logs remain local in artifacts/m16-7.

`pose-red.json` and `pose3-before-skin-update.png` show a rejected frame where
the label/rifle had advanced but the GPU skin still showed raised arms. The
final request runs after skinning; the independent pose image probe now passes.
