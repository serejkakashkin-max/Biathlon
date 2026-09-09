# TASK-065 — Tactical utility report

Status: PASS, 2026-09-08. Added TacticalUtilityDefinition/Row, TacticalUtilityEvaluator,
TacticalCandidate/Score, provisional JSON weights and TacticalUtilityTests. Eight ordered
intent rows consume eight normalized features plus per-intent preferences. Scores expose bias,
feature sum, personality contribution and canonical total. Ineligible actions cannot win;
Hold is required as fallback. Equal totals use approved enum order. Outputs are WP064 decisions,
with no force/resource changes or hidden physical multipliers.

337/337 standalone and Unity EditMode pass; Console zero errors/four upstream warnings.
Tests verify exact arithmetic, preference-driven choices, eligibility despite greater score,
ties, target/schema/numeric validation, unchanged output on rejected input and zero .NET warmed
allocation. Existing full race and shooting fingerprints remain unchanged. PlayMode 6/6 at M5.

Weights are provisional authored model data, not calibrated competitive AI. Preference 0.5 is
neutral; actual persistent personality traits are mapped in WP074. Caller supplies normalized
features and constraint-derived candidate eligibility; WP067–073 integrate observation/target/
terrain/range policy. This evaluator alone does not claim believable pack behavior.
Next: WP066 exact tactical cadence and planning horizon.
