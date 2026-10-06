# M13 — physical speed coupled to manual heading

The old standalone heading preview distributed distance uniformly within a tick.
The added shared integration instead follows the force solver's acceleration,
stopping and speed-cap intervals. A comparative test shows that a half tick from
rest travels one quarter of the full distance, rather than half. A stopped skier
can pivot but no longer translates during the remainder of the tick.

The composed preview uses actual heading for slope and wind and actual yaw for
turning cost. It preserves absolute heading under neutral input, and does not
inherit braking from the route's centre-line bend. Inputs and physics are shared
for player and AI; no difficulty multipliers or shooting changes were introduced.

Changed systems: SkiForceTrajectory/SkiForceSystem, ManualSkiSteeringSystem,
ManualSkiMotionSystem, TrackSample's internal geometry adapter and the explicit
world-heading CorneringSystem overload. No existing live caller was switched.
See ADR-0034 for the frozen-interval approximation and required contact integration.

Validation on 2026-10-06:

- .NET focused manual/route/force/boundary/cornering suite: 48 PASS, 0 FAIL/SKIP.
- .NET legacy player, Sprint and pack replay selection: 23 PASS, 0 FAIL/SKIP.
- Native Unity force-trajectory class: 8 PASS; composed-motion class: 6 PASS.
- Warm 10,000-step trajectory repeat and composed-preview allocation checks pass.
- Unity compilation completed. Console inspection contains one tool filter error
  from an unsupported combined class selector; separate class runs succeeded.
  No project compile error was reported. That rejected selector is not a test run.

Receipts: docs/quality/m13/owner-force-heading-20261006/. The new code does not
change race output until explicitly integrated. M13 remains open; no build made.
Next: oriented course/body admission, physical contact handling, route lifecycle
and AI integration, then v2 replay/host wiring and the consolidated owner build.
