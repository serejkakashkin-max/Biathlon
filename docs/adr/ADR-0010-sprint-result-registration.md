# ADR-0010 — Sprint result registration and ties

Accepted WP058, 2026-09-08.

Sprint ranks by individual elapsed time. Results retain canonical whole ticks and fractional
parts for diagnostics, separately from registered result units. ResultTimingDefinition owns
precision and rounding; result_timing_v1 uses ten units per second and truncates positive
elapsed time to completed tenths. This truncation choice is a project policy: the audited
ECR specifies tenths but does not resolve rounding of finer simulated timestamps. It must
not be described as a verified IBU truncation rule. Nearest-even is an explicit alternative
supported by the schema. Registration uses decimal/integer arithmetic, never a binary64
formatted display string, and checks result-unit overflow.

Equal registered times receive equal ranks with skipped following places (1,1,3). Athlete
ID orders tied rows for deterministic display only; finer diagnostic time does not break
a registered tie. Nonfinishers have rank zero and null time/gap fields. DNS, DNF and Invalid
remain distinct; Invalid is a simulation result status, not an asserted official sanction.
Every roster entrant must supply a terminal lifecycle before creating the final snapshot.
Post-publication jury corrections and race-format-specific disqualification rules remain
future work. Duplicate records are rejected rather than silently replacing results.

Snapshots are versioned, retain profile/timing JSON and validate registration/rank/gap/order
when loaded. These consistency checks are not cryptographic tamper authentication. A result
book is bound to complete canonical profile data, not just a potentially reused profile ID.
Result creation/export is a cold path; per-tick simulation stays separate.

Sources accessed 2026-09-08: [IBU ECR November 2025](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/3193632d7b17912ec4a12fc321cecac3/Rules_2025_EN_cap03.pdf),
§9.2.2.1 elapsed Sprint time, §9.3.2 registration to 0.1 s, §9.2.5 and §9.4.1.1 equal ranks
and skipped places. Full race adjustments/protests are outside this scoped implementation.
