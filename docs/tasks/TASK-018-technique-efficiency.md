# TASK-018 — WP-018 technique efficiency

Dependencies WP-017 PASS. Pure data-driven technique efficiency from Ski Technique/Climbing stats,
terrain grade and muscular fatigue. Automatic choice maximizes delivered propulsion efficiency;
explicit selection is an extension point for WP-033. Observe selection and each modifier.
Apply efficiency once after physiology's delivered watts (form and power fatigue already applied).
Validate versioned definitions, stat/input bounds; no AI identity or RNG. Test grade/skill/fatigue
comparisons, selection/ties, same-effort resource cost, movement integration and zero allocations.
Run .NET/Unity core and inspect Console. Tuck/downhill constraints belong to WP-019/020.
