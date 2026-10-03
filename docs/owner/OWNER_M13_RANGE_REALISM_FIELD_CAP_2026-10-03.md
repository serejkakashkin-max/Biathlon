# Owner directive — M13 range realism and maximum field

Date: 2026-10-03. Applies to WP-169 and subsequent production venues. G2 remains APPROVED; the Master Plan is unchanged.

The owner requests realistic spacing between shooting mats, identification and correction of the unexplained white rectangles between the mats and spectators, mandatory research using existing real course/stadium plans, and a maximum of 40 participants.

The production field maximum is 40 total athletes, including the player. Sprint and qualified Pursuit production content must use that maximum. Standard Mass Start remains 30 athletes with 30 shooting lanes. Historical 60-athlete stress fixtures, recordings and qualification evidence remain explicitly historical; they do not qualify the new production configuration. This explicit owner instruction overrides the earlier production field-size examples in the GDD without changing the deterministic engine's ability to run larger technical fixtures.

## Primary references

- [IBU official sport/event downloads](https://www.biathlonworld.com/inside-ibu/downloads/downloads-documents-sport-and-event), September 2026 rules.
- [IBU Event and Competition Rules 2026](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf), sections 3.4.2–3.4.4, printed pages 31–33. Shooting lanes are 2.75–3 m wide, mats are 2 × 1.5 m and 1–2 cm thick, entry is from the left and exit to the right when facing the targets, and the rear competitor ramp is 10–12 m wide. Use 30 target banks and a 50 m firing distance.
- [Lenzerheide 2025 official media guide](https://www.biathlon-lenzerheide.ch/microsites-assets/biathlon-lenzerheide/pdf/medien/media-guide-lenzerheide-2025-en.pdf), printed pages 11–12 stadium maps and 13–18 discipline course maps.
- [Osrblie 2019 official stadium plans](https://osrblie2019.biathlon.sk/public/site/files/source/Mapy/Stadion/Plan_of_stadium_final.pdf), second PDF page: range, penalty loop, coaches, media and spectator areas.
- [Oberhof official course overview](https://www.biathlon-oberhof.com/program/courses/).

These are geometric and operational references, not licensed production venues or permission to copy branding/map artwork. Text and source references have been checked. Lenzerheide PDF pages11/12 and Osrblie PDF page2 were also rendered and visually inspected; the read-only source/render hashes and findings are recorded in [reference review](../quality/m13/production-range-realism-reference-review-2026-10-03.md).

## Engineering status and acceptance

The current Woodland range has 6 m lane pitch and opposite lateral flow. These are production defects to correct, not accepted demo exceptions. Halving the pitch alone is unsafe: the existing private U paths require space for their turns, body envelopes and 2.5 m corridors. Redesign the physical range paths and their authored guards coherently, retaining full athlete bodies, collision safety, equal counted paths and deterministic admission. Do not replace this with presentation-only moved mats, teleportation, ghosting, reduced safety envelopes, unequal paid distance or a global range queue.

The white rectangles are under diagnosis. The current leading hypothesis is exposed packed-snow private path surfaces; this is not yet a confirmed cause. Verify against actual native renderers before recording a fix.

Implement the field cap in authored production data and generated scene/config defaults. Reject an oversized production roster before mutation. Qualify the changed range through native Unity tests and actual views plus complete Sprint40 → qualified Pursuit40 and Mass30 deterministic races/replay. Preserve old proof files and mark their scope accurately.

Continue bounded live-route presentation task TASK169T2 before importing new range data. Pause propagation of the current wide range to Pine Ridge and the production Windows review build until the corrected configuration is qualified. WP-169 remains IN_PROGRESS; this directive creates no new owner approval gate.
