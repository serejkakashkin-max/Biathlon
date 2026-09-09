# TASK-071B1 — Staged athlete report

Status: PASS, 2026-09-08. SprintAthleteSession.Step now composes PrepareStep/CommitStep.
Preparation calculates physiology and path into owned scratch without advancing any authoritative
subsystem or emitting events. A readonly SprintStepPreview exposes value state and allows copied
segment reads using an owner/revision handle. Shooting actions and bout initialization occur
only in commit. Every preparation attempt invalidates the earlier handle; commits are single-use.

359/359 standalone and Unity EditMode pass at this revision; Console zero errors/four upstream
warnings. Complete staged/direct Sprint comparison checks physiology, route/motion, shooting,
range, checkpoints and event immutability before commit, then exact complete replay. Tests cover
foreign/default/replaced/consumed/failed-prepare handles and zero .NET warmed prepare+commit
allocation. Prior pinned full-race/shooting results remain unchanged.

Ownership contract: simulation thread only, with no external direct mutation of exposed lifecycle/
shooting objects between prepare/commit. This is not a concurrent transaction or a rollback of
arbitrary throwing event sinks. Next: B2 constrained forward-prefix application, then B3 shared
physical path/time mapping. WP071 remains incomplete.
