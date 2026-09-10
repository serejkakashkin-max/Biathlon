# TASK-085A — Mass Start grid and common gun

Status: DONE — 2026-09-10. WP085 remains IN PROGRESS; next TASK085B.

Added MassStartGridDefinition (schema1/stable ID), immutable MassStartGrid and common
SimultaneousStartScheduler. The provisional layout has three columns,2m column spacing and3m
row spacing. Thirty roster slots form ten rows, with rear centers27m behind the front-row
reference. This is synthetic project data, not an asserted official grid specification.

Grid construction binds the full competition/body/definition/area/roster context and validates
format, capacity, stable unique IDs, quantized spacing, body clearance and area margins. Slot
coordinates are signed relative to the front-row center. CreateLaunchPlacements translates them
to nonnegative pack-kernel positions using an explicit FrontReferenceNanometers; translation is
never credited as travelled route distance. Roster order is caller-owned seeding, never hidden
performance sorting. Clock context includes grid, frequency and common first tick.

The whole field receives ScheduledStartDue in the same tick. Capacity for every simultaneous
starter is validated before any scheduler state/event mutation. Thirty CompetitionLifecycle
instances start at that exact tick. Invalid clocks/tick order and malformed grids reject.

Five tests cover slots/body geometry, context roundtrip, full simultaneous lifecycle start,
invalid inputs/capacity, repeat events and zero allocations over warm scheduling. A separate
integration within the fixture passes all30 grid placements through the existing PackForceSession
for1200 ticks at20Hz. First tick supplies zero power (common gun); subsequent commands supply
identical350W test power and fixed lanes. Both runs match, every relative swept movement stays
clear, and every athlete travels over100m. Minimum travelled distance is504840986125nm and trace
FNV64 is80c93f9e09952b64, matching .NET/Unity. Actual travel subtracts each initial coordinate.

Validation: .NET15/15 (grid, pack kernel, interval and Pursuit starts), Unity MCP grid5/5.
Console0 errors,4 retained MCP warnings; no compilation failure. Current discovery472.
No new full472 or PlayMode/visual run is claimed. Existing race code/tuning was not changed.
Evidence: artifacts/physiology/wp085a-final.trx, wp085a-unity-final.json, wp085a-unity-results.xml,
wp085a-console-final.json; compact verification under docs/quality/wp085a.

Limits: fixed supplied-power start-corridor kernel test, not full physiological AI Mass Start.
The actual approach-to-route composition remains TASK085B. RacePathMotion currently rejects
negative progress. Integration must retain common clocks, physical rear-row travel, physiology,
speed, fractional boundary time and replay state. Never teleport rear rows to route zero or
reset their physiology. Do not mark WP085 complete until that handoff is implemented/tested;
WP086 PPSS composition and M7 are still open. No owner blocker or scope change.
