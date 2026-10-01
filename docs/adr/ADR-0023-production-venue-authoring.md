# ADR0023 — production venue manifest and generated hooks

2026-09-30, WP167 after explicit owner G2 approval. Extends ADR0003/0011/0014/0017.

A version1 VenueManifest binds existing track, world geometry, physical lane, junction and connection resources. Resource IDs are relative extensionless names, with no traversal or arbitrary filesystem paths. Exact source definitions remain canonical; parsing binds their existing identity/snapshot checks. Semantic zones derive from track segments, not another tuning table.

Camera markers, ambience event hooks and asset placements have stable IDs and anchored offsets. Unity generation produces a disposable presentation prefab with stable typed bindings, disabled cameras and future audio hooks. It never writes to race state. JSON contains no Unity object references. Ambience event IDs are hooks for M15, not a claim that audio content already exists.

Range layout requires an explicit side_entry contract, a horizontal firing direction, lane pitch and rear apron depth. Declaring the contract does not prove the geometry meets it: WP168 validates world distances/seams/placement and reports failures. The legacy synthetic venue is a tooling fixture and must not be relabelled a production venue by passing schema checks.

WP167 schema/reference checks are distinct from WP168 geometric readiness and WP169–174/177 dense-race admission acceptance. In particular the old common exclusive entry region can still queue the group. Production acceptance requires physical-path/admission work with deterministic/replay tests; generated decorative markers cannot fix that.
