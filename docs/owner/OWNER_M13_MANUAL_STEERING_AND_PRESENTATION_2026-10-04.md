# Owner M13 steering and presentation directive — 2026-10-04

Status: owner-authorized implementation; acceptance OPEN. Master Plan and approved G1/G2 remain in force.

The owner reported abrupt lateral torso movement, abrupt automatic course turns, sparse venue dressing and too many long straight sections. The local reference installation is `C:/game/Biathlon 2006/Winter`; it is a read-only comparison source. Its assets, animation data, branding and course files are not licensed for import into BIATHLON.

The owner explicitly answered the steering clarification: **«Полностью ручное руление направлением»**. A/D and the gamepad direction input must control the athlete's ski heading. This authorizes a change to the previous route-following live control direction, rather than requiring another G1 approval. Animation rotation or lateral-only steering does not satisfy this request.

Required work within the continuing M13 delivery:

- Correct measured near-athlete torso/support-transfer jerk and discontinuous tuck transitions while retaining leg support, pole rhythm and shared authoritative physiology.
- Introduce real fixed-tick, data-driven manual ski direction, with observable heading/turn state, physical course/body constraints, costs and deterministic replay. AI must obey the same physics. Preserve legacy saved command meanings with an explicit versioned contract.
- Validate downhill, technical turns, input release and left/right reversal in normal Unity gameplay. Manual mode must not silently follow the route, return to its centre or rotate at polyline knots.
- Use the existing venue pipeline to add appropriate buildings, fencing, flags and spectators, and address monotonous course sections. Keep distinct tactical/visual identities and validate every race-affecting route change.
- Report each coherent result in PROJECT_STATE and GitHub. Continue toward the M13 gate; no additional inter-task owner confirmation is needed.

Standing constraints remain: hidden-only standalone checks; no hidden shooting RNG, silent shooting rebalance, physical clearance shrink, teleporting or presentation authority. G2 is approved and is not reopened. The [2026-10-05 owner scope directive](OWNER_SINGLE_VENUE_SCOPE_2026-10-05.md) subsequently defers WP170–174 and selects Woodland as the sole production venue. Pine Mass/Pursuit defects are preserved in deferred evidence; WP170 is not DONE.

Evidence and reference limits are recorded in [motion/reference review](../quality/m13/owner-motion-reference-review-20261004.md). The implementation ADR will define the changed control, body-heading and replay contracts before architectural changes are accepted.
