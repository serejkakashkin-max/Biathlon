# TASK-053 — Competition profiles

Status: PASS, 2026-09-08. Added immutable CompetitionProfile and ordered bout contracts,
six senior JSON profiles plus explicit reduced Sprint test profile; existing route projection
now derives from the profile. ADR-0008 documents exact-distance policy and current IBU source.

270/270 .NET and Unity EditMode pass; Console zero errors, four upstream warnings.
Evidence: artifacts/physiology/wp053-standalone.trx and wp053-{editmode,console}.json.
All six distances/sequences/starts, roundtrip, route projection, synthetic route integration,
invalid/missing structure and fractional millimetre rejection verified. Prior traces pass.

Sprint capacity 60 is a product configuration, not an IBU quota claim. Standard Mass Start 30
and Pursuit are represented as data; their orchestration follows M7. Eligibility, timing and
penalty routing follow subsequent WPs. No gameplay formula/tuning change or commit.
Next: WP054 race lifecycle/timing.
