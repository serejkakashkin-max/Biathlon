# TASK-003 — WP-003 effort and sustainable power

Dependency: WP-002 PASS. Implement control thousandths → relative effort → watts, athlete sustainable
power with form/fatigue efficiency, reserve-limited above-threshold output and observable requested/actual power.
No skiing and no player/AI distinction. Form domain [0,1.10] follows GDD; all game tuning from definition.
Tests: full control sweep monotonicity, known watts/effort identity, fresh/fatigued/exhausted/zero form,
invalid inputs, no allocations, Foundation unchanged, Unity EditMode and Console.
Risk: provisional fatigue efficiency and reserve-limited peak curve; document for later balance review.
Rollback only EffortSystem/EffortOutput/tests/docs from this task.
