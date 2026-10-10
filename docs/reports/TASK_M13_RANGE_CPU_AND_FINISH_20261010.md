# M13 — Remaining range hitches and independent finish

2026-10-10. Owner-directed correction after the RangeHandoff build. M13 remains
OPEN for the owner's player review; M14 has not started.

## Changes

The confirmed remaining CPU spike comes from whole-body course checks near the
stadium snow shoulder after shooting. A single owned convex capsule now first
proves the complete athlete rectangle; uncertain cases retain the existing
detailed proof. No collision/body dimensions or tolerance values were relaxed.
Diagnostic sample/query counters expose expensive future cases.

Distant shooters retain ordinary distance-based skin and pose detail instead
of forcing full detail throughout the range. Nearby player setup remains fully
animated. Phase changes still update immediately. The target stage no longer
cycles active state every paid tick. The chase camera stops rendering the whole
world behind the opaque aiming camera and returns for setup/recovery/departure.

The finish now leaves the final lap on the left outside edge of the stadium.
It has a 9 m wide, 63.296 m straight approach, three marked lanes over the last
50 m, a red finish line and 30 m of runout. Its whole approach/runout stays at
least 133.03 m from the penalty centreline. Start and finish are 217.038 m apart.
The recipe updates the geometry, three format routes/profiles, junction,
environment corridor, finish gantry and 40 obstructing canopies. Earlier laps,
range movements, manual steering and IBU lane selection are unchanged.

Research: [IBU September 2026 rules, 3.5.1/3.5.1.1](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf)
specify a straight 50–75 m finish approach, 9 m width/three lanes and at least
30 m of unobstructed finish zone. The [official Lenzerheide stadium plan](https://www.biathlon-lenzerheide.ch/microsites-assets/biathlon-lenzerheide/pdf/infos-besuch/trib%C3%BCne-wm.pdf)
informs separate finish/transit/range areas; this is our fictional layout.
The [official Hochfilzen course/stadium resources](https://www.biathlon-hochfilzen.at/course-maps.html)
were also consulted. This change makes no whole-venue homologation claim.

## Bounded evidence

Evidence directory: `docs/quality/m13/setup-finish-20261010/`.

| Desktop actual Mass30 simulation diagnostic | Before | After |
|---|---:|---:|
| Whole-run mean tick | 6.131 ms | 5.892 ms |
| Worst tick | 320.488 ms | 84.864 ms |
| Mean while athletes are setting up | 10.827 ms | 9.155 ms |
| All 30 first bouts completed | tick 6124 | tick 6124 |

The run includes at least 600 ticks after the player's first completed bout and
continues until every athlete has completed one bout. The initial certificate
proposal requiring positive clearance alone did not help; localized counters
showed that the expensive bodies were within the existing numerical shoulder
tolerance. The final certificate tests the existing continuous-proof threshold.
Receipts: `cpu-before.trx`, `cpu-localized.trx`, `cpu-certificate.trx`, compact
`cpu-comparison.json` and sampled inclusive `cpu-hotspots-before.json`.

- .NET: 30 focused ribbon/finish/physical completion cases PASS.
- .NET: complete signed Mass3/Sprint1/Pursuit3 replays, 3/3 PASS. Every accepted
  world/resource event reproduces without AI.
- Unity MCP PlayMode: 8/8 selected cases PASS across AthleteDetail, AthleteRifle,
  RangePresentation and production environment/finish. These cover actual
  shooting state, far/near detail, immediate pose phases, allocation-free warm
  pose scheduling, chase camera ownership and upward finish paint normals.
- Native High Game View: `stadium-finish-overview.png`, `finish-straight.png`,
  `finish-view.json`. The frozen simulation frame is visual evidence only.
- Windows Unity 6000.6.0f1 build: Succeeded, 0 errors / 0 warnings; High default.
- Hidden Mass30/Sprint30 player smoke: PASS, EXIT0; 400 paid ticks per scene,
  initialized content, quality selection and scene transition verified.
  Rendering was suspended in the hidden window; this supplies no FPS evidence.

Two missing namespace imports were corrected before final test runs. A capture
probe initially serialized Unity Vector3 recursively; the probe now serializes
plain x/y/z and both captures succeeded. These were development/probe errors,
not retained player faults. The original new owner log has optional stripped
URP shader warnings and no exception storm; it has no frame-time history.

The final Console inspection finds the MCP plugin's own log-clear IOException
(locked `Temp/mcp-server/ai-editor-logs.txt`), including one retry after player
exit. Console clearing is unavailable in this editor session; raw logs are not
claimed empty. These entries belong to Console / Clear Logs, not game code.
Final compilation, native tests and player build have no compiler/game errors.

## Determinism and limitations

The ribbon context is versioned v10. Certificate choice can alter resolved
intervals; the final-lap course is 124.452 m shorter than the previous spur
recipe. Updated route/profile/context identities are intentional. No athlete
power, shooting geometry, difficulty or RNG changes were made. See ADR-0043.

CPU timing here is a diagnostic in desktop .NET, not native rendered FPS.
The remaining 84.864 ms diagnostic peak does not establish hitch-free play.
Only hidden player checks are authorized; actual perceived setup and departure
smoothness remain for the owner to confirm in this build. G1/G2 remain approved;
one Woodland venue, max30 and High default remain. Easy-shooting feedback stays
open for M15. PROJECT_STATE history is preserved and only appended.

## Delivery

The new Windows build is `Builds/WoodlandM13SetupFinish-20261010/`.
Launch `START_WOODLAND_REVIEW.cmd`; Russian controls/review notes are in
`READ_ME_RU.txt`. Build and hidden-smoke receipts are `build-summary.json` and
`player-verification.json`; executable SHA256 is
`718444f6a718fa668da96eb0a58445b009673310e6129152a63dbf7a92b8de00`.
Next action: owner checks setup, the first-bout departure and the final-lap
finish, then decides M13 closure. No automatic transition to M14.
