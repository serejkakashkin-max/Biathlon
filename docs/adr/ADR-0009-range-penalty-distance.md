# ADR-0009 — Spatial range and penalty distance

Accepted, 2026-09-08, WP056.

Range entry, firing line and exit are authored inside a race loop, not at its lap boundary.
The per-bout adapter resolves the unique range section on the specified shooting lap.
Ambiguous repeated access sections and penalty lengths inconsistent with CompetitionProfile
are rejected during construction. The current adapter requires ordered access/rejoin anchors
on that same loop; arbitrary branching networks need a future explicit route contract.

Physics supplies displacement in integer nanometres. The firing line clamps progress until
the shared shooting resolver reaches Complete, including its setup, bolt and exit durations.
Submitting that result consumes no movement that tick. Unused distance is returned and never
banked for later movement. At a stationary shooting stop the caller discards it and resets
speed; at a completed spatial detour the caller may consume it on the next route segment
within the same tick, preserving the tick's time budget. Physics integrations must split at
surface changes; this adapter only accounts for distance and does not compute force or speed.

Each unclosed target adds exactly one full authored penalty circuit. The main-route coordinate
stays at the penalty entry while circuits are traversed. The entry-to-rejoin main segment is
still traversed afterwards: rejoin is an access-zone endpoint, not a free shortcut. Thus total
travel increases by exactly miss_count × penalty_length, independently of access-zone length.
No fixed time surcharge replaces movement. Penalty sampling uses its own authored track.

RangeTraversalPhaseChanged carries phase enum; PenaltyLoopCompleted carries remaining count.
The future orchestrator is responsible for a single event publisher when forwarding these
receipts to lifecycle bookkeeping, and for activation at the authored entry. WP054's combined
lap/range action is an intermediate contract: WP059 must separate spatial range arrival from
actual lap completion. This ADR preserves the authored track, not the intermediate shortcut.

No ski, shooting or balance tuning changes. .NET/Unity deterministic trace regressions remain
mandatory. End-to-end race timing and lane ownership belong to WP059 and the later AI field.
