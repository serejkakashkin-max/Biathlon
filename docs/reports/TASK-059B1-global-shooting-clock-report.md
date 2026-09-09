# TASK-059B1 — Shared shooting clock

PASS, 2026-09-08. ShootingSession and ShootingAiController accept a global initial tick.
All subsystem deadlines/events share that origin; session physiology may use the actual
athlete definition instead of the training template. Exact context binds origin and override.
Zero-origin contexts/trace fixtures remain unchanged. ShootingCommandTape schema 2 records
a nonzero initial_tick; schema 1 is retained for existing zero-origin tapes.

298/298 standalone and Unity EditMode pass; Console zero errors/four upstream warnings.
Evidence: artifacts/physiology/wp059b1-{standalone.trx,editmode.json,console.json}.
Complete shifted bouts have identical aim/sway/recoil/geometry and translated event/deadline
ticks; their tapes replay exact frames/events. AI decision translation and mismatch atomicity
pass. Existing M4 trace/statistical fingerprints and allocation tests remain unchanged.
No physiology is reset by this change: the orchestrator still supplies continuous state.
Next: TASK-059B2 surface-boundary motion, then complete athlete composition. No owner gate.
