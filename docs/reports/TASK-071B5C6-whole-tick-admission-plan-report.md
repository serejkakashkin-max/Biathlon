# TASK-071B5C6 — Whole-tick admission transaction

Added JunctionAdmissionTickPlan with Begin, AddSubstep, inspection, Validate, Commit and Cancel.
It reuses a cold-allocated private admission copy, sharing immutable geometry while keeping all
mutable buffers separate. Planned positions, junction ownership, retained tails, ghost bodies,
clock and events remain private across multiple fractional substeps. Only a fully covered and
sealed tick can replace authoritative admission state, once, before publishing buffered events
in original order. Nonthrowing/nonreentrant external event sinks remain required.

JunctionAdmissionSession now tracks authoritative revision and whether a tick is in progress.
The latter distinguishes a zero-time partial tick from an untouched boundary. Stale concurrent
plans fail validation. Failed substeps invalidate the plan; Begin restarts from authority.
Fixed-capacity event overflow is detected after the nonthrowing private sink receives events;
the plan is discarded and no partial journal reaches authority. Snapshot copying preserves
incoming-tail protection between successive planned ticks.

Four added tests verify private seam/release state and journal, single commit, late invalid
fraction rollback, event overflow, concurrent stale plans, zero-time partial authority rejection,
tail preservation and 1000 reused two-substep ticks with zero warmed .NET allocation.
Standalone and Unity MCP EditMode 400/400 PASS, Console zero errors/four upstream warnings.
Evidence uses artifacts/physiology/wp071b5c6-*. Prior PlayMode 6/6 at B5C4 retained; no new
presentation behavior. git diff --check passes with existing CRLF conversion warnings. .NET
provides allocation evidence; the known Unity allocation-counter limitation remains.

No force, balance, old race progress, shooting or regression fingerprints changed. This is an
admission transaction only: it does not yet jointly plan or commit SprintAthleteSession physiology,
range obligations and shared force integration. The working copy is not exposed to callers;
inspection returns values. Boundary quantization, automatic follower replanning and complete
pack-race coordination remain pending. WP071 remains IN PROGRESS; no owner gate applies.
