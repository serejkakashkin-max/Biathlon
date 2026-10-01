# WP168 — geometric venue validator

2026-09-30. M13 follows explicit G2 approval.

TASK168A: immutable versioned validation policy and pure Domain diagnostics for world distances, closed loops, coincident seams, grade consistency/limits, widths, range mats and side approach, penalty length/placement, directed accessibility, shared entrance funnel and missing prefabs.

TASK168B: Unity Resources validator/menu and standalone BalanceLab command. Both produce the same canonical JSON for the legacy venue fixture. Input/schema failures are not a PASS. The CLI distinguishes findings (exit1) from invalid input (exit2).

TASK168C: reproducible small geometry fixture, adversarial tests and Unity prefab-resource integration. One-lane test policy is separate from the 30-lane production policy.

Final verification 2026-10-01: .NET focused **40/40 PASS** (authoring15, validator17, intersections8); Unity validator **17/17 PASS**, intersections **8/8 PASS** (593 discovered, filtered runs); full PlayMode **168/168 PASS**,0failed/0skipped. Console **0 errors,5 existing deprecation/diagnostic warnings, compilation_failed=false**. Actual Resources prefabs resolve. CLI positive one-lane test **PASS/exit0**; legacy venue **FAIL/exit1**,154 diagnostics identical byte-for-byte to Unity; unsafe input **invalid/exit2**. Evidence: `docs/quality/m13/wp168-verification.json`.

Final hardening binds range layout to its actual physical fork/rejoin, checks penalty reachability for each lane, diagnoses coincident starts split across junction IDs, and detects nonadjacent centreline intersections/touches/overlaps with a data-driven vertical-clearance threshold. Deliberate mutations cover these false-PASS risks.

The optional broad .NET session from30September was interrupted without a retained completion result and remains UNVERIFIED; no full .NET PASS is claimed. Current changes are offline authoring validation and Unity tooling, not race output changes.

No authoritative race logic or tuning changes. The old synthetic stadium intentionally fails production geometry; this is evidence that invalid content is caught, not a regression in the owner's approved G2 visual direction. WP168 does not claim any production venue complete.

Known boundary: static centreline checks do not certify swept collision envelopes, independent concurrent admission or real-world graphical quality. These remain mandatory venue acceptance. Next: WP169 upgrade benchmark to Venue01, beginning with range topology/mat-stop support; preserve owner side-entry/no-artificial-funnel requirements.
