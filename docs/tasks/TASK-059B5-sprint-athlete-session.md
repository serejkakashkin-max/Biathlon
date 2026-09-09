# TASK-059B5 — Complete Sprint athlete session

Compose start clock, continuous physiology, shared ski/path physics, two shooting bouts,
actual penalty traversal, checkpoint receipts and precise finish. Inputs carry ski effort and
aim/trigger intent; HR/fatigue come from the session's physiology, never caller-specified
shooting performance. Preserve start/end-boundary policy and stop at the firing line.

Bind all definitions/profile/route/athlete identities. Test full scripted/AI Sprint, penalty
distance and timing, fatigue/HR continuity, interval-clock translation, invalid-input atomicity,
events and repeated runs. Track per-transition initialization separately from allocation-free
active ticks. Field orchestration, CLI/replay and M5 capacity gate remain subsequent tasks.
