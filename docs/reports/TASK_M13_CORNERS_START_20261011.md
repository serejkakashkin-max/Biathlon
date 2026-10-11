# M13 — Corner performance, skiing pose and separate start

Owner test candidate, 2026-10-11. M13 OPEN; M14 not started.

Build: `Builds/WoodlandM13CornersStart-20261011/START_WOODLAND_REVIEW.cmd`.
Maximum 30 athletes, High default. Previous builds retained.

Changed systems:

- AI steering selects less ski deflection when achievable yaw is identical. A
  reproduced plateau now uses -478 rather than -1000, with the same actual turn.
  In bounded first-lap .NET traces, actor 0 median speed at main 60–130m changes
  3.52→6.59m/s, and at 650–735m 3.46→8.34m/s. Changed traffic/start states mean these
  are practical observations, not an isolated physics comparison or speed bonus.
- Rigid athlete equipment is instanced with unchanged meshes/material textures.
  The per-frame adapter skips redundant visual route sampling when an accepted
  physical surface is already supplied. Camera and pose work were measured
  separately; the camera interpolation itself was not the large CPU cost locally.
- Conservative contact exclusion retains cancellation of co-moving velocities.
  Distant pack rows require no interval search; actual contacts remain possible.
  Bend lookahead also skips segments behind the athlete.
- Technical downhill coasting no longer overrides active skating. Its pose is
  shallower; player Space tuck still has priority on every grade.
- Separate 8m-wide start: 50m straight, 40m radius merge, 10m tangent join. All
  mass-start rows are on the straight. Start approach length 122.832m, launch 35m.
  Interval starts use the same portal. The retained old lap portal reads TIMING.
  Main lap 960.005m, penalty 150m/6m width and separate finish remain.
- Rebound 1368 retained tape placements outside road edges; removed one crossing
  another road. Added start tape and removed one tree from its corridor. Existing
  snow humps/shoulders preserved. No further texture/quality reduction.

Diagnosis and evidence:

The initial hypotheses were dense contact work, excessive independent equipment
draws, and camera/pose transition work. Native instrumentation separated simulation,
snapshot, pose, camera and range calls. A first S-shaped start experiment caused
expensive dense contacts and was rejected. The final gentle merge and relative
velocity exclusion removed that regression. `live-after.csv` is that rejected
experiment; `live-final.csv` is the shipping candidate. `live-error.txt` belongs to
an earlier invalid probe that unbound a previously controlled player clock; it is
not final runtime evidence.

Bounded native Mass30 capture (RTX5080, original High profile):

| Work | Before | Final |
| --- | ---: | ---: |
| First bend mean draw calls | 1063 | 517 |
| Setup mean draw calls | 1966 | 1254 |
| First bend mean pose work | 1.94ms | 1.40ms |
| First bend mean simulation/tick | 3.12ms | 3.49ms |
| Setup mean frame duration | 16.67ms | 16.67ms |

One tick/frame is used around the first bend and setup, four elsewhere. Pack states
and start geometry change, so these are bounded captures, not a controlled FPS
benchmark. Final first-bend frame p95 is about 19ms with a roughly 37ms maximum;
the cold start capture also contains an initialization spike. No complete-race or
universal hitch-free claim. `summarize.py` reproduces `native-summary.json`.

Validation:

- .NET focused suite: 37 PASS (sweep, snow steering, approach and first-lap trace).
  Additional final signed-start replay cases: 2 PASS, recorded controls replayed
  without AI and matching poses, motion and physical events. AI decision-only
  journal entries are separate from physical tape events. No failing test disabled.
- Native PlayMode: equipment 1, locomotion 5, course surfaces/ground 8 PASS.
  MCP class filters require short fixture names; qualified/combined attempts ran
  zero tests and are not counted as successful validation.
- Fresh runtime Console inspection: no new errors/exceptions. Native captures
  `start-final.png`, `downhill-final.png`, `last-bend-final.png` include original
  High scenery and show the corrected portal/fence positions.
- Windows build Succeeded, Unity 6000.6.0f1, 0 errors, 0 warnings, 355747001 bytes.
  Mass30/Sprint30 paid 400-tick controls and real scene transition PASS, exit 0.
  Smoke rendering was suspended in its hidden window; it is startup/control evidence.
  Receipt: `artifacts/production-player-smoke/20261011-075017-277/verification.json`.

Decision: [ADR-0049](../adr/ADR-0049-m13-corner-cost-and-separate-start.md).
Evidence: `docs/quality/m13/corners-setup-start-20261011/`.
PROJECT_STATE history is preserved; a dated record is appended only.
Next: owner tests the crowded first bend, prone setup and new start, then decides
whether M13 can close.
