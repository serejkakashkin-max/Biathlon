# ADR-0039 — IBU shooting lane selection in production

2026-10-10. Owner-directed correction during M13. Supplements ADR-0017 and
ADR-0038; does not change the owner-approved shortened competition distances.

Source: [IBU Event and Competition Rules, September 2026](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf),
articles 3.4.2.2, 3.4.2.3, 3.4.5 and 8.2.1, accessed 2026-10-10.

Numbering begins at 1 on the right when looking toward the targets. Sprint
competitors choose among ready targets in the correct stance: right half prone,
left half standing. Pursuit takes the lowest available number in sequence.
Mass Start first shooting uses the start number; later bouts fill from the right
in arrival order. Athletes enter the range from the left and leave to the right.

Woodland's three profiles explicitly bind `shooting_lane_rules_revision` to
`ibu_ecr_2026`. This rule authority is separate from compressed race distances.
The authoring recipe and generators retain that binding when regenerated.
Unknown revisions are rejected instead of silently substituting a policy.

The generic `prefer_nearest_available_mat` setting is removed from mat capture,
world composition and reservation preparation. Capture tolerances cannot select
shooting lanes. The reusable reservation book filters Sprint by the bout's
stance before checking availability; the helper currently chooses the lowest
free number in that half. That helper preference is allowed by the rules, not
mandated by IBU. Pursuit uses all lanes and has no approach-distance override.
Existing Mass Start bib/arrival coordination is retained. A free-choice Sprint
UI belongs to subsequent player-flow work; this correction does not claim that
the current automatic approach exposes every legal manual selection.

Lane assignment, arrival rank, accepted body-clear release and replay events
remain separate. No occupied lane is made free to avoid a capacity failure.
No extra mats, participants, time/progress shortcuts or shooting tuning are added.
Legacy synthetic profiles without the new revision retain their recorded policy.

Validation receipts and final production-field status are recorded in the
M13 closure report and the append-only project state.
