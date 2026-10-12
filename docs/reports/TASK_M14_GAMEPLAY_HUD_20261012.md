# M14 — race and shooting HUD

Replaced production debug panels with retained UI Toolkit race/shooting panels
matching the illustrated menu's petrol/snow palette. Actual athlete time, lap,
checkpoint position/gap, shooting history, speed, HR, reserve, fatigue and
penalties are visible. Shooting shows five target closures, bolt prompts, breath
strain/recovery and wind. F3 toggles existing telemetry. No center reticle added.

## Implementation

- `GameplayHudModel`: Unity-free formatting of accepted observations; Simulation
  snapshot adds individual elapsed time and start countdown only.
- `GameplayHud`, HUD JSON and RU/EN catalog: retained non-interactive UI, bounded
  10 Hz updates, 100/115/130% text and independent document lifetime.
- `ProductionVenueHost`, `RangePresentation`, `CareerRacePresentation`: replace
  production legacy overlays, preserve optical camera/target updates, inherit
  player identity/preferences and support pause/resume safely.

## Evidence

`docs/quality/m14/gameplay-hud-20261012/` contains .NET results, native test
responses, transparent HUD captures at 1600x900 and 1024x768/130%, and an actual
2560x1440 career Game View capture. Checked visual layout and optical-center
clearance. Independent HUD hides during pause and rebuilds on resume.

- .NET: 15 PASS (13 model cases, snapshot deterministic comparison, race timing).
- Native EditMode: 13 model cases PASS.
- Native PlayMode: 7 PASS across HUD lifecycle/bout, three range camera/shot
  regressions, legacy race HUD, legacy debug HUD and real career launch/pause.
- Unity compiled; Console inspected. No new gameplay exception/compile error.
  MCP logged an unsupported combined class filter and a transient busy request;
  each relevant class was subsequently run separately. Earlier MCP connection
  errors are unrelated to gameplay. Test-tool total counts describe discovered
  assemblies, not the number executed; above counts use passing result entries.

Initial checks caught a test assuming no checkpoint at tick 100 (the athlete
had already crossed one) and UIDocument's null/recreated root on disable/enable.
The test now follows actual timing and the adapter rebuilds bindings on resume.
Existing failing tests were not disabled. Old M13/Pursuit issues remain recorded
in PROJECT_CURRENT; this is not a broad whole-suite or whole-season pass.

## Impact / continuation

No authority or balance changes. Existing snapshot/control audit remains equal.
HUD skips unchanged observations and legacy production formatting, but no FPS
improvement is claimed from these focused checks. Graphics quality is unchanged.
Next: WP-185 results/standings feedback and resolve known Pursuit composition
before claiming the complete career event loop. M14 remains OPEN; no build.
