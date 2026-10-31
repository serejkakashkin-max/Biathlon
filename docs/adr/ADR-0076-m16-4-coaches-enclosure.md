# ADR-0076 — M16.4 coaches' enclosure

2026-10-31. Presentation-only addition requested by the owner.

Use the actual range origin, direction and outside mat anchors for a fenced
team-staff platform behind the athletes. IBU ECR2026 §3.4.2.5 calls for a separate
coaches' enclosure at least2m deep and30cm higher than the shooting ramp, with a
view of the targets. Reference: [official IBU rules, p32](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf).
The existing approved venue has a rear exit aisle; retain it. The decorative front
fence is17.2m behind the firing datum, beyond that aisle, rather than moving the
approved race geometry to reproduce the rule's10–12m ramp dimension exactly.
The3.2m-deep platform spans the30mats, has a rear gate/step, and leaves open ground
for a media strip behind it. No claim of complete venue certification.

Check the entire rectangular footprint against every segment of every authored
physical corridor, including private mats and all format branches, with3m rigid
clearance. Segment/rectangle crossings count even when both endpoints lie outside.
Sample actual ground under the platform; raise its top if needed, and bury the
foundation below the lowest support samples. Position coaches at the top plane.
Aim each telescope from the coach's right eye toward the nearest actual target
bank, with a25mm eyepiece gap. No colliders or shooting occluders.

Reuse the approved Hero athlete's faces, body and three skin LODs. An Editor-only
authoring operation bakes the existing face/hair/body morphs and static arm pose.
The saved staff art has no Animator, SkinnedMeshRenderer, athlete animation scripts,
rifles, skis or poles. Remove the rifle harness from the copied rubber submesh,
colour the former bib as winter clothing and retain the original textile/skin
normal maps. The original athlete assets, shaders and animations remain unchanged.
Six face shapes, eight skin tones, ages44–69, grey/dark hair, stubble/clean faces,
three headwear choices, heights and six national palettes produce twelve distinct
staff identities. Coach-only bind-space age/beard shading never changes athletes.

Each static body uses one shared material and three native LODs. Railings, snow
deck, access step, national plaques, scopes/tripods, cases and clipboards use one
combined mesh and that same material. No new Update/LateUpdate callback, per-tick
planning, runtime mesh baking or per-frame material creation. These are finite
rendering costs, not a zero-GPU-cost claim. Existing graphics tiers and textures
are preserved. Filter all-LOD tree crowns and small winter dressing only when
they overlap the new enclosure's clearance; do this once during construction.

The optional `coaches_resource` keeps older/fixture recipes compatible. Definitions
have stable IDs/schema versions; explicit native bindings replace scene-name
lookups. Environment shutdown releases only generated scenery, retaining shared
baked assets. Domain/Simulation/AI/Career/Replay, race tuning, replay and saves are
unchanged. Qualification: `docs/quality/m16-4/coaches/README.md`.
