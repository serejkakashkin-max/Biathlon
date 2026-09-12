# TASK-109 — Training adaptation

WP-109 complete. Adaptation multiplies the session's explicit gains by quality, data-driven age factor, recovery factor, potential and diminishing remaining potential. Fixed-point attributes use explicit decimal ties-to-even rounding; updates never exceed potential and validate all inputs before mutation.

Age knots and quality/recovery bounds are authored in career_adaptation_v1.json. The same pure function accepts any athlete, without player/AI privileges. Annual decline values are defined for WP114 but are not applied by training.

Validation: .NET career10/10; Unity adaptation2/2. Tests verify lower quality/fatigue/age effects, potential sensitivity, zero-quality behavior, 2,000-session diminishing returns, potential cap and atomic invalid-quality rejection. Existing save and standings regressions pass. No race physics or M8 tuning changes.

Next: WP110 carryover fatigue and recovery; session application alone is not a weekly orchestrator.
