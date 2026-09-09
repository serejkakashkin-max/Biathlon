# ADR-0014 — Explicit physical range alternatives

2026-09-09. Extends ADR0012/0013; verification tracked by TASK-079B.

WP079's serialized firing footprint made30-athlete fields finish safely but wait unreasonably.
The final-lap route also passed through that queue, inducing thousands of sprint toggles. This
cannot be fixed by a speed multiplier or by removing shooting bodies from collision checks.

An optional schema1 PhysicalRangeLaneDefinition identifies unique open lane loops on the exact
track snapshot and their equal-length alternative to a base-route range portion. Version1 accepts
one constant terrain segment, forking at its authored start and joining at penalty access. Every
lane has exactly the same length and slope, curvature, width, snow, exposure, muscle factor,
draft/overtake flags, risk and semantic metadata as the replaced section. Unsupported alternative
physics is rejected rather than silently simulated with base-route parameters.

PhysicalRangeLaneMap binds the exact route plan and requirements. A roster slot has one fixed
lane. Mapping applies only on required shooting laps; HasRange alone denotes physical access,
not a scheduled bout. Incoming/outgoing endpoint ownership distinguishes main/branch at a seam.
No coordinate conversion changes cumulative travel, split time, penalty credit or ski forces.

PackSprintFieldSession optionally creates actor-specific projections. Entry, main exit and direct
penalty exit all require authored directed coincident connections before the first tick. Receipts
must split at branch boundaries; projection rejects crossing a seam without a split. The existing
continuous sweeps, stationary starts/suffixes, incoming tails, whole-body junction occupancy, FIFO
admission and atomic commit remain authoritative. Shooting bodies stay physically present in
their lane. The final lap uses the main bypass. Existing physical seam events record transitions.
No AI utility, pace, difficulty or shooting-control tuning changes.

The synthetic stadium topology places the range fork at800m, the mat at lane50m (base850m), and
its100m alternatives merge at base900m. Its penalty conflict region is898..902m around the actual
directed connections. The920m rejoin receipt is bookkeeping after20m of real travel; it is not a
second connection and does not reserve the whole898..922m interval. Bodies/tails and every
incident lane/penalty mouth remain covered. Legacy wide-region fixtures and acceptance checks
are preserved unchanged as regression evidence.

With no lane definition, original context/behavior remain unchanged. With a definition, append
its JSON and physical_range_lanes_v1 / unique_roster_slot_lane / last_lap_main_bypass policies to
the context. Each supported roster member must have a unique lane; oversized fields fail early.
Static slot assignments match the existing synthetic logical lane ordinals. Dynamic lane reuse,
Mass Start lane/order rules (WP087), production stadium world geometry and visual validation are
separate work. These fixtures prove headless topology and shared physics, not a rendered venue.
