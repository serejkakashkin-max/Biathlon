# ADR-0080 — M16.4 personal start visibility, loader handoff and mouse orbit

2026-10-31. Owner requests that a delayed player's avatar appears only for the
personal three-second countdown, that race HUD never overlays loading, and that
holding LMB while skiing allows looking around the athlete.

## Reproduction

Two native regressions fail on the original behavior: the selected waiting
athlete is active before their countdown, and the independently owned HUD is
enabled/attached while the real career loader is still preparing Venue/Ready.
The previous start test explicitly expected early avatar visibility; the owner's
new directive supersedes that visual expectation. Keep that test and update its
acceptance, adding exact one-tick-before/at-boundary Sprint/Pursuit cases.

The candidates were unconditional selected-prestart visibility, an independently
sorted HUD document, and an early handoff. Native probes confirm the first two;
accepted start times, gaps and countdown clocks themselves remain correct.

## Visibility and loading

The selected prestart athlete's accepted waiting transform continues to update
for camera tracking, but its hierarchy/equipment and animation/VFX are hidden
until remaining accepted start time reaches the existing configured countdown.
Use the same initial-tick floor as personal preparation. Show immediate starters
through the existing local three-second hold. Other waiting athletes retain their
existing policy. Starting/admitting a physical actor remains simulation-owned;
no body is admitted earlier or moved to another grid slot. Skip continues to
execute every accepted command/tick and stops at the same boundary.

Detach the career's independent GameplayHud UIDocument after its cold bindings
are composed, before the next loading frame. Keep it detached for Venue/Ready.
Hide the menu loader, reenable HUD and enable the host in one synchronous handoff
after arming the personal countdown. No GUI layer is merely covered by another
one. Extend the compiled loader receipt to assert `hud_hidden` throughout all
stages. This preserves the previous blocking-work/load-paint protocol and clocks.

## Camera gesture

The existing chase camera accepts bounded mouse deltas once per rendered input
update while LMB is held during an active ski/penalty/non-shooting transit view.
Horizontal motion wraps through360degrees. Vertical motion and final elevation
are limited, including steep grades, to avoid crossing the camera's vertical
pole. Clamp mouse spikes; preserve the existing ground/obstruction probing and
normal follow pose. On release, damp yaw/pitch and aim point back to normal.
The native automatic-frame probe exposed a pre-existing ordering issue: the
environment submitted1244trees from the prior camera while the final frame's
frustum required1210. Run environment submission after the follow/orbit camera
(execution orders200/100); replay/spectator camera composition also precedes it.
The same native probe must match current-frame tree counts exactly. This changes
ordering only, without another render pass or reducing visibility/detail.
Focus loss, pause, prestart, shooting, full tables, help, replay or spectator
takeover cancel orbit. A hold begun outside the active view requires release
before becoming a camera drag. Camera settings live in the new versioned
`camera_orbit_v1` resource. No extra camera, production render target or world
render pass is introduced. Warmed camera math/input application allocates0bytes.

[Input System's pointer implementation](https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/InputSystem/Runtime/Devices/Pointer.cs)
accumulates mouse delta over its input update and resets it on the next update.
Consume that relative delta once; do not multiply pixel movement by frame dt or
sample it per simulation tick. Time-based damping belongs only to the view.

LMB remains the trigger on the range. When an active look drag crosses into
shooting, require release of the bound trigger before its next press can fire.
Other aim/breath/bolt input and accepted shot geometry remain unchanged. No
camera angle or mouse-look gesture enters authoritative ski/shot commands or
save state. Ordinary controls, seeding, start gaps, countdown length, physique,
AI, replay schema and balance are unchanged.

Evidence and build: `docs/quality/m16-4/start-loader-orbit/README.md`.
