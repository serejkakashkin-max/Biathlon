# ADR-0097 — Separate M16.7 Humanoid asset and inspection studio

2026-11-05. Owner closes M16.6 and requests a realistic, editable biathlete
according to `docs/M16_7_OWNER_TASK.md`. The existing G2 athlete uses the typed,
read-only presentation boundary in ADR-0020 and the hybrid animation boundary in
ADR-0069. Its gameplay controller and contact/ground/grip assumptions are already
qualified. A different anatomical skeleton must not silently replace that rig.

## Decision

Keep the new character under `Assets/Biathlon/Characters/Biathlete_16_7/`.
The editable Blender source retains separate garments and equipment. Export a
runtime FBX with a valid Unity Humanoid avatar, one weighted body per LOD and
independent rifle/skis/poles. Six explicit, serialized equipment sockets belong
to this asset. Root motion is disabled; the inspection poses cannot access a
race, clock, save, shooting outcome or physics state. This is an asset-specific
rig, not a new authoritative character controller.

Only the owner-provided CC0 anatomical source/skeleton/skin maps and retained
walk/run file are incorporated. Casual clothing is replaced by a separate
weighted compression shell; the sport equipment is authored in Blender. The
downloaded alpine skis, safety glasses and ordinary rifles are measured sources,
not final equipment. The paid CGTrader model is an art-quality reference only.
Preserve the source's AI-assisted disclosure and provenance.

Unity imports four-weight Humanoid skinning and project-owned URP/Lit materials.
Textile maps are genuinely 4096x4096; supplied skin maps are 2048x2048. Blender's
scene-linear material colors convert at the Unity property boundary. ORM/roughness
data remain linear and are packed as metallic/smoothness for URP. Normal maps
use normal import mode. Do not change global render assets to improve this model.

The FBX importer may create automatic LODGroups from mesh names. Remove those in
the authored prefab and create exactly one measured three-level LODGroup. Keeping
both registers the same renderers twice and produces native warnings. Unpack the
imported model before changing the rifle's parent so the saved prefab preserves
the independent socket attachment.

Provide a separate orbit studio scene and Windows review build. Its cloned UI
panel, camera, lights, probe and high-quality lease are local to the scene. The
ordinary game remains accessible from the studio. Four inspection poses test
basic deformation, foot-preserving knee flexion and rifle reparent/reset. These
are not skating or shooting animation qualification.

## Verification and limits

Native tests cover avatar, sockets, independent equipment, root motion, UVs,
material ownership/import, measured LODs and repeated finite deformation. Existing
G2 tests continue to verify the race/camera presentation boundary. Actual Blender
renders and standalone captures are required in addition to these numeric gates.

On this Windows driver a hidden window can omit the automatic world rendering
pass while its native UI still renders. A UI-only frame falsely passed a whole
image brightness check. The opt-in development review now explicitly submits the
actual camera, renders the native UI to a separate transparent stencil surface
and composites both on the GPU. The model area excludes the UI rail in the image
gate; a retained PowerShell verifier reproduces the former failure independently.
An additional visual probe caught a one-frame GPU-skinning delay: the label and
rifle socket had advanced while the skinned body still showed the preceding
pose. Submit the explicit camera request after WaitForEndOfFrame, when skinning
and native UI for that pose are complete. The image verifier separately checks
raised sleeves in pose2 and their absence in pose3, excluding the central cap.
The retained original probe fails and the final player probe passes. Ordinary
play has no capture targets or extra render requests.

The installed Unity-MCP package independently requires dependency generation34
instead of30. Preserve that resolver change and exclude it from this commit.
The remaining24 ProjectSettings files match task-start bytes after guarded
restoration of temporary build settings. This is not a character render change.

M16.7 stays IN PROGRESS. This checkpoint supplies editable geometry and a working
inspection build. Reference-equivalent photorealism, manual cloth/joint cleanup,
finger articulation/contact and the new rig's complete race skating/aim/ground
adaptation remain unqualified. The existing race athlete is not replaced. No
stat, propulsion, RNG, shot geometry, save schema or race outcome policy changes.

Evidence: `docs/quality/m16-7/qualification.json` and
`docs/M16_7_WP_STATUS.md`. No claim that the complete simulation suite was rerun.

API references: [PanelSettings clearColor](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/UIElements.PanelSettings-clearColor.html),
[URP render requests](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/User-Render-Requests.html).
