# M13 bot recovery — 2026-10-11

Build: `Builds/WoodlandM13BotRecovery-20261011/START_WOODLAND_REVIEW.cmd`.
M13 remains OPEN; M14 has not started. Previous builds are retained.

The production penalty-stall diagnostic initially failed at 385 consecutive
slow ticks. Behind-target recovery alone reached 78; adding earlier braking
alone was insufficient (129). Preserving a bounded recovery deflection below
the shared minimum steering speed gives 11 ticks, about 0.55s. These are actual
production field ticks, not a teleport or corrected route-distance counter.

AI changes: recover a penalty target behind the skis; preserve low-speed course
recovery commands; brake before bends with a 0.85 decision margin. Shared athlete
capacity, force cap, fatigue and player steering remain. Automatic range transit
retains its previous planning law. Exact normal-query caching removes repeated
15-point edge calculations without changing admission or graphics.

Evidence: `docs/quality/m13/bot-recovery-20261011/`. Final delivery receipt records
native tests, full Mass30 completion, player build launch and its hash. Signed
Mass Start/Sprint replay passes; the broader selection also retains its known
Pursuit setup failure. Six old standalone-controller failures were reproduced
with pre-change code/policy and identical failure values. None was disabled.

Elapsed CPU peaks in the recovery diagnostic overlap Unity compilation and do
not establish player FPS. The exact repeated-query regression verifies zero
additional disk work; body/course/grip checks remain complete. The owner should
check recovery after a course departure, especially on the penalty circuit.

PROJECT_STATE history is retained byte-for-byte before the dated appended record.
Owner rendering, package and settings edits remain outside these commits.
