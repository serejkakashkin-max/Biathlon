# ADR-0056 — Retained race and shooting observations

2026-10-12. M14 WP-182/183, with existing checkpoint timing surfaced for WP-184.

Replace the production host's always-visible debug boxes with a UI Toolkit HUD.
Race time, lap, checkpoint rank/gap, shooting history and remaining penalties sit
at the upper left; HR/reserve/fatigue at the upper right. Contextual skiing or
shooting information occupies the lower left. The center remains free of UI.
F3 retains opt-in ski telemetry. Direct legacy component consumers remain valid.

`GameplayHudModel` formats detached accepted `AthletePresentationState` data in
RU/EN. Presentation can depend on Simulation, which remains Unity-free and does
not depend on Presentation. Snapshot time observes the athlete lifecycle rather
than deriving an incorrect Sprint time from the global race clock. Rank/gap are
labelled as checkpoint comparisons; the UI never invents a live classification.
The shooting UI observes closed targets, bolt state, breath debt/strain and wind
components in labelled world axes. It neither resolves hits nor alters sway.

The adapter creates retained elements once per layout/preferences change and
refreshes their values at a data-driven 0.1-second cadence, skipping identical
snapshot references. No new simulation snapshots or world searches are made by
the HUD per tick. Production legacy text formatting is disabled. This bounds UI
work but is not a claim about whole-player FPS or zero string allocations.

A separately owned UIDocument avoids inheriting the hidden menu UIDocument.
Disabling the race hides it; enabling rebuilds bindings because Unity recreates
the document tree. Destroying the HUD destroys its document and panel settings.
All elements ignore picking and cannot steal aiming input. Preferences follow
the menu, long names truncate visually, and large text is checked at 4:3.

No changes to movement, shot geometry, physiology, AI, replay authority, graphic
quality, event seeding or save schema. Full results, dashboards, training,
equipment and replay remain separate M14 work. No player build in this slice.

## Owner visual revision, 2026-10-12

The owner rejected the first HUD as too large and visually dated. Replace its
stacked text panels with compact corner instruments: italic race clock paired
with lap, one checkpoint line, isolated speed, and a small pulse/energy panel
at the lower right. Shooting uses five small closure rings and a two-line
weapon/breath prompt. Cool cyan highlights, muted labels, thin bars and rounded
translucent slate surfaces replace full-height boxes. No blur or extra render
passes. HUD tokens are independently tunable from the menu's large typography.

Name, history and full control help move to F1. Penalties, assisted transit and
finish/prestart status remain conditional, immediately visible messages. F3
keeps telemetry. Default measured panel bounds occupy 4.3% for the skiing
capture and 6.24% for shooting; 130% text at 4:3 occupies 12.887%. Measurements
exclude explicitly opened help/debug panels. Update cadence and authority
boundaries remain unchanged; this is a visual correction, not an FPS claim.
