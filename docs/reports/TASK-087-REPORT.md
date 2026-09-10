# TASK-087 — Mass Start range lane/order report

PASS — 2026-09-10. WP087 DONE; next WP088–089 stress/regression.

Added versioned first-bib/subsequent-arrival policy, preallocated physical projections/logical lane
assignments, speculative arrival counters and committed replay events. Field occupants/tails/FIFO
remain authoritative; a reserved rule ordinal never removes another athlete. See ADR0017 for the
verified IBU source and synthetic admission-line semantics. Existing fixed policy remains available
as a compatibility fixture, not a claim about Mass Start rules.

.NET8/8 focused includes ordered race2, old complete30-AI audit1, Pursuit field2 and physical lanes3;
additional fixed MassStart baseline3/3. Unity MCP2/2,481 discovered,13.117s. Two complete six-AI runs
have identical frames,events/results;24 assignments follow bib order then actual admission order.
Physical branches match logical shooting lanes. Altered ranks/clock reject. Both runtimes finish
14765ticks with result SHA256ee26c5ba66f114d52abdee03ba36034d24997e05eb42af7ad783bc9c90d5772e.
Console0 errors,3 retained MCP warnings; no compilation failure. No new full suite/PlayMode claim.

Initial test failure exposed premature lane remapping while leaving the previous bout. Restricting
selection to a preview actually reaching the next fork resolves it. Fixed Sprint/Pursuit/Mass Start
regressions pass; ordered-policy output intentionally differs and carries distinct replay context.
No numerical tuning changed.30-AI repeated ordered fields and narrow/rolling stress are WP088–089.
Evidence: artifacts/physiology/wp087-final.trx, wp087-baseline.trx, wp087-unity.json,
wp087-unity-results.xml, wp087-console.json; compact hashes under docs/quality/wp087.
