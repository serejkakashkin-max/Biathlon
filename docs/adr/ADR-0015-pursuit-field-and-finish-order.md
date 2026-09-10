# ADR-0015 — Pursuit field clock and finish order

2026-09-10. WP-082; follows ADR0010 and ADR0014.

Reuse SprintFieldSession/PackSprintFieldSession with an optional immutable Pursuit qualification.
The historical class names and Sprint result API remain for compatibility. Missing qualification
continues to select the original interval/Sprint path. A qualification requires the exact target
profile and canonical qualifying roster; identity/order/size mismatches fail before race events.
The scheduler admits every tied start event in its exact scheduled tick. Physical staging and
continuous occupancy still apply; a scheduled start does not grant overlap or remove a body.

The common athlete runtime executes profile-defined PPSS, range stops and actual penalties.
The optional physical range alternatives apply on all four required laps and bypass the final
lap. AI selects the stance from the current profile bout. No separate Pursuit physics is added.

Pursuit checkpoint gaps and observed places use global crossing times; Sprint continues to use
individual elapsed times. Perception exposes the actual format and clock. The legacy property
ElapsedGapToCheckpointLeaderSeconds returns null for a global-clock race; the format-neutral
CheckpointGapToLeaderSeconds provides the measured value.

PursuitResultSnapshot ranks by exact global FinishTime (whole ticks plus billionth-of-tick parts),
not individual elapsed duration or registered display precision. Exactly indistinguishable
crossings share rank with skipped places; stable athlete ID orders tied rows only for canonical
serialization. This is a deterministic simulation policy, not a claim about external photo-finish
or jury procedure. The snapshot retains stable ID/schema, complete qualification context, clock,
first start and each athlete's scheduled start/global finish. Reload requires caller-owned exact
qualification and clock, recomputes canonical order/ranks and rejects inconsistent identities,
starts, times or size. These checks establish consistency, not cryptographic authenticity.

The current field supports complete finisher snapshots. DNS/DNF/withdrawal handling within an
active Pursuit field is not introduced by this change; incomplete/nonterminal snapshots reject.
Do not represent this as the full M7 robustness or championship gate. WP083 tactics and WP084
broader Pursuit scenarios remain. PursuitResult is separate from the legacy Sprint Result property.
Race result-recorded events still flow through the shared replay sink.
