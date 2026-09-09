# TASK-058 — Sprint classification/results

PASS, 2026-09-08. Added ResultTimingDefinition/data, SprintResultBook, immutable result rows
and versioned validated JSON snapshot. Books accept terminal lifecycle receipts from the
configured full profile/roster/clock. Sprint ranks use personal registered time; equal times
share ranks and skip places. Stable IDs only order tied display rows. DNS/DNF/invalid rows
carry no competitive rank or invented finish time. Exact elapsed timestamps remain available.

294/294 standalone and Unity EditMode pass; Console zero errors, four upstream warnings.
Evidence: artifacts/physiology/wp058-{standalone.trx,editmode.json,console.json}.
Exact registration boundaries across tick rates, late starters, ties, submission-order
independence, terminal outcomes, duplicate/profile rejection and snapshot consistency tested.
Existing deterministic fingerprints remain passing. Result creation/export allocates on the
cold path; no per-tick model change or tuning change. No commit.

ADR-0010 distinguishes audited IBU tenths/equal-rank requirements from the project's explicit
truncation policy. Jury corrections, sanctions and non-Sprint ranking remain later work.
Next: WP059 complete headless Sprint, starting with separate range/lap lifecycle notifications.
