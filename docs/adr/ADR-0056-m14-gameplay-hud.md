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

## M16.2 owner follow-up, 2026-10-30 — cursor and split history

The live production host owns gameplay cursor capture. Active play, personal
start preparation and TAB retain a hidden, locked cursor; deactivation/pause,
replay UI, post-finish spectator controls and focus loss release it. Reenable and
focus gain restore capture only for a live player. This also bounds mouse aiming
to relative motion inside a window. No cursor component is added per athlete and
no scene search runs per frame. The existing held-input focus boundary is shared.
Unity's [Cursor.lockState contract](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/cursor/lockstate)
documents focus/Editor resets and why locking prevents mouse UI interaction.
TAB therefore remains a keyboard/gamepad overlay; waiting-skip retains Enter/A.

CheckpointHistorySelection is detached presentation state: selected occurrence,
latest player-passed occurrence and follow mode. Opening TAB selects latest;
PgUp/PgDn browse passed occurrences, End restores follow. Gamepad uses right-stick
left/right and R3. These are overlay-only UI navigation, like existing spectator
navigation, not remapped ski/aim actions. A/D and existing TAB rebindings retain
their gameplay meaning. Labels show the actual rebound TAB control. A new split
never replaces a manually selected earlier line. Reaching latest resumes follow;
reopening TAB selects latest again. Pause/preferences retain an open selection;
rewind/new race rebases it, so unavailable future occurrences cannot be selected.

The existing immutable CheckpointTracker splits supply each table. No second
timing history, estimated arrivals or race stepping is introduced by browsing.
Occurrence ordinal distinguishes the same physical gate on different laps; the
table names its lap and cumulative race kilometres. Sprint uses elapsed time,
Pursuit/Mass Start global time, with provisional results updated for late arrivals.
An earlier line is not a frozen classification at the moment the player crossed it.
The UI marks latest/history explicitly, preserves flags and all 30 rows, and shows
disabled navigation edges before any split or at a boundary. All elements ignore
picking. The ordinary refresh cadence and closed-table cost remain bounded.

Domain, Simulation, AI, Career, Replay, tuning, save schema and command tapes are
unchanged in this follow-up. Native tests cover paging, actual rows, input edges,
no UI clock advancement, cursor lifecycle, large 4:3 layout and normal shooting
mouse input. Explicit compiled-player QA supplies ordinary AI controls only to
advance to real checkpoints, then freezes accepted views for native captures.
