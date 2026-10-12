# ADR-0060 — M14 fair career starts and owner controls

2026-10-12. Owner-requested correction; M14 acceptance remains pending.

The owner could win a new career Sprint with a miss by more than one minute,
still ran wide with full steering, and requested clear timing gates, start preparation,
an effort dial, junior starts, World Cup bibs and equipment/pulse graphics.

## Shared authority and balance

`PackAiRaceRunner` now applies the prepared entrant's career form and pressure at
the player boundary as well as the AI boundary. Previously the live player supplied
form 1 while AI supplied the actual fatigue-adjusted career form. Device input is
intent; readiness belongs to the entrant and cannot be overridden by the adapter.

Production venue AI uses explicit resource references for pace, pre-range recovery
and shooting strategy. Rivals no longer spend excessive time coasting or waiting
for perfect aim. Physics, resources and geometric hit resolution are shared.
All-out effort retains reserve/fatigue/HR costs. Fixed-seed novice Sprint tests use
the real production field with precise manual route/aim intent against 29 AI peers.

New careers choose a junior novice (18–20) or senior debut (21–30) and age at
December 31. Ability is correlated, with bounded specialization; novice peers span
several actual ability levels. Junior/development/World Cup calendars are explicit.
Existing weekly training/growth applies to every athlete. Aging out and earned
promotion change circuits after season archival; old save fields retain defaults.
This is a playable league abstraction, not a claim to implement every IBU qualification.

World Cup uses September 2026 IBU points and full placing countback. Yellow is
the total leader; red is the current discipline leader; both roles combine visibly.
No invented leader appears before qualifying results exist. Open/local saves retain
their prior scoring; old races do not acquire fabricated defending discipline leaders.

## Steering and pace

An actual-drive regression reproduced loss of the opening Woodland corner at
10 m/s: old novice grip limited lateral acceleration despite full requested yaw.
The shared gameplay grip range is now 4.5–7 m/s². The existing bounded, dissipative
snow impulse solver remains unchanged; no road-centre or tangent steering is added.
Very tight turns still require a sensible speed. S provides visible braking.

W raises the stored pace dial; release holds it; S lowers it and requests the shared
physical brake. At zero, the brake remains applied rather than propelling the skier.
Focus loss sends zero analog intent/brake instead of the previous recovery preset.
Range automation retains ownership over its approach/departure. Commands record
all intent, including braking; the UI displays the actual chosen dial position.

The data-defined 3-second countdown advances presentation time only. Authority,
device command clocks and the race accumulator remain at zero through the GO
boundary. Pausing deactivates the host, so preparation does not run in the background.

## Presentation and cost

Keep the existing snow/ink/teal identity and menu background. The added controls
form a short athlete setup, not a text page. Pulse is a small retained vector heart
whose beat frequency follows observed HR. The effort bar is compact and explicit.
The shop and actual athlete skis read one palette catalog; material variants are
cached and retain GPU instancing. Bibs reuse per-athlete property blocks.

Three rectangular timing gates and blue snow stripes coincide with authoritative
checkpoints. The last is after the penalty rejoin. Gate supports sit outside the
corridor and are grounded once. The finish gantry widens laterally to 14 m without
raising the thin ground paint. No added colliders or per-frame geometry rebuilds.

## Sports sources

- [Official IBU event rules page](https://www.biathlonworld.com/inside-ibu/sports-and-event/event-competition-rules-biathlon).
- [September 2026 rules](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf):
  §1.3 age classes/younger athletes competing higher; §3.5 finish corridor;
  §9.2.7 penalty timing; §15.7 leader bibs; §15.8 World Cup points/countback.

Gate appearance is an owner-requested visual convention, not a rule imposing
exactly three overhead structures. Validation and final build evidence are recorded
in the owner correction report and append-only production state.
