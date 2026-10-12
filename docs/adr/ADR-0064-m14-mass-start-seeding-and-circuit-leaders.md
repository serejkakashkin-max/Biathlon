# ADR-0064 — Pre-race Mass Start order and circuit leader bibs

2026-10-12. M14 owner correction. M14 remains open.

Two reproductions failed before the fix: the first development Mass Start
assigned the saved player's original roster slot one even when another athlete
led the season; CareerLeaderBibs rejected all development circuits, so no overall
leader had a colored bib. The existing shader correctly rendered its assigned
color; the classification gate was the cause.

Calendars explicitly declare leader_bib_roles, meeting_id on each race,
mass_start_total_priority and mass_start_number_from_right. World Cup uses
yellow overall/red discipline/combined roles. Development (the game's IBU Cup
analogue) uses a yellow overall leader and no red discipline bib. The old open
Woodland season retains the owner's yellow/red convention as a game series;
it is not labeled an official World Cup. The junior prototype retains its
existing custom season ranking and no World Cup bibs. These distinctions are
not a conversion of old careers to a different circuit or score table.

CareerMassStartSeeding captures only completed, earlier calendar results.
The order supplies entrants, physical grid slots, bib numbers and the first
shooting lane. PlayerIndex is looked up by stable player ID after sorting.
Later shooting lanes remain assigned by actual range arrival order. National
suit colors, athlete stats and equipment remain attached to stable IDs.

The September 2026 rules change the WC priority to **20** overall-ranked
competitors, with the remaining slots ordered by current-event points, best
single event result and total-score order. The development priority is **15**.
The calendar's meeting IDs make current-event filtering explicit. Local and
the current 30-athlete junior prototype use their season table. Real Junior
Cup Mass Start 60 uses event results and, in 2026, has no official season
points/classifications; that full circuit model remains outside this correction.
The fixed prototype roster has no additional athletes or IBU qualifying-point
list to select from; unresolved number ties retain the table's stable ordering.
No additional qualification list or defending champion is invented.

Grid numbers go right to left when facing the direction of start, with no
change in row spacing, paid distance or body size. StartOrderVersion=1 in new
replay metadata selects this composition. Metadata without the field keeps
the previous roster order and left-to-right geometry. Context/command/result
checks still guard incompatible replays.

A full reordered Mass Start exposed a pre-existing ice contact failure at tick
3242 on the penalty loop. The bounded 5x3 snow-normal probe repeatedly selected
an inadequate direction and exhausted all 32 passive responses. Roster-order
and neutral-snow differential probes isolated this interaction. A retry-only
refinement did not resolve it; exact normals from the first contact did.
The recorded icy-weather preset therefore selects the existing exact contact
normal algorithm through manual_course_contact_ice_v1. Other snow retains
the faster bounded probes. Body admission, impulses, response budget, course
geometry and physics are unchanged. This runs on reached course contacts,
not in rendering or every simulation tick. Historical weather without the
optional contact-resource reference uses its previous solver definition.
ManualRead is shared by headless, live and replay initialization/seek so the
recorded weather selects identical contact definitions everywhere.

## Official source

[IBU September 2026 Event and Competition Rules](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf):
§1.7.7 (start positions/right-to-left), §15.6.5.1 (WC 20 + event qualifiers),
§15.7.2 (yellow/red/combined), §16.4.3 (IBU Cup overall leader), §16.8.1
(IBU Cup 15 + event qualifiers), §18.3.2/18.4 (Junior Cup Mass Start 60 and no
season scores). This supersedes ADR-0062's broad exclusion of development
leader bibs; its World Cup/national appearance rules remain.
