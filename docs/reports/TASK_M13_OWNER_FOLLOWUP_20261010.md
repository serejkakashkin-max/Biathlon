# M13 owner follow-up — reached mats, reload, reserve and shorter course

2026-10-10. Implemented for owner testing. M13 remains OPEN; M14 has not started.

Owner build: **Builds/WoodlandM13OwnerFollowup-20261010/START_WOODLAND_REVIEW.cmd**.
Russian controls and review notes: READ_ME_RU.txt beside the executable.
Supersedes WoodlandM13OwnerControls-20261010. One Woodland venue, maximum30 racers.

## Changes

AI effort rewrites now preserve the complete shooting command, including the
manual-bolt policy. That lost policy caused later bouts to accept repeated left
clicks after one reload. Manual sessions apply handling movement on accepted
reload, not discharge. Ordinary deterministic breathing/sway continues between
shots. Physical 50m/115mm/45mm geometry and hit resolution remain unchanged.

Mat capture now requires the actual athlete position to be within the existing
0.5m tolerance of the assigned endpoint. A terminal projection several metres
beyond the mat cannot start shooting. Bounded steering can recover missed mat
and exit targets. Automatic exit skips scripted closing-distance braking for a
moving athlete; physical contact impulses can still change speed.

New versioned production definitions increase breathing/heartbeat/muscular sway
and reduce reserve recovery from500W to120W. Anaerobic drain becomes2400W;
fatigue gain/recovery become0.00045/s and0.00006/s. Existing career stat curves
still modify capacity, recovery and fatigue. AI and player share these models;
AI chooses tuck using its downhill decision limits. Player-held tuck remains free.

The main lap is **960.005m**, down20% from1200m. Forest depth and elevation are
compressed together; stadium dimensions and the150m penalty circuit remain.
Route stations, profiles, junction guards, directed passages, bank windows,
roadside objects, trees and ground are rebound. Initial bodies sharing the
straight start road retain physical contact checks without false junction queues.

Penalty board feet sit beyond the road edge, and boards overlapping a connecting
road are omitted. Forest authoring removes334 trees; conservative whole-grove
frustum rejection avoids processing every tree in invisible groups. No new assets
or hidden race speed multipliers are introduced.

Decision and exact tuning: [ADR-0046](../adr/ADR-0046-owner-m13-range-reload-reserve-and-short-course.md).

## Validation

Evidence: `docs/quality/m13/owner-followup-20261010/`.

- `controls-replay-cpu-final.trx`:19 focused .NET cases PASS, including control
  preservation, reload handling, sway/breath/HR, recovery/stat curves, physical
  mat admission and deterministic shooting replay.
- `arrivals-and-third-final.trx`:2 integration cases PASS. All30 athletes start
  first shooting within0.5m of their assigned mat; worst measured error0.4892m.
  The actual third manually controlled bout blocks100 further left clicks until
  another reload.
- `cpu-final.trx`: explicit30-athlete CPU diagnostic PASS. All30 first bouts
  complete within5270 ticks; mean1.502ms/tick, setup1.957ms, worst104.641ms at
  cold tick1. No >100ms tick after1 in this bounded workload.
- Final context revision:3 additional arrival/third-bout/replay cases PASS.
- Both playable formats:2 additional30-athlete/400-tick startup cases PASS.
  Paid millimetre rounding required rebinding four branch endpoints by at most
  0.281mm to their exact main-road samples. The initial smoke caught the Sprint
  mismatch; the corrected candidate retains the original portal checks.
- Native Unity: range capture/departure2, production composition5, forest9 and
  vegetation3 cases PASS. The forest shadow expectation now follows actual
  distance/LOD after shortening rather than assuming no nearby shadow sources.
- Venue validation PASS with0 diagnostics. Native first-arrival and penalty
  junction captures inspected. Recent post-build Console has no errors/exceptions.

Final Windows build Succeeded, Unity6000.6.0f1,0 errors/0 warnings,
355699018 bytes. Hidden Mass30/Sprint30 startup,400-tick controls and scene
transition smoke PASS, exit0. Hidden rendering is suspended and supplies no FPS
or GPU evidence. Build and executable hashes are retained with the smoke receipt.

## Impact and limits

Race output intentionally changes with course geometry, production physiology,
sway and arrival/control fixes. Replay contexts identify changed policies;
historical provisional definitions stay intact. Deterministic replay remains
covered. Physical contacts are retained, so a collision can exchange momentum.

The CPU diagnostic and native captures do not certify hitch-free visible Windows
play, a full race, or final balance. Owner tests shooting preparation, the first
post-range section, off-course movement, recharge cost and shorter pacing.
The next action is owner acceptance or specific feedback; no automatic M13 closure.

PROJECT_STATE remains append-only; its2933802 pre-task bytes are preserved and
verified against the recorded SHA256 prefix.
