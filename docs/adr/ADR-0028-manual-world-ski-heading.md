# ADR-0028 — Manual world ski heading

Status: accepted first kernel contract from explicit owner steering approval, 2026-10-05. Production integration is pending.

## Context

The owner explicitly chose full manual ski direction with A/D, then narrowed production to one polished venue. GDD sections 4 and 7 specify direction and line control. ADR-0006 stores absolute lateral targets; ADR-0021 converts live directional input into those targets. Both still follow route direction automatically. Changing a presentation rotation or renaming lateral intent cannot satisfy the new instruction.

## Decision

- Keep `PlayerCommand` v1, its parser, its JSON, existing lateral steering, race contexts and replay behavior unchanged. `ManualSkiCommand` is a separate explicit player stream v2 (`biathlon_player_command_v2`): signed `steering_input` replaces `line_intent`; fixed tick, athlete, effort and action semantics remain. Its effort-only adaptation is not a steering command. Stream selection will be explicit; a v1 row must never silently become yaw input.
- Introduce immutable world X/Z, absolute yaw and yaw rate. Yaw zero is world +Z, positive yaw turns toward +X. Steering has no route tangent, course curvature, centre line or target lateral position argument. Neutral input damps yaw rate to zero and retains the resulting absolute heading. It does not return to centre or follow a curve.
- Versioned JSON supplies maximum yaw rate, active angular acceleration, passive neutral deceleration, speed response, low-speed grip regularization and bounded path integration subdivision. The production definition is not yet selected or imported; unit-test definitions are test inputs only. Actual grip acceleration and posture response are explicit inputs supplied by the later shared speed/capability solver, equally for player and AI.
- A yaw-rate request is limited by speed, posture and lateral grip capability. Changes approach that target with bounded angular acceleration. Existing yaw momentum does not disappear when grip falls: `ExceedsGrip` and peak lateral acceleration demand expose the excess for the later speed/braking/slip solver. This kernel alone does not apply a braking force or complete the turn cost.
- Each event-free preview exposes the final pose and approximate trajectory samples for later edge/contact prefix validation. It consumes supplied nonnegative **horizontal world-plane metres**, not the legacy slope-path odometer, and never awards route progress. The first kernel integrates that planar distance with uniform density over the tick and fixed composite Simpson quadrature of continuous yaw. A fractional sample repartitions the quadrature on that prefix; it is not exact swept-body evidence or an admission guarantee. Integration must reserve its numerical approximation error or replace it with a qualified trajectory representation. Existing acceleration-dependent speed ramps, slope/planar distance conversion and boundary splits still require joint integration; sampling is not a claim that such physics is already connected. Default poses/previews are invalid and must be explicitly initialized.
- The speed argument is horizontal/world-plane speed for the yaw/grip calculation. It is not automatically interchangeable with legacy slope-path `SkiState.SpeedMetersPerSecond`. The caller must supply a consistent converted speed and later qualify acceleration-varying speed/grip demand over the accepted interval; the first frame's demand uses the supplied speed rather than an unprovided velocity profile.
- X/Z and yaw rate use ADR-0002's nine-place, ties-to-even state encoding. Heading wraps through integer nanoradians with one full turn encoded as `round(2*pi*1e9)`; this is numerical encoding, not tuning. A requested physical rate ceiling rounds toward zero so state encoding cannot exceed it. Same-runtime/build repeatability remains the guarantee; cross-runtime qualification is pending.

## Required next integration

The new kernel is not enabled in any playable host, race factory, field or replay tape. Before declaring manual control available:

1. Separate physical odometer distance from route station and lateral coordinates. Maintain continuous world pose at polyline and topology seams; project locally onto current and admitted next route occurrences, never the globally nearest course point.
2. Extend speed/terrain evaluation to actual direction and achieved yaw, including grade projection, turn cost, grip excess, safe contact prefixes and course-edge stopping. Do not silently rotate heading or centre an athlete at an edge. Resolve backward heading explicitly against the forward-only scope of ADR-0004; no positive progress may be awarded for backwards world movement.
3. Use actual heading in same-loop, junction, range apron and retained-tail body sweeps. Unchanged body dimensions cannot be replaced by route-aligned bodies that miss rotating contacts.
4. Let AI convert observed route/traffic plans into signed yaw commands and enter the same authoritative kernel. Difficulty changes controller quality, not physical ability.
5. Record every actor's signed steering and all new tuning/geometry bindings in an explicitly versioned context/tape; reconstruct exact state/events/results without running AI. Preserve v1 contexts and tapes.
6. Connect input and presentation to authoritative world pose/yaw. Validate start, course turns, penalty loop and shooting-range approach; no automatic docking exception is authorized by this ADR.

## Alternatives

Lateral-only A/D remains the accepted legacy mode but does not meet the new owner direction request. Presentation smoothing can fix visual jerks but cannot change authoritative movement. Unity rigidbody authority conflicts with the deterministic, headless, shared-physics contract.

## Consequences and acceptance

This atomic step supplies pure contracts, an allocation-free fixed-tick preview and tests. It changes no race results or balance because it is disconnected. Tests cover neutral world direction, angular acceleration, neutral subtick stopping, mirrored turns, speed/posture/grip limits, circular trajectory and prefix samples, yaw wrapping, invalid inputs, command schema isolation and long-run exact repeatability. A playable manual-steering claim requires all integration work above and native Unity validation on the single production venue.

## 2026-10-06 intermediate route/AI continuation

`ManualRouteProjectionMap.TrySampleAhead` now exposes a target on the current
selected route occurrence, or its immediate successor only with explicit branch
admission. It never searches the globally nearest lap. `ManualAiSteeringController`
turns that target into signed yaw intent using a versioned decision policy; its
output feeds the same `ManualSkiSteeringSystem` contract as player v2 input.
Target lookup and decision are allocation-free after construction. A missing
admitted target yields an invalid plan rather than an automatic turn or free
movement. The policy currently exists only as a tested schema, without chosen
production tuning or a live race caller. This does not satisfy integration items
2–6 above or authorize declaring auto turns removed in the playable build.

## 2026-10-06 intermediate AI passing-line yaw intent

The AI route controller can now aim at a caller-selected lateral passing
line on its locally admitted route target. The caller supplies an already
calculated usable half-width; the target is clamped to it. That target changes
the signed yaw request, never the accepted world pose or route credit. A
versioned context marker identifies the policy surface. This provides a
physical steering intent for a future world-roster planner; it does not
choose a safe passing side, grant body/course admission or replace the live
pack AI. The player command remains separate and unassisted.
