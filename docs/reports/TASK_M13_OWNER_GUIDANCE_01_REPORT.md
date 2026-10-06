# TASK_M13_OWNER_GUIDANCE_01 — Assigned mat, forks and bounded range assistance

2026-10-05. Status: implementation, focused/native behavior and production views PASS;
shipping full regression/three-format replay/hidden Windows checks IN_PROGRESS.

Original pooled cyan chevrons follow the exact selected physical race path. They show
the reserved mat, pulse within3m, identify real format/private-mat forks, and distinguish
another required penalty loop from its final exit. They never change a route or reserve
a lane. No Biathlon2006 resources were imported.

Data-driven assistance replaces intent only during the final3m on the assigned private
mat path and the first1m after shooting. Ordinary shared physics handles braking,
forward travel, body-safe lateral work, arrival and departure. Recorded commands remain
replayable. Manual controls return immediately after the bounded departure. Core and
presentation responsibilities remain separate.

Five focused core tests PASS, including all three format branches, exact boundary
selection, validation and allocation checks. Native EditMode5 PASS; final native
PlayMode1 PASS exercises assigned-lane arrows, paid arrival/departure, returned
manual input, continued penalties and final penalty exit. This is a fixture integration
test; actual Woodland GameView is reviewed separately.

Initial actual High view found overlapping oversized mat arrows and an oversized label.
The final version uses three small separated mat chevrons and a smaller label. Fork
arrows retain their readable full size. Final normal native High captures are
`native-delivery-mat.png`, `native-delivery-mat-assist.png` and `native-delivery-fork.png`
under `artifacts/owner-traffic-guidance-20261005/`. The actual fresh Woodland host has
one host/HUD and the authored environment. At tick18510 it shows3 small mat chevrons,
flashing=true, lane1, final-approach assistance at2.188151m. Paid arrival occurs at
tick18517. Departure assistance runs23 frames and manual commands return after
1.147109812m of paid travel(recorded effort0/lateral0.85). The next route fork is
selected from the actual format/private mat/remaining penalty obligations, and an
approach indicator stops before the firing position. Earlier oversized-mat and
duplicate-host scratch views are retained as rejected evidence, not final views.

Files: RangeGuidanceDefinition/resource; PlayerRangeAssistanceSystem/RaceRouteGuidance;
PlayerRouteGuidancePresentation; ProductionVenueHost adapter; EditMode/PlayMode tests.
The pure read-only indicators have no deterministic impact; explicit approach/departure
commands may change player travel and are accepted by the same simulation and replay.
Full manual-heading integration and production UI polish remain separate OPEN work.


## Shipping evidence and owner correction — 2026-10-05

The frozen implementation(a79e4cc/58567d9/c9a660c) passed the full core suite:
1067/1067, zero skips. Fresh completed Mass30(23087ticks), Sprint40(43157ticks),
and actual-Sprint-qualified Pursuit40(46092ticks) all passed persisted exact
state/event/result replay without AI. Final native all29 stationary-start bypass,
actual qualified Pursuit40, guidance and build-lease checks passed.
The Windows Development/High build was delivered and subsequently played by the
owner. Hidden startup/400paid-tick checks passed for all three formats; rendering
was suspended, so these checks are not sustained GPU/FPS evidence. Build reported
0errors and496warnings(36Sentis pixel/460ConvGeneric compute shader warnings).
Owner23 disk paths and6 native settings assets/dirty flags were verified preserved.
Portable replay receipt hashes: `docs/quality/m13/traffic-guidance-shipping-20261005.json`.

Owner hands-on review requests additional changes. Long merge delays remain
OPEN(Sprint actor16 waited2003ticks/100.15s at Main150.106m). Early mat navigation,
tuck pose and visible shooting preparation require correction. Full manual direction
is still disconnected from the playable host. No whole-traffic or M13 closure is
claimed. New work follows the owner directive permitting rare physical contacts;
the previous strict no-contact fixtures remain valid legacy evidence.
