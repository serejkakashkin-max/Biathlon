# ADR-0008 — Competition profiles and rule provenance

Accepted WP053, 2026-09-08. CompetitionProfile owns nominal distance, lap/shooting plan,
start model and configured field capacity. The existing CompetitionRouteRequirements is
its immutable route-facing projection. Profiles never derive distance by rounding a uniform
lap: 10 km / 3 laps need not divide into whole millimetres. Actual route occurrences must
sum exactly to the declared distance. Penalty traversal is additional distance, not base laps.

Six senior profiles use the distances/sequences/starts from IBU ECR Table 1 (p8) and §6.1.
Sprint uses the published 30 s default; the schema permits other configured intervals.
Sprint field_capacity=60 is the project's simulation capacity, NOT an IBU eligibility quota.
Pursuit 60 and standard mass start 30 distinguish those models; MS60 and junior exceptions
are outside the implemented formats. Entry qualification remains later work. Reduced routes
use category Test and project_synthetic_v1 provenance, separate from standard profiles.

Rules remain data: the loader validates shape, enum/start consistency, ordered shooting laps,
positive exact distances, five-shot magazine and unique IDs. It does not hardcode six distance
lookup tables into runtime. Tests pin the authored senior data against the audited source.

Sources accessed 2026-09-08:
- [IBU current download index](https://www.biathlonworld.com/inside-ibu/downloads/downloads-documents-sport-and-event), lists November 2025 ECR.
- [IBU ECR November 2025](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/3193632d7b17912ec4a12fc321cecac3/Rules_2025_EN_cap03.pdf), Table 1, §6.1; category distances §1.4.

This is a scoped rule implementation, not certification of every IBU operational rule.
