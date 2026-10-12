# M14 — continued-career Mass Start order and leader bibs

2026-10-12. M14 remains **OPEN / owner testing pending**.

The saved roster always placed the player first, regardless of season ranking.
Leader bibs were gated to World Cup only, incorrectly suppressing the overall
leader in the development series. Two focused tests reproduced both failures
before changes; no existing save was edited or migrated.

## Delivered

- WP-181: pre-race Mass Start bib number shown in the menu; one stable-ID start
  list supplies the physical grid, bibs and first shooting lane. Numbering runs
  right to left facing the start. Later mats still use actual arrival order.
- WP-189: development series shows its overall leader in yellow, with no
  invented red discipline leader. World Cup retains yellow/red/combined roles;
  the first Mass Start has no current-season red leader until a Mass Start has
  actually been held. Legacy Woodland series uses its explicit game policy.
- Calendar policies and meeting IDs select the correct priority block:
  WC 20 overall-ranked starters, development 15, remainder by current meeting
  points/best result/overall order. Local/junior prototype uses its season table.
- WP-191/192: new replay metadata records start_order_version=1. Old metadata
  preserves old roster/grid composition. Manual contact definitions selected by
  recorded weather are shared by live, headless and replay initialization/seek.
- Reordered ice Mass Start revealed a penalty-edge failure at tick 3242. The
  bounded direction probe exhausted its 32-response budget. Icy weather now
  selects the existing exact normal algorithm through a versioned definition;
  ordinary snow retains fast probes. No admission checks, body sizes, course
  geometry, texture quality, athlete abilities or hidden bonuses changed.

Read-only probe of the owner's current development save: **overall rank 7 →
start number 7**, one yellow bib, no red bib at the first Mass Start. The older
open-series save remains rank 1 → start number 1. Evidence contains only these
classification values, not personal profile contents.

Systems/files: CareerCalendar, CareerStandings.BeforeStart, CareerLeaderBibs,
CareerMassStartSeeding, CareerRaceProvider, ProductionVenueRaceFactory,
MassStartGrid, replay metadata/adapters, ProductionVenueHost and pre-race UI.
Data: four production calendars, recorded weather and ice-contact definition.
[Decision and official-rule references](../adr/ADR-0064-m14-mass-start-seeding-and-circuit-leaders.md).

## Validation

- 56 .NET core/career/grid/ribbon/journal/replay checks pass.
- 8 native EditMode seeding/classification/journal checks pass.
- 3 native PlayMode checks pass: continued-career launch, actual rendered yellow
  bib and grid; existing replay navigation; icy Mass Start replay/backward seek.
- Complete 30-athlete icy Mass Start repeated after save reload: **28,344 ticks,
  600 shots**, identical authority result and events. SHA256
  `a264c7465cb01209d59cd825a4368e44f67fd0d02e84bd7a8eb96d7eeb35c89e`;
  event hash `16558113876112233285`. Start-order changes legitimately affect
  racing positions; stats and race balance tuning are unchanged.
- Grid and close-up leader screenshots inspected. The actual pre-race fixture
  retains a zero countdown/race clock and leaves its save unchanged.
- Final Unity compilation/Console and Windows build/startup evidence recorded
  in [mass-seeding-20261012](../quality/m14/mass-seeding-20261012/).
- Windows x64 Development/DX11, Unity 6000.6.0f1: Succeeded, zero errors and
  warnings, 370,488,662 bytes. Standalone startup exits zero, renders 1600x900,
  success=true. Graphics quality preserved.

Launch: [START_M14_TEST.cmd](../../Builds/M14MassSeeding20261012/START_M14_TEST.cmd).

## Limits / next step

The prototype has a fixed eligible roster, no extra qualifying-point list and
no complete Junior Cup Mass Start 60 model. True unresolved number ties retain
stable table order. No previous-season defending champion is fabricated.
Existing compatibility checks continue to reject unrelated changed replay
definitions. Next: owner tests this build and decides further M14 corrections
or acceptance.
