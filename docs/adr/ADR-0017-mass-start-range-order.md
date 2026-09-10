# ADR-0017 — Mass Start range order

2026-09-10. Optional versioned MassStartRangePolicyDefinition extends ADR0014/0016.

IBU ECR November2025 Article8.2.1 assigns the first bout by start number and later bouts in arrival
order from the right. Source (accessed2026-09-10):
https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/3193632d7b17912ec4a12fc321cecac3/Rules_2025_EN_cap03.pdf

The synthetic physical fork is the range admission line. Lane indices are authored right-to-left.
Its exclusive junction permits one accepted entrant per tick, so accepted tick order is unambiguous;
existing FIFO/physical priority resolves contenders before entry. The first bout retains bib lanes.
Each subsequent bout assigns lanes1..N in accepted arrival order. Different bouts may overlap in
time; physical branch occupancy still prevents admission into an occupied lane/exit footprint.

All actor/lane projections and logical assignments are preallocated. Lane choices and arrival
counters are scratch until final trajectory validation. Capped proposals do not consume an ordinal.
A precision fallback resets tentative counters, rebuilds the retained path with the committed next
lane, and rechecks it against stationary bodies. Logical shooting assignment and physical mapping
commit together before the athlete reaches the mat. Assignment events are replayed, including bib
lanes on the gun tick. No simulation force, shot geometry, capacity bonus or body removal changes.

A new bout becomes logically active before the previous physical range exit is complete. Therefore
lane selection must wait until the next fork is actually reachable in the current movement preview.
Changing its map earlier would teleport an exiting athlete between lane branches; a regression
caught this and the corrected guard is covered by a complete repeated race.

Without an explicit policy, legacy fixed synthetic lane behavior/context remain unchanged. Ordered
policy requires a Mass Start grid and enough authored physical lanes. This supports complete fields
of up to30 with four bouts, not Mass Start60 or juries/withdrawal rules. Production world geometry
must preserve the authored entry ordering and right-to-left lane numbering.
