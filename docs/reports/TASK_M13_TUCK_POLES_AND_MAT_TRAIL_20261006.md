# M13 — tuck pole clearance and separated mat guidance

2026-10-06. Additional owner feedback within the open M13 batch.

The compact tuck puts the hands close together; straight rearward poles crossed
the skinned torso. A triangle intersection test on the shipped hero reproduced
the defect at 0.759 m behind the grip. The presentation definition now splays
each shaft outward by 25 degrees while preserving grip, length and airborne
pitch. The same test passes on flat ground and two descending grades. It tests
the shaft centreline after the first 10 cm of intentional hand contact; it is
not a general cloth/contact solver or proof for every animation transition.

Assigned-mat chevrons now use a presentation-only corridor with two metres of
additional clearance from the mat row, then a curved lead toward the selected
mat. Every arrow footprint stops outside the rear edge with a visible snow gap.
Three small flashing arrows also remain outside that edge. The redundant fork
overlay is suppressed while the selected-mat trail is active. Early appearance,
lane selection, bounded docking assistance and penalty branch indications remain.
No mat-number text was reintroduced. The curve is an original mesh arrangement;
no asset was extracted from the reference game.

Native Unity PlayMode results: ground/contact/tuck 5 PASS; actual Woodland early
guide plus range/penalty assistance 2 PASS. The guide regression follows the
approach into shooting, checks all visible mesh vertices for mat clearance,
observes the curved turn and flashing phase, and rejects duplicate fork arrows.
The original 1075-test frozen traffic result is a separate earlier checkpoint;
these presentation-only changes do not alter deterministic race output, physics,
shooting hit geometry or difficulty. No additional full simulation run is needed.

Both changes were visually inspected in Unity. The ad-hoc guidance capture call
timed out at the MCP transport despite writing its image; automatic tool retries
later executed the helper in Edit Mode and logged Destroy-in-EditMode errors.
These are capture-helper errors, not successful clean runtime evidence. The
temporary roots were removed, the original scene retained, and Unity confirmed
non-playing, non-compiling and sceneDirty=False. The native test receipts, which
predate that helper, remain the correctness evidence. The captured guide image
is only a layout illustration; it includes background actors from the open scene.

Portable receipts and images: `docs/quality/m13/owner-batch-20261006/`.
No Windows build was made. M13 remains open: physical steering/contact integration,
player traffic delays and terrain readability still require work before the
consolidated owner review build. PROJECT_STATE history is only appended.
