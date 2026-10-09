# M13 — four manual range cycles / owned AI passing lines

Date: 2026-10-09. Component qualification PASS; M13 remains OPEN.

The retained three-athlete four-bout regression previously rejected tick15737
on the fourth mat approach. Two moving bodies converged at the edge of the
private mat curve. A captured one-interval geometry probe reproduced the
rejection; finer cell covers at64/128 did not admit it. The local polygon was
non-simple, so an exact-simple-polygon proof could not apply. No course proof,
body size, physical boundary or work limit was relaxed.

AI now forecasts approaching moving range neighbours from relative achieved
velocity and four separating-axis time intervals for complete rotated bodies.
Only ahead bodies on range approach/exit participate in this extra forecast;
ordinary racing retains its current contact policy. Candidate line checks are
planning queries, never lateral movement. A passing line belongs to the current
physical occurrence and is discarded when that occurrence changes. Player
signed input and the shared physical solver remain unchanged. Context v6
identifies changed manual AI output. The forecast allocates zero bytes when warm.

Unqualified broader avoidance and curved-pass trials caused earlier regressions
and were removed. Their FAIL receipts remain. Temporary tagged instrumentation
was removed. The initial geometry hypothesis expected admission without an
independent proof; its scratch source is retained as
`fourth-mat-geometry-hypothesis-probe.cs.txt`, outside Unity. The actual retained
four-bout integration assertion was neither disabled nor weakened.

Validation, under `docs/quality/m13/field-stepper-20261008/`:

- Focused .NET9/9 PASS: corridor6, four-bout cases2, exact four-bout repeat1.
- Entire manual .NET517/517 PASS, zero skipped;3m04s.
- One actor completes all four bouts and returns to base at tick16360.
- Three actors do the same at tick16667, maximum2 movement pieces per tick.
- These witnesses incurred zero penalties; they do not qualify actual penalty debt.
- Two runs compare every accepted pose, velocity, physical motion, physiology,
  piece count and event exactly through all four bouts.
- Native Unity corridor6/6, exact four-bout repeat1/1 and Mass30 stationary-player
  400-tick course-bend regression1/1 PASS. TotalTests is discovery count, not execution.
- Unity compilation false; Error/Exception Console empty.

Changed: `ManualAiObstacleCorridor`, `ManualScopedAiMovement`, their tests and the
field stepper's repeated-range tests. The manual simulation output changes through
AI intent; there is no hidden speed bonus, paid traffic wait, pose/velocity snap,
legacy fallback, body shrink or presentation authority. Shooting geometry and
authoritative shooting tuning are unchanged.

M13 / WP175 / WP177 / WP178 remain OPEN; WP176 PASS. Production host still selects
lateral-v1. Full formats, actual penalty debt/finish, reusable mats, achieved draft,
manual AI-free replay, full2–3m mat capture and presentation/HUD integration remain
to qualify. No owner Windows build was created. Next: lifecycle/resource integration
and complete manual races, then final M13 checks and the owner's review build.
PROJECT_STATE history is append-only. Owner shooting-feel issue remains pending.
