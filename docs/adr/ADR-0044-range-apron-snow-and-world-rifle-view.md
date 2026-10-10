# ADR-0044 — Actual range apron snow and venue rifle view

2026-10-10. Accepted implementation of the owner's M13 correction request.
M13 owner review remains OPEN. Master Plan and IBU mat assignment are unchanged.

## Failure and decision

The owner screenshot stops at tick 4256 with `WorldRejected / Course /
woodland_basin_v1_athlete_29; candidate clock not paid`. A reproducible Mass30
case with a player waiting 500 ticks then requesting 650 effort units fails
with the same actor and error at tick 4306. Its accepted body is still on the
visible flat range apron, but the private mat hairpin alone cannot certify snow
under it after a legitimate body contact. This rejects the whole shared clock.

Use the existing flat apron rectangle as an additional conservative snow proof
for RangeApproach/RangeExit occurrences. Derive its bounds once from the exact
authored private paths, widths, sample spacing and native float vertex arithmetic.
Native tests compare all four limits with `RangeApronDrawPlan`. Bind the exact
track, visual geometry, range paths and occupancy snapshots to a fresh world.
The context records the additional authority; old replay contexts do not silently
inherit it. Neither Unity geometry nor presentation controls authoritative state.

The interior fast proof bounds a full body circumcircle and the complete
continuous movement interval. Near the apron edge the existing ribbon sweep
also considers its exact rotated rectangle margin. Its contact normal samples
the same continuous maximum of conservative certificates on both sides, avoiding
the former early-out jump at zero. No position, heading, odometer, phase, mat
assignment or clock is corrected or skipped. Body contacts, progression and
firing-plane gates still apply. No larger body tolerance or invisible new snow
shoulder was selected.

## Presentation

Remove original trees beyond the playable corridor: dense within 45m, sparse
original silhouettes through 80m. Remove the hidden timing lodge and plateau hut.
Keep existing tree IDs/poses/species and High settings. Move the event screen
onto the existing flat stadium ground; do not move the course or terrain.

Generate painted start slots from actual initial body positions, numbered mat
boards and target-bank panels, and three wind flags per lane. Flags observe real
shooting wind from detached snapshots, including hanging cloth in calm conditions.

The production shooting camera observes the venue's existing banks and uses the
project's original HeroRifle, including its native rear diopter and front post.
Its optical axis intersects the exact authoritative pre-ballistic sight ray on
the target plane for prone and standing. RMB retains fine aim; held Tab gives a
wider venue overview and restores the player model. Only one gameplay camera
renders the world. The isolated benchmark fallback remains available.

This changes presentation magnification, not shot geometry, sway, assistance or
hit rules. Easy-shooting tuning remains an M15 review issue; there is no hidden RNG.

## Practical art acceptance

Per the owner's repeated instruction to avoid excessive art precision, snow
footing placement uses a 2.5cm appearance allowance. Noncollidable tape retains
a 1m minimum separation from the paid corridor; rigid scenery retains 3m.
Exact render joins and simulation tolerances retain their existing checks.
These limits do not alter any gameplay collision rule. No failing test is removed.

## Consequences and validation

Race paths after apron contacts may differ: the physically present common snow
surface now allows ordinary recovery steering. Signed command replay binds and
reproduces this geometry. Focused native and desktop evidence is recorded in
`docs/quality/m13/range-world-review-20261010/` and the accompanying task report.
Hidden checks establish CPU/control correctness; sustained GPU FPS remains for
the owner's visible playtest. This one-venue footprint supports the current flat
+Z range; a differently oriented future venue requires its own bound geometry.
