# TASK-065 — Data-driven tactical utility

Schema 1 model with one ordered row per approved intent: base bias, eight bounded feature
weights and a personality modifier. Validate complete unique rows and numeric domains. Inputs
are normalized features, per-intent personality preferences and eligible target candidates.
Compute inspectable base/feature/personality scores; deterministic intent-order tie break.
Ineligible actions cannot win; Hold is the explicit fallback. Return WP064 intent with caller
lifetime. No simulation mutation. Feature extraction and target choice follow controller work.

Tests: exact contribution arithmetic, data/personality effects, eligibility/targets, stable ties,
invalid context atomicity, cold JSON and allocation-free evaluation. Preserve all race hashes.
