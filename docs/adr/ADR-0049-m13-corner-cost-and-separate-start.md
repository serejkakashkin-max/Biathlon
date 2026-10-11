# ADR-0049 — Minimum necessary ski deflection and a separate straight start

2026-10-11. Implemented for owner review; M13 remains OPEN.

The owner requests a straight start corridor separate from the counted course,
retaining the old stadium arch as a timing portal. An optional `launch_distance_m`
in StartApproachDefinition locates the launch line inside its open corridor. Rear
rows start behind this position. Absent values retain the previous mass-start
contract. Explicit launch corridors can also serve interval and pursuit starts;
their scheduled starts and elapsed race clocks remain authoritative. Travel from
the launch to the course is paid physical distance without counted lap progress.
Exact track/route/approach snapshots continue to bind projection and replay.

Woodland uses a 50m straight staging corridor, a 40m radius merge and a final 10m
straight that preserves the exact course entry tangent. The corridor is 122.832m
long and 8m wide. Launch distance 35m puts all ten three-athlete rows on the straight.
The main lap, separate finish and penalty distance retain their existing geometry.
The old portal at the lap seam is labelled TIMING; the launch portal is START.

The shared snow solver can achieve the same yaw for several steering inputs. AI
previously preferred the largest input on such a plateau, spending unnecessary
cornering energy and slowing athletes. The planner now prefers the smallest input
with the same yaw error. This is an intent change, bound by its v3 context; shared
grip, power, fatigue and contact physics are unchanged. Presentation selects the
technical downhill coast only at the authored low effort threshold. Active skiing
continues to skate, and explicit player tuck retains priority.

Contact broad-phase exclusion uses relative initial velocity with both acceleration
bounds, rather than the sum of absolute speeds. A co-moving pack's shared forward
speed cannot close its gaps. Circumscribed bodies, rotational coverage, tolerances
and detailed checks for nearby/crossing bodies remain intact. Bend lookahead starts
at the sampled segment instead of scanning all previous segments each tick.

Repeated rigid skis, poles and rifles use GPU instancing with the original meshes
and cloned original materials. Source enabled/active state and shadow variants still
control visibility. Skinned body animation and the first-person rifle are retained.
Unsupported hardware falls back to ordinary rendering. No textures, resolution or
High quality settings are reduced by this change. Restart/disable/destroy restores
the original renderers and releases owned materials.

Fence placements are rebound outside their route edges and removed if they invade
another road. Existing snow banks and mounds remain. Local native evidence reduces
draw calls substantially, but does not establish hitch-free performance across a
complete race. Closure remains the owner's decision after playing the new build.
